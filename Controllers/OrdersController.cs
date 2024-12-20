﻿using System;
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

        private bool IsAdmin()
        {
            return HttpContext.Session.GetInt32("AdminId") != null;
        }

        private bool CanModifyOrder(Order order)
        {
            // Check if Admin
            if (HttpContext.Session.GetInt32("AdminId") != null)
            {
                return true; // Admins can modify any order
            }

            // Check if Customer
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null || order == null)
            {
                return false; // Customer is not logged in or order is null
            }

            // Customer can only modify their own orders that are in "Saved" status
            return order.CustomerId == customerId && string.Equals(order.OrderStatus, "Saved", StringComparison.OrdinalIgnoreCase);
        }

        // GET: Orders/History
        public async Task<IActionResult> History()
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            var adminId = HttpContext.Session.GetInt32("AdminId");

            // Check if user is neither logged in as admin nor as customer
            if (customerId == null && adminId == null)
            {
                return RedirectToAction("Login", "Admins");
            }

            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Orderitems)
                    .ThenInclude(oi => oi.Book)
                .OrderByDescending(o => o.OrderDate);

            // Show all orders for admin, only customer's orders for regular users
            var orders = IsAdmin() || customerId == 1
                ? await query.ToListAsync()
                : await query.Where(o => o.CustomerId == customerId).ToListAsync();

            return View(orders);
        }

        // GET: Orders/Details/5
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

            // Check if user has permission to view this order
            if (!IsAdmin() && HttpContext.Session.GetInt32("CustomerId") != 1
                && order.CustomerId != HttpContext.Session.GetInt32("CustomerId"))
            {
                return RedirectToAction(nameof(History));
            }

            return View(order);
        }

        // GET: Orders/Create
        public IActionResult Create()
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null && !IsAdmin())
            {
                return RedirectToAction("Login", "Customers");
            }

            ViewBag.Books = _context.Books.ToList();
            return View(new Order { CustomerId = customerId ?? 1 });
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
            if (customerId == null && !IsAdmin())
            {
                return RedirectToAction("Login", "Customers");
            }

            if (orderItems == null || !orderItems.Any(i => i.Quantity > 0))
            {
                ModelState.AddModelError("", "Please select at least one book");
                ViewBag.Books = _context.Books.ToList();
                return View(new Order { CustomerId = customerId ?? 1 });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    int nextOrderId = (await _context.Orders.MaxAsync(o => (int?)o.OrderId) ?? 0) + 1;

                    var order = new Order
                    {
                        OrderId = nextOrderId,
                        CustomerId = customerId ?? 1,
                        OrderDate = DateTime.Now,
                        OrderStatus = action == "Save" ? "Saved" : "Confirmed"
                    };

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    int nextOrderItemId = (await _context.Orderitems.MaxAsync(oi => (int?)oi.OrderItemId) ?? 0) + 1;

                    foreach (var item in orderItems.Where(i => i.Quantity > 0))
                    {
                        var book = await _context.Books.FindAsync(item.BookId);
                        if (book == null || item.Quantity > book.Stock)
                        {
                            throw new InvalidOperationException($"Insufficient stock for book: {book?.Title ?? "Unknown"}");
                        }

                        var orderItem = new Orderitem
                        {
                            OrderItemId = nextOrderItemId++,
                            OrderId = order.OrderId,
                            BookId = item.BookId,
                            Quantity = item.Quantity
                        };

                        if (action == "Confirm")
                        {
                            book.Stock -= item.Quantity;
                            _context.Update(book);
                        }

                        _context.Orderitems.Add(orderItem);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Order created successfully.";
                    return RedirectToAction(nameof(Details), new { id = order.OrderId });
                }
                catch (InvalidOperationException ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", ex.Message);
                    ViewBag.Books = _context.Books.ToList();
                    return View(new Order { CustomerId = customerId ?? 1 });
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
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
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound(); // Order does not exist
            }

            if (!CanModifyOrder(order))
            {
                TempData["ErrorMessage"] = "You don't have permission to modify this order.";
                return RedirectToAction(nameof(History));
            }

            if (order.OrderStatus != "Saved")
            {
                TempData["ErrorMessage"] = "Only saved orders can be confirmed.";
                return RedirectToAction(nameof(Details), new { id = order.OrderId });
            }

            // Confirm the order
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    foreach (var item in order.Orderitems)
                    {
                        if (item.Book == null || item.Quantity > item.Book.Stock)
                        {
                            throw new InvalidOperationException($"Insufficient stock for book: {item.Book?.Title ?? "Unknown"}");
                        }

                        item.Book.Stock -= item.Quantity;
                        _context.Update(item.Book);
                    }

                    order.OrderStatus = "Confirmed";
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Order confirmed successfully.";
                    return RedirectToAction(nameof(Details), new { id = order.OrderId });
                }
                catch (InvalidOperationException ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = ex.Message;
                    return RedirectToAction(nameof(Details), new { id = id });
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

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Orderitems)
                    .ThenInclude(oi => oi.Book)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            if (!CanModifyOrder(order))
            {
                TempData["ErrorMessage"] = "You don't have permission to edit this order.";
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
            var order = await _context.Orders
                .Include(o => o.Orderitems)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            if (!CanModifyOrder(order))
            {
                TempData["ErrorMessage"] = "You don't have permission to edit this order.";
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

                    int nextOrderItemId = (await _context.Orderitems.MaxAsync(oi => (int?)oi.OrderItemId) ?? 0) + 1;

                    // Add new order items
                    foreach (var item in orderItems.Where(i => i.Quantity > 0))
                    {
                        var book = await _context.Books.FindAsync(item.BookId);
                        if (book == null || item.Quantity > book.Stock)
                        {
                            throw new InvalidOperationException($"Insufficient stock for book: {book?.Title ?? "Unknown"}");
                        }

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
                catch (InvalidOperationException ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", ex.Message);
                    ViewBag.Books = await _context.Books.ToListAsync();
                    return View(order);
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

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Orderitems)
                    .ThenInclude(oi => oi.Book)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            if (!CanModifyOrder(order))
            {
                TempData["ErrorMessage"] = "You don't have permission to delete this order.";
                return RedirectToAction(nameof(History));
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var order = await _context.Orders
                        .Include(o => o.Orderitems)
                            .ThenInclude(oi => oi.Book)
                        .FirstOrDefaultAsync(m => m.OrderId == id);

                    if (order == null)
                    {
                        return NotFound();
                    }

                    if (!CanModifyOrder(order))
                    {
                        TempData["ErrorMessage"] = "You don't have permission to delete this order.";
                        return RedirectToAction(nameof(History));
                    }

                    // If order was confirmed, restore book stock
                    if (order.OrderStatus == "Confirmed")
                    {
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