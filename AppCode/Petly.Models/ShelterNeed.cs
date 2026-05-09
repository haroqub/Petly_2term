using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Petly.Models;

[Table("shelterneed")]
public class ShelterNeed
{
    [Key]
    [Column("needId")]
    public int NeedId { get; set; }

    [Column("shelterId")]
    public int ShelterId { get; set; }

    [Required(ErrorMessage = "Додайте опис потреби")]
    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Додайте спосіб допомоги або оплати")]
    [Column("paymentDetails")]
    [StringLength(255)]
    public string PaymentDetails { get; set; } = string.Empty;

    [Column("isFulfilled")]
    public bool IsFulfilled { get; set; }

    [Column("fulfilledAt")]
    public DateTime? FulfilledAt { get; set; }

    [ForeignKey(nameof(ShelterId))]
    public Shelter? Shelter { get; set; }
}
