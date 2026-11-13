using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SportCourtManagent_Server.Enums;
using SportCourtManagent_Server.Helpers;
using SportCourtManagent_Server.Models;

namespace SportCourtManagent_Server.BackgroundServices
{
    public class MaintenanceWorkerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MaintenanceWorkerService> _logger;

        public MaintenanceWorkerService(IServiceProvider serviceProvider, ILogger<MaintenanceWorkerService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MaintenanceWorkerService đã khởi động.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessMaintenanceSchedulesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi trong quá trình xử lý của MaintenanceWorkerService.");
                }

                // Quét định kỳ mỗi 30 giây
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        private async Task ProcessMaintenanceSchedulesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTimeHelper.VietnamNow;

            // 1. Chuyển các lịch Scheduled đã đến giờ thành InProgress và đổi Court thành Maintenance
            var startingSchedules = await context.MaintenanceSchedules
                .Include(m => m.Court)
                .Where(m => m.Status == MaintenanceStatus.Scheduled
                         && m.StartDateTime <= now
                         && now < m.EndDateTime)
                .ToListAsync();

            foreach (var schedule in startingSchedules)
            {
                schedule.Status = MaintenanceStatus.InProgress;
                if (schedule.Court != null && schedule.Court.Status != CourtStatus.Maintenance 
                    && schedule.Court.Status != CourtStatus.Inactive && !schedule.Court.IsDeleted)
                {
                    schedule.Court.Status = CourtStatus.Maintenance;
                }
            }

            // 2. Chuyển các lịch InProgress đã hết giờ thành Completed và trả Court về Available
            var endingSchedules = await context.MaintenanceSchedules
                .Include(m => m.Court)
                .Where(m => (m.Status == MaintenanceStatus.InProgress || m.Status == MaintenanceStatus.Scheduled)
                         && m.EndDateTime <= now)
                .ToListAsync();

            foreach (var schedule in endingSchedules)
            {
                schedule.Status = MaintenanceStatus.Completed;
                if (string.IsNullOrWhiteSpace(schedule.Result))
                {
                    schedule.Result = "Bảo trì hoàn thành theo lịch tự động.";
                }

                if (schedule.Court != null && !schedule.Court.IsDeleted && schedule.Court.Status == CourtStatus.Maintenance)
                {
                    // Kiểm tra xem sân có lịch bảo trì nào khác đang còn hiệu lực không
                    var hasOtherOngoing = await context.MaintenanceSchedules
                        .AnyAsync(m => m.CourtId == schedule.CourtId
                                    && m.MaintenanceId != schedule.MaintenanceId
                                    && (m.Status == MaintenanceStatus.InProgress 
                                        || (m.Status == MaintenanceStatus.Scheduled && m.StartDateTime <= now && now < m.EndDateTime)));

                    if (!hasOtherOngoing)
                    {
                        schedule.Court.Status = CourtStatus.Available;
                    }
                }
            }

            // 3. Nếu Admin/Manager đã chuyển sân sang Available hoặc Inactive thủ công nhưng lịch bảo trì vẫn còn InProgress/Scheduled
            var manuallyRestoredSchedules = await context.MaintenanceSchedules
                .Include(m => m.Court)
                .Where(m => (m.Status == MaintenanceStatus.InProgress || m.Status == MaintenanceStatus.Scheduled)
                         && m.Court != null 
                         && m.Court.Status != CourtStatus.Maintenance)
                .ToListAsync();

            foreach (var schedule in manuallyRestoredSchedules)
            {
                schedule.Status = MaintenanceStatus.Completed;
                schedule.Result = "Kết thúc bảo trì sớm do Quản trị viên cập nhật trạng thái sân.";
            }

            if (startingSchedules.Any() || endingSchedules.Any() || manuallyRestoredSchedules.Any())
            {
                await context.SaveChangesAsync();
                _logger.LogInformation("MaintenanceWorkerService đã cập nhật: {started} lịch bắt đầu, {ended} lịch kết thúc, {restored} lịch hoàn tất do cập nhật sân.",
                    startingSchedules.Count, endingSchedules.Count, manuallyRestoredSchedules.Count);
            }
        }
    }
}

