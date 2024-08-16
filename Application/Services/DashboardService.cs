using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces;

namespace Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _dashboardRepository;
        public DashboardService(IDashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository;
        }

        public async Task<DashboardDataDto> GetDashboard(List<Guid> sellers)
        {
            var transactions = await _dashboardRepository.GetTransactionsBySellers(sellers);
            var now = DateTime.UtcNow;

            var last7Days = transactions.Where(t => t.CreatedAt >= now.AddDays(-7)).ToList();
            var last30Days = transactions.Where(t => t.CreatedAt >= now.AddDays(-30)).ToList();
            var today = transactions.Where(t => t.CreatedAt.Date == now.Date).ToList();

            var barChartData = last7Days.GroupBy(t => t.CreatedAt.Date)
                .Select(g => new BarChartDataDto
                {
                    Day = g.Key.ToString("dd/MM"),
                    Count = g.Count()
                }).ToList();

            var pieChartData = last30Days.GroupBy(t => t.PaymentType)
                .Select(g => new PieChartDataDto
                {
                    PaymentType = g.Key,
                    Count = g.Count()
                }).ToList();
             
            var allHours = Enumerable.Range(0, 24).Select(h => h.ToString("D2")).ToList();
            var lineChartDotsData = allHours.GroupJoin(today,
                                                       h => h,
                                                       t => t.CreatedAt.Hour.ToString("D2"),
                                                       (hour, ts) => new LineChartDataDto
                                                       {
                                                           Hour = hour,
                                                           TotalAmount = ts.Sum(t => t.Amount)
                                                       }).ToList();

            var lineChartData = last30Days.GroupBy(t => t.CreatedAt.Date)
                .Select(g => new LineChartDataDto
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    BankSlip = g.Count(x => x.PaymentType == "BANKSLIP"),
                    Pix = g.Count(x => x.PaymentType == "PIX"),
                    Credit = g.Count(x => x.PaymentType == "CREDIT")
                }).ToList();

            var overviewData = new OverviewDataDto
            {
                ApprovedAmount = transactions.Where(t => t.Status == "APPROVED").Sum(t => t.Amount),
                ApprovedCount = transactions.Count(t => t.Status == "APPROVED"),
                RejectedAmount = transactions.Where(t => t.Status == "REJECTED").Sum(t => t.Amount),
                RejectedCount = transactions.Count(t => t.Status == "REJECTED"),
                CancelledAmount = transactions.Where(t => t.Status == "CANCELLED").Sum(t => t.Amount),
                CancelledCount = transactions.Count(t => t.Status == "CANCELLED"),
                PendingAmount = transactions.Where(t => t.Status == "PENDING").Sum(t => t.Amount),
                PendingCount = transactions.Count(t => t.Status == "PENDING")
            };

            var dashboardData = new DashboardDataDto
            {
                BarChartData = barChartData,
                PieChartData = pieChartData,
                LineChartDotsData = lineChartDotsData,
                LineChartData = lineChartData,
                OverviewData = overviewData
            };

            return dashboardData;
        }

        public async Task<List<TransactionDataDto>> GetHistoryTransactions(List<Guid> sellers)
        {
            var transactions = await _dashboardRepository.GetTransactionsBySellers(sellers);

            var historyData = new List<TransactionDataDto>();

            foreach (var item in transactions)
            {
                historyData.Add(new TransactionDataDto
                {
                    Id = item.Id,
                    Amount = Convert.ToDouble(item.Amount),
                    CreatedAt = item.CreatedAt,
                    Status = item.Status,
                    Customer = item.NameCustumer,
                    PaymentType = item.PaymentType,
                    SellerId = item.SellerId,
                    Description = item.Description,
                    PaidAt = item.PaidAt
                });
            }
            return historyData;
        }
    }
}
