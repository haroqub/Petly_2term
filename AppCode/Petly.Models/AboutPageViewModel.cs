using System.ComponentModel.DataAnnotations;

namespace Petly.Models;

public class AboutPageViewModel
{
    [Required(ErrorMessage = "Додайте короткий підпис")]
    [Display(Name = "Короткий підпис")]
    [StringLength(120, ErrorMessage = "До 120 символів")]
    public string Eyebrow { get; set; } = "Petly поруч із тими, хто шукає дім";

    [Required(ErrorMessage = "Додайте заголовок")]
    [Display(Name = "Головний заголовок")]
    [StringLength(80, ErrorMessage = "До 80 символів")]
    public string HeroTitle { get; set; } = "Про нас";

    [Required(ErrorMessage = "Додайте вступний текст")]
    [Display(Name = "Вступний текст")]
    [StringLength(500, ErrorMessage = "До 500 символів")]
    public string HeroText { get; set; } = "Ми будуємо дружній простір, де притулки, волонтери та майбутні власники можуть зустрітися швидше, зрозуміліше і без зайвого хаосу.";

    [Required(ErrorMessage = "Додайте заголовок блоку")]
    [Display(Name = "Заголовок основного блоку")]
    [StringLength(100, ErrorMessage = "До 100 символів")]
    public string ContentTitle { get; set; } = "Що робить Petly";

    [Required(ErrorMessage = "Додайте основний текст")]
    [Display(Name = "Основний текст")]
    [StringLength(2000, ErrorMessage = "До 2000 символів")]
    public string ContentText { get; set; } = "Ми - команда Petly, і ми створили цей застосунок, щоб допомогти тваринам знайти свій дім, а людям - вірного друга.\nНаша мета - зробити процес адопції простим, прозорим і зручним для кожного.\nМи об'єднуємо притулки, користувачів та адміністраторів в одній системі, де легко переглядати анкети тварин і подавати заявки.\nДля нас важливо, щоб кожен улюбленець отримав шанс на турботливу родину.\nМи постійно вдосконалюємо Petly, щоб зробити його надійним інструментом підтримки для притулків і майбутніх власників.";

    [Required(ErrorMessage = "Додайте заголовок контактів")]
    [Display(Name = "Заголовок контактів")]
    [StringLength(80, ErrorMessage = "До 80 символів")]
    public string ContactsTitle { get; set; } = "Наші контакти";

    [Required(ErrorMessage = "Додайте контакти")]
    [Display(Name = "Контакти")]
    [StringLength(600, ErrorMessage = "До 600 символів")]
    public string Contacts { get; set; } = "+3800000000000\npetly@gmail.com\nм.Львів\n9:00-20:00";

    public bool CanEdit { get; set; }
}
