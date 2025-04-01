using System.ComponentModel.DataAnnotations;

namespace GymManagement.Models
{
  public class Customer : User
  {
    [Required]
    [StringLength(50)]
    public string MembershipType { get; set; } = string.Empty;

    [Required]
    public DateTime SubscriptionDate { get; set; }

    [Required]
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    [Required]
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    // ✅ 加上这两行
    public int GymBranchId { get; set; }
    public GymBranch? GymBranch { get; set; }

    // EF Core requires a parameterless constructor
    public Customer()
    {
      SubscriptionDate = DateTime.UtcNow;  // Set default if not specified
    }
  }
}
