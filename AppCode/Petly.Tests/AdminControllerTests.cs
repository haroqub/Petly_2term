using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Petly.Business.Services;
using Petly.Controllers;
using Petly.DataAccess.Data;
using Petly.Models;
using Xunit;

namespace Petly.Tests;

public class AdminControllerTests
{
    [Fact]
    public void AnalyticsRequiresSystemAdminRole()
    {
        AuthorizeAttribute? attribute = typeof(AdminController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal("system_admin", attribute.Roles);
    }

    [Fact]
    public async Task AnalyticsReturnsDashboardModel()
    {
        await using ApplicationDbContext db = CreateDbContext();

        db.Roles.AddRange(
            new IdentityRole<int> { Id = 1, Name = "user", NormalizedName = "USER" },
            new IdentityRole<int> { Id = 2, Name = "shelter_admin", NormalizedName = "SHELTER_ADMIN" });

        db.Users.AddRange(
            new ApplicationUser
            {
                Id = 10,
                UserName = "user1@petly.com",
                Email = "user1@petly.com",
                RegistrationDate = DateTime.Today.AddDays(-2),
                Status = "Активний",
                Name = "Ira"
            },
            new ApplicationUser
            {
                Id = 20,
                UserName = "shelter@petly.com",
                Email = "shelter@petly.com",
                RegistrationDate = DateTime.Today.AddDays(-6),
                Status = "Активний",
                Name = "Oleh"
            });

        db.UserRoles.AddRange(
            new IdentityUserRole<int> { UserId = 10, RoleId = 1 },
            new IdentityUserRole<int> { UserId = 20, RoleId = 2 });

        db.Shelters.Add(new Shelter
        {
            AccountId = 20,
            ShelterName = "Pet House",
            AdminName = "Oleh",
            Location = "Lviv"
        });

        db.Pets.AddRange(
            new Pet
            {
                PetId = 1,
                ShelterId = 20,
                PetName = "Luna",
                Type = "Кіт",
                Status = "Доступний",
                CreatedAt = DateTime.Today.AddDays(-1)
            },
            new Pet
            {
                PetId = 2,
                ShelterId = 20,
                PetName = "Max",
                Type = "Собака",
                Status = "Прилаштований",
                CreatedAt = DateTime.Today.AddDays(-4)
            });

        db.AdoptionApplications.AddRange(
            new AdoptionApplication
            {
                AdoptId = 1,
                PetId = 1,
                UserId = 10,
                Status = AdoptionStatuses.Pending,
                SubmissionDate = DateTime.Today.AddDays(-1)
            },
            new AdoptionApplication
            {
                AdoptId = 2,
                PetId = 2,
                UserId = 10,
                Status = AdoptionStatuses.Approved,
                SubmissionDate = DateTime.Today.AddDays(-3)
            });

        await db.SaveChangesAsync();

        DashboardService service = new(db);

        // --- СТВОРЕННЯ ЗАГЛУШКИ ДЛЯ USERMANAGER ---
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        var userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);

        // Передаємо обидва сервіси у контролер
        AdminController controller = new(service, userManagerMock.Object);

        IActionResult result = await controller.Analytics(7);

        ViewResult view = Assert.IsType<ViewResult>(result);
        AdminDashboardViewModel model = Assert.IsType<AdminDashboardViewModel>(view.Model);
        Assert.Equal(7, model.SelectedPeriodDays);
        Assert.Equal(2, model.TotalUsers);
        Assert.Equal(1, model.TotalShelterAdmins);
        Assert.Equal(2, model.TotalPets);
        Assert.Equal(1, model.AvailablePets);
        Assert.Equal(1, model.AdoptedPets);
        Assert.Equal(2, model.TotalApplications);
        Assert.Equal(1, model.PendingApplications);
        Assert.Equal(1, model.ApprovedApplications);
        Assert.Equal(50, model.AdoptionSuccessRate);
        Assert.NotEmpty(model.ApplicationsByDay);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}