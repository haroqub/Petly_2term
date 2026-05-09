using System.ComponentModel.DataAnnotations;

namespace Petly.Models
{
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Введіть ім'я")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введіть прізвище")]
        public string Surname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введіть email")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? ExistingImagePath { get; set; }

        public string? NewImageFileName { get; set; }

        public bool ShowNameFields { get; set; } = true;
    }
}
