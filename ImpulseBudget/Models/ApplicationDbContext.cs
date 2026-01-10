using Microsoft.EntityFrameworkCore;

namespace ImpulseBudget.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<IncomeSource> IncomeSources { get; set; }
        public DbSet<Bill> Bills { get; set; }
        public DbSet<Debt> Debts { get; set; }
        public DbSet<BankTransaction> BankTransactions { get; set; }
    }
}
