using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using GymManagement.Models;
using GymManagement.Data;
using GymManagement.Helpers;
using GymManagement.Services;
using GymManagement.ViewModels;
using GymManagement.ViewModels.Payments;

namespace GymManagement.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerController : Controller
    {
        private readonly AppDbContext _dbContext;
        private readonly UserManager<User> _userManager;
        private readonly PdfService _pdfService;

        public CustomerController(AppDbContext dbContext, UserManager<User> userManager, PdfService pdfService)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _pdfService = pdfService;
        }

        private async Task<Customer> GetCurrentCustomerAsync()
        {
            var userId = _userManager.GetUserId(User);
            var customer = await _dbContext.Customers
                .Include(c => c.Bookings)
                .Include(c => c.Payments)
                .FirstOrDefaultAsync(c => c.Id == userId);

            return customer!;
        }

        public async Task<IActionResult> Dashboard(string? category, string? trainer, string? status, int page = 1)
        {
            var customer = await GetCurrentCustomerAsync();
            int pageSize = 10;
            var now = DateTime.Now;
            if (customer == null)
            {
                return NotFound("Customer not found.");
            }

            var bookingsQuery = _dbContext.Bookings
                .Include(b => b.Session).ThenInclude(s => s.GymClass)
                .Include(b => b.Session).ThenInclude(s => s.Trainer)
                .Include(b => b.Session).ThenInclude(s => s.Room)
                .Where(b => b.CustomerId == customer.Id)
                .AsQueryable();

            if (!string.IsNullOrEmpty(category))
                bookingsQuery = bookingsQuery.Where(b => b.Session.Category.ToString() == category);

            if (!string.IsNullOrEmpty(trainer))
                bookingsQuery = bookingsQuery.Where(b => b.Session.Trainer.Name == trainer);

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<BookingStatus>(status, out var parsedStatus))
                bookingsQuery = bookingsQuery.Where(b => b.Status == parsedStatus);

            var allBookingVMs = await bookingsQuery
                .OrderByDescending(b => b.Session.SessionDateTime)
                .Select(b => new BookingViewModel
                {
                    BookingId = b.BookingId,
                    ClassName = b.Session.GymClass.ClassName,
                    SessionDateTime = b.Session.SessionDateTime,
                    Status = b.Status.ToString(),
                    TrainerName = b.Session.Trainer.Name,
                    RoomName = b.Session.Room.RoomName,
                    Category = b.Session.Category.ToString()
                })
                .ToListAsync();

            var upcoming = allBookingVMs.Where(b => b.SessionDateTime > now).ToList();
            var past = allBookingVMs.Where(b => b.SessionDateTime <= now).ToList();

            var vm = new CustomerDashboardViewModel
            {
                Name = customer.Name,
                MembershipType = customer.MembershipType.ToString(),
                MembershipStatus = customer.MembershipStatus.ToString(),
                SubscriptionDate = customer.SubscriptionDate,
                MembershipExpiry = customer.MembershipExpiry,
                ProfileImageName = customer.ProfileImageName ?? "default.png",
                WalletBalance = customer.WalletBalance,
                Payments = customer.Payments.Select(p => new PaymentViewModel
                {
                    Price = p.Price,
                    PaymentMethod = p.PaymentMethod,
                    PaymentDate = p.PaymentDate,
                    Type = p.Type.ToString()
                }).ToList(),
                UpcomingBookings = upcoming.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                PastBookings = past,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(upcoming.Count / (double)pageSize)
            };

            ViewBag.CategoryOptions = Enum.GetNames(typeof(SessionCategory)).ToList();
            ViewBag.TrainerOptions = await _dbContext.Trainers.Select(t => t.Name).Distinct().ToListAsync();
            ViewBag.StatusOptions = Enum.GetNames(typeof(BookingStatus)).ToList();
            ViewBag.SelectedCategory = category;
            ViewBag.SelectedTrainer = trainer;
            ViewBag.SelectedStatus = status;

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Membership()
        {
            var customer = await GetCurrentCustomerAsync();
            var vm = new MembershipViewModel
            {
                Name = customer.Name,
                MembershipType = customer.MembershipType.ToString(),
                MembershipStatus = customer.MembershipStatus.ToString(),
                ExpiryDate = customer.MembershipExpiry,
                RemainingDays = customer.MembershipExpiry.HasValue
                    ? (customer.MembershipExpiry.Value.Date - DateTime.UtcNow.Date).Days : 0,
                WalletBalance = customer.WalletBalance,
                Payments = customer.Payments.Select(p => new PaymentViewModel
                {
                    Price = p.Price,
                    PaymentMethod = p.PaymentMethod,
                    PaymentDate = p.PaymentDate,
                    Type = p.Type.ToString(),
                    PaymentId = p.PaymentId
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> RenewMembership(string plan)
        {
            var customer = await GetCurrentCustomerAsync();

            if (!Enum.TryParse<MembershipType>(plan, out var selectedType))
            {
                TempData["Toast"] = "Invalid membership plan.";
                return RedirectToAction("Membership");
            }

            decimal price = PricingPlans.GetPrice(selectedType);

            if (customer.WalletBalance < price)
            {
                TempData["Toast"] = $"❌ Insufficient balance! You need ${price} but only have ${customer.WalletBalance}. Please recharge.";
                return RedirectToAction("Membership");
            }

            customer.WalletBalance -= price;
            customer.MembershipType = selectedType;
            customer.MembershipStatus = MembershipStatus.Active;
            customer.SubscriptionDate = DateTime.UtcNow;

            if (customer.MembershipExpiry.HasValue && customer.MembershipExpiry.Value > DateTime.UtcNow)
            {
                customer.MembershipExpiry = selectedType switch
                {
                    MembershipType.Monthly => customer.MembershipExpiry.Value.AddMonths(1),
                    MembershipType.Quarterly => customer.MembershipExpiry.Value.AddMonths(3),
                    MembershipType.Yearly => customer.MembershipExpiry.Value.AddYears(1),
                    _ => customer.MembershipExpiry
                };
            }
            else
            {
                customer.MembershipExpiry = selectedType switch
                {
                    MembershipType.Monthly => DateTime.UtcNow.AddMonths(1),
                    MembershipType.Quarterly => DateTime.UtcNow.AddMonths(3),
                    MembershipType.Yearly => DateTime.UtcNow.AddYears(1),
                    _ => DateTime.UtcNow
                };
            }

            _dbContext.Payments.Add(new Payment
            {
                CustomerId = customer.Id,
                Price = price,
                PaymentMethod = "Wallet",
                PaymentDate = DateTime.UtcNow,
                Type = PaymentType.Membership
            });

            _dbContext.Update(customer);
            await _dbContext.SaveChangesAsync();

            TempData["Toast"] = $"✅ Membership renewed as {selectedType}! ${price} has been deducted from your wallet.";
            return RedirectToAction("Membership");
        }

        [HttpGet]
        public async Task<IActionResult> Recharge()
        {
            var customer = await GetCurrentCustomerAsync();
            return View(new WalletRechargeViewModel { CurrentBalance = customer.WalletBalance });
        }

        [HttpPost]
        public async Task<IActionResult> Recharge(WalletRechargeViewModel vm)
        {
            var customer = await GetCurrentCustomerAsync();
            if (!ModelState.IsValid)
            {
                vm.CurrentBalance = customer.WalletBalance;
                return View(vm);
            }

            customer.WalletBalance += vm.Amount;

            _dbContext.Payments.Add(new Payment
            {
                CustomerId = customer.Id,
                Price = vm.Amount,
                PaymentMethod = vm.PaymentMethod,
                PaymentDate = DateTime.UtcNow,
                Type = PaymentType.Recharge
            });

            _dbContext.Update(customer);
            await _dbContext.SaveChangesAsync();

            TempData["Toast"] = $"Successfully recharged ${vm.Amount}";
            return RedirectToAction("Membership");
        }

        [HttpGet]
        public async Task<IActionResult> Payments(string? type, DateTime? from, DateTime? to)
        {
            var customer = await GetCurrentCustomerAsync();
            var payments = customer.Payments.OrderByDescending(p => p.PaymentDate).ToList();

            if (!string.IsNullOrEmpty(type) && Enum.TryParse<PaymentType>(type, out var parsedType))
                payments = payments.Where(p => p.Type == parsedType).ToList();

            if (from.HasValue)
                payments = payments.Where(p => p.PaymentDate >= from.Value).ToList();

            if (to.HasValue)
                payments = payments.Where(p => p.PaymentDate <= to.Value).ToList();

            var chartData = payments
                .GroupBy(p => p.PaymentDate.ToString("yyyy-MM"))
                .Select(g => new ChartBarViewModel
                {
                    Month = g.Key,
                    Total = g.Sum(p => p.Price)
                }).ToList();

            var vm = new PaymentsPageViewModel
            {
                WalletBalance = customer.WalletBalance,
                Payments = payments.Select(p => new PaymentViewModel
                {
                    Price = p.Price,
                    PaymentMethod = p.PaymentMethod,
                    Type = p.Type.ToString(),
                    PaymentDate = p.PaymentDate,
                    PaymentId = p.PaymentId
                }).ToList(),
                ChartData = chartData,
                FilterType = type,
                FromDate = from,
                ToDate = to
            };

            return View(vm);
        }

        [HttpGet]
public async Task<IActionResult> DownloadInvoice(int id)
{
    var payment = await _dbContext.Payments
        .Include(p => p.Customer)
        .FirstOrDefaultAsync(p => p.PaymentId == id);

    if (payment == null)
        return NotFound();

    var html = $@"
        <html>
        <head>
            <style>
                body {{ font-family: Arial, sans-serif; padding: 40px; }}
                h2 {{ color: #ffc107; }}
                table {{ width: 100%; border-collapse: collapse; margin-top: 20px; }}
                td {{ padding: 8px; }}
                .bold {{ font-weight: bold; }}
            </style>
        </head>
        <body>
            <h2>Gym Invoice</h2>
            <p><span class='bold'>Invoice ID:</span> {payment.PaymentId}</p>
            <p><span class='bold'>Customer:</span> {payment.Customer.Name}</p>
            <p><span class='bold'>Email:</span> {payment.Customer.Email}</p>
            <p><span class='bold'>Date:</span> {payment.PaymentDate:yyyy-MM-dd}</p>
            <p><span class='bold'>Payment Method:</span> {payment.PaymentMethod}</p>
            <p><span class='bold'>Type:</span> {payment.Type}</p>
            <h3>Total: ${payment.Price:F2}</h3>
        </body>
        </html>";

    var pdf = _pdfService.GeneratePdf(html);
    return File(pdf, "application/pdf", $"Invoice_{payment.PaymentId}.pdf");
}






        // 4. My Bookings
        public async Task<IActionResult> MyBookings()
        {
            var customer = await GetCurrentCustomerAsync();  // 获取 Customer
            var bookings = await _dbContext.Bookings
                .Include(b => b.Session)
                .ThenInclude(s => s.Trainer)
                .Where(b => b.UserId == customer.Id)
                .ToListAsync();

            return View(bookings);  // 返回 MyBookings 视图
        }

        // 5. Book a Session
        [HttpGet]
        public async Task<IActionResult> BookSession(int page = 1)
        {
            int pageSize = 6;
            var customer = await GetCurrentCustomerAsync();
            var bookedSessionIds = await _dbContext.Bookings
                .Where(b => b.CustomerId == customer.Id && b.Status != BookingStatus.Canceled)
                .Select(b => b.SessionId)
                .ToListAsync();
            var totalSessions = await _dbContext.Sessions
                .Include(s => s.Room)
                .Include(s => s.GymClass)
                .Include(s => s.Trainer)
                .OrderBy(s => s.SessionDateTime)
                .ToListAsync();

            var pagedSessions = totalSessions
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SessionViewModel
            {
                SessionId = s.SessionId,
                SessionName = s.SessionName,
                ClassName = s.GymClass.ClassName,
                SessionDateTime = s.SessionDateTime,
                RoomName = s.Room.RoomName,
                Trainer = s.Trainer,
                IsBookedByCurrentUser = bookedSessionIds.Contains(s.SessionId),
                TotalBookings = _dbContext.Bookings.Count(b => b.SessionId == s.SessionId && b.Status != BookingStatus.Canceled),
                Category = s.Category
            })
            .ToList();



            var viewModel = new PagedSessionViewModel
            {
                Sessions = pagedSessions,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalSessions.Count() / (double)pageSize)
            };

            return View(viewModel);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> MarkBookingPending([FromBody] BookingRequest request)
        {
            if (request == null || request.SessionId <= 0)
                return BadRequest(new { error = "Invalid session id." });

            var customer = await GetCurrentCustomerAsync();
            if (customer == null)
                return Unauthorized(new { error = "Customer not found." });

            var session = await _dbContext.Sessions.FindAsync(request.SessionId);
            if (session == null)
                return NotFound(new { error = "Session not found." });

            var booking = new Booking
            {
                SessionId = request.SessionId,
                CustomerId = customer.Id,
                UserId = customer.Id,
                BookingDate = DateTime.Now,
                Status = BookingStatus.Pending
            };

            try
            {
                _dbContext.Bookings.Add(booking);
                await _dbContext.SaveChangesAsync();
                return Ok(new { message = "Booking set to pending." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
            }
        }



        // 6. Trainer List
        public async Task<IActionResult> TrainerList()
        {
            var trainers = await _dbContext.Trainers.ToListAsync();  // 获取所有教练信息
            return View(trainers);  // 返回 TrainerList 视图
        }

        // 7. Session Calendar
        public async Task<IActionResult> SessionCalendar()
        {
            var sessions = await _dbContext.Sessions
                .Include(s => s.Trainer)
                .OrderBy(s => s.SessionDateTime)
                .ToListAsync();

            return View(sessions);  // 返回 SessionCalendar 视图
        }

        [HttpPost]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            var booking = await _dbContext.Bookings
                .Include(b => b.Session)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null || booking.Status == BookingStatus.Canceled)
            {
                TempData["Toast"] = "Booking not found or already canceled.";
                return RedirectToAction("Dashboard");
            }

            if (booking.Session.SessionDateTime <= DateTime.Now)
            {
                TempData["Toast"] = "Cannot cancel past or ongoing sessions.";
                return RedirectToAction("Dashboard");
            }

            booking.Status = BookingStatus.Canceled;
            await _dbContext.SaveChangesAsync();

            TempData["Toast"] = "Booking canceled successfully.";
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> BookAgain(int bookingId)
        {
            var booking = await _dbContext.Bookings
                .Include(b => b.Session)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null || booking.Status != BookingStatus.Canceled)
            {
                TempData["Toast"] = "Only canceled bookings can be rebooked.";
                return RedirectToAction("Dashboard");
            }

            if (booking.Session.SessionDateTime <= DateTime.Now)
            {
                TempData["Toast"] = "Cannot rebook past sessions.";
                return RedirectToAction("Dashboard");
            }

            booking.Status = BookingStatus.Pending;
            booking.BookingDate = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            TempData["Toast"] = "Booking re-submitted successfully.";
            return RedirectToAction("Dashboard");
        }




    }
}
