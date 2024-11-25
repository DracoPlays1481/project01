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
                return RedirectToAction("Login", "Customers");
            }

            IQueryable<Order> ordersQuery = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Orderitems)
                    .ThenInclude(oi => oi.Book);

            // If not admin, filter by customer
            if (customerId != 1)
            {
                ordersQuery = ordersQuery.Where(o => o.CustomerId == customerId);
            }

            // Apply ordering after all filters
            var orders = await ordersQuery.OrderByDescending(o => o.OrderDate).ToListAsync();
            ViewBag.IsAdmin = customerId == 1;

            return View(orders);
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
                return RedirectToAction("Login", "Customers");
            }

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Orderitems)
                    .ThenInclude(oi => oi.Book)
                .FirstOrDefaultAsync(m => m.OrderId == id && (customerId == 1 || m.CustomerId == customerId));

            if (order == null)
            {
                return NotFound();
            }

            ViewBag.IsAdmin = customerId == 1;
            return View(order);
        }

        // POST: Orders/ConfirmOrder/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmOrder(int id)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Login", "Customers");
            }

            var order = await _context.Orders
                .Include(o => o.Orderitems)
                    .ThenInclude(oi => oi.Book)
                .FirstOrDefaultAsync(o => o.OrderId == id && (customerId == 1 || o.CustomerId == customerId));

            if (order == null)
            {
                return NotFound();
            }

            if (order.OrderStatus != "Saved")
            {
                TempData["ErrorMessage"] = "This order has already been confirmed.";
                return RedirectToAction(nameof(Details), new { id = order.OrderId });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Check stock availability for all items
                    foreach (var item in order.Orderitems)
                    {
                        if (item.Quantity > item.Book.Stock)
                        {
                            TempData["ErrorMessage"] = $"Not enough stock available for {item.Book.Title}";
                            return RedirectToAction(nameof(Details), new { id = order.OrderId });
                        }
                    }

                    // Update stock and confirm order
                    foreach (var item in order.Orderitems)
                    {
                        item.Book.Stock -= item.Quantity;
                        _context.Update(item.Book);
                    }

                    order.OrderStatus = "Confirmed";
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Order confirmed successfully.";
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "An error occurred while confirming the order.";
                }
            }

            return RedirectToAction(nameof(Details), new { id = order.OrderId });
        }

        // GET: Orders/Create
        public IActionResult Create()
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Login", "Customers");
            }

            ViewBag.Books = _context.Books.ToList();
            return View(new Order { CustomerId = customerId.Value });
        }

        public class OrderItemViewModel
        {
            public int BookId { get; set; }
            public int Quantity { get; set; }
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string action, List<OrderItemViewModel> orderItems)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Login", "Customers");
            }

            if (orderItems == null || !orderItems.Any(i => i.Quantity > 0))
            {
                ModelState.AddModelError("", "Please select at least one book");
                ViewBag.Books = _context.Books.ToList();
                return View(new Order { CustomerId = customerId.Value });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Get the next available OrderId
                    int nextOrderId = (await _context.Orders.MaxAsync(o => (int?)o.OrderId) ?? 0) + 1;

                    var order = new Order
                    {
                        OrderId = nextOrderId,
                        CustomerId = customerId.Value,
                        OrderDate = DateTime.Now,
                        OrderStatus = action == "Save" ? "Saved" : "Confirmed"
                    };

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    // Get the next available OrderItemId
                    int nextOrderItemId = (await _context.Orderitems.MaxAsync(oi => (int?)oi.OrderItemId) ?? 0) + 1;

                    foreach (var item in orderItems.Where(i => i.Quantity > 0))
                    {
                        var orderItem = new Orderitem
                        {
                            OrderItemId = nextOrderItemId++,
                            OrderId = order.OrderId,
                            BookId = item.BookId,
                            Quantity = item.Quantity
                        };

                        _context.Orderitems.Add(orderItem);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Order created successfully.";
                    return RedirectToAction(nameof(Details), new { id = order.OrderId });
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
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
                return RedirectToAction("Login", "Customers");
            }

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Orderitems)
                    .ThenInclude(oi => oi.Book)
                .FirstOrDefaultAsync(m => m.OrderId == id && (customerId == 1 || m.CustomerId == customerId));

            if (order == null)
            {
                return NotFound();
            }

            if (order.OrderStatus != "Saved")
            {
                TempData["ErrorMessage"] = "Only saved orders can be edited.";
                return RedirectToAction(nameof(History));
            }

            ViewBag.Books = await _context.Books.ToListAsync();
            return View(order);
        }

        // POST: Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, List<OrderItemViewModel> orderItems)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Login", "Customers");
            }

            var order = await _context.Orders
                .Include(o => o.Orderitems)
                .FirstOrDefaultAsync(o => o.OrderId == id && (customerId == 1 || o.CustomerId == customerId));

            if (order == null)
            {
                return NotFound();
            }

            if (order.OrderStatus != "Saved")
            {
                TempData["ErrorMessage"] = "Only saved orders can be edited.";
                return RedirectToAction(nameof(History));
            }

            if (orderItems == null || !orderItems.Any(i => i.Quantity > 0))
            {
                ModelState.AddModelError("", "Please select at least one book");
                ViewBag.Books = await _context.Books.ToListAsync();
                return View(order);
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Remove existing order items
                    _context.Orderitems.RemoveRange(order.Orderitems);
                    await _context.SaveChangesAsync();

                    // Get the next available OrderItemId
                    int nextOrderItemId = (await _context.Orderitems.MaxAsync(oi => (int?)oi.OrderItemId) ?? 0) + 1;

                    foreach (var item in orderItems.Where(i => i.Quantity > 0))
                    {
                        var orderItem = new Orderitem
                        {
                            OrderItemId = nextOrderItemId++,
                            OrderId = order.OrderId,
                            BookId = item.BookId,
                            Quantity = item.Quantity
                        };

                        _context.Orderitems.Add(orderItem);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Order updated successfully.";
                    return RedirectToAction(nameof(Details), new { id = order.OrderId });
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
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
                return RedirectToAction("Login", "Customers");
            }

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Orderitems)
                    .ThenInclude(oi => oi.Book)
                .FirstOrDefaultAsync(m => m.OrderId == id && (customerId == 1 || m.CustomerId == customerId));

            if (order == null)
            {
                return NotFound();
            }

            if (order.OrderStatus != "Saved")
            {
                TempData["ErrorMessage"] = "Only saved orders can be deleted.";
                return RedirectToAction(nameof(History));
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
                return RedirectToAction("Login", "Customers");
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var order = await _context.Orders
                        .Include(o => o.Orderitems)
                        .FirstOrDefaultAsync(m => m.OrderId == id && (customerId == 1 || m.CustomerId == customerId));

                    if (order == null)
                    {
                        return NotFound();
                    }

                    if (order.OrderStatus != "Saved")
                    {
                        TempData["ErrorMessage"] = "Only saved orders can be deleted.";
                        return RedirectToAction(nameof(History));
                    }

                    _context.Orderitems.RemoveRange(order.Orderitems);
                    _context.Orders.Remove(order);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Order deleted successfully.";
                    return RedirectToAction(nameof(History));
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrderId == id);
        }
    }
}