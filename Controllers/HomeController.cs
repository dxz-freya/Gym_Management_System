using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GymManagement.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // 首页
        [HttpGet]
        public IActionResult Index()
        {
            _logger.LogInformation("Visited Home page");
            return View();
        }

        // 课程时间表页
        [HttpGet]
        public IActionResult ClassSchedule()
        {
            _logger.LogInformation("Visited Class Schedule page");
            return View();
        }

        // 会员方案页
        [HttpGet]
        public IActionResult MembershipOptions()
        {
            _logger.LogInformation("Visited Membership Options page");
            return View();
        }

        // 联系我们页（GET）
        [HttpGet]
        public IActionResult Contact()
        {
            _logger.LogInformation("Visited Contact page");
            return View();
        }

        // 联系我们页（POST）
        [HttpPost]
        public IActionResult Contact(string Name, string Email, string Subject, string Message)
        {
            _logger.LogInformation("Contact form submitted by {Name} ({Email})", Name, Email);

            TempData["ToastMessage"] = $"Thank you, {Name}! Your message has been received.";
            TempData["ToastClass"] = "success";
            TempData["ToastTitle"] = "Message Sent";

            return RedirectToAction("Contact");
        }

        // 错误页
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            _logger.LogError("Unhandled error occurred");
            return View();
        }
    }
}
