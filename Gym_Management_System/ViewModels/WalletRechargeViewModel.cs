using System.ComponentModel.DataAnnotations;

namespace GymManagement.ViewModels
{
    public class WalletRechargeViewModel
    {
        public decimal CurrentBalance { get; set; }

        [Required]
        [Range(1, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = string.Empty;
    }
}
