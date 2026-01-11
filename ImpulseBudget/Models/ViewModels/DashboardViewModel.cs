namespace ImpulseBudget.Models
{
    public class DashboardViewModel
    {
        public int IncomeSourceCount { get; set; }

        public int BillCount { get; set; }

        public int DebtCount { get; set; }

        public int TransactionCount { get; set; }

        public decimal StartingBalance { get; set; }

        public List<ProjectionPoint> Projection { get; set; } = new();

        public decimal CurrentBalance { get; set; }

    }
}
