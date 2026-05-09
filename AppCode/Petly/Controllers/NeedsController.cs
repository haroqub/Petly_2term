using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Petly.Business.Services;
using Petly.Models;

namespace Petly.Controllers;

public class NeedsController : Controller
{
    private readonly NeedService _needService;
    private readonly UserManager<ApplicationUser> _userManager;

    public NeedsController(NeedService needService, UserManager<ApplicationUser> userManager)
    {
        _needService = needService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var currentUser = await GetCurrentUserAsync();
        var role = await GetCurrentRoleAsync(currentUser);
        var currentUserId = currentUser?.Id;

        ViewBag.CanCreate = role == "shelter_admin";
        ViewBag.UserRole = role;

        var needs = await _needService.GetNeedsAsync(currentUserId, role);
        return View(needs);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int shelterId)
    {
        var currentUser = await GetCurrentUserAsync();
        if (!await CanCreateAsync(currentUser))
        {
            return Forbid();
        }

        if (currentUser == null || currentUser.Id != shelterId)
        {
            return Forbid();
        }

        return View(new ShelterNeedFormViewModel { ShelterId = shelterId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ShelterNeedFormViewModel model)
    {
        var currentUser = await GetCurrentUserAsync();
        if (!await CanCreateAsync(currentUser))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (currentUser == null || currentUser.Id != model.ShelterId)
        {
            return Forbid();
        }

        var need = new ShelterNeed
        {
            ShelterId = model.ShelterId,
            Description = model.Description.Trim(),
            PaymentDetails = model.PaymentDetails.Trim(),
            IsFulfilled = false
        };

        await _needService.AddNeedAsync(need);
        TempData["Success"] = "Потребу успішно додано.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var need = await _needService.GetNeedAsync(id);
        if (need == null)
        {
            return NotFound();
        }

        if (!await CanManageNeedAsync(need))
        {
            return Forbid();
        }

        var model = new ShelterNeedFormViewModel
        {
            NeedId = need.NeedId,
            ShelterId = need.ShelterId,
            Description = need.Description,
            PaymentDetails = need.PaymentDetails
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ShelterNeedFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var need = await _needService.GetNeedAsync(model.NeedId);
        if (need == null)
        {
            return NotFound();
        }

        if (!await CanManageNeedAsync(need))
        {
            return Forbid();
        }

        need.Description = model.Description.Trim();
        need.PaymentDetails = model.PaymentDetails.Trim();

        await _needService.UpdateNeedAsync(need);
        TempData["Success"] = "Потребу оновлено.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var need = await _needService.GetNeedAsync(id);
        if (need == null)
        {
            return NotFound();
        }

        if (!await CanManageNeedAsync(need))
        {
            return Forbid();
        }

        return View(need);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkFulfilled(int id)
    {
        return await ChangeFulfillmentStatusAsync(id, true, "Потребу позначено як виконану.");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkActive(int id)
    {
        return await ChangeFulfillmentStatusAsync(id, false, "Потребу повернено в активні.");
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var need = await _needService.GetNeedAsync(id);
        if (need == null)
        {
            return NotFound();
        }

        if (!await CanManageNeedAsync(need))
        {
            return Forbid();
        }

        await _needService.DeleteNeedAsync(id);
        TempData["Success"] = "Потребу видалено.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ChangeFulfillmentStatusAsync(int id, bool isFulfilled, string message)
    {
        var need = await _needService.GetNeedAsync(id);
        if (need == null)
        {
            return NotFound();
        }

        if (!await CanManageNeedAsync(need))
        {
            return Forbid();
        }

        await _needService.SetNeedFulfillmentAsync(need, isFulfilled);
        TempData["Success"] = message;
        return RedirectToAction(nameof(Index));
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        return await _userManager.GetUserAsync(User);
    }

    private async Task<string> GetCurrentRoleAsync(ApplicationUser? currentUser)
    {
        if (currentUser == null)
        {
            return "user";
        }

        var roles = await _userManager.GetRolesAsync(currentUser);
        return roles.FirstOrDefault() ?? "user";
    }

    private async Task<bool> CanCreateAsync(ApplicationUser? currentUser)
    {
        if (currentUser == null)
        {
            return false;
        }

        var roles = await _userManager.GetRolesAsync(currentUser);
        return roles.Contains("shelter_admin");
    }

    private async Task<bool> CanManageNeedAsync(ShelterNeed need)
    {
        var currentUser = await GetCurrentUserAsync();
        var role = await GetCurrentRoleAsync(currentUser);

        return role == "system_admin" || (role == "shelter_admin" && currentUser?.Id == need.ShelterId);
    }
}
