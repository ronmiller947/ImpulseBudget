namespace ImpulseBudget.Models
{
    public class ProjectionPoint
    {
        public DateTime WeekStart { get; set; }
        public DateTime WeekEnd { get; set; }
        public decimal StartingBalance { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalBills { get; set; }
        public decimal TotalDebtPayments { get; set; }
        public decimal EndingBalance { get; set; }

        public bool IsShortfall => EndingBalance < 0;
    }
}
