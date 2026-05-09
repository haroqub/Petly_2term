using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

public class SuccessStoriesControllerTests
{
    [Fact]
    public async Task ViewStoriesList()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        
        var pet1 = new Pet { PetId = 1, PetName = "Барсік", Status = "Прилаштований" };
        var pet2 = new Pet { PetId = 2, PetName = "Рекс", Status = "Прилаштований" };
        db.Pets.AddRange(pet1, pet2);

        db.SuccessStories.AddRange(
            new SuccessStory
            {
                Id = 1,
                PetId = 1,
                Title = "Барсік вдома!",
                StoryText = "Все пройшло чудово.",
                CreatedAt = DateTime.UtcNow
            },
            new SuccessStory
            {
                Id = 2,
                PetId = 2,
                Title = "Рекс знайшов родину",
                StoryText = "Рекс тепер живе в селі.",
                CreatedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        SuccessStoriesController controller = CreateController(scope);

        IActionResult result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<List<SuccessStory>>(view.Model);
        
        Assert.Equal(2, model.Count);
        Assert.Contains(model, s => s.Title == "Барсік вдома!");
        Assert.Contains(model, s => s.Title == "Рекс знайшов родину");
    }

    [Fact]
    public async Task OpenCreatePage()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        await CreateUserAsync(scope.UserManager, scope.RoleManager, "admin@petly.test", "pass123", "shelter_admin", userId: 1);
        
        db.Pets.Add(new Pet { PetId = 3, PetName = "Мурка", Status = "Прилаштований" });
        await db.SaveChangesAsync();

        SuccessStoriesController controller = CreateController(scope, "shelter_admin", userId: 1);

        IActionResult result = await controller.Create();

        var view = Assert.IsType<ViewResult>(result);
        var petsList = Assert.IsType<List<Pet>>((object)controller.ViewBag.Pets);
        var pet = Assert.Single(petsList);
        Assert.Equal("Мурка", pet.PetName);
    }

    [Fact]
    public async Task CreateStory()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        await CreateUserAsync(scope.UserManager, scope.RoleManager, "admin@petly.test", "pass123", "shelter_admin", userId: 2);
        
        db.Pets.Add(new Pet { PetId = 4, PetName = "Шарік", Status = "Прилаштований" });
        await db.SaveChangesAsync();

        SuccessStoriesController controller = CreateController(scope, "shelter_admin", userId: 2);
        
        var newStory = new SuccessStory
        {
            PetId = 4,
            Title = "Шарік тепер щасливий",
            StoryText = "Довга історія про Шаріка..."
        };

        IActionResult result = await controller.Create(newStory, null);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        var savedStory = await db.SuccessStories.SingleAsync();
        Assert.Equal(4, savedStory.PetId);
        Assert.Equal("Шарік тепер щасливий", savedStory.Title);
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
        
        services.AddSingleton<SuccessStoryService>();
        
        var inMemorySettings = new Dictionary<string, string> {
            {"CloudinarySettings:CloudName", "test_cloud"},
            {"CloudinarySettings:ApiKey", "test_key"},
            {"CloudinarySettings:ApiSecret", "test_secret"}
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        services.AddTransient<SuccessStoriesController>();

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        return new TestIdentityScope(
            serviceProvider,
            serviceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
            serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>());
    }

    private static SuccessStoriesController CreateController(TestIdentityScope scope, string? role = null, int? userId = null)
    {
        var controller = scope.ServiceProvider.GetRequiredService<SuccessStoriesController>();
        var httpContext = new DefaultHttpContext();

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

    private static ClaimsPrincipal CreatePrincipal(int userId, string? role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };

        if (!string.IsNullOrWhiteSpace(role))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    private static async Task<ApplicationUser> CreateUserAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<int>> roleManager,
        string email,
        string password,
        string role,
        int? userId = null)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<int>(role));
        }

        var user = new ApplicationUser
        {
            Id = userId ?? default,
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

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) {}
    }
     [Fact]
    public async Task OpenEditPage()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        await CreateUserAsync(scope.UserManager, scope.RoleManager, "admin@petly.test", "pass123", "shelter_admin", userId: 3);
        
        db.Pets.Add(new Pet { PetId = 5, PetName = "Сніжок", Status = "Прилаштований" });
        db.SuccessStories.Add(new SuccessStory
        {
            Id = 10,
            PetId = 5,
            Title = "Сніжок вдома",
            StoryText = "Текст"
        });
        await db.SaveChangesAsync();

        SuccessStoriesController controller = CreateController(scope, "shelter_admin", userId: 3);

        IActionResult result = await controller.Edit(10);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<SuccessStory>(view.Model);
        Assert.Equal(10, model.Id);
        Assert.Equal("Сніжок вдома", model.Title);
    }

    [Fact]
    public async Task UpdateStory()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        await CreateUserAsync(scope.UserManager, scope.RoleManager, "admin@petly.test", "pass123", "shelter_admin", userId: 4);
        
        db.Pets.Add(new Pet { PetId = 6, PetName = "Рижик", Status = "Прилаштований" });
        db.SuccessStories.Add(new SuccessStory
        {
            Id = 15,
            PetId = 6,
            Title = "Старий заголовок",
            StoryText = "Старий текст"
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        SuccessStoriesController controller = CreateController(scope, "shelter_admin", userId: 4);
        
        var updatedStory = new SuccessStory
        {
            Id = 15,
            PetId = 6,
            Title = "Новий заголовок",
            StoryText = "Новий текст"
        };

        IActionResult result = await controller.Edit(15, updatedStory, null);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        var saved = await db.SuccessStories.SingleAsync(x => x.Id == 15);
        Assert.Equal("Новий заголовок", saved.Title);
        Assert.Equal("Новий текст", saved.StoryText);
    }
}