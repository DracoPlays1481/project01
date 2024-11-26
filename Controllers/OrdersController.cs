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

        private async Task<int> GenerateNewOrderId()
        {
            var maxId = await _context.Orders.MaxAsync(o => (int?)o.OrderId) ?? 0;
            return maxId + 1;
        }

        private async Task<int> GenerateNewOrderItemId()
        {
            var maxId = await _context.Orderitems.MaxAsync(oi => (int?)oi.OrderItemId) ?? 0;
            return maxId + 1;
        }

        public async Task<IActionResult> History()
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Orderitems)
                    .ThenInclude(oi => oi.Book)
                .OrderByDescending(o => o.OrderDate);

            // If customer is logged in, only show their orders
            if (customerId != null)
            {
                query = query.Where(o => o.CustomerId == customerId);
            }

            var orders = await query.ToListAsync();
            return View(orders);
        }

        public async Task<IActionResult> Details(int? id)
        {
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

            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId != null && order.CustomerId != customerId)
            {
                return RedirectToAction(nameof(History));
            }

            return View(order);
        }

        public async Task<IActionResult> Create()
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Login", "Customers");
            }

            ViewBag.Books = await _context.Books.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(List<OrderItemViewModel> orderItems, string action)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Login", "Customers");
            }

            if (ModelState.IsValid)
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var orderId = await GenerateNewOrderId();
                        var order = new Order
                        {
                            OrderId = orderId,
                            CustomerId = customerId.Value,
                            OrderDate = DateTime.Now,
                            OrderStatus = action == "Confirm" ? "Confirmed" : "Saved"
                        };

                        _context.Add(order);
                        await _context.SaveChangesAsync();

                        var itemsToAdd = orderItems.Where(i => i.Quantity > 0).ToList();
                        foreach (var item in itemsToAdd)
                        {
                            var book = await _context.Books.FindAsync(item.BookId);
                            if (book == null || (order.OrderStatus == "Confirmed" && book.Stock < item.Quantity))
                            {
                                throw new InvalidOperationException($"Insufficient stock for book: {book?.Title ?? "Unknown"}");
                            }

                            var orderItem = new Orderitem
                            {
                                OrderItemId = await GenerateNewOrderItemId(),
                                OrderId = order.OrderId,
                                BookId = item.BookId,
                                Quantity = item.Quantity
                            };

                            _context.Add(orderItem);

                            if (order.OrderStatus == "Confirmed")
                            {
                                book.Stock -= item.Quantity;
                                _context.Update(book);
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

        public async Task<IActionResult> Edit(int? id)
        {
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

            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId != null && order.CustomerId != customerId)
            {
                return RedirectToAction(nameof(History));
            }

            if (order.OrderStatus != "Saved")
            {
                TempData["ErrorMessage"] = "Only saved orders can be edited.";
                return RedirectToAction(nameof(Details), new { id = order.OrderId });
            }

            ViewBag.Books = await _context.Books.ToListAsync();
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, List<OrderItemViewModel> orderItems)
        {
            var order = await _context.Orders
                .Include(o => o.Orderitems)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId != null && order.CustomerId != customerId)
            {
                return RedirectToAction(nameof(History));
            }

            if (order.OrderStatus != "Saved")
            {
                TempData["ErrorMessage"] = "Only saved orders can be edited.";
                return RedirectToAction(nameof(Details), new { id = order.OrderId });
            }

            if (ModelState.IsValid)
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Remove existing items
                        await _context.Database.ExecuteSqlRawAsync("DELETE FROM ORDERITEM WHERE OrderID = {0}", id);

                        // Add new items
                        foreach (var item in orderItems.Where(i => i.Quantity > 0))
                        {
                            var orderItem = new Orderitem
                            {
                                OrderItemId = await GenerateNewOrderItemId(),
                                OrderId = order.OrderId,
                                BookId = item.BookId,
                                Quantity = item.Quantity
                            };
                            _context.Add(orderItem);
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        TempData["SuccessMessage"] = "Order updated successfully.";
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

        public async Task<IActionResult> Delete(int? id)
        {
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

            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId != null && order.CustomerId != customerId)
            {
                return RedirectToAction(nameof(History));
            }

            if (order.OrderStatus != "Saved")
            {
                TempData["ErrorMessage"] = "Only saved orders can be deleted.";
                return RedirectToAction(nameof(Details), new { id = order.OrderId });
            }

            return View(order);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Orderitems)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId != null && order.CustomerId != customerId)
            {
                return RedirectToAction(nameof(History));
            }

            if (order.OrderStatus != "Saved")
            {
                TempData["ErrorMessage"] = "Only saved orders can be deleted.";
                return RedirectToAction(nameof(Details), new { id = order.OrderId });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    await _context.Database.ExecuteSqlRawAsync("DELETE FROM ORDERITEM WHERE OrderID = {0}", id);
                    await _context.Database.ExecuteSqlRawAsync("DELETE FROM [ORDER] WHERE OrderID = {0}", id);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Order deleted successfully.";
                    return RedirectToAction(nameof(History));
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Error deleting order.";
                    return RedirectToAction(nameof(Delete), new { id = order.OrderId });
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Orderitems)
                    .ThenInclude(oi => oi.Book)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId != null && order.CustomerId != customerId)
            {
                return RedirectToAction(nameof(History));
            }

            if (order.OrderStatus != "Saved")
            {
                TempData["ErrorMessage"] = "Only saved orders can be confirmed.";
                return RedirectToAction(nameof(Details), new { id = order.OrderId });
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