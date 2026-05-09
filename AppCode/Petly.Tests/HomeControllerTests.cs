using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Petly.Business.Services;
using Petly.Controllers;
using Petly.DataAccess.Data;
using Petly.Models;
using Xunit;

namespace Petly.Tests;

public class HomeControllerTests
{
    [Fact]
    public async Task HomeController_Index()
    {
        await using var db = CreateDbContext();
        db.Pets.AddRange(
            new Pet
            {
                PetId = 1,
                ShelterId = 2,
                PetName = "Лайма",
                Type = "Собака",
                Breed = "Лабрадор",
                Status = "Доступний",
                CreatedAt = new DateTime(2026, 4, 10, 10, 0, 0, DateTimeKind.Utc)
            },
            new Pet
            {
                PetId = 2,
                ShelterId = 3,
                PetName = "Мурчик",
                Type = "Кіт",
                Breed = "Метис",
                Status = "Доступний",
                CreatedAt = new DateTime(2026, 4, 11, 10, 0, 0, DateTimeKind.Utc)
            },
            new Pet
            {
                PetId = 3,
                ShelterId = 4,
                PetName = "Рекс",
                Type = "Собака",
                Breed = "Вівчарка",
                Status = "Прилаштований",
                CreatedAt = new DateTime(2026, 4, 12, 10, 0, 0, DateTimeKind.Utc)
            });
        await db.SaveChangesAsync();

        string contentRoot = CreateContentRoot();
        try
        {
            TestIdentityScope scope = CreateIdentityScope(db, contentRoot);
            HomeController controller = CreateController(scope);

            IActionResult result = await controller.Index("Собака", "Лаб");

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<List<Pet>>(view.Model);
            var pet = Assert.Single(model);
            Assert.Equal(1, pet.PetId);
            Assert.Equal("Лайма", pet.PetName);
            Assert.Equal("Собака", controller.ViewBag.TypeFilter);
            Assert.Equal("Лаб", controller.ViewBag.SearchTerm);
        }
        finally
        {
            DeleteDirectory(contentRoot);
        }
    }

    [Fact]
    public async Task HomeController_About()
    {
        await using var db = CreateDbContext();
        string contentRoot = CreateContentRoot();

        try
        {
            await CreateAboutPageAsync(contentRoot, new AboutPageViewModel
            {
                Eyebrow = "Petly поруч",
                HeroTitle = "Про платформу",
                HeroText = "Ми допомагаємо тваринам знайти дім.",
                ContentTitle = "Що ми робимо",
                ContentText = "Об'єднуємо притулки та майбутніх власників.",
                ContactsTitle = "Контакти команди",
                Contacts = "+380001112233\npetly@test.com",
                CanEdit = false
            });

            TestIdentityScope scope = CreateIdentityScope(db, contentRoot);
            await CreateUserAsync(scope.UserManager, scope.RoleManager, "admin@petly.com", "pass123", "system_admin", userId: 1);
            HomeController controller = CreateController(scope, "system_admin", userId: 1);

            IActionResult result = await controller.About();

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<AboutPageViewModel>(view.Model);
            Assert.Equal("Petly поруч", model.Eyebrow);
            Assert.Equal("Про платформу", model.HeroTitle);
            Assert.Equal("Що ми робимо", model.ContentTitle);
            Assert.Equal("Контакти команди", model.ContactsTitle);
            Assert.True(model.CanEdit);
        }
        finally
        {
            DeleteDirectory(contentRoot);
        }
    }

