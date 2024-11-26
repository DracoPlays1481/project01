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
            return await _context.Admins.AnyAsync(a => a.AdminId == HttpContext.Session.GetInt32("AdminId"));
        }

        private async Task<bool> CanModifyOrder(Order order)
        {
            if (order == null) return false;

            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null) return false;

            // Admin can modify any order
            if (await IsAdmin()) return true;

            // Customer can only modify their own saved orders
            return order.CustomerId == customerId && order.OrderStatus == "Saved";
        }

        // GET: Orders/History
        public async Task<IActionResult> History()
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction("Login", "Customers");
            }

            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Orderitems)
                    .ThenInclude(oi => oi.Book)
                .OrderByDescending(o => o.OrderDate);

            var orders = await IsAdmin()
                ? await query.ToListAsync()
                : await query.Where(o => o.CustomerId == customerId).ToListAsync();

            return View(orders);
        }

        // Rest of the OrdersController code remains the same, just replace any direct checks 
        // for CustomerId == 1 with await IsAdmin() calls
    }
}