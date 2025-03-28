using System.ComponentModel.DataAnnotations;

namespace GymManagement.Models
{
  public class Room
  {
    [Key]
    public int RoomId { get; set; }

    [StringLength(100)]
    public string RoomName { get; set; } = string.Empty;

    [Required]
    public int Capacity { get; set; }

    [Required]
    public bool IsAvailable { get; set; }

    [Required]
    public int BranchId { get; set; }  // Foreign Key

    public GymBranch? GymBranch { get; set; }
  }
}
