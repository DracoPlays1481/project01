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

        private async Task<bool> IsAdmin()
        {
            var adminId = HttpContext.Session.GetInt32("AdminId");
            return adminId != null && await _context.Admins.AnyAsync(a => a.AdminId == adminId);
        }

        private async Task<bool> IsAuthenticated()
        {
            return await IsAdmin() || HttpContext.Session.GetInt32("CustomerId") != null;
        }

        private IActionResult RedirectToLogin()
        {
            // If there's an admin ID in the session, redirect to admin login
            if (HttpContext.Session.GetInt32("AdminId") != null)
            {
                return RedirectToAction("Login", "Admins");
            }
            // Otherwise redirect to customer login
            return RedirectToAction("Login", "Customers");
        }

        private async Task<bool> CanModifyOrder(Order order)
        {
            if (order == null) return false;

            // Admin can modify any order
            if (await IsAdmin()) return true;

            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null) return false;

            // Customer can only modify their own saved orders
            return order.CustomerId == customerId && order.OrderStatus == "Saved";
        }

        // GET: Orders/History
        public async Task<IActionResult> History()
        {
            if (!await IsAuthenticated())
            {
                return RedirectToLogin();
            }

            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Orderitems)
                    .ThenInclude(oi => oi.Book)
                .OrderByDescending(o => o.OrderDate);

            var isAdmin = await IsAdmin();
            var customerId = HttpContext.Session.GetInt32("CustomerId");

            var orders = isAdmin
                ? await query.ToListAsync()
                : await query.Where(o => o.CustomerId == customerId).ToListAsync();

            return View(orders);
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (!await IsAuthenticated())
            {
                return RedirectToLogin();
            }

            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Orderitems)
                    .ThenInclude(oi => oi.Book)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            // Check if user has permission to view this order
            var isAdmin = await IsAdmin();
            var customerId = HttpContext.Session.GetInt32("CustomerId");

            if (!isAdmin && order.CustomerId != customerId)
            {
                return RedirectToAction(nameof(History));
            }

            return View(order);
        }

        // GET: Orders/Create
        public async Task<IActionResult> Create()
        {
            if (!await IsAuthenticated())
            {
                return RedirectToLogin();
            }

            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (!await IsAdmin() && customerId == null)
            {
                return RedirectToAction("Login", "Customers");
            }

            ViewBag.Books = await _context.Books.ToListAsync();
            return View();
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(List<OrderItemViewModel> orderItems, string action)
        {
            if (!await IsAuthenticated())
            {
                return RedirectToLogin();
            }

            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (!await IsAdmin() && customerId == null)
            {
                return RedirectToAction("Login", "Customers");
            }

            if (ModelState.IsValid)
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var order = new Order
                        {
                            CustomerId = customerId.Value,
                            OrderDate = DateTime.Now,
                            OrderStatus = action == "Confirm" ? "Confirmed" : "Saved"
                        };

                        _context.Add(order);
                        await _context.SaveChangesAsync();

                        foreach (var item in orderItems.Where(i => i.Quantity > 0))
                        {
                            var orderItem = new Orderitem
                            {
                                OrderId = order.OrderId,
                                BookId = item.BookId,
                                Quantity = item.Quantity
                            };

                            _context.Add(orderItem);

                            if (order.OrderStatus == "Confirmed")
                            {
                                var book = await _context.Books.FindAsync(item.BookId);
                                if (book != null)
                                {
                                    if (book.Stock < item.Quantity)
                                    {
                                        throw new InvalidOperationException($"Insufficient stock for book: {book.Title}");
                                    }
                                    book.Stock -= item.Quantity;
                                    _context.Update(book);
                                }
                            }
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return RedirectToAction(nameof(Details), new { id = order.OrderId });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("", $"Error creating order: {ex.Message}");
                    }
                }
            }

            ViewBag.Books = await _context.Books.ToListAsync();
            return View();
        }

        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!await IsAuthenticated())
            {
                return RedirectToLogin();
            }

            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Orderitems)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            if (!await CanModifyOrder(order))
            {
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
            if (!await IsAuthenticated())
            {
                return RedirectToLogin();
            }

            var order = await _context.Orders
                .Include(o => o.Orderitems)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            if (!await CanModifyOrder(order))
            {
                return RedirectToAction(nameof(History));
            }

            if (ModelState.IsValid)
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Remove existing order items
                        _context.Orderitems.RemoveRange(order.Orderitems);

                        // Add new order items
                        foreach (var item in orderItems.Where(i => i.Quantity > 0))
                        {
                            var orderItem = new Orderitem
                            {
                                OrderId = order.OrderId,
                                BookId = item.BookId,
                                Quantity = item.Quantity
                            };

                            _context.Add(orderItem);
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return RedirectToAction(nameof(Details), new { id = order.OrderId });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("", $"Error updating order: {ex.Message}");
                    }
                }
            }

            ViewBag.Books = await _context.Books.ToListAsync();
            return View(order);
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!await IsAuthenticated())
            {
                return RedirectToLogin();
            }

            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Orderitems)
                    .ThenInclude(oi => oi.Book)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            if (!await CanModifyOrder(order))
            {
                return RedirectToAction(nameof(History));
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!await IsAuthenticated())
            {
                return RedirectToLogin();
            }

            var order = await _context.Orders
                .Include(o => o.Orderitems)
                    .ThenInclude(oi => oi.Book)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            if (!await CanModifyOrder(order))
            {
                return RedirectToAction(nameof(History));
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    if (order.OrderStatus == "Confirmed")
                    {
                        // Restore book stock for confirmed orders
                        foreach (var item in order.Orderitems)
                        {
                            if (item.Book != null)
                            {
                                item.Book.Stock += item.Quantity;
                                _context.Update(item.Book);
                            }
                        }
                    }

                    _context.Orderitems.RemoveRange(order.Orderitems);
                    _context.Orders.Remove(order);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return RedirectToAction(nameof(History));
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    return RedirectToAction(nameof(Delete), new { id = order.OrderId });
                }
            }
        }

        // POST: Orders/ConfirmOrder/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmOrder(int id)
        {
            if (!await IsAuthenticated())
            {
                return RedirectToLogin();
            }

            var order = await _context.Orders
                .Include(o => o.Orderitems)
                    .ThenInclude(oi => oi.Book)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            if (!await CanModifyOrder(order))
            {
                return RedirectToAction(nameof(History));
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    foreach (var item in order.Orderitems)
                    {
                        if (item.Book != null)
                        {
                            if (item.Book.Stock < item.Quantity)
                            {
                                throw new InvalidOperationException($"Insufficient stock for book: {item.Book.Title}");
                            }
                            item.Book.Stock -= item.Quantity;
                            _context.Update(item.Book);
                        }
                    }

                    order.OrderStatus = "Confirmed";
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Order confirmed successfully.";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = $"Error confirming order: {ex.Message}";
                }
            }

            return RedirectToAction(nameof(Details), new { id = order.OrderId });
        }
    }

    public class OrderItemViewModel
    {
        public int BookId { get; set; }
        public int Quantity { get; set; }
    }
}