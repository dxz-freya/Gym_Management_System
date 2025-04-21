using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GymManagement.Data;
using GymManagement.Models;
using GymManagement.ViewModels;
using Microsoft.AspNetCore.Identity;


using System.Security.Claims;

namespace GymManagement.Controllers
{
  [Authorize(Roles = "Receptionist")]
  public class ReceptionistController : Controller
  {
    private readonly AppDbContext _db;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    public ReceptionistController(AppDbContext db, UserManager<User> userManager, SignInManager<User> signInManager)
    {
      _db = db;
      _userManager = userManager;
      _signInManager = signInManager;
    }



    // 获取当前 Receptionist 实体
    private Receptionist? GetCurrentReceptionist()
    {
      string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
      if (string.IsNullOrEmpty(userId)) return null;

      return _db.Receptionists.FirstOrDefault(r => r.Id == userId);
    }

    [Authorize(Roles = "Receptionist")]
    [HttpPost]
    public async Task<IActionResult> ToggleAvailabilityAjax(bool isAvailable)
    {
      var user = await _userManager.GetUserAsync(User);
      if (user is not Receptionist receptionist) return Unauthorized();

      receptionist.IsAvailable = isAvailable;
      await _userManager.UpdateAsync(receptionist);
      await _signInManager.RefreshSignInAsync(receptionist);

      return Json(new
      {
        available = isAvailable.ToString().ToLower(),
        text = isAvailable ? "Available" : "Not Available",
        css = isAvailable ? "btn-success" : "btn-secondary"
      });
    }



    public IActionResult Dashboard()
    {
      var receptionist = GetCurrentReceptionist();
      if (receptionist == null)
        return NotFound("Receptionist not found.");

      var today = DateTime.Today;
      var branchId = receptionist.BranchId;

      var todaySessions = _db.Sessions
          .Include(s => s.Trainer)
          .Include(s => s.Room)
          .Include(s => s.GymClass)
          .Include(s => s.Bookings)
              .ThenInclude(b => b.Customer)
          .Where(s => s.SessionDateTime.Date == today && s.Room.BranchId == branchId)
          .ToList();

      var todayBookings = todaySessions.SelectMany(s => s.Bookings).ToList();

      var model = new ReceptionistDashboardViewModel
      {
        TodaySessions = todaySessions,
        TodayTotalBookings = todayBookings.Count,
        TodayCheckedIn = todayBookings.Count(b => b.Status == BookingStatus.CheckedIn),
        CheckInBookings = todayBookings
    .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn)
    .OrderBy(b => b.Session.SessionDateTime)
    .ToList()

      };

      return View(model);
    }


    // Search Member Info by Receptionist's Branch
    public IActionResult SearchMembersByBranch()
    {
      var receptionist = GetCurrentReceptionist();
      if (receptionist == null) return NotFound("Receptionist not found.");

      var members = _db.Customers
        .Where(c => c.GymBranchId == receptionist.BranchId)
        .ToList();

      return View(members);
    }



    // Search Booking Info by Receptionist's Branch
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

    // Check In Member
    [HttpPost]
    public IActionResult CheckInMember(int bookingId)
    {
      var booking = _db.Bookings
          .Include(b => b.Session)
          .Include(b => b.Customer)
          .FirstOrDefault(b => b.BookingId == bookingId);

      if (booking == null)
        return NotFound("Booking not found.");

      if (booking.Status == BookingStatus.Canceled)
        return BadRequest("Cannot check in a canceled booking.");

      if (booking.Status == BookingStatus.CheckedIn)
        return RedirectToAction("Dashboard"); // Already done

      // 打卡
      booking.Status = BookingStatus.CheckedIn;
      booking.CheckInTime = DateTime.Now;
      _db.SaveChanges();

      return RedirectToAction("Dashboard");
    }


    // Get Room Info
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

    // Branch Session Calendar
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

    [Authorize(Roles = "Receptionist")]
    public async Task<IActionResult> ManageBookings(string filter, string value, int page = 1)
    {

      var receptionist = GetCurrentReceptionist();
      if (receptionist == null) return NotFound("Receptionist not found.");

      var branchId = receptionist.BranchId;

      // 限定只查当前 Receptionist 分店的 bookings
      var bookings = await _db.Bookings
          .Include(b => b.Customer)
          .Include(b => b.Session).ThenInclude(s => s.GymClass)
          .Include(b => b.Session).ThenInclude(s => s.Trainer)
          .Include(b => b.Session).ThenInclude(s => s.Room)
          .Where(b => b.Session.Room.BranchId == branchId)
          .OrderByDescending(b => b.Session.SessionDateTime)
          .ToListAsync();

      // 筛选过滤
      switch (filter)
      {
        case "class":
          bookings = bookings.Where(b => b.Session.GymClass.ClassName == value).ToList();
          break;
        case "trainer":
          bookings = bookings.Where(b => b.Session.Trainer.Name == value).ToList();
          break;
        case "date":
          if (DateTime.TryParse(value, out var parsedDate))
            bookings = bookings.Where(b => b.Session.SessionDateTime.Date == parsedDate).ToList();
          break;
        case "status":
          if (Enum.TryParse<BookingStatus>(value, out var status))
            bookings = bookings.Where(b => b.Status == status).ToList();
          break;
      }

      // 限定当前分店的 Customer
      var customers = await _db.Customers
          .Where(c => c.GymBranchId == branchId)
          .Select(c => new SelectListItem
          {
            Value = c.Id,
            Text = c.Name
          }).ToListAsync();

      // 限定当前分店的 Session
      var sessions = await _db.Sessions
          .Include(s => s.GymClass)
          .Include(s => s.Room)
          .Where(s => s.Room.BranchId == branchId)
          .OrderBy(s => s.SessionDateTime)
          .Select(s => new SelectListItem
          {
            Value = s.SessionId.ToString(),
            Text = $"{s.GymClass.ClassName} - {s.SessionDateTime:MM/dd HH:mm}"
          }).ToListAsync();

      var vm = new ManageBookingsViewModel
      {
        AllBookings = bookings,
        CustomerList = customers,
        SessionList = sessions,
        NewBooking = new CreateBookingInputModel()
      };

      return View(vm);
    }




