namespace GymManagement.ViewModels
{
  public class GymClassViewModel
  {
    public required int GymClassId { get; set; }
    public required string ClassName { get; set; }
    public required DateTime AvailableTime { get; set; }
    public required int Duration { get; set; }
    public required string Description { get; set; }
  }
}