    [Fact]
    public async Task HomeController_EditAbout()
    {
        await using var db = CreateDbContext();
        string contentRoot = CreateContentRoot();

        try
        {
            TestIdentityScope scope = CreateIdentityScope(db, contentRoot);
            await CreateUserAsync(scope.UserManager, scope.RoleManager, "admin@petly.com", "pass123", "system_admin", userId: 1);
            HomeController controller = CreateController(scope, "system_admin", userId: 1);

            var model = new AboutPageViewModel
            {
                Eyebrow = "  Petly поруч із тими, хто шукає дім  ",
                HeroTitle = "  Про нас  ",
                HeroText = "  Допомагаємо тваринам і людям знайти одне одного.  ",
                ContentTitle = "  Як ми працюємо  ",
                ContentText = "  Публікуємо анкети тварин і підтримуємо притулки.  ",
                ContactsTitle = "  Наші контакти  ",
                Contacts = "  +380009998877\npetly@petly.com  ",
                CanEdit = true
            };

            IActionResult result = await controller.EditAbout(model);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(HomeController.About), redirect.ActionName);
            Assert.Equal("Дані сторінки \"Про Нас\" оновлено.", controller.TempData["Success"]);

            string aboutPagePath = Path.Combine(contentRoot, "App_Data", "about-page.json");
            await using var stream = System.IO.File.OpenRead(aboutPagePath);
            var saved = await JsonSerializer.DeserializeAsync<AboutPageViewModel>(stream);

            Assert.NotNull(saved);
            Assert.Equal("Petly поруч із тими, хто шукає дім", saved!.Eyebrow);
            Assert.Equal("Про нас", saved.HeroTitle);
            Assert.Equal("Допомагаємо тваринам і людям знайти одне одного.", saved.HeroText);
            Assert.Equal("Як ми працюємо", saved.ContentTitle);
            Assert.Equal("Публікуємо анкети тварин і підтримуємо притулки.", saved.ContentText);
            Assert.Equal("Наші контакти", saved.ContactsTitle);
            Assert.Equal("+380009998877\npetly@petly.com", saved.Contacts);
            Assert.False(saved.CanEdit);
        }
        finally
        {
            DeleteDirectory(contentRoot);
        }
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static TestIdentityScope CreateIdentityScope(ApplicationDbContext db, string contentRootPath)
    {
        var services = new ServiceCollection();

        services.AddSingleton(db);
        services.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment(contentRootPath));
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
        services.AddSingleton<PetService>();
        services.AddTransient<HomeController>();

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        return new TestIdentityScope(
            serviceProvider,
            serviceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
            serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>());
    }

    private static HomeController CreateController(TestIdentityScope scope, string? role = null, int? userId = null)
    {
        var controller = scope.ServiceProvider.GetRequiredService<HomeController>();
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
            IdentityResult roleResult = await roleManager.CreateAsync(new IdentityRole<int>(role));
            Assert.True(roleResult.Succeeded, string.Join(", ", roleResult.Errors.Select(error => error.Description)));
        }

        var user = new ApplicationUser
        {
            Id = userId ?? default,
            UserName = email,
            Email = email,
            RegistrationDate = DateTime.UtcNow,
            Status = "Активний"
        };

        IdentityResult createResult = await userManager.CreateAsync(user, password);
        Assert.True(createResult.Succeeded, string.Join(", ", createResult.Errors.Select(error => error.Description)));

        IdentityResult addToRoleResult = await userManager.AddToRoleAsync(user, role);
        Assert.True(addToRoleResult.Succeeded, string.Join(", ", addToRoleResult.Errors.Select(error => error.Description)));

        return user;
    }

    private static string CreateContentRoot()
    {
        string path = Path.Combine(Path.GetTempPath(), "petly-home-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static async Task CreateAboutPageAsync(string contentRootPath, AboutPageViewModel model)
    {
        string appDataPath = Path.Combine(contentRootPath, "App_Data");
        Directory.CreateDirectory(appDataPath);

        string aboutPagePath = Path.Combine(appDataPath, "about-page.json");
        await using var stream = System.IO.File.Create(aboutPagePath);
        await JsonSerializer.SerializeAsync(stream, model, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private sealed record TestIdentityScope(
        ServiceProvider ServiceProvider,
        UserManager<ApplicationUser> UserManager,
        RoleManager<IdentityRole<int>> RoleManager);

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public TestWebHostEnvironment(string contentRootPath)
        {
            ApplicationName = nameof(Petly);
            ContentRootPath = contentRootPath;
            ContentRootFileProvider = new PhysicalFileProvider(contentRootPath);
            EnvironmentName = "Development";
            WebRootPath = contentRootPath;
            WebRootFileProvider = new PhysicalFileProvider(contentRootPath);
        }

        public string ApplicationName { get; set; }

        public IFileProvider WebRootFileProvider { get; set; }

        public string WebRootPath { get; set; }

        public string EnvironmentName { get; set; }

        public string ContentRootPath { get; set; }

        public IFileProvider ContentRootFileProvider { get; set; }
    }
}
