using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EWDProject.Models;

namespace EWDProject.Controllers
{
    public class OrdersController : Controller
    {
        private readonly KardContext _context;

        public OrdersController(KardContext context)
        {
            _context = context;
        }

        // GET: Orders/History
        public async Task<IActionResult> History()
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Index", "Customers");
            }

            var orders = await _context.Orders
                .Include(o => o.Orderitems)
                .ThenInclude(oi => oi.Book)
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // GET: Orders/Create
        public IActionResult Create()
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Index", "Customers");
            }

            ViewBag.Books = _context.Books.ToList();
            return View(new Order { CustomerId = customerId.Value });
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order order, string action, List<Orderitem> orderItems)
        {
            if (ModelState.IsValid)
            {
                var customerId = HttpContext.Session.GetInt32("CustomerId");
                if (customerId == null)
                {
                    return RedirectToAction("Index", "Customers");
                }

                order.CustomerId = customerId.Value;
                order.OrderDate = DateTime.Now;
                order.OrderStatus = action == "Save" ? "Saved" : "Confirmed";

                // Get the next available OrderId
                int nextOrderId = await _context.Orders
                    .Select(o => o.OrderId)
                    .DefaultIfEmpty()
                    .MaxAsync() + 1;

                order.OrderId = nextOrderId;

                _context.Add(order);
                await _context.SaveChangesAsync();

                foreach (var item in orderItems)
                {
                    if (item.Quantity > 0)
                    {
                        // Get the next available OrderItemId
                        int nextOrderItemId = await _context.Orderitems
                            .Select(oi => oi.OrderItemId)
                            .DefaultIfEmpty()
                            .MaxAsync() + 1;

                        item.OrderItemId = nextOrderItemId;
                        item.OrderId = order.OrderId;
                        _context.Orderitems.Add(item);
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id = order.OrderId });
            }
            ViewBag.Books = _context.Books.ToList();
            return View(order);
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Index", "Customers");
            }

            var order = await _context.Orders
                .Include(o => o.Orderitems)
                .ThenInclude(oi => oi.Book)
                .FirstOrDefaultAsync(m => m.OrderId == id && m.CustomerId == customerId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Index", "Customers");
            }

            var order = await _context.Orders
                .Include(o => o.Orderitems)
                .ThenInclude(oi => oi.Book)
                .FirstOrDefaultAsync(m => m.OrderId == id && m.CustomerId == customerId);

            if (order == null || order.OrderStatus != "Saved")
            {
                return NotFound();
            }

            ViewBag.Books = _context.Books.ToList();
            return View(order);
        }

        // POST: Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Order order, List<Orderitem> orderItems)
        {
            if (id != order.OrderId)
            {
                return NotFound();
            }

            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null || customerId != order.CustomerId)
            {
                return RedirectToAction("Index", "Customers");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingOrder = await _context.Orders
                        .Include(o => o.Orderitems)
                        .FirstOrDefaultAsync(o => o.OrderId == id);

                    if (existingOrder == null || existingOrder.OrderStatus != "Saved")
                    {
                        return NotFound();
                    }

                    // Remove existing order items
                    _context.Orderitems.RemoveRange(existingOrder.Orderitems);

                    // Add new order items
                    foreach (var item in orderItems)
                    {
                        if (item.Quantity > 0)
                        {
                            // Get the next available OrderItemId
                            int nextOrderItemId = await _context.Orderitems
                                .Select(oi => oi.OrderItemId)
                                .DefaultIfEmpty()
                                .MaxAsync() + 1;

                            item.OrderItemId = nextOrderItemId;
                            item.OrderId = order.OrderId;
                            _context.Orderitems.Add(item);
                        }
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Details), new { id = order.OrderId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.OrderId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            ViewBag.Books = _context.Books.ToList();
            return View(order);
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Index", "Customers");
            }

            var order = await _context.Orders
                .Include(o => o.Orderitems)
                .ThenInclude(oi => oi.Book)
                .FirstOrDefaultAsync(m => m.OrderId == id && m.CustomerId == customerId);

            if (order == null || order.OrderStatus != "Saved")
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Index", "Customers");
            }

            var order = await _context.Orders
                .Include(o => o.Orderitems)
                .FirstOrDefaultAsync(m => m.OrderId == id && m.CustomerId == customerId);

            if (order != null && order.OrderStatus == "Saved")
            {
                _context.Orderitems.RemoveRange(order.Orderitems);
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(History));
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrderId == id);
        }
    }
}