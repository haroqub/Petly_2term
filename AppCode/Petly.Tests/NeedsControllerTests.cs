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
using Petly.Business.Services;
using Petly.Controllers;
using Petly.DataAccess.Data;
using Petly.Models;
using Xunit;

namespace Petly.Tests;

public class NeedsControllerTests
{
    [Fact]
    public async Task ViewNeedsList()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        await CreateUserAsync(scope.UserManager, scope.RoleManager, "shelter5@petly.test", "pass123", "shelter_admin", userId: 5);
        db.Shelters.AddRange(
            new Shelter
            {
                AccountId = 5,
                ShelterName = "Shelter Alpha",
                Location = "Kyiv",
                AdminName = "Ira"
            },
            new Shelter
            {
                AccountId = 6,
                ShelterName = "Shelter Beta",
                Location = "Lviv",
                AdminName = "Oleh"
            });
        db.ShelterNeeds.AddRange(
            new ShelterNeed
            {
                NeedId = 1,
                ShelterId = 5,
                Description = "Потрібен корм для котів",
                PaymentDetails = "Можна привезти в притулок"
            },
            new ShelterNeed
            {
                NeedId = 3,
                ShelterId = 5,
                Description = "Потрібні миски",
                PaymentDetails = "Передати особисто"
            },
            new ShelterNeed
            {
                NeedId = 2,
                ShelterId = 6,
                Description = "Потрібні ковдри",
                PaymentDetails = "Переказ на рахунок"
            });
        await db.SaveChangesAsync();

        NeedsController controller = CreateController(scope, "shelter_admin", userId: 5);

        IActionResult result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<List<ShelterNeedGroupViewModel>>(view.Model);
        Assert.Equal(2, model.Count);
        Assert.True((bool)controller.ViewBag.CanCreate);
        Assert.Equal("shelter_admin", controller.ViewBag.UserRole);

        var firstShelter = Assert.Single(model, x => x.ShelterId == 5);
        Assert.True(firstShelter.CanManage);
        Assert.Equal("Київ", firstShelter.Location);
        Assert.Equal(2, firstShelter.Needs.Count);
        Assert.Contains(firstShelter.Needs, x => x.NeedId == 1 && x.Description == "Потрібен корм для котів");
        Assert.Contains(firstShelter.Needs, x => x.NeedId == 3 && x.PaymentDetails == "Передати особисто");

