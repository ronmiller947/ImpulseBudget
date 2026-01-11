namespace ImpulseBudget.Models
{
    public class DebtSimState
    {
        public decimal Balance { get; set; }
        public DateTime NextDueDate { get; set; }
        public decimal MinimumPayment { get; set; }
    }
}
