using System;
using System.Collections.Generic;
using System.Linq;

namespace GymManagement.ViewModels.Payments
{
    public class PaymentsPageViewModel
    {
        public decimal WalletBalance { get; set; }

        public List<PaymentViewModel> Payments { get; set; } = new();
        public List<ChartBarViewModel> ChartData { get; set; } = new();

        public string? SelectedType { get; set; }
        public string? SelectedPeriod { get; set; }

        // ✅ 用于 Razor 前端筛选和图表
        public string? FilterType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        // ✅ 柱状图数据（月份 + 每月总额）
        public List<string> ChartLabels => ChartData.Select(c => c.Month).ToList();
        public List<decimal> ChartValues => ChartData.Select(c => c.Total).ToList();

        // ✅ 饼图数据（类型分类）
        public List<string> ChartTypes => Payments.Select(p => p.Type).Distinct().ToList();
        public List<decimal> ChartTypeTotals =>
            ChartTypes.Select(type => Payments.Where(p => p.Type == type).Sum(p => p.Price)).ToList();
    }
}
