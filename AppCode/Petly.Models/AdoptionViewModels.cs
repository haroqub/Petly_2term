using System.ComponentModel.DataAnnotations;

namespace Petly.Models;

public class AdoptionRequestViewModel
{
    public int PetId { get; set; }

    public string PetName { get; set; } = string.Empty;

    public string? PetType { get; set; }

    public int? PetAge { get; set; }

    public string PetAgeText { get; set; } = string.Empty;

    public string? PetPhotoUrl { get; set; }

    [Required(ErrorMessage = "Введіть ім'я")]
    public string ApplicantName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть прізвище")]
    public string ApplicantSurname { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть ваш вік")]
    [Range(18, 120, ErrorMessage = "Подати заявку можуть лише повнолітні користувачі 18+")]
    public int? ApplicantAge { get; set; }

    [Required(ErrorMessage = "Вкажіть номер телефону або інші контакти")]
    [StringLength(255, ErrorMessage = "Контактні дані мають містити до 255 символів")]
    public string ContactInfo { get; set; } = string.Empty;
}
