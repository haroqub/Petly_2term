using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; 

namespace Petly.Models
{
    public class SuccessStory
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Оберіть тваринку")]
        [Display(Name = "Тваринка")]
        public int PetId { get; set; }

        [ForeignKey("PetId")]
        public Pet Pet { get; set; } 

        [Required(ErrorMessage = "Додайте заголовок історії")]
        [Display(Name = "Заголовок")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Напишіть текст історії")]
        [Display(Name = "Історія")]
        public string StoryText { get; set; }

        [Display(Name = "Посилання на фото")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Дата публікації")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}