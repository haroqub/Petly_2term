using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
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
using Petly.Controllers;
using Petly.Business.Services;
using Petly.DataAccess.Data;
using Petly.Models;
using Xunit;

namespace Petly.Tests;

public class NotificationsControllerTests
{
    [Fact]
    public async Task Index_ReturnsUserNotifications()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);

        var user = await CreateUserAsync(scope.UserManager, scope.RoleManager, "user@test.com", "pass123", "user");

        db.Notifications.Add(new Notification
        {
            Id = 1,
            UserId = user.Id,
            Message = "Hello",
            Type = "Test"
        });

        await db.SaveChangesAsync();

        var controller = CreateController(scope, user.Id);

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<List<Notification>>(view.Model);

        Assert.Single(model);
        Assert.Equal("Hello", model[0].Message);
    }

    [Fact]
    public async Task MarkAsRead_Redirects()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);

        var user = await CreateUserAsync(scope.UserManager, scope.RoleManager, "user@test.com", "pass123", "user");

        var controller = CreateController(scope, user.Id);

        var result = await controller.MarkAsRead(1);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task Delete_Redirects()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);

        var user = await CreateUserAsync(scope.UserManager, scope.RoleManager, "user@test.com", "pass123", "user");

        var controller = CreateController(scope, user.Id);

        var result = await controller.Delete(1);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task MarkAllAsRead_Redirects()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);

        var user = await CreateUserAsync(scope.UserManager, scope.RoleManager, "user@test.com", "pass123", "user");

        var controller = CreateController(scope, user.Id);

        var result = await controller.MarkAllAsRead();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
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
        var services = new ServiceCollection();

        services.AddSingleton(db);
        services.AddSingleton<IOptions<IdentityOptions>>(Options.Create(new IdentityOptions()));
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();
        services.AddSingleton<ILookupNormalizer, UpperInvariantLookupNormalizer>();
        services.AddSingleton<IdentityErrorDescriber>();

        services.AddSingleton<IUserStore<ApplicationUser>,
            UserStore<ApplicationUser, IdentityRole<int>, ApplicationDbContext, int>>();

        services.AddSingleton<IRoleStore<IdentityRole<int>>,
            RoleStore<IdentityRole<int>, ApplicationDbContext, int>>();

        services.AddSingleton<ILogger<UserManager<ApplicationUser>>>(NullLogger<UserManager<ApplicationUser>>.Instance);
        services.AddSingleton<ILogger<RoleManager<IdentityRole<int>>>>(NullLogger<RoleManager<IdentityRole<int>>>.Instance);

        services.AddSingleton<UserManager<ApplicationUser>>();
        services.AddSingleton<RoleManager<IdentityRole<int>>>();

        services.AddScoped<NotificationService>();
        services.AddScoped<NotificationsController>();

        var provider = services.BuildServiceProvider();

        return new TestIdentityScope(
            provider,
            provider.GetRequiredService<UserManager<ApplicationUser>>(),
            provider.GetRequiredService<RoleManager<IdentityRole<int>>>());
    }


    private static NotificationsController CreateController(TestIdentityScope scope, int userId)
    {
        var controller = scope.ServiceProvider.GetRequiredService<NotificationsController>();

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }, "Test")),
            Session = new TestSession()
        };

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        controller.TempData = new TempDataDictionary(
            httpContext,
            new TestTempDataProvider());

        return controller;
    }


    private static async Task<ApplicationUser> CreateUserAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<int>> roleManager,
        string email,
        string password,
        string role)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole<int>(role));

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            RegistrationDate = DateTime.UtcNow,
            Status = "Активний"
        };

        await userManager.CreateAsync(user, password);
        await userManager.AddToRoleAsync(user, role);

        return user;
    }

    private sealed record TestIdentityScope(
        ServiceProvider ServiceProvider,
        UserManager<ApplicationUser> UserManager,
        RoleManager<IdentityRole<int>> RoleManager);

    private sealed class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public IEnumerable<string> Keys => _store.Keys;
        public string Id => Guid.NewGuid().ToString();
        public bool IsAvailable => true;

        public void Clear() => _store.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value)
            => _store.TryGetValue(key, out value);
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context)
            => new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}