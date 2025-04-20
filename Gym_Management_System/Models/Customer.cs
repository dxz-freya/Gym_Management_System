using System.ComponentModel.DataAnnotations;

namespace GymManagement.Models
{
    public class Customer : User
    {
        [Required]
        public MembershipType MembershipType { get; set; }

        [Required]
        public MembershipStatus MembershipStatus { get; set; }

        public DateTime? MembershipExpiry { get; set; }
        public DateTime SubscriptionDate { get; set; }

        public decimal WalletBalance { get; set; } = 0m; // 👈 用于余额显示与扣款

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();

        public int GymBranchId { get; set; }
        public GymBranch? GymBranch { get; set; }
    }

    public enum MembershipType
    {
        Monthly,
        Quarterly,
        Yearly
    }

    public enum MembershipStatus
    {
        Active,
        Expired,
        Suspended
    }
}
