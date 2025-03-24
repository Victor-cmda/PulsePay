using System;
using System.Collections.Generic;

namespace Application.DTOs
{
    public class DashboardDataDto
    {
        public IEnumerable<BarChartDataDto> BarChartData { get; set; }
        public IEnumerable<PieChartDataDto> PieChartData { get; set; }
        public IEnumerable<LineChartDataDto> LineChartDotsData { get; set; }
        public IEnumerable<LineChartDataDto> LineChartData { get; set; }
        public OverviewDataDto OverviewData { get; set; }
    }

    public class BarChartDataDto
    {
        public string Day { get; set; }
        public int Count { get; set; }
    }

    public class PieChartDataDto
    {
        public string PaymentType { get; set; }
        public int Count { get; set; }
    }

    public class LineChartDataDto
    {
        public string Date { get; set; }
        public string Hour { get; set; }
        public int Pix { get; set; }
        public int Credit { get; set; }
        public int BankSlip { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class OverviewDataDto
    {
        public decimal ApprovedAmount { get; set; }
        public int ApprovedCount { get; set; }
        public decimal RejectedAmount { get; set; }
        public int RejectedCount { get; set; }
        public decimal CancelledAmount { get; set; }
        public int CancelledCount { get; set; }
        public decimal PendingAmount { get; set; }
        public int PendingCount { get; set; }
    }
}
