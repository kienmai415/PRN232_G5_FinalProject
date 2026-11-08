# Phần Vận hành — Bằng

Nhánh làm việc: `codex/bang`, phát triển từ `main` của repository
`kienmai415/PRN232_G5_FinalProject` (commit gốc `73fe304`).

## Phạm vi phụ trách FE và BE

| Chức năng | Frontend | Backend |
| --- | --- | --- |
| Nhân viên, phân ca, chấm công | ManagerController, StaffScheduleController | StaffManagementController, StaffService |
| Giao việc, báo cáo, nghiệm thu | TaskController, StaffTaskController | TaskManagementController, StaffTasksController, TaskItemService |
| Bảo trì sân | MaintenanceController, StaffMaintenanceController | MaintenanceManagementController, MaintenanceScheduleService |
| Dashboard | Các trang dashboard quản lý/nhân viên hiện có | StaffDashboardController |

Các module trong bảng đã tồn tại ở main. Nhánh này giữ chúng và bổ sung các
kiểm tra nghiệp vụ bên dưới, không viết lại toàn bộ module.

## Thay đổi trên nhánh

- Công việc đi theo luồng Pending → InProgress → Completed → Approved.
- Chỉ được nghiệm thu khi nhân viên đã báo hoàn thành (Completed).
- Khi từ chối, bắt buộc ghi lý do; công việc quay về InProgress để làm lại.
- Nhân viên báo hoàn thành có thời điểm CompletedAt; nghiệm thu giữ nguyên thời điểm đó.
- Chặn sửa công việc đang chờ nghiệm thu hoặc đã duyệt; chặn hạn sửa về quá khứ.
- Khi tạo/sửa việc, nhân viên phải đang hoạt động, có vai trò Staff và thuộc cơ sở.
- Booking gắn vào công việc phải thuộc cùng cơ sở.
- Nhân viên bảo trì phải đang hoạt động, có vai trò Staff và thuộc cơ sở.
- Đường dẫn chi tiết ca trực/bảo trì trả 404 khi ID thuộc cơ sở khác.
- Các controller quản lý công việc/bảo trì trả 400 cho dữ liệu nghiệp vụ sai,
  409 cho xung đột nghiệp vụ thay vì trả lỗi 500.
- FE chỉ hiện nút nghiệm thu với Completed, nút sửa với Pending/InProgress;
  hiển thị thông báo lỗi nghiệm thu do API trả về.
- Thao tác bắt đầu công việc ở FE được kiểm tra anti-forgery token.

## Kiểm thử không cần database

Từ thư mục repository, chạy:

```powershell
dotnet run --project tests/Operations.RegressionTests/Operations.RegressionTests.csproj --configuration Release
```

Đây là chương trình regression trả exit code khác 0 khi thất bại, dùng repository
giả để chạy service/controller thật. Không gọi database hoặc dịch vụ thanh toán.

Build cả FE và BE bằng solution chung ở thư mục cha:

```powershell
dotnet build ../SportCourtManagement.slnx --configuration Release
```

## Kịch bản demo với database thử nghiệm

1. Đăng nhập Manager/Admin, chọn cơ sở, phân công một nhân viên Staff đang hoạt động.
2. Tạo việc có hạn tương lai; kiểm tra không cho chọn nhân viên ngoài cơ sở.
3. Đăng nhập nhân viên được giao, mở `/staff/tasks`, bấm Bắt đầu làm.
4. Báo hoàn thành với mô tả kết quả, có thể kèm ảnh theo chức năng hiện có.
5. Manager mở `/manager/tasks`, chọn Từ chối với lý do.
6. Nhân viên làm lại và gửi kết quả; Manager duyệt nghiệm thu.
7. Kiểm tra không sửa/duyệt lại công việc Approved.
8. Kiểm tra chi tiết ca trực/bảo trì không hiển thị khi dùng complexId sai.

Chưa kiểm thử thủ công toàn bộ UI và luồng SQL Server thật trong lần thay đổi này.
Kiểm tra complexId ở trên là kiểm tra quan hệ của bản ghi với route, không thay
thế việc phân quyền Manager theo từng cơ sở ở toàn hệ thống.
