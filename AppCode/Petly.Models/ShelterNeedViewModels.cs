using System.ComponentModel.DataAnnotations;

namespace Petly.Models;

public class ShelterNeedGroupViewModel
{
    public int ShelterId { get; set; }

    public string ShelterName { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public bool CanManage { get; set; }

    public int ActiveNeedsCount => Needs.Count(need => !need.IsFulfilled);

    public int FulfilledNeedsCount => Needs.Count(need => need.IsFulfilled);

    public List<ShelterNeedListItemViewModel> Needs { get; set; } = new();
}

public class ShelterNeedListItemViewModel
{
    public int NeedId { get; set; }

    public string Description { get; set; } = string.Empty;

    public string PaymentDetails { get; set; } = string.Empty;

    public bool IsFulfilled { get; set; }

    public DateTime? FulfilledAt { get; set; }
}

public class ShelterNeedFormViewModel
{
    public int NeedId { get; set; }

    public int ShelterId { get; set; }

    [Required(ErrorMessage = "Додайте опис потреби")]
    [Display(Name = "Що потрібно")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Додайте спосіб допомоги або оплати")]
    [Display(Name = "Як допомогти")]
    [StringLength(255, ErrorMessage = "До 255 символів")]
    public string PaymentDetails { get; set; } = string.Empty;
}
