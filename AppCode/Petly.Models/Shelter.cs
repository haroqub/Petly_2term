using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Petly.Models;

[Table("shelter")]
public class Shelter
{
    [Key]
    [Column("accountId")]
    public int AccountId { get; set; }

    [Column("shelterName")]
    public string? ShelterName { get; set; }

    [Column("location")]
    public string? Location { get; set; }

    [Column("adminName")]
    public string? AdminName { get; set; }

    public List<ShelterNeed> Needs { get; set; } = new();
}
