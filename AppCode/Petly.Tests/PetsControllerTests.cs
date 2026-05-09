using System.Reflection;
using System.Security.Claims;
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

public class PetsControllerTests
{
    [Fact]
    public async Task GetPets()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        var user = await CreateUserAsync(scope.UserManager, scope.RoleManager, "user@test.com", "pass123", "user");
        
        db.Pets.Add(new Pet { PetId = 1, PetName = "Barsik", ShelterId = 10 });
        db.Pets.Add(new Pet { PetId = 2, PetName = "Murka", ShelterId = 20 });
        await db.SaveChangesAsync();

        var controller = CreateController(scope, "user", user.Id);

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<List<Pet>>(view.Model);
        Assert.Equal(2, model.Count);


        //Assert.Equal("user", controller.ViewBag.Role);
    }

    [Fact]
    public async Task GetOwnPets()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        var admin = await CreateUserAsync(scope.UserManager, scope.RoleManager, "shelter@test.com", "pass123", "shelter_admin");
        
        db.Pets.Add(new Pet { PetId = 1, PetName = "My Pet", ShelterId = admin.Id });
        db.Pets.Add(new Pet { PetId = 2, PetName = "Other Pet", ShelterId = admin.Id + 999 });
        await db.SaveChangesAsync();

        var controller = CreateController(scope, "shelter_admin", admin.Id);

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<List<Pet>>(view.Model);
        Assert.Single(model);
        Assert.Equal("My Pet", model[0].PetName);
    }

    [Fact]
    public async Task GetPetDetails()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        db.Pets.Add(new Pet { PetId = 1, PetName = "Buddy" });
        await db.SaveChangesAsync();

        var controller = CreateController(scope, "user", 100);

        var result = await controller.Details(1);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Pet>(view.Model);
        Assert.Equal("Buddy", model.PetName);
    }

    [Fact]
    public async Task Adopt_RedirectsToAdoptionCreate()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        var controller = CreateController(scope, "user", 100);

        var result = controller.Adopt(13);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Create", redirect.ActionName);
        Assert.Equal("Adoption", redirect.ControllerName);
        Assert.Equal(13, redirect.RouteValues!["petId"]);
    }

    [Fact]
    public async Task GetPetDetails_NotFound()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        var controller = CreateController(scope, "user", 100);

        var result = await controller.Details(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CreatePet()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        var admin = await CreateUserAsync(scope.UserManager, scope.RoleManager, "shelter-create@test.com", "pass123", "shelter_admin");
        db.Shelters.Add(new Shelter { AccountId = admin.Id, ShelterName = "Shelter One" });
        await db.SaveChangesAsync();

        var controller = CreateController(scope, "shelter_admin", admin.Id);

        var result = await controller.Create();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Shelter One", controller.ViewBag.ShelterName);
    }

    [Fact]
    public async Task CreatePost()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);

        var admin = await CreateUserAsync(
            scope.UserManager,
            scope.RoleManager,
            "shelter@test.com",
            "pass123",
            "shelter_admin"
        );

        db.Shelters.Add(new Shelter 
        { 
            AccountId = admin.Id, 
            ShelterName = "My Shelter" 
        });

        await db.SaveChangesAsync();

        var controller = CreateController(scope, "shelter_admin", admin.Id);

        var newPet = new Pet
        {
            PetName = "New Dog",
            Type = "Собака",
            Breed = "Лабрадор",
            Gender = "Чоловіча",
            Age = 3,
            Size = "Середній",
            Vaccinated = true,
            Sterilized = false,
            Status = "Доступний",
            PhotoUrl = "/images/pets/new-dog.jpg",
            Description = "Дружній пес"
        };

        var result = await controller.Create(newPet);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Тварину додано", controller.TempData["Success"]);

        var savedPet = await db.Pets.FirstOrDefaultAsync(p => p.PetName == "New Dog");
        Assert.NotNull(savedPet);
        Assert.Equal(admin.Id, savedPet.ShelterId);
        Assert.Equal("Лабрадор", savedPet.Breed);
        Assert.Equal("/images/pets/new-dog.jpg", savedPet.PhotoUrl);
        Assert.Equal("Дружній пес", savedPet.Description);
        Assert.Equal("Доступний", savedPet.Status);
    }

    [Fact]
    public async Task CreatePost_WithoutShelterRecord_ReturnsForbid()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);

        var admin = await CreateUserAsync(
            scope.UserManager,
            scope.RoleManager,
            "noshelter@test.com",
            "pass123",
            "shelter_admin"
        );

        var controller = CreateController(scope, "shelter_admin", admin.Id);

        var result = await controller.Create(new Pet { PetName = "No Shelter Pet" });

        Assert.IsType<ForbidResult>(result);
        Assert.False(await db.Pets.AnyAsync());
    }
    [Fact]
    public async Task EditPet()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        var admin = await CreateUserAsync(scope.UserManager, scope.RoleManager, "shelter@test.com", "pass123", "shelter_admin");
        
        var pet = new Pet { PetId = 1, PetName = "Lucky", ShelterId = admin.Id };
        db.Pets.Add(pet);
        await db.SaveChangesAsync();

        var controller = CreateController(scope, "shelter_admin", admin.Id);

        var result = await controller.Edit(1);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Pet>(view.Model);
        Assert.Equal("Lucky", model.PetName);
    }

    [Fact]
    public async Task EditPost()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        var admin = await CreateUserAsync(scope.UserManager, scope.RoleManager, "shelter@test.com", "pass123", "shelter_admin");
        
        var existingPet = new Pet { PetId = 1, PetName = "Old Name", ShelterId = admin.Id };
        db.Pets.Add(existingPet);
        await db.SaveChangesAsync();

        var controller = CreateController(scope, "shelter_admin", admin.Id);
        var updateModel = new Pet { PetId = 1, PetName = "Updated Name" };

       var result = await controller.Edit(updateModel);

       var redirect = Assert.IsType<RedirectToActionResult>(result);
       Assert.Equal("Index", redirect.ActionName);

       var updated = await db.Pets.FirstAsync(p => p.PetId == 1);
       Assert.Equal("Updated Name", updated.PetName);
    }

    [Fact]
    public async Task DeletePet()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        var admin = await CreateUserAsync(scope.UserManager, scope.RoleManager, "admin@test.com", "pass", "shelter_admin");
        db.Pets.Add(new Pet { PetId = 1, PetName = "Dog", ShelterId = admin.Id });
        await db.SaveChangesAsync();

        var controller = CreateController(scope, "shelter_admin", admin.Id);

        var result = await controller.Delete(1);

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<Pet>(view.Model);
    }

    [Fact]
    public async Task DeletePost()
    {
        await using var db = CreateDbContext();
        TestIdentityScope scope = CreateIdentityScope(db);
        var admin = await CreateUserAsync(scope.UserManager, scope.RoleManager, "shelter@test.com", "pass123", "shelter_admin");
        
        var pet = new Pet { PetId = 5, PetName = "To Delete", ShelterId = admin.Id };
        db.Pets.Add(pet);
        await db.SaveChangesAsync();

        var controller = CreateController(scope, "shelter_admin", admin.Id);

        var result = await controller.DeleteConfirmed(5);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.False(await db.Pets.AnyAsync(p => p.PetId == 5));
    }

    [Fact]
    public void Controller_RequiresAuthorization()
    {
        var attr = typeof(PetsController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(attr);
    }

    [Fact]
    public void ShelterAdminActions_RequireSpecificRole()
    {
        AssertActionRole(nameof(PetsController.Create), "shelter_admin");
        AssertActionRole(nameof(PetsController.Edit), "shelter_admin", typeof(int));
        AssertActionRole(nameof(PetsController.Delete), "shelter_admin", typeof(int));
    }

    private static void AssertActionRole(string actionName, string role, params Type[] parameterTypes)
    {
        MethodInfo method = typeof(PetsController).GetMethod(actionName, parameterTypes)!;
        var authorize = method.GetCustomAttributes<AuthorizeAttribute>(inherit: true).FirstOrDefault();
        Assert.NotNull(authorize);
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
        services.AddSingleton<PetService>();
        services.AddTransient<PetsController>();
        services.AddSingleton<NotificationService>();

        var serviceProvider = services.BuildServiceProvider();
        return new TestIdentityScope(
            serviceProvider,
            serviceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
            serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>());
    }

    private static PetsController CreateController(TestIdentityScope scope, string? role = null, int? userId = null)
    {
        var controller = scope.ServiceProvider.GetRequiredService<PetsController>();
        var httpContext = new DefaultHttpContext();

        if (userId.HasValue)
        {
           var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.Value.ToString()) };
           if (!string.IsNullOrEmpty(role)) claims.Add(new Claim(ClaimTypes.Role, role));
           httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        }

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, new TestTempDataProvider());
        return controller;
    }

    private static async Task<ApplicationUser> CreateUserAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<int>> roleManager, string email, string password, string role)
    {
        if (!await roleManager.RoleExistsAsync(role)) await roleManager.CreateAsync(new IdentityRole<int>(role));
        var user = new ApplicationUser { UserName = email, Email = email, RegistrationDate = DateTime.UtcNow, Status = "Активний" };
        await userManager.CreateAsync(user, password);
        await userManager.AddToRoleAsync(user, role);
        return user;
    }

    private sealed record TestIdentityScope(ServiceProvider ServiceProvider, UserManager<ApplicationUser> UserManager, RoleManager<IdentityRole<int>> RoleManager);

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
