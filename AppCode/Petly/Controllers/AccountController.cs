using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Petly.Business.Services;
using System.Security.Claims;
using Petly.Models;

namespace Petly.Controllers;

[Authorize]
public class AccountController : Controller
{
    private readonly AccountService _accountService;
    private readonly IEmailService _emailService;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(
        AccountService accountService,
        IEmailService emailService,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _accountService = accountService;
        _emailService = emailService;
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login() => View(new LoginViewModel());

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied(string? returnUrl = null)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user != null)
        {
            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                isPersistent: model.RememberMe,
                lockoutOnFailure: false);
            if (result.Succeeded)
            {
                await SetSessionAsync(user);
                TempData["Success"] = $"Привіт, {user.Name ?? "користувачу"}!";
                return RedirectToAction("Index", "Home");
            }
        }

        ModelState.AddModelError(string.Empty, "Неправильний email або пароль");
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (await _accountService.EmailExistsAsync(model.Email))
        {
            ModelState.AddModelError("Email", "Такий email вже існує");
            return View(model);
        }

        var profile = await _accountService.CreateUserAsync(model);

        await _signInManager.SignInAsync(profile, isPersistent: true);
        await SetSessionAsync(profile);

        TempData["Success"] = "Користувача успішно зареєстровано";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            TempData["Success"] = "Якщо email існує в системі, ми надіслали код.";
            return RedirectToAction(nameof(Login));
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        try
        {
            await _emailService.SendPasswordResetCodeAsync(user.Email, token, 60);
            TempData["Success"] = "Код для відновлення надіслано на вашу пошту.";
            return RedirectToAction(nameof(ResetPassword), new { email = user.Email });
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "Не вдалося надіслати код на email.");
            return View(model);
        }
    }


    [HttpGet]
    [AllowAnonymous]
    public IActionResult VerifyResetCode(string email) => RedirectToAction(nameof(ResetPassword), new { email });
    
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public IActionResult ResendResetCode(string email) => RedirectToAction(nameof(ForgotPassword));

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string email)
    {
        if (string.IsNullOrEmpty(email)) return RedirectToAction(nameof(ForgotPassword));

        return View(new ResetPasswordViewModel
        {
            Email = email,
        });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null) return RedirectToAction(nameof(Login));

        var result = await _userManager.ResetPasswordAsync(user, model.Code, model.NewPassword);
        if (result.Succeeded)
        {
            TempData["Success"] = "Пароль успішно змінено. Тепер ви можете увійти.";
            return RedirectToAction(nameof(Login));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = "system_admin")]
    public async Task<IActionResult> Users()
    {
        var users = await _accountService.GetAllUsersAsync();
        return View(users);
    }

    [HttpGet]
    [Authorize(Roles = "system_admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var profile = await _accountService.GetUserAsync(id);
        if (profile == null) return NotFound();
        return View(profile);
    }

    [HttpPost]
    [Authorize(Roles = "system_admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (await _accountService.EmailExistsAsync(model.Email, model.AccountId))
        {
            ModelState.AddModelError("Email", "Такий email вже використовується");
            return View(model);
        }

        await _accountService.UpdateUserAsync(model);
        TempData["Success"] = "Дані користувача оновлено";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [Authorize(Roles = "system_admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _accountService.DeleteUserAsync(id);
        TempData["Success"] = "Користувача видалено";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync(); 
        HttpContext.Session.Clear(); 
        return RedirectToAction(nameof(Login));
    }

    private async Task SetSessionAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "user";

        HttpContext.Session.SetInt32("AccountId", user.Id);
        HttpContext.Session.SetString("UserName", user.Name ?? user.Email!);
        HttpContext.Session.SetString("Role", role);
        HttpContext.Session.SetString("UserEmail", user.Email!);
    }

  public async Task<IActionResult> Profile()
{
    var user = await _accountService.GetCurrentUserAsync(User);
    if (user == null) return RedirectToAction("Login");

    var roles = await _userManager.GetRolesAsync(user);
    var showNameFields = ShouldShowProfileNameFields(roles);

    var model = new EditProfileViewModel
    {
        Name = user.Name,
        Surname = user.Surname,
        Email = user.Email,
        ExistingImagePath = string.IsNullOrEmpty(user.ImagePath)
                            ? "/images/default-profile.png"
                            : user.ImagePath,
        ShowNameFields = showNameFields
    };

    return View(model);
}

[HttpPost]
public async Task<IActionResult> Profile(EditProfileViewModel model, IFormFile? profileImage)
{
    var user = await _accountService.GetCurrentUserAsync(User);
    if (user == null) return RedirectToAction("Login");

    var roles = await _userManager.GetRolesAsync(user);
    var showNameFields = ShouldShowProfileNameFields(roles);
    model.ShowNameFields = showNameFields;

    if (!showNameFields)
    {
        ModelState.Remove(nameof(EditProfileViewModel.Name));
        ModelState.Remove(nameof(EditProfileViewModel.Surname));
        model.Name = user.Name ?? string.Empty;
        model.Surname = user.Surname ?? string.Empty;
    }

    if (!ModelState.IsValid)
    {
        model.ExistingImagePath = string.IsNullOrEmpty(user.ImagePath)
                            ? "/images/default-profile.png"
                            : user.ImagePath;
        return View(model);
    }

    user.Name = model.Name;
    user.Surname = model.Surname;
    user.Email = model.Email;

    if (profileImage != null && profileImage.Length > 0)
    {
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var ext = Path.GetExtension(profileImage.FileName).ToLower();
        if (!allowedExtensions.Contains(ext))
        {
            ModelState.AddModelError(string.Empty, "Неприпустимий тип файлу");
            return View(model);
        }

        var fileName = $"{Guid.NewGuid()}{ext}";
        var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/profiles", fileName);

        using var stream = new FileStream(path, FileMode.Create);
        await profileImage.CopyToAsync(stream);

        user.ImagePath = $"/images/profiles/{fileName}";
    }

    await _accountService.UpdateProfileAsync(user, model, user.ImagePath);

    model.ExistingImagePath = user.ImagePath ?? "/images/default-profile.png";
    TempData["Success"] = "Профіль успішно оновлено!";
    return View(model);
}

private static bool ShouldShowProfileNameFields(IList<string> roles)
{
    return !roles.Contains("system_admin") && !roles.Contains("shelter_admin");
}

}
