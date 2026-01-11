using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ImpulseBudget.Models;

namespace ImpulseBudget.Controllers
{
    public class BankTransactionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BankTransactionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: BankTransactions
        public async Task<IActionResult> Index()
        {
            return View(await _context.BankTransactions.ToListAsync());
        }

        // GET: BankTransactions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bankTransaction = await _context.BankTransactions
                .FirstOrDefaultAsync(m => m.Id == id);
            if (bankTransaction == null)
            {
                return NotFound();
            }

            return View(bankTransaction);
        }

        // GET: BankTransactions/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: BankTransactions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Date,Description,Amount,Category,IsIncoming")] BankTransaction bankTransaction)
        {
            if (ModelState.IsValid)
            {
                // Normalize sign based on direction:
                // Incoming = positive; Outgoing = negative
                var absAmount = Math.Abs(bankTransaction.Amount);
                bankTransaction.Amount = bankTransaction.IsIncoming ? absAmount : -absAmount;

                bankTransaction.IsImported = false; // manual entry

                _context.Add(bankTransaction);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(bankTransaction);
        }

        // GET: BankTransactions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bankTransaction = await _context.BankTransactions.FindAsync(id);
            if (bankTransaction == null)
            {
                return NotFound();
            }
            return View(bankTransaction);
        }

        // POST: BankTransactions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Date,Description,Amount,Category,IsIncoming,IsImported")] BankTransaction bankTransaction)
        {
            if (id != bankTransaction.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var absAmount = Math.Abs(bankTransaction.Amount);
                    bankTransaction.Amount = bankTransaction.IsIncoming ? absAmount : -absAmount;

                    _context.Update(bankTransaction);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BankTransactionExists(bankTransaction.Id))
                        return NotFound();
                    else
                        throw;
                }

                return RedirectToAction(nameof(Index));
            }
            return View(bankTransaction);
        }

        // GET: BankTransactions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bankTransaction = await _context.BankTransactions
                .FirstOrDefaultAsync(m => m.Id == id);
            if (bankTransaction == null)
            {
                return NotFound();
            }

            return View(bankTransaction);
        }

        // POST: BankTransactions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var bankTransaction = await _context.BankTransactions.FindAsync(id);
            if (bankTransaction != null)
            {
                _context.BankTransactions.Remove(bankTransaction);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BankTransactionExists(int id)
        {
            return _context.BankTransactions.Any(e => e.Id == id);
        }
    }
}
