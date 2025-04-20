using System.ComponentModel.DataAnnotations;

namespace GymManagement.ViewModels
{
    public class NewBookingViewModel
    {
        [Required]
        public string CustomerId { get; set; } = "";

        [Required]
        public int SessionId { get; set; }
    }
}
