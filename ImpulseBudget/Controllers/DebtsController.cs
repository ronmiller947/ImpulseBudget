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
    public class DebtsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DebtsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Debts
        public async Task<IActionResult> Index()
        {
            return View(await _context.Debts.ToListAsync());
        }

        // GET: Debts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var debt = await _context.Debts
                .FirstOrDefaultAsync(m => m.Id == id);
            if (debt == null)
            {
                return NotFound();
            }

            return View(debt);
        }

        // GET: Debts/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Debts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Balance,AprPercent,MinimumPayment,NextDueDate,IsEssential")] Debt debt)
        {
            if (ModelState.IsValid)
            {
                _context.Add(debt);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(debt);
        }

        // GET: Debts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var debt = await _context.Debts.FindAsync(id);
            if (debt == null)
            {
                return NotFound();
            }
            return View(debt);
        }

        // POST: Debts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Balance,AprPercent,MinimumPayment,NextDueDate,IsEssential")] Debt debt)
        {
            if (id != debt.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(debt);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DebtExists(debt.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(debt);
        }

        // GET: Debts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var debt = await _context.Debts
                .FirstOrDefaultAsync(m => m.Id == id);
            if (debt == null)
            {
                return NotFound();
            }

            return View(debt);
        }

        // POST: Debts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var debt = await _context.Debts.FindAsync(id);
            if (debt != null)
            {
                _context.Debts.Remove(debt);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DebtExists(int id)
        {
            return _context.Debts.Any(e => e.Id == id);
        }

        public IActionResult CreateFromTransaction(string description, decimal amount)
        {
            var model = new Debt
            {
                Name = description,
                Balance = 0,
                AprPercent = 0, // you'll update this manually
                MinimumPayment = amount,
                NextDueDate = DateTime.Today,
                IsEssential = false
            };

            return View("Create", model);
        }
    }
}
