using System.ComponentModel.DataAnnotations;

namespace Petly.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Введіть email")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть пароль")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Введіть ім'я")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть прізвище")]
    public string Surname { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть email")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть пароль")]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "Мінімум 6 символів")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Підтвердіть пароль")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Паролі не збігаються")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string Role { get; set; } = "user"; 
}

public class UserEditViewModel
{
    public int AccountId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Surname { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = "user";

    public string Status { get; set; } = "Активний";

    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Введіть email")]
    [EmailAddress(ErrorMessage = "Введіть коректну електронну пошту")]
    public string Email { get; set; } = string.Empty;
}

public class VerifyResetCodeViewModel
{
    [Required(ErrorMessage = "Введіть email")]
    [EmailAddress(ErrorMessage = "Введіть коректну електронну пошту")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть код підтвердження")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Код має містити 6 цифр")]
    public string Code { get; set; } = string.Empty;

    public int CodeLifetimeMinutes { get; set; } = 10;
}

public class ResetPasswordViewModel
{
    [Required(ErrorMessage = "Введіть email")]
    [EmailAddress(ErrorMessage = "Введіть коректну електронну пошту")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть код із листа")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть новий пароль")]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "Мінімум 6 символів")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Підтвердьте новий пароль")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Паролі не збігаються")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class AdminUserViewModel
{
    public int AccountId { get; set; }
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "user";
    public string Status { get; set; } = "Активний";
    public DateTime RegistrationDate { get; set; }
    public string? ShelterName { get; set; }
}
