using GymManagement.ViewModels.Payments;


namespace GymManagement.ViewModels
{
    public class PaymentsViewModel
    {
        public decimal WalletBalance { get; set; }
        public List<PaymentViewModel> Payments { get; set; } = new();
        public string? FilterType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public List<string> ChartLabels { get; set; } = new();
        public List<decimal> ChartValues { get; set; } = new();
        public List<string> ChartTypes { get; set; } = new();
        public List<decimal> ChartTypeTotals { get; set; } = new();
    }
}
