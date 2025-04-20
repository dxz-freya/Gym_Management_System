using System;
using System.Collections.Generic;
using GymManagement.ViewModels.Payments;


namespace GymManagement.ViewModels
{
    public class MembershipViewModel
    {
        public string Name { get; set; }
        public string MembershipType { get; set; }
        public string MembershipStatus { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int RemainingDays { get; set; }
        public List<PaymentViewModel> Payments { get; set; } = new();
        public decimal WalletBalance { get; set; }

    }
}
