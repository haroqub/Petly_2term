using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Petly.Models;

public class Notification
{
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = null!;

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; } = null!;
}