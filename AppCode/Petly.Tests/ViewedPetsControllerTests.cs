using Xunit;
using Microsoft.EntityFrameworkCore;
using Petly.DataAccess.Data;
using Petly.Models;
using System.Threading.Tasks;
using System.Linq;

namespace Petly.Tests;

public class ViewedPetsTests
{
   private ApplicationDbContext GetDbContext()
{
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // 👈 ОЦЕ ГОЛОВНЕ
        .Options;

    return new ApplicationDbContext(options);
}

    [Fact]
    public async Task AddViewedPet_ShouldAddRecord()
    {
        var context = GetDbContext();

        var viewedPet = new ViewedPet
        {
            UserId = 1,
            PetId = 10,
            ViewedAt = System.DateTime.Now
        };

        context.ViewedPets.Add(viewedPet);
        await context.SaveChangesAsync();

        var result = context.ViewedPets.FirstOrDefault();

        Assert.NotNull(result);
        Assert.Equal(1, result.UserId);
        Assert.Equal(10, result.PetId);
    }

    [Fact]
    public async Task ViewedPets_ShouldReturnOrderedByDate()
    {
        var context = GetDbContext();

        context.ViewedPets.Add(new ViewedPet
        {
            UserId = 1,
            PetId = 1,
            ViewedAt = System.DateTime.Now.AddMinutes(-10)
        });

        context.ViewedPets.Add(new ViewedPet
        {
            UserId = 1,
            PetId = 2,
            ViewedAt = System.DateTime.Now
        });

        await context.SaveChangesAsync();

        var result = context.ViewedPets
            .OrderByDescending(v => v.ViewedAt)
            .ToList();

        Assert.Equal(2, result.First().PetId);
    }
}