namespace GymManagement.ViewModels.Payments
{
    public class PaymentViewModel
    {
        public int PaymentId { get; set; }
        public decimal Price { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public string Type { get; set; } = string.Empty;
    }
}
