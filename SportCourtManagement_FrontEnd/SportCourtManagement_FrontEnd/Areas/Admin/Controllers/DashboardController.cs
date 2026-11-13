using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Models.ViewModels.Admin;
using SportCourtManagement_FrontEnd.Services.Interfaces;

namespace SportCourtManagement_FrontEnd.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class DashboardController(IReportService reportService) : Controller
{
    /// <summary>
    /// Hiển thị Dashboard tổng quan vận hành, số liệu ca trực, bảo trì sân và doanh thu
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var vm = new DashboardViewModel
        {
            Summary = await reportService.GetDashboardAsync(),
            CourtStats = await reportService.GetComplexStatsAsync(),
            Data = await reportService.GetAdminDashboardAsync()
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> GetData()
    {
        var data = await reportService.GetAdminDashboardAsync();
        return Json(new { success = true, data });
    }
}
