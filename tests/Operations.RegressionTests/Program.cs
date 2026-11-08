using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using SportCourtManagent_Server.Controllers.Manager;
using SportCourtManagent_Server.DataAccess.Interfaces;
using SportCourtManagent_Server.DTOs.Maintenance;
using SportCourtManagent_Server.DTOs.Staff;
using SportCourtManagent_Server.DTOs.Task;
using SportCourtManagent_Server.Enums;
using SportCourtManagent_Server.Models;
using SportCourtManagent_Server.Services.Implements;
using SportCourtManagent_Server.Services.Interfaces;

// Runs without a database, credentials or a third-party test package.
var checks = new List<(string Name, Func<Task> Run)>
{
    ("Full task lifecycle and completion timestamp", async () =>
    {
        var fixture = new TaskFixture(TaskItemStatus.Pending);
        await fixture.Service.StartTaskAsync(7, 1);
        Check(fixture.Item.Status == TaskItemStatus.InProgress);
        await fixture.Service.CompleteTaskAsync(7, 1, new() { ResultNote = "Đã vệ sinh sân" });
        var completedAt = fixture.Item.CompletedAt;
        Check(completedAt.HasValue && fixture.Item.Status == TaskItemStatus.Completed);
        await fixture.Service.VerifyTaskAsync(10, 1, new() { IsApproved = true });
        Check(fixture.Item.Status == TaskItemStatus.Approved && fixture.Item.CompletedAt == completedAt);
    }),
    ("Cannot verify pending/in-progress/already approved tasks", async () =>
    {
        foreach (var status in new[] { TaskItemStatus.Pending, TaskItemStatus.InProgress, TaskItemStatus.Approved })
        {
            var fixture = new TaskFixture(status);
            await Throws<InvalidOperationException>(() => fixture.Service.VerifyTaskAsync(10, 1, new() { IsApproved = true }));
            Check(fixture.Item.Status == status && fixture.Writes == 0);
        }
    }),
    ("Rejection requires a reason and permits resubmission", async () =>
    {
        var fixture = new TaskFixture(TaskItemStatus.Completed);
        await Throws<ArgumentException>(() => fixture.Service.VerifyTaskAsync(10, 1, new() { IsApproved = false, Note = " " }));
        Check(fixture.Writes == 0);
        await fixture.Service.VerifyTaskAsync(10, 1, new() { IsApproved = false, Note = "Vệ sinh lại góc sân" });
        Check(fixture.Item.Status == TaskItemStatus.InProgress && fixture.Item.CompletedAt == null);
        await fixture.Service.CompleteTaskAsync(7, 1, new() { ResultNote = "Đã làm lại" });
        await fixture.Service.VerifyTaskAsync(10, 1, new() { IsApproved = true });
        Check(fixture.Item.Status == TaskItemStatus.Approved);
    }),
    ("Other staff cannot start or complete a task", async () =>
    {
        var fixture = new TaskFixture(TaskItemStatus.Pending);
        await Throws<KeyNotFoundException>(() => fixture.Service.StartTaskAsync(99, 1));
        await Throws<KeyNotFoundException>(() => fixture.Service.CompleteTaskAsync(99, 1, new() { ResultNote = "Done" }));
        Check(fixture.Writes == 0);
    }),
    ("Cannot edit completed/approved tasks", async () =>
    {
        foreach (var status in new[] { TaskItemStatus.Completed, TaskItemStatus.Approved })
        {
            var fixture = new TaskFixture(status);
            await Throws<InvalidOperationException>(() => fixture.Service.UpdateTaskAsync(10, 1, ValidUpdate()));
            Check(fixture.Writes == 0);
        }
    }),
    ("Reject past due dates on update", async () =>
    {
        var fixture = new TaskFixture(TaskItemStatus.Pending);
        var request = ValidUpdate();
        request.DueDate = DateTime.UtcNow.AddDays(-1);
        await Throws<ArgumentException>(() => fixture.Service.UpdateTaskAsync(10, 1, request));
        Check(fixture.Writes == 0);
    }),
    ("Reject inactive/non-staff/foreign/unassigned staff", async () =>
    {
        for (var scenario = 0; scenario < 4; scenario++)
        {
            var fixture = new TaskFixture(TaskItemStatus.Pending);
            var request = ValidUpdate();
            if (scenario == 0) fixture.Staff.IsActive = false;
            if (scenario == 1) fixture.Staff.UserRoles.Clear();
            if (scenario == 2) fixture.BelongsToComplex = false;
            if (scenario == 3) request.AssignedStaffId = null;
            await Throws<ArgumentException>(() => fixture.Service.UpdateTaskAsync(10, 1, request));
            Check(fixture.Writes == 0);
        }
    }),
    ("Reject a booking belonging to another complex", async () =>
    {
        var fixture = new TaskFixture(TaskItemStatus.Pending);
        var request = ValidUpdate();
        request.BookingId = 20;
        await Throws<ArgumentException>(() => fixture.Service.UpdateTaskAsync(10, 1, request));
        Check(fixture.Writes == 0);
    }),
    ("Manager receives conflict for invalid verification", async () =>
    {
        var fixture = new TaskFixture(TaskItemStatus.Pending);
        var controller = new TaskManagementController(fixture.Service);
        var result = await controller.VerifyTask(10, 1, new() { IsApproved = true });
        Check(result is ConflictObjectResult);
    }),
    ("Shift detail cannot use another complex route", async () =>
    {
        var service = Stub<IStaffService>.Create((method, _) => method.Name switch
        {
            "GetShiftByIdAsync" => Task.FromResult(new StaffShiftResponse { ComplexId = 20 }),
            _ => throw new NotSupportedException(method.Name)
        });
        Check(await new StaffManagementController(service).GetShiftById(10, 1) is NotFoundObjectResult);
    }),
    ("Maintenance assignment rejects staff outside its complex", async () =>
    {
        var staff = new User
        {
            UserId = 7, IsActive = true,
            UserRoles = new List<UserRole> { new() { Role = new Role { RoleName = "Staff" } } }
        };
        var staffRepository = Stub<IStaffRepository>.Create((method, _) => method.Name switch
        {
            "GetStaffWithRolesAsync" => Task.FromResult<User?>(staff),
            "IsStaffOfComplexAsync" => Task.FromResult(false),
            _ => throw new NotSupportedException(method.Name)
        });
        var courtRepository = Stub<ICourtRepository>.Create((method, _) => method.Name switch
        {
            "GetById" => new Court { CourtId = 2, ComplexId = 10 },
            _ => throw new NotSupportedException(method.Name)
        });
        var complexRepository = Stub<ICourtComplexRepository>.Create((method, _) => method.Name switch
        {
            "GetByIdWithDetailsAsync" => Task.FromResult<CourtComplex?>(new CourtComplex { ComplexId = 10 }),
            _ => throw new NotSupportedException(method.Name)
        });
        var bookings = Stub<IBookingRepository>.Create((method, _) => method.Name switch
        {
            "GetBookingsByCourtAsync" => Task.FromResult<IEnumerable<Booking>>(Array.Empty<Booking>()),
            _ => throw new NotSupportedException(method.Name)
        });
        // Any attempted write is a test failure.
        var repository = Stub<IMaintenanceScheduleRepository>.Create((method, _) => throw new Exception($"Unexpected write: {method.Name}"));
        var service = new MaintenanceScheduleService(repository, courtRepository, complexRepository, staffRepository, bookings);
        await Throws<ArgumentException>(() => service.CreateMaintenanceAsync(10, new()
        {
            CourtId = 2, AssignedStaffId = 7, MaintenanceType = MaintenanceType.Routine,
            StartDateTime = DateTime.UtcNow.AddDays(2), EndDateTime = DateTime.UtcNow.AddDays(3), Reason = "Bảo trì"
        }));
    }),
    ("Maintenance detail cannot use another complex route", async () =>
    {
        var service = Stub<IMaintenanceScheduleService>.Create((method, _) => method.Name switch
        {
            "GetMaintenanceByIdAsync" => Task.FromResult(new MaintenanceResponse { ComplexId = 20 }),
            _ => throw new NotSupportedException(method.Name)
        });
        var controller = new MaintenanceManagementController(service);
        Check(await controller.GetMaintenanceById(10, 1) is NotFoundObjectResult);
    })
};

