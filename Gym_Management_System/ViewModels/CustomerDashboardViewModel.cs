using System;
using System.Collections.Generic;

namespace GymManagement.ViewModels
{
    public class CustomerDashboardViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string MembershipStatus { get; set; } = string.Empty;
        public string MembershipType { get; set; } = string.Empty;
        public DateTime SubscriptionDate { get; set; }
        public DateTime? MembershipExpiry { get; set; }  // 🔹 加这个

        public string ProfileImageName { get; set; } = "default.png";

        public List<PaymentViewModel> Payments { get; set; } = new();

        public List<BookingViewModel> UpcomingBookings { get; set; } = new();
        public List<BookingViewModel> PastBookings { get; set; } = new();

        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
    }
}
