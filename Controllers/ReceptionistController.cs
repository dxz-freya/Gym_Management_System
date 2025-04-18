using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GymManagement.Data;
using GymManagement.Models;
using System.Security.Claims;

namespace GymManagement.Controllers
{
  [Authorize(Roles = "Receptionist")]
  public class ReceptionistController : Controller
  {
    private readonly AppDbContext _db;

    public ReceptionistController(AppDbContext db)
    {
      _db = db;
    }

    // 获取当前 Receptionist 实体
    private Receptionist? GetCurrentReceptionist()
    {
      string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
      if (string.IsNullOrEmpty(userId)) return null;

      return _db.Receptionists.FirstOrDefault(r => r.Id == userId);
    }

    // 🔹 Search Member Info by Receptionist's Branch
    public IActionResult SearchMembersByBranch()
    {
      var receptionist = GetCurrentReceptionist();
      if (receptionist == null) return NotFound("Receptionist not found.");

      var members = _db.Customers
        .Where(c => c.GymBranchId == receptionist.BranchId)
        .ToList();

      return View(members);
    }

    // 🔹 Search Booking Info by Receptionist's Branch
    public IActionResult GetBookingsByBranch()
    {
      var receptionist = GetCurrentReceptionist();
      if (receptionist == null) return NotFound("Receptionist not found.");

      var bookings = _db.Bookings
        .Include(b => b.Customer)
        .Include(b => b.Session)
          .ThenInclude(s => s.GymClass)
        .Include(b => b.Session.Room)
        .Where(b => b.Session.Room.BranchId == receptionist.BranchId)
        .ToList();

      return View(bookings);
    }

    // 🔹 Check In Member
    [HttpPost]
    public IActionResult CheckInMember(int bookingId)
    {
      var booking = _db.Bookings.FirstOrDefault(b => b.BookingId == bookingId);
      if (booking == null) return NotFound("Booking not found.");

      booking.Status = BookingStatus.CheckedIn;
      booking.CheckInTime = DateTime.UtcNow;
      _db.SaveChanges();

      return RedirectToAction("GetBookingsByBranch");
    }

    // 🔹 Get Room Info
    public IActionResult GetRoomInfoByBranch()
    {
      var receptionist = GetCurrentReceptionist();
      if (receptionist == null) return NotFound("Receptionist not found.");

      var rooms = _db.Rooms
        .Include(r => r.Sessions)
        .Where(r => r.BranchId == receptionist.BranchId)
        .ToList();

      return View(rooms);
    }

    // 🔹 Branch Session Calendar
    public IActionResult GetBranchSchedule()
    {
      var receptionist = GetCurrentReceptionist();
      if (receptionist == null) return NotFound("Receptionist not found.");

      var schedule = _db.Sessions
        .Include(s => s.GymClass)
        .Include(s => s.Trainer)
        .Include(s => s.Room)
        .Where(s => s.Room.BranchId == receptionist.BranchId)
        .OrderBy(s => s.SessionDateTime)
        .ToList();

      return View(schedule);
    }
  }
}
