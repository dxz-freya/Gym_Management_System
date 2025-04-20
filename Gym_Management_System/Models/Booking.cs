using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymManagement.Models
{
  public class Booking
  {
    [Key]
    public int BookingId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty; // 添加 UserId 字段

    [ForeignKey("UserId")]
    public User User { get; set; } // 外键与 User 关联

    [Required]
    public DateTime BookingDate { get; set; }

    [Required]
    public BookingStatus Status { get; set; }

    // Foreign key to Customer (IdentityUser-based)
    [Required]
    public string CustomerId { get; set; } = string.Empty;

    [ForeignKey("CustomerId")]
    public Customer Customer { get; set; } = null!;

    // Foreign key to Session
    [Required]
    public int SessionId { get; set; }

    [ForeignKey("SessionId")]
    public Session Session { get; set; } = null!;

    // Optional check-in time
    public DateTime? CheckInTime { get; set; }

    // Optional foreign key to Receptionist
    public string? ReceptionistId { get; set; }

    [ForeignKey("ReceptionistId")]
    public Receptionist? Receptionist { get; set; }
  }

  public enum BookingStatus
  {
    Pending,
    Confirmed,
    Canceled,
    CheckedIn
  }
}
