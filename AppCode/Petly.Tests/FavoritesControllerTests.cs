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

public class FavoritesControllerTests
{
    [Fact]
    public async Task AddFavorite()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        var user = await CreateUserAsync(scope.UserManager, scope.RoleManager, "user@test.com", "pass123", "user");
        db.Pets.Add(new Pet { PetId = 1, PetName = "Barsik", ShelterId = 10 });
        await db.SaveChangesAsync();

        var controller = CreateController(scope, "user", user.Id);

        var result = await controller.Add(1);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        var favorite = await db.Favorites.SingleAsync();
        Assert.Equal(user.Id, favorite.UserId);
        Assert.Equal(1, favorite.PetId);
    }

    [Fact]
    public async Task RemoveFavorite()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        var user = await CreateUserAsync(scope.UserManager, scope.RoleManager, "user@test.com", "pass123", "user");

        db.Pets.Add(new Pet { PetId = 1, PetName = "Barsik", ShelterId = 10 });
        db.Favorites.Add(new Favorite { Id = 1, UserId = user.Id, PetId = 1 });
        await db.SaveChangesAsync();

        var controller = CreateController(scope, "user", user.Id);

        var result = await controller.Remove(1);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.False(await db.Favorites.AnyAsync(f => f.Id == 1));
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
        services.AddSingleton<IUserStore<ApplicationUser>, UserStore<ApplicationUser, IdentityRole<int>, ApplicationDbContext, int>>();
        services.AddSingleton<IRoleStore<IdentityRole<int>>, RoleStore<IdentityRole<int>, ApplicationDbContext, int>>();
        services.AddSingleton<ILogger<UserManager<ApplicationUser>>>(NullLogger<UserManager<ApplicationUser>>.Instance);
        services.AddSingleton<ILogger<RoleManager<IdentityRole<int>>>>(NullLogger<RoleManager<IdentityRole<int>>>.Instance);
        services.AddSingleton<UserManager<ApplicationUser>>();
        services.AddSingleton<RoleManager<IdentityRole<int>>>();
        services.AddScoped<FavoritesController>();
        services.AddScoped<NotificationService>();

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        return new TestIdentityScope(
            serviceProvider,
            serviceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
            serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>());
    }

    private static FavoritesController CreateController(TestIdentityScope scope, string? role = null, int? userId = null)
    {
        var controller = scope.ServiceProvider.GetRequiredService<FavoritesController>();
        var httpContext = new DefaultHttpContext
        {
            Session = new TestSession()
        };

        if (userId.HasValue)
        {
            httpContext.User = CreatePrincipal(userId.Value, role);
        }

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
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

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static async Task<ApplicationUser> CreateUserAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<int>> roleManager,
        string email,
        string password,
        string role)
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
            RegistrationDate = DateTime.UtcNow,
            Status = "Активний"
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
