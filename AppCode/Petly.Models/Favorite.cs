using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Petly.Models;

[Table("favorite")]
public class Favorite
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }
    public int PetId { get; set; }

    [ForeignKey("PetId")]
    public Pet Pet { get; set; }
}