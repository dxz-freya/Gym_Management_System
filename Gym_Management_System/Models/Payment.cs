using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagement.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Price { get; set; }

        [Required]
        public string CustomerId { get; set; } = string.Empty;

        [ForeignKey("CustomerId")]
        public Customer Customer { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = string.Empty; // e.g. Wallet, Credit Card

        [Required]
        public DateTime PaymentDate { get; set; }

        [Required]
        public PaymentType Type { get; set; }  // Recharge or Membership
    }

    public enum PaymentType
    {
        Recharge,
        Membership
    }
}
