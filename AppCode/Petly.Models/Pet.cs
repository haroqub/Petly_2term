using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Petly.Models;

[Table("pet")]
public class Pet
{
    [Key]
    [Column("petId")]
    public int PetId { get; set; }

    [Column("shelterId")]
    public int ShelterId { get; set; }

    [Required(ErrorMessage = "Введіть ім'я тварини")]
    [Display(Name = "Ім'я тварини")]
    [Column("petName")]
    public string PetName { get; set; } = string.Empty;

    [Display(Name = "Тип тварини")]
    [Column("type")]
    public string? Type { get; set; }

    [Display(Name = "Порода")]
    [Column("breed")]
    public string? Breed { get; set; }

    [Display(Name = "Стать")]
    [Column("gender")]
    public string? Gender { get; set; }

    [Display(Name = "Вік")]
    [Column("age")]
    public int? Age { get; set; }
    
    [NotMapped]
    public string AgeText => Age switch
    {
        1 => "рік",
        2 or 3 or 4 => "роки",
        _ => "років"
    };

    [Display(Name = "Розмір")]
    [Column("size")]
    public string? Size { get; set; }

    [Display(Name = "Вакцинована")]
    [Column("vaccinated")]
    public bool Vaccinated { get; set; }

    [Display(Name = "Стерилізована")]
    [Column("sterilized")]
    public bool Sterilized { get; set; }

    [Display(Name = "Статус")]
    [Column("status")]
    public string Status { get; set; } = "Available";

    [Display(Name = "Посилання або шлях до фото")]
    [Column("photoUrl")]
    public string? PhotoUrl { get; set; }

    [Display(Name = "Опис")]
    [Column("description")]
    public string? Description { get; set; }

    [Column("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