foreach (var (name, run) in checks)
{
    await run();
    Console.WriteLine($"PASS: {name}");
}
Console.WriteLine($"Passed {checks.Count} regression checks.");

static UpdateTaskRequest ValidUpdate() => new()
{
    Title = "Vệ sinh sân", Category = TaskCategory.Cleanup, Priority = TaskPriority.Medium,
    AssignedStaffId = 7, DueDate = DateTime.UtcNow.AddDays(2)
};

static void Check(bool condition)
{
    if (!condition) throw new Exception("Assertion failed");
}

static async Task Throws<T>(Func<Task> action) where T : Exception
{
    try { await action(); }
    catch (T) { return; }
    throw new Exception($"Expected {typeof(T).Name}");
}

public class Stub<T> : DispatchProxy where T : class
{
    public Func<MethodInfo, object?[], object?> Handler { get; set; } = null!;
    public static T Create(Func<MethodInfo, object?[], object?> handler)
    {
        var instance = DispatchProxy.Create<T, Stub<T>>();
        ((Stub<T>)(object)instance).Handler = handler;
        return instance;
    }
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        => Handler(targetMethod!, args ?? []);
}

public sealed class TaskFixture
{
    public TaskItem Item { get; }
    public User Staff { get; } = new()
    {
        UserId = 7, IsActive = true,
        UserRoles = new List<UserRole> { new() { Role = new Role { RoleName = "Staff" } } }
    };
    public bool BelongsToComplex { get; set; } = true;
    public int Writes { get; private set; }
    public TaskItemService Service { get; }

    public TaskFixture(TaskItemStatus status)
    {
        Item = new TaskItem { TaskId = 1, ComplexId = 10, AssignedStaffId = 7, Status = status, Title = "Task" };
        var tasks = Stub<ITaskItemRepository>.Create((method, args) => method.Name switch
        {
            "GetByIdAsync" => (object)Task.FromResult<TaskItem?>(Item),
            "UpdateAsync" => Save((TaskItem)args[0]!),
            _ => throw new NotSupportedException(method.Name)
        });
        var staff = Stub<IStaffRepository>.Create((method, _) => method.Name switch
        {
            "GetStaffWithRolesAsync" => Task.FromResult<User?>(Staff),
            "IsStaffOfComplexAsync" => Task.FromResult(BelongsToComplex),
            _ => throw new NotSupportedException(method.Name)
        });
        var bookings = Stub<IBookingRepository>.Create((method, _) => method.Name switch
        {
            "GetDetailAsync" => Task.FromResult<Booking?>(new Booking { Court = new Court { ComplexId = 20 } }),
            _ => throw new NotSupportedException(method.Name)
        });
        var notifications = Stub<INotificationRepository>.Create((_, _) => Task.CompletedTask);
        var complexes = Stub<ICourtComplexRepository>.Create((method, _) => throw new NotSupportedException(method.Name));
        Service = new TaskItemService(tasks, complexes, staff, bookings, notifications);
    }

    private Task<TaskItem> Save(TaskItem item)
    {
        Writes++;
        return Task.FromResult(item);
    }
}
