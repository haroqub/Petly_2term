using System;

namespace Petly.Models
{
    public class ViewedPet
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public ApplicationUser User { get; set; }

        public int PetId { get; set; }
        public Pet Pet { get; set; }

        public DateTime ViewedAt { get; set; }
    }
}