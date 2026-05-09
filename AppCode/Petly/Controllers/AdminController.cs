using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Petly.Business.Services;
using Petly.Models;

namespace Petly.Controllers;

[Authorize(Roles = "system_admin")]
public class AdminController : Controller
{
    private readonly DashboardService _dashboardService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(DashboardService dashboardService, UserManager<ApplicationUser> userManager)
    {
        _dashboardService = dashboardService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Analytics(int days = 30, int? shelterId = null)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        var model = await _dashboardService.GetAnalyticsAsync(days, shelterId, currentUser?.Id ?? 0);
        return View(model);
    }
}
