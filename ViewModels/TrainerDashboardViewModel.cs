public class TrainerDashboardViewModel
{
  public required string Name { get; set; }
  public required string Specialization { get; set; }
  public required int TotalSessions { get; set; }
  public required int TotalStudents { get; set; }
  public required List<SessionViewModel> UpcomingSessions { get; set; }
}