        var secondShelter = Assert.Single(model, x => x.ShelterId == 6);
        Assert.False(secondShelter.CanManage);
        Assert.Equal("Львів", secondShelter.Location);
        Assert.Single(secondShelter.Needs);
        Assert.Contains(secondShelter.Needs, x => x.NeedId == 2);
    }

    [Fact]
    public async Task OpenCreatePage()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        await CreateUserAsync(scope.UserManager, scope.RoleManager, "shelter5@petly.test", "pass123", "shelter_admin", userId: 5);
        NeedsController controller = CreateController(scope, "shelter_admin", userId: 5);

        IActionResult result = await controller.Create(5);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ShelterNeedFormViewModel>(view.Model);
        Assert.Equal(5, model.ShelterId);
    }

    [Fact]
    public async Task OpenCreatePageWithoutAccess()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        await CreateUserAsync(scope.UserManager, scope.RoleManager, "user5@petly.test", "pass123", "user", userId: 5);
        NeedsController controller = CreateController(scope, "user", userId: 5);

        IActionResult result = await controller.Create(5);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task OpenCreatePageWithAnotherShelterIdReturnsForbid()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        await CreateUserAsync(scope.UserManager, scope.RoleManager, "shelter5@petly.test", "pass123", "shelter_admin", userId: 5);
        NeedsController controller = CreateController(scope, "shelter_admin", userId: 5);

        IActionResult result = await controller.Create(6);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task CreateNeed()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        await CreateUserAsync(scope.UserManager, scope.RoleManager, "shelter7@petly.test", "pass123", "shelter_admin", userId: 7);
        NeedsController controller = CreateController(scope, "shelter_admin", userId: 7);
        var model = new ShelterNeedFormViewModel
        {
            ShelterId = 7,
            Description = "  Потрібні ліки для тварин  ",
            PaymentDetails = "  Оплата на картку  "
        };

        IActionResult result = await controller.Create(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Потребу успішно додано.", controller.TempData["Success"]);

        var saved = await db.ShelterNeeds.SingleAsync();
        Assert.Equal(7, saved.ShelterId);
        Assert.Equal("Потрібні ліки для тварин", saved.Description);
        Assert.Equal("Оплата на картку", saved.PaymentDetails);
    }

    [Fact]
    public async Task OpenEditPage()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        await CreateUserAsync(scope.UserManager, scope.RoleManager, "shelter4@petly.test", "pass123", "shelter_admin", userId: 4);
        db.Shelters.Add(new Shelter
        {
            AccountId = 4,
            ShelterName = "Shelter Delta",
            Location = "Dnipro",
            AdminName = "Marta"
        });
        db.ShelterNeeds.Add(new ShelterNeed
        {
            NeedId = 10,
            ShelterId = 4,
            Description = "Потрібен наповнювач",
            PaymentDetails = "Можна передати особисто"
        });
        await db.SaveChangesAsync();

        NeedsController controller = CreateController(scope, "shelter_admin", userId: 4);

        IActionResult result = await controller.Edit(10);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ShelterNeedFormViewModel>(view.Model);
        Assert.Equal(10, model.NeedId);
        Assert.Equal(4, model.ShelterId);
        Assert.Equal("Потрібен наповнювач", model.Description);
        Assert.Equal("Можна передати особисто", model.PaymentDetails);
    }

    [Fact]
    public async Task OpenEditPageNeedNotFound()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        await CreateUserAsync(scope.UserManager, scope.RoleManager, "shelter4@petly.test", "pass123", "shelter_admin", userId: 4);
        NeedsController controller = CreateController(scope, "shelter_admin", userId: 4);

        IActionResult result = await controller.Edit(404);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OpenEditPageWithoutAccess()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        await CreateUserAsync(scope.UserManager, scope.RoleManager, "shelter7@petly.test", "pass123", "shelter_admin", userId: 7);
        db.Shelters.Add(new Shelter
        {
            AccountId = 8,
            ShelterName = "Shelter Sigma",
            Location = "Odesa",
            AdminName = "Taras"
        });
        db.ShelterNeeds.Add(new ShelterNeed
        {
            NeedId = 11,
            ShelterId = 8,
            Description = "Потрібні переноски",
            PaymentDetails = "Можна надіслати поштою"
        });
        await db.SaveChangesAsync();

        NeedsController controller = CreateController(scope, "shelter_admin", userId: 7);

        IActionResult result = await controller.Edit(11);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UpdateNeed()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        await CreateUserAsync(scope.UserManager, scope.RoleManager, "shelter9@petly.test", "pass123", "shelter_admin", userId: 9);
        db.Shelters.Add(new Shelter
        {
            AccountId = 9,
            ShelterName = "Shelter Nova",
            Location = "Kyiv",
            AdminName = "Olha"
        });
        db.ShelterNeeds.Add(new ShelterNeed
        {
            NeedId = 12,
            ShelterId = 9,
            Description = "Старий опис",
            PaymentDetails = "Старі реквізити"
        });
        await db.SaveChangesAsync();

        NeedsController controller = CreateController(scope, "shelter_admin", userId: 9);
        var model = new ShelterNeedFormViewModel
        {
            NeedId = 12,
            ShelterId = 9,
            Description = "  Новий опис потреби  ",
            PaymentDetails = "  Нові реквізити  "
        };

        IActionResult result = await controller.Edit(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Потребу оновлено.", controller.TempData["Success"]);

        var updated = await db.ShelterNeeds.SingleAsync(x => x.NeedId == 12);
        Assert.Equal("Новий опис потреби", updated.Description);
        Assert.Equal("Нові реквізити", updated.PaymentDetails);
    }

    [Fact]
    public async Task DeleteNeed()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        await CreateUserAsync(scope.UserManager, scope.RoleManager, "admin@petly.test", "pass123", "system_admin", userId: 1);
        db.Shelters.Add(new Shelter
        {
            AccountId = 3,
            ShelterName = "Shelter East",
            Location = "Kharkiv",
            AdminName = "Roman"
        });
        db.ShelterNeeds.Add(new ShelterNeed
        {
            NeedId = 13,
            ShelterId = 3,
            Description = "Потрібні миски",
            PaymentDetails = "Передача в притулок"
        });
        await db.SaveChangesAsync();

        NeedsController controller = CreateController(scope, "system_admin", userId: 1);

        IActionResult result = await controller.DeleteConfirmed(13);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Потребу видалено.", controller.TempData["Success"]);
        Assert.False(await db.ShelterNeeds.AnyAsync(x => x.NeedId == 13));
    }

    [Fact]
    public async Task ShelterAdminCanMarkOwnNeedFulfilled()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        await CreateUserAsync(scope.UserManager, scope.RoleManager, "shelter21@petly.test", "pass123", "shelter_admin", userId: 21);
        db.Shelters.Add(new Shelter
        {
            AccountId = 21,
            ShelterName = "Shelter Own",
            Location = "Kyiv",
            AdminName = "Nadia"
        });
        db.ShelterNeeds.Add(new ShelterNeed
        {
            NeedId = 21,
            ShelterId = 21,
            Description = "Потрібні ліки",
            PaymentDetails = "Передати в притулок"
        });
        await db.SaveChangesAsync();

        NeedsController controller = CreateController(scope, "shelter_admin", userId: 21);

        IActionResult result = await controller.MarkFulfilled(21);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Потребу позначено як виконану.", controller.TempData["Success"]);

        var updated = await db.ShelterNeeds.SingleAsync(x => x.NeedId == 21);
        Assert.True(updated.IsFulfilled);
        Assert.NotNull(updated.FulfilledAt);
    }

    [Fact]
    public async Task ShelterAdminCannotMarkAnotherShelterNeedFulfilled()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        await CreateUserAsync(scope.UserManager, scope.RoleManager, "shelter22@petly.test", "pass123", "shelter_admin", userId: 22);
        db.Shelters.Add(new Shelter
        {
            AccountId = 23,
            ShelterName = "Shelter Other",
            Location = "Lviv",
            AdminName = "Ihor"
        });
        db.ShelterNeeds.Add(new ShelterNeed
        {
            NeedId = 23,
            ShelterId = 23,
            Description = "Потрібні ковдри",
            PaymentDetails = "Переказ на рахунок"
        });
        await db.SaveChangesAsync();

        NeedsController controller = CreateController(scope, "shelter_admin", userId: 22);

        IActionResult result = await controller.MarkFulfilled(23);

        Assert.IsType<ForbidResult>(result);
        Assert.False((await db.ShelterNeeds.SingleAsync(x => x.NeedId == 23)).IsFulfilled);
    }

    [Fact]
    public async Task SystemAdminCanReturnFulfilledNeedToActive()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        await CreateUserAsync(scope.UserManager, scope.RoleManager, "admin24@petly.test", "pass123", "system_admin", userId: 24);
        db.Shelters.Add(new Shelter
        {
            AccountId = 25,
            ShelterName = "Shelter Active",
            Location = "Odesa",
            AdminName = "Olena"
        });
        db.ShelterNeeds.Add(new ShelterNeed
        {
            NeedId = 25,
            ShelterId = 25,
            Description = "Потрібні переноски",
            PaymentDetails = "Можна надіслати поштою",
            IsFulfilled = true,
            FulfilledAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        NeedsController controller = CreateController(scope, "system_admin", userId: 24);

        IActionResult result = await controller.MarkActive(25);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Потребу повернено в активні.", controller.TempData["Success"]);

        var updated = await db.ShelterNeeds.SingleAsync(x => x.NeedId == 25);
        Assert.False(updated.IsFulfilled);
        Assert.Null(updated.FulfilledAt);
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
        services.AddSingleton<NeedService>();
        services.AddTransient<NeedsController>();

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        return new TestIdentityScope(
            serviceProvider,
            serviceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
            serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>());
    }

    private static NeedsController CreateController(TestIdentityScope scope, string? role = null, int? userId = null)
    {
        var controller = scope.ServiceProvider.GetRequiredService<NeedsController>();
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
}
