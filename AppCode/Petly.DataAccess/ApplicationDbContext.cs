using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Petly.Models;

namespace Petly.DataAccess.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Pet> Pets { get; set; }
    public DbSet<AdoptionApplication> AdoptionApplications { get; set; }
    public DbSet<Shelter> Shelters { get; set; }
    public DbSet<ShelterNeed> ShelterNeeds { get; set; }

 
    public DbSet<ViewedPet> ViewedPets { get; set; } 

    public DbSet<Favorite> Favorites { get; set; }
    public DbSet<AnalyticsAccessLog> AnalyticsAccessLogs { get; set; }   

    public DbSet<SuccessStory> SuccessStories { get; set; }


    public DbSet<Notification> Notifications { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ShelterNeed>()
            .HasOne(sn => sn.Shelter)
            .WithMany(shelter => shelter.Needs)
            .HasForeignKey(sn => sn.ShelterId);
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
