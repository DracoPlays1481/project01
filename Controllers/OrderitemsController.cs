using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EWDProject.Models;

namespace EWDProject.Controllers
{
    public class OrderitemsController : Controller
    {
        private readonly KardContext _context;

        public OrderitemsController(KardContext context)
        {
            _context = context;
        }

        // GET: Orderitems
        public async Task<IActionResult> Index()
        {
            var kardContext = _context.Orderitems.Include(o => o.Book).Include(o => o.Order);
            return View(await kardContext.ToListAsync());
        }

        // GET: Orderitems/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderitem = await _context.Orderitems
                .Include(o => o.Book)
                .Include(o => o.Order)
                .FirstOrDefaultAsync(m => m.OrderItemId == id);
            if (orderitem == null)
            {
                return NotFound();
            }

            return View(orderitem);
        }

        // GET: Orderitems/Create
        public IActionResult Create()
        {
            ViewData["BookId"] = new SelectList(_context.Books, "BookId", "BookId");
            ViewData["OrderId"] = new SelectList(_context.Orders, "OrderId", "OrderId");
            return View();
        }

        // POST: Orderitems/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OrderItemId,OrderId,BookId,Quantity")] Orderitem orderitem)
        {
            if (ModelState.IsValid)
            {
                _context.Add(orderitem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["BookId"] = new SelectList(_context.Books, "BookId", "BookId", orderitem.BookId);
            ViewData["OrderId"] = new SelectList(_context.Orders, "OrderId", "OrderId", orderitem.OrderId);
            return View(orderitem);
        }

        // GET: Orderitems/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderitem = await _context.Orderitems.FindAsync(id);
            if (orderitem == null)
            {
                return NotFound();
            }
            ViewData["BookId"] = new SelectList(_context.Books, "BookId", "BookId", orderitem.BookId);
            ViewData["OrderId"] = new SelectList(_context.Orders, "OrderId", "OrderId", orderitem.OrderId);
            return View(orderitem);
        }

        // POST: Orderitems/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("OrderItemId,OrderId,BookId,Quantity")] Orderitem orderitem)
        {
            if (id != orderitem.OrderItemId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(orderitem);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderitemExists(orderitem.OrderItemId))
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
            ViewData["BookId"] = new SelectList(_context.Books, "BookId", "BookId", orderitem.BookId);
            ViewData["OrderId"] = new SelectList(_context.Orders, "OrderId", "OrderId", orderitem.OrderId);
            return View(orderitem);
        }

        // GET: Orderitems/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderitem = await _context.Orderitems
                .Include(o => o.Book)
                .Include(o => o.Order)
                .FirstOrDefaultAsync(m => m.OrderItemId == id);
            if (orderitem == null)
            {
                return NotFound();
            }

            return View(orderitem);
        }

        // POST: Orderitems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var orderitem = await _context.Orderitems.FindAsync(id);
            if (orderitem != null)
            {
                _context.Orderitems.Remove(orderitem);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderitemExists(int id)
        {
            return _context.Orderitems.Any(e => e.OrderItemId == id);
        }
    }
}
