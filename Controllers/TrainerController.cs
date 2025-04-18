// TrainerController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GymManagement.Data;
using GymManagement.Models;
using GymManagement.ViewModels;
using System.Security.Claims;

namespace GymManagement.Controllers
{
  [Authorize(Roles = "Trainer")]
  public class TrainerController : Controller
  {
    private readonly AppDbContext _dbContext;

    public TrainerController(AppDbContext dbContext)
    {
      _dbContext = dbContext;
    }

    // 🔹 教练仪表盘
    public IActionResult Dashboard()
    {
      string? trainerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
      if (string.IsNullOrEmpty(trainerId))
      {
        return BadRequest("Invalid trainer identifier.");
      }

      var trainer = _dbContext.Trainers
          .Include(t => t.GymClasses)
          .Include(t => t.Sessions)
          .FirstOrDefault(t => t.Id == trainerId);

      if (trainer == null)
      {
        return NotFound("Trainer not found.");
      }

      return View(trainer);

    }

    // 🔹 查看教练安排的课程
    public IActionResult ViewSessions()
    {
      string? trainerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
      if (string.IsNullOrEmpty(trainerId))
      {
        return BadRequest("Invalid trainer identifier.");
      }
      var sessions = _dbContext.Sessions
          .Include(s => s.GymClass)
          .Include(s => s.Room)
          .Where(s => s.TrainerId == trainerId)
          .OrderBy(s => s.SessionDateTime)
          .ToList();

      return View(sessions);
    }

    // 🔹 查看课程详情
    public IActionResult SessionDetails(int sessionId)
    {
      string? trainerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
      if (string.IsNullOrEmpty(trainerId))
      {
        return BadRequest("Invalid trainer identifier.");
      }


      var session = _dbContext.Sessions
          .Include(s => s.GymClass)
          .Include(s => s.Room)
          .Include(s => s.Bookings)
              .ThenInclude(b => b.Customer)
          .FirstOrDefault(s => s.SessionId == sessionId && s.TrainerId == trainerId);

      if (session == null)
      {
        return NotFound("Session not found.");
      }

      return View(session);
    }

    // 🔹 标记课程考勤
    [HttpPost]
    public IActionResult MarkAttendance(int bookingId)
    {
      var booking = _dbContext.Bookings.FirstOrDefault(b => b.BookingId == bookingId);

      if (booking == null)
      {
        return NotFound("Booking not found.");
      }

      booking.Status = BookingStatus.CheckedIn;
      booking.CheckInTime = DateTime.UtcNow;
      _dbContext.SaveChanges();

      return RedirectToAction("SessionDetails", new { sessionId = booking.SessionId });
    }

    // 🔹 修改个人信息 - 显示表单
    public IActionResult EditProfile()
    {
      string trainerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

      var trainer = _dbContext.Trainers.FirstOrDefault(t => t.Id == trainerId);
      if (trainer == null)
      {
        return NotFound("Trainer not found.");
      }

      var model = new EditTrainerProfileViewModel
      {
        TrainerId = trainer.Id,
        Name = trainer.Name,
        Email = trainer.Email,
        Specialization = trainer.Specialization,
        ExperienceStarted = trainer.ExperienceStarted
      };

      return View(model);
    }

    // 🔹 修改个人信息 - 提交表单
    [HttpPost]
    public IActionResult EditProfile(EditTrainerProfileViewModel model)
    {
      if (!ModelState.IsValid)
      {
        return View(model);
      }

      string trainerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
      var trainer = _dbContext.Trainers.FirstOrDefault(t => t.Id == trainerId);

      if (trainer == null)
      {
        return NotFound("Trainer not found.");
      }

      trainer.Name = model.Name;
      trainer.Email = model.Email;
      trainer.Specialization = string.IsNullOrWhiteSpace(model.Specialization)
          ? "Unknown"
          : model.Specialization;
      trainer.ExperienceStarted = model.ExperienceStarted ?? DateTime.Now;

      _dbContext.SaveChanges();

      return RedirectToAction("Dashboard");
    }
  }
}
