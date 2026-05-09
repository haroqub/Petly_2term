using System.Reflection;
using System.Security.Claims;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Petly.Business.Services;
using Petly.Controllers;
using Petly.DataAccess.Data;
using Petly.Models;
using Xunit;

namespace Petly.Tests;

public class AccountControllerAdminTests
{
    [Fact]
    public async Task LoginSuccess()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        ApplicationUser user = await CreateUserAsync(
            scope.UserManager,
            scope.RoleManager,
            "admin@petly.com",
            "secret123",
            "system_admin",
            "Admin",
            "System");

        AccountController controller = CreateController(scope);
        var model = new LoginViewModel
        {
            Email = "admin@petly.com",
            Password = "secret123"
        };

        IActionResult result = await controller.Login(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
        Assert.Equal(user.Id, controller.HttpContext.Session.GetInt32("AccountId"));
        Assert.Equal("system_admin", controller.HttpContext.Session.GetString("Role"));
        Assert.Equal("admin@petly.com", controller.HttpContext.Session.GetString("UserEmail"));
        Assert.Equal("Привіт, Admin!", controller.TempData["Success"]);
    }

    [Fact]
    public async Task LoginWrongPassword()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        await CreateUserAsync(
            scope.UserManager,
            scope.RoleManager,
            "admin@petly.com",
            "secret123",
            "system_admin");

        AccountController controller = CreateController(scope);
        var model = new LoginViewModel
        {
            Email = "admin@petly.com",
            Password = "wrong-password"
        };

