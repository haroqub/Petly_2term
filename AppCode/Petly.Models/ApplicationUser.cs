using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Petly.Models;

public class ApplicationUser : IdentityUser<int>
{

    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(100)]
    public string? Surname { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; } = "Активний";

    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
    public string? ImagePath { get; set; }

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}