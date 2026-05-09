using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Petly.Models;

[Table("adoptionapplication")]
public class AdoptionApplication
{
    [Key]
    [Column("adoptId")]
    public int AdoptId { get; set; }

    [Column("userId")]
    public int UserId { get; set; }

    [Column("petId")]
    public int PetId { get; set; }

    [Column("status")]
    public string Status { get; set; } = "Очікує";

    [Column("submissionDate")]
    public DateTime SubmissionDate { get; set; } = DateTime.Now;

    [Column("applicantName")]
    [MaxLength(100)]
    public string? ApplicantName { get; set; }

    [Column("applicantSurname")]
    [MaxLength(100)]
    public string? ApplicantSurname { get; set; }

    [Column("applicantAge")]
    public int? ApplicantAge { get; set; }

    [Column("contactInfo")]
    [MaxLength(255)]
    public string? ContactInfo { get; set; }

    [ForeignKey("PetId")]
    public Pet? Pet { get; set; }

    [ForeignKey("UserId")]
    public ApplicationUser? UserProfile { get; set; }
}
