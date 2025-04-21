namespace GymManagement.ViewModels
{
  public class EditTrainerProfileViewModel
  {
    // User.Id 是 string 类型（因为继承自 IdentityUser）
    public string TrainerId { get; set; } = "";

    // Required name field shown in profile
    public required string Name { get; set; }

    // Required email field
    public required string? Email { get; set; }

    // Optional specialization
    public string? Specialization { get; set; }

    // Optional date when experience started
    public DateTime? ExperienceStarted { get; set; }
  }
}