        IActionResult result = await controller.Login(model);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(model, view.Model);
        var error = Assert.Single(controller.ModelState[string.Empty]!.Errors);
        Assert.Equal("Неправильний email або пароль", error.ErrorMessage);
        Assert.Null(controller.HttpContext.Session.GetInt32("AccountId"));
    }

    [Fact]
    public async Task ViewUsersList()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);

        ApplicationUser user = await CreateUserAsync(
            scope.UserManager,
            scope.RoleManager,
            "user@petly.com",
            "pass123",
            "user",
            "Ira",
            "User",
            new DateTime(2026, 3, 24),
            "Активний");

        ApplicationUser shelterAdmin = await CreateUserAsync(
            scope.UserManager,
            scope.RoleManager,
            "shelter@petly.com",
            "pass123",
            "shelter_admin",
            registrationDate: new DateTime(2026, 3, 23));

        db.Shelters.Add(new Shelter
        {
            AccountId = shelterAdmin.Id,
            ShelterName = "Pet House",
            AdminName = "Oleh",
            Location = "Lviv"
        });
        await db.SaveChangesAsync();

        AccountController controller = CreateController(scope, "system_admin", userId: 500);

        IActionResult result = await controller.Users();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<List<AdminUserViewModel>>(view.Model);
        Assert.Equal(2, model.Count);
        Assert.Contains(model, x => x.Email == user.Email && x.Name == "Ira" && x.Role == "user");
        Assert.Contains(model, x => x.Email == shelterAdmin.Email && x.Name == "Oleh" && x.ShelterName == "Pet House");
    }

    [Fact]
    public void AdminActionsRequireSystemAdminRole()
    {
        AssertActionRole(nameof(AccountController.Users), "system_admin");
        AssertActionRole(nameof(AccountController.Edit), "system_admin", typeof(int));
        AssertActionRole(nameof(AccountController.Edit), "system_admin", typeof(UserEditViewModel));
        AssertActionRole(nameof(AccountController.Delete), "system_admin", typeof(int));
    }

    [Fact]
    public void AccessDeniedIsAvailableForForbiddenRequests()
    {
        MethodInfo method = typeof(AccountController).GetMethod(nameof(AccountController.AccessDenied), new[] { typeof(string) })!;
        Assert.NotNull(method.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public void AccessDeniedReturnsForbiddenView()
    {
        using ApplicationDbContext db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        AccountController controller = CreateController(scope, "user", userId: 10);

        IActionResult result = controller.AccessDenied("/Account/Users");

        Assert.IsType<ViewResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, controller.Response.StatusCode);
        Assert.Equal("/Account/Users", controller.ViewBag.ReturnUrl);
    }

    [Fact]
    public async Task OpenEditPage()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        ApplicationUser user = await CreateUserAsync(
            scope.UserManager,
            scope.RoleManager,
            "member@petly.com",
            "pass123",
            "user",
            "Nazar",
            "Test");

        AccountController controller = CreateController(scope, "system_admin", userId: 500);

        IActionResult result = await controller.Edit(user.Id);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<UserEditViewModel>(view.Model);
        Assert.Equal(user.Id, model.AccountId);
        Assert.Equal("member@petly.com", model.Email);
        Assert.Equal("Nazar", model.Name);
        Assert.Equal("Test", model.Surname);
    }

    [Fact]
    public async Task OpenEditPageUserNotFound()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        AccountController controller = CreateController(scope, "system_admin", userId: 500);

        IActionResult result = await controller.Edit(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateUser()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        ApplicationUser user = await CreateUserAsync(
            scope.UserManager,
            scope.RoleManager,
            "old@petly.com",
            "oldpass1",
            "user",
            "Old",
            "Name");

        AccountController controller = CreateController(scope, "system_admin", userId: 500);
        var model = new UserEditViewModel
        {
            AccountId = user.Id,
            Email = "new@petly.com",
            Name = "Updated",
            Surname = "User",
            Status = "Заблокований",
            Role = "user",
            Password = "newpass123"
        };

        IActionResult result = await controller.Edit(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Users", redirect.ActionName);
        Assert.Equal("Дані користувача оновлено", controller.TempData["Success"]);

        ApplicationUser updated = await scope.UserManager.FindByIdAsync(user.Id.ToString()) ?? throw new InvalidOperationException();
        Assert.Equal("new@petly.com", updated.Email);
        Assert.Equal("new@petly.com", updated.UserName);
        Assert.Equal("Updated", updated.Name);
        Assert.Equal("User", updated.Surname);
        Assert.Equal("Заблокований", updated.Status);
        Assert.True(await scope.UserManager.CheckPasswordAsync(updated, "newpass123"));
    }

    [Fact]
    public async Task ForgotPasswordSendsCodeAndRedirectsToReset()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        await CreateUserAsync(
            scope.UserManager,
            scope.RoleManager,
            "member@petly.com",
            "pass123",
            "user");

        var emailService = new FakeEmailService();
        AccountController controller = CreateController(scope, emailService: emailService);

        IActionResult result = await controller.ForgotPassword(new ForgotPasswordViewModel
        {
            Email = "member@petly.com"
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ResetPassword", redirect.ActionName);
        Assert.Equal("member@petly.com", redirect.RouteValues!["email"]);
        Assert.Equal("member@petly.com", emailService.LastRecipientEmail);
        Assert.False(string.IsNullOrWhiteSpace(emailService.LastCode));
        Assert.Equal("Код для відновлення надіслано на вашу пошту.", controller.TempData["Success"]);
    }

    [Fact]
    public async Task ResetPasswordFlowUpdatesPassword()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        ApplicationUser user = await CreateUserAsync(
            scope.UserManager,
            scope.RoleManager,
            "reset@petly.com",
            "pass1234",
            "user");

        AccountController controller = CreateController(scope);
        string code = await scope.UserManager.GeneratePasswordResetTokenAsync(user);

        IActionResult result = await controller.ResetPassword(new ResetPasswordViewModel
        {
            Email = "reset@petly.com",
            Code = code,
            NewPassword = "newpass123",
            ConfirmPassword = "newpass123",
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirect.ActionName);
        Assert.Equal("Пароль успішно змінено. Тепер ви можете увійти.", controller.TempData["Success"]);

        ApplicationUser updated = await scope.UserManager.FindByIdAsync(user.Id.ToString()) ?? throw new InvalidOperationException();
        Assert.True(await scope.UserManager.CheckPasswordAsync(updated, "newpass123"));
    }

    [Fact]
    public async Task UpdateUserDuplicateEmail()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        await CreateUserAsync(
            scope.UserManager,
            scope.RoleManager,
            "first@petly.com",
            "pass123",
            "user",
            "First",
            "User");

        ApplicationUser secondUser = await CreateUserAsync(
            scope.UserManager,
            scope.RoleManager,
            "second@petly.com",
            "pass123",
            "user",
            "Second",
            "User");

        AccountController controller = CreateController(scope, "system_admin", userId: 500);
        var model = new UserEditViewModel
        {
            AccountId = secondUser.Id,
            Email = "first@petly.com",
            Name = "Second",
            Surname = "User",
            Status = "Активний",
            Role = "user"
        };

        IActionResult result = await controller.Edit(model);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(model, view.Model);
        var error = Assert.Single(controller.ModelState["Email"]!.Errors);
        Assert.Equal("Такий email вже використовується", error.ErrorMessage);

        ApplicationUser unchanged = await scope.UserManager.FindByIdAsync(secondUser.Id.ToString()) ?? throw new InvalidOperationException();
        Assert.Equal("second@petly.com", unchanged.Email);
    }

    [Fact]
    public async Task DeleteShelterAccount()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        ApplicationUser shelterAdmin = await CreateUserAsync(
            scope.UserManager,
            scope.RoleManager,
            "shelter-admin@petly.com",
            "pass123",
            "shelter_admin");

        db.Shelters.Add(new Shelter
        {
            AccountId = shelterAdmin.Id,
            ShelterName = "Safe Paw",
            AdminName = "Oksana",
            Location = "Kyiv"
        });
        db.Pets.Add(new Pet
        {
            PetId = 101,
            ShelterId = shelterAdmin.Id,
            PetName = "Barsik"
        });
        db.ShelterNeeds.Add(new ShelterNeed
        {
            NeedId = 55,
            ShelterId = shelterAdmin.Id,
            Description = "Food",
            PaymentDetails = "Card"
        });
        await db.SaveChangesAsync();

        AccountController controller = CreateController(scope, "system_admin", userId: 500);

        IActionResult result = await controller.Delete(shelterAdmin.Id);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Users", redirect.ActionName);
        Assert.Equal("Користувача видалено", controller.TempData["Success"]);
        Assert.Null(await scope.UserManager.FindByIdAsync(shelterAdmin.Id.ToString()));
        Assert.False(await db.Shelters.AnyAsync(s => s.AccountId == shelterAdmin.Id));
        Assert.False(await db.Pets.AnyAsync(p => p.ShelterId == shelterAdmin.Id));
        Assert.False(await db.ShelterNeeds.AnyAsync(n => n.ShelterId == shelterAdmin.Id));
    }

    private static void AssertActionRole(string actionName, string role, params Type[] parameterTypes)
    {
        MethodInfo method = typeof(AccountController).GetMethod(actionName, parameterTypes)!;
        var authorize = Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>(inherit: true));
        Assert.Equal(role, authorize.Roles);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static TestIdentityScope CreateIdentityScope(ApplicationDbContext db)
    {
        var identityOptionsValue = new IdentityOptions();
        identityOptionsValue.Tokens.ProviderMap[TokenOptions.DefaultProvider] =
            new TokenProviderDescriptor(typeof(TestTokenProvider));

        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddSingleton<IOptions<IdentityOptions>>(Options.Create(identityOptionsValue));
        services.AddSingleton<IOptions<AuthenticationOptions>>(Options.Create(new AuthenticationOptions()));
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();
        services.AddSingleton<ILookupNormalizer, UpperInvariantLookupNormalizer>();
        services.AddSingleton<IdentityErrorDescriber>();
        services.AddSingleton<TestTokenProvider>();
        services.AddSingleton<IUserStore<ApplicationUser>, UserStore<ApplicationUser, IdentityRole<int>, ApplicationDbContext, int>>();
        services.AddSingleton<IRoleStore<IdentityRole<int>>, RoleStore<IdentityRole<int>, ApplicationDbContext, int>>();
        services.AddSingleton<ILogger<UserManager<ApplicationUser>>>(NullLogger<UserManager<ApplicationUser>>.Instance);
        services.AddSingleton<ILogger<RoleManager<IdentityRole<int>>>>(NullLogger<RoleManager<IdentityRole<int>>>.Instance);
        services.AddSingleton<ILogger<SignInManager<ApplicationUser>>>(NullLogger<SignInManager<ApplicationUser>>.Instance);
        services.AddSingleton<UserManager<ApplicationUser>>();
        services.AddSingleton<RoleManager<IdentityRole<int>>>();
        services.AddSingleton<IUserClaimsPrincipalFactory<ApplicationUser>, UserClaimsPrincipalFactory<ApplicationUser, IdentityRole<int>>>();
        services.AddSingleton<IAuthenticationSchemeProvider, AuthenticationSchemeProvider>();
        services.AddSingleton<IUserConfirmation<ApplicationUser>, DefaultUserConfirmation<ApplicationUser>>();
        services.AddSingleton<SignInManager<ApplicationUser>, TestSignInManager>();
        services.AddSingleton<AccountService>();
        services.AddSingleton<IEmailService, FakeEmailService>();
        services.AddTransient<AccountController>();

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        return new TestIdentityScope(
            serviceProvider,
            serviceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
            serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>());
    }

    private static AccountController CreateController(
        TestIdentityScope scope,
        string? role = null,
        int? userId = null,
        FakeEmailService? emailService = null)
    {
        var controller = emailService == null
            ? scope.ServiceProvider.GetRequiredService<AccountController>()
            : ActivatorUtilities.CreateInstance<AccountController>(scope.ServiceProvider, emailService);

        var httpContext = new DefaultHttpContext
        {
            Session = new TestSession()
        };

        if (userId.HasValue)
        {
            httpContext.User = CreatePrincipal(userId.Value, role);
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        controller.TempData = new TempDataDictionary(httpContext, new TestTempDataProvider());

        return controller;
    }

    private static ClaimsPrincipal CreatePrincipal(int userId, string? role = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };

        if (!string.IsNullOrWhiteSpace(role))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        return new ClaimsPrincipal(identity);
    }

    private static async Task<ApplicationUser> CreateUserAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<int>> roleManager,
        string email,
        string password,
        string role,
        string? name = null,
        string? surname = null,
        DateTime? registrationDate = null,
        string? status = null)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            IdentityResult roleResult = await roleManager.CreateAsync(new IdentityRole<int>(role));
            Assert.True(roleResult.Succeeded, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            Name = name,
            Surname = surname,
            RegistrationDate = registrationDate ?? DateTime.UtcNow,
            Status = status ?? "Активний"
        };

        IdentityResult createResult = await userManager.CreateAsync(user, password);
        Assert.True(createResult.Succeeded, string.Join(", ", createResult.Errors.Select(e => e.Description)));

        IdentityResult addToRoleResult = await userManager.AddToRoleAsync(user, role);
        Assert.True(addToRoleResult.Succeeded, string.Join(", ", addToRoleResult.Errors.Select(e => e.Description)));

        return user;
    }

    private sealed record TestIdentityScope(
        ServiceProvider ServiceProvider,
        UserManager<ApplicationUser> UserManager,
        RoleManager<IdentityRole<int>> RoleManager);

    private sealed class FakeEmailService : IEmailService
    {
        public string? LastRecipientEmail { get; private set; }

        public string? LastCode { get; private set; }

        public Task SendPasswordResetCodeAsync(string recipientEmail, string code, int lifetimeMinutes, CancellationToken cancellationToken = default)
        {
            LastRecipientEmail = recipientEmail;
            LastCode = code;
            return Task.CompletedTask;
        }
    }

    private sealed class TestSignInManager : SignInManager<ApplicationUser>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public TestSignInManager(
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<SignInManager<ApplicationUser>> logger,
            IAuthenticationSchemeProvider schemes,
            IUserConfirmation<ApplicationUser> confirmation)
            : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
        {
            _userManager = userManager;
        }

        public override async Task<Microsoft.AspNetCore.Identity.SignInResult> PasswordSignInAsync(string userName, string password, bool isPersistent, bool lockoutOnFailure)
        {
            ApplicationUser? user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                return Microsoft.AspNetCore.Identity.SignInResult.Failed;
            }

            bool isValid = await _userManager.CheckPasswordAsync(user, password);
            return isValid
                ? Microsoft.AspNetCore.Identity.SignInResult.Success
                : Microsoft.AspNetCore.Identity.SignInResult.Failed;
        }

        public override Task SignInAsync(ApplicationUser user, bool isPersistent, string? authenticationMethod = null)
        {
            return Task.CompletedTask;
        }

        public override Task SignOutAsync()
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TestTokenProvider : IUserTwoFactorTokenProvider<ApplicationUser>
    {
        public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<ApplicationUser> manager, ApplicationUser user)
        {
            return Task.FromResult(false);
        }

        public Task<string> GenerateAsync(string purpose, UserManager<ApplicationUser> manager, ApplicationUser user)
        {
            return Task.FromResult($"{purpose}:{user.Id}");
        }

        public Task<bool> ValidateAsync(string purpose, string token, UserManager<ApplicationUser> manager, ApplicationUser user)
        {
            return Task.FromResult(token == $"{purpose}:{user.Id}");
        }
    }

    private sealed class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public IEnumerable<string> Keys => _store.Keys;

        public string Id { get; } = Guid.NewGuid().ToString();

        public bool IsAvailable => true;

        public void Clear() => _store.Clear();

        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Remove(string key) => _store.Remove(key);

        public void Set(string key, byte[] value) => _store[key] = value;

        public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value) => _store.TryGetValue(key, out value);
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}