    [HttpPost]
    public async Task<IActionResult> CreateBooking(ManageBookingsViewModel model)
    {
      if (!ModelState.IsValid)
      {
        TempData["Message"] = "Invalid form submission.";
        return RedirectToAction("ManageBookings");
      }

      // 验证 Customer 是否存在
      var customerExists = await _db.Customers.AnyAsync(c => c.Id == model.NewBooking.CustomerId);
      if (!customerExists)
      {
        TempData["Message"] = "Selected customer does not exist.";
        return RedirectToAction("ManageBookings");
      }

      // 验证 Session 是否存在
      var session = await _db.Sessions
          .Include(s => s.Bookings)
          .FirstOrDefaultAsync(s => s.SessionId == model.NewBooking.SessionId);

      if (session == null)
      {
        TempData["Message"] = "Selected session does not exist.";
        return RedirectToAction("ManageBookings");
      }

      // 检查是否已满
      if (session.Bookings.Count >= session.Capacity)
      {
        TempData["Message"] = "Session is full.";
        return RedirectToAction("ManageBookings");
      }

      // 检查是否已经预定过
      var alreadyBooked = await _db.Bookings.AnyAsync(b =>
          b.CustomerId == model.NewBooking.CustomerId && b.SessionId == model.NewBooking.SessionId);

      if (alreadyBooked)
      {
        TempData["Message"] = "This customer is already booked for the selected session.";
        return RedirectToAction("ManageBookings");
      }

      // 添加新 Booking
      var booking = new Booking
      {
        SessionId = model.NewBooking.SessionId,
        CustomerId = model.NewBooking.CustomerId,
        Status = BookingStatus.Pending,
        BookingDate = DateTime.Now,
        UserId = model.NewBooking.CustomerId
      };

      _db.Bookings.Add(booking);
      await _db.SaveChangesAsync();

      TempData["Message"] = "Booking successfully created.";
      return RedirectToAction("ManageBookings");
    }



    [HttpPost]
    public IActionResult ConfirmBooking(int bookingId, string @class, string trainer, string date, string status, int page)
    {
      var booking = _db.Bookings.FirstOrDefault(b => b.BookingId == bookingId);
      if (booking != null && booking.Status == BookingStatus.Pending)
      {
        booking.Status = BookingStatus.Confirmed;
        _db.SaveChanges();
        TempData["Message"] = $"Booking #{bookingId} has been confirmed.";
      }
      else
      {
        TempData["Message"] = $"Booking #{bookingId} not found or already processed.";
      }

      return RedirectToAction("ManageBookings", new { @class, trainer, date, status, page });
    }

    [HttpPost]
    public IActionResult CancelBooking(int bookingId, string @class, string trainer, string date, string status, int page)
    {
      var booking = _db.Bookings.FirstOrDefault(b => b.BookingId == bookingId);
      if (booking != null && booking.Status == BookingStatus.Pending)
      {
        booking.Status = BookingStatus.Canceled;
        _db.SaveChanges();
        TempData["Message"] = $"Booking #{bookingId} has been canceled.";
      }
      else
      {
        TempData["Message"] = $"Booking #{bookingId} not found or already processed.";
      }

      return RedirectToAction("ManageBookings", new { @class, trainer, date, status, page });
    }

    [HttpGet]
    public IActionResult TimetableBooking()
    {
      var receptionist = GetCurrentReceptionist();
      if (receptionist == null) return NotFound();

      var sessions = _db.Sessions
          .Include(s => s.GymClass)
          .Include(s => s.Room)
          .Include(s => s.Trainer)
          .Where(s => s.Room.BranchId == receptionist.BranchId)
          .OrderBy(s => s.SessionDateTime)
          .ToList();

      var customers = _db.Customers
          .Where(c => c.GymBranchId == receptionist.BranchId)
          .Select(c => new SelectListItem
          {
            Text = c.Name,
            Value = c.Id
          }).ToList();

      var vm = new TimetableBookingViewModel
      {
        Sessions = sessions,
        CustomerList = customers
      };

      return View(vm);
    }

    [HttpPost]
    public IActionResult BookFromTimetable(string customerId, int sessionId)
    {
      if (string.IsNullOrEmpty(customerId)) return BadRequest("Customer ID required.");

      var alreadyBooked = _db.Bookings.Any(b => b.CustomerId == customerId && b.SessionId == sessionId);
      if (alreadyBooked) return BadRequest("Customer already booked.");

      var booking = new Booking
      {
        CustomerId = customerId,
        SessionId = sessionId,
        Status = BookingStatus.Confirmed,
        BookingDate = DateTime.Now,
        UserId = customerId
      };

      _db.Bookings.Add(booking);
      _db.SaveChanges();

      return RedirectToAction("TimetableBooking");
    }


  }
}
