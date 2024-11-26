using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EWDProject.Models;

namespace EWDProject.Controllers
{
    public class AdminsController : Controller
    {
        private readonly KardContext _context;

        public AdminsController(KardContext context)
        {
            _context = context;
        }

        // GET: Admins/Login
        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("AdminId") != null)
            {
                return RedirectToAction(nameof(Dashboard));
            }
            return View();
        }

        // POST: Admins/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(Admin loginModel)
        {
            if (string.IsNullOrEmpty(loginModel.Email) || string.IsNullOrEmpty(loginModel.Password))
            {
                ModelState.AddModelError("", "Email and password are required");
                return View(loginModel);
            }

            var admin = await _context.Admin
                .FirstOrDefaultAsync(a => a.Email == loginModel.Email &&
                                        a.Password == loginModel.Password);

            if (admin != null)
            {
                HttpContext.Session.SetInt32("AdminId", admin.AdminId);
                HttpContext.Session.SetString("AdminName", admin.Name);
                return RedirectToAction(nameof(Dashboard));
            }

            ModelState.AddModelError("", "Invalid email or password");
            return View(loginModel);
        }

        // GET: Admins/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var admin = await _context.Admin.FindAsync(adminId);
            if (admin == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction(nameof(Login));
            }

            // Calculate statistics for the dashboard
            ViewBag.Statistics = new
            {
                TotalBooks = await _context.Books.CountAsync(),
                TotalCustomers = await _context.Customers.CountAsync(),
                TotalOrders = await _context.Orders.CountAsync(),
                TotalRevenue = await _context.Orders
                    .Where(o => o.OrderStatus == "Confirmed")
                    .SelectMany(o => o.Orderitems)
                    .SumAsync(oi => oi.Book.Price * oi.Quantity)
            };

            return View(admin);
        }

        // POST: Admins/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        // GET: Admins/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId != id)
            {
                return RedirectToAction(nameof(Dashboard));
            }

            var admin = await _context.Admin.FindAsync(id);
            if (admin == null)
            {
                return NotFound();
            }
            return View(admin);
        }

        // POST: Admins/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AdminId,Password,Name,Address,Phone,Email")] Admin admin)
        {
            if (id != admin.AdminId)
            {
                return NotFound();
            }

            var adminId = HttpContext.Session.GetInt32("AdminId");
            if (adminId != id)
            {
                return RedirectToAction(nameof(Dashboard));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingAdmin = await _context.Admin
                        .AsNoTracking()
                        .FirstOrDefaultAsync(a => a.Email == admin.Email && a.AdminId != admin.AdminId);

                    if (existingAdmin != null)
                    {
                        ModelState.AddModelError("Email", "This email is already in use");
                        return View(admin);
                    }

                    _context.Update(admin);
                    await _context.SaveChangesAsync();

                    // Update session if name changed
                    if (HttpContext.Session.GetString("AdminName") != admin.Name)
                    {
                        HttpContext.Session.SetString("AdminName", admin.Name);
                    }

                    return RedirectToAction(nameof(Dashboard));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AdminExists(admin.AdminId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(admin);
        }

        private bool AdminExists(int id)
        {
            return _context.Admin.Any(e => e.AdminId == id);
        }
    }
}