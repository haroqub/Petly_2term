using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Petly.Business.Services;
using Petly.Models;

namespace Petly.Controllers;

[Authorize] 
public class AdoptionController : Controller
{
    private readonly AdoptionService _adoptionService;
    private readonly PetService _petService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly NotificationService _notificationService;

   public AdoptionController(
    AdoptionService adoptionService,
    PetService petService,
    UserManager<ApplicationUser> userManager,
    NotificationService notificationService)
{
    _adoptionService = adoptionService;
    _petService = petService;
    _userManager = userManager;
    _notificationService = notificationService;
}

    public async Task<IActionResult> Index()
    {
        var accountId = GetUserId();
        if (!accountId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var sessionRole = HttpContext.Session.GetString("Role");

        var isSystemAdmin = User.IsInRole("system_admin") || sessionRole == "system_admin";
        var isShelterAdmin = User.IsInRole("shelter_admin") || sessionRole == "shelter_admin";

        List<AdoptionApplication> applications;

        if (isSystemAdmin)
        {
            applications = await _adoptionService.GetAllApplicationsAsync();
            ViewBag.IsAdmin = true;
            ViewBag.CanManageApplications = false;
        }
        else if (isShelterAdmin)
        {
            applications = await _adoptionService.GetShelterApplicationsAsync(accountId.Value);
            ViewBag.IsAdmin = true;
            ViewBag.CanManageApplications = true;
        }
        else
        {
            applications = await _adoptionService.GetUserApplicationsAsync(accountId.Value);
            ViewBag.IsAdmin = false;
            ViewBag.CanManageApplications = false;
        }

        return View("Adoption", applications);
    }

    [Authorize(Roles = "user")]
    public async Task<IActionResult> Create(int petId)
    {
        var pet = await _petService.GetPetAsync(petId);
        if (pet == null) return NotFound();

        var currentUser = await _userManager.GetUserAsync(User);

        var model = new AdoptionRequestViewModel
        {
            PetId = pet.PetId,
            PetName = pet.PetName,
            PetType = pet.Type,
            PetAge = pet.Age,
            PetAgeText = pet.AgeText,
            PetPhotoUrl = pet.PhotoUrl,
            ApplicantName = currentUser?.Name ?? string.Empty,
            ApplicantSurname = currentUser?.Surname ?? string.Empty
        };

        return View("Adopt", model);
    }

[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Roles = "user")]
public async Task<IActionResult> Adopt(AdoptionRequestViewModel model)
{
    var accountId = GetUserId();
    if (!accountId.HasValue)
    {
        return RedirectToAction("Login", "Account");
    }

    if (!ModelState.IsValid)
    {
        var pet = await _petService.GetPetAsync(model.PetId);
        if (pet == null)
        {
            return NotFound();
        }

        model.PetName = pet.PetName;
        model.PetType = pet.Type;
        model.PetAge = pet.Age;
        model.PetAgeText = pet.AgeText;
        model.PetPhotoUrl = pet.PhotoUrl;

        return View("Adopt", model);
    }

    try
    {
        await _adoptionService.CreateApplicationAsync(
            model.PetId,
            accountId.Value,
            model.ApplicantName,
            model.ApplicantSurname,
            model.ApplicantAge!.Value,
            model.ContactInfo);


        await _notificationService.CreateAsync(
            accountId.Value,
            "Заявка подана",
            $"Ви успішно подали заявку на {model.PetName}");
    }
    catch (KeyNotFoundException)
    {
        return NotFound();
    }
    catch (InvalidOperationException ex)
    {
        TempData["Error"] = ex.Message;
        return RedirectToAction(nameof(Index));
    }

    TempData["Success"] = "Вашу заявку успішно подано!";
    return RedirectToAction(nameof(Index));
}

[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Roles = "shelter_admin")]
public async Task<IActionResult> Approve(int adoptId)
{
    var accountId = GetUserId();
    if (!accountId.HasValue)
    {
        return RedirectToAction("Login", "Account");
    }

    if (!IsShelterAdmin())
    {
        return Forbid();
    }

    try
    {
        await _adoptionService.UpdateApplicationStatusAsync(
            adoptId,
            AdoptionStatuses.Approved,
            accountId.Value);

        var userId = await _adoptionService.GetApplicantUserIdAsync(adoptId);

        await _notificationService.CreateAsync(
            userId,
            "Заявку схвалено",
            "Ваша заявка на адопцію була схвалена 🎉");
    }
    catch (KeyNotFoundException)
    {
        return NotFound();
    }
    catch (UnauthorizedAccessException)
    {
        return Forbid();
    }
    catch (InvalidOperationException ex)
    {
        TempData["Error"] = ex.Message;
        return RedirectToAction(nameof(Index));
    }

    TempData["Success"] = "Заявку схвалено!";
    return RedirectToAction(nameof(Index));
}

[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Roles = "shelter_admin")]
public async Task<IActionResult> Reject(int adoptId)
{
    var accountId = GetUserId();
    if (!accountId.HasValue)
    {
        return RedirectToAction("Login", "Account");
    }

    if (!IsShelterAdmin())
    {
        return Forbid();
    }

    try
    {
        await _adoptionService.UpdateApplicationStatusAsync(
            adoptId,
            AdoptionStatuses.Rejected,
            accountId.Value);


        var userId = await _adoptionService.GetApplicantUserIdAsync(adoptId);

        await _notificationService.CreateAsync(
            userId,
            "Заявку відхилено",
            "На жаль, вашу заявку на адопцію відхилено.");
    }
    catch (KeyNotFoundException)
    {
        return NotFound();
    }
    catch (UnauthorizedAccessException)
    {
        return Forbid();
    }
    catch (InvalidOperationException ex)
    {
        TempData["Error"] = ex.Message;
        return RedirectToAction(nameof(Index));
    }

    TempData["Success"] = "Заявку відхилено.";
    return RedirectToAction(nameof(Index));
}

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "user")]
    public async Task<IActionResult> Delete(int adoptId)
    {
        var accountId = GetUserId();
        if (!accountId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            await _adoptionService.DeleteApplicationAsync(adoptId, accountId.Value);
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Вашу заявку успішно скасовано.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = "shelter_admin,system_admin")]
    public async Task<IActionResult> UserDetails(int adoptId)
    {
        var accountId = GetUserId();
        if (!accountId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        AdoptionApplication? currentApplication;

        try
        {
            currentApplication = await _adoptionService.GetApplicationDetailsAsync(
                adoptId,
                accountId.Value,
                User.IsInRole("system_admin"));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        if (currentApplication == null)
        {
            return NotFound("Заявку не знайдено.");
        }

        if (currentApplication.UserProfile == null)
        {
            return NotFound("Користувача не знайдено.");
        }

        return View(currentApplication);
    }

    protected int? GetUserId()
    {
        var userId = _userManager.GetUserId(User)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(userId, out var parsedUserId) ? parsedUserId : null;
    }

    private bool IsShelterAdmin()
    {
        return User.IsInRole("shelter_admin")
            || HttpContext.Session.GetString("Role") == "shelter_admin";
    }
}
