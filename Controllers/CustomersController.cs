using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EWDProject.Models;

namespace EWDProject.Controllers
{
    public class CustomersController : Controller
    {
        private readonly KardContext _context;

        public CustomersController(KardContext context)
        {
            _context = context;
        }

        // GET: Customers/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var customer = await _context.Customers
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customer == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction(nameof(Login));
            }

            return View(customer);
        }

        // GET: Customers/Login
        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("CustomerId") != null)
            {
                return RedirectToAction(nameof(Dashboard));
            }
            return View();
        }

        // POST: Customers/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(Customer loginModel)
        {
            if (string.IsNullOrEmpty(loginModel.Email) || string.IsNullOrEmpty(loginModel.Password))
            {
                ModelState.AddModelError("", "Email and password are required");
                return View(loginModel);
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == loginModel.Email &&
                                        c.Password == loginModel.Password);

            if (customer != null)
            {
                HttpContext.Session.SetInt32("CustomerId", customer.CustomerId);
                HttpContext.Session.SetString("CustomerName", customer.Name);
                return RedirectToAction(nameof(Dashboard));
            }

            ModelState.AddModelError("", "Invalid email or password");
            return View(loginModel);
        }

        // GET: Customers/Register
        public IActionResult Register()
        {
            if (HttpContext.Session.GetInt32("CustomerId") != null)
            {
                return RedirectToAction(nameof(Dashboard));
            }
            return View();
        }

        // POST: Customers/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([Bind("Name,Password,Address,Phone,Email")] Customer customer)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Customers.AnyAsync(c => c.Email == customer.Email))
                {
                    ModelState.AddModelError("Email", "This email is already registered");
                    return View(customer);
                }

                int nextCustomerId = await _context.Customers
                    .Select(c => c.CustomerId)
                    .DefaultIfEmpty()
                    .MaxAsync() + 1;

                customer.CustomerId = nextCustomerId;
                _context.Add(customer);
                await _context.SaveChangesAsync();

                // Automatically log in the user after registration
                HttpContext.Session.SetInt32("CustomerId", customer.CustomerId);
                HttpContext.Session.SetString("CustomerName", customer.Name);

                return RedirectToAction(nameof(Dashboard));
            }
            return View(customer);
        }

        // POST: Customers/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        // GET: Customers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId != id)
            {
                return RedirectToAction(nameof(Dashboard));
            }

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // POST: Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CustomerId,Password,Name,Address,Phone,Email")] Customer customer)
        {
            if (id != customer.CustomerId)
            {
                return NotFound();
            }

            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId != id)
            {
                return RedirectToAction(nameof(Dashboard));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingCustomer = await _context.Customers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Email == customer.Email && c.CustomerId != customer.CustomerId);

                    if (existingCustomer != null)
                    {
                        ModelState.AddModelError("Email", "This email is already in use");
                        return View(customer);
                    }

                    _context.Update(customer);
                    await _context.SaveChangesAsync();

                    // Update session if name changed
                    if (HttpContext.Session.GetString("CustomerName") != customer.Name)
                    {
                        HttpContext.Session.SetString("CustomerName", customer.Name);
                    }

                    return RedirectToAction(nameof(Dashboard));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CustomerExists(customer.CustomerId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(customer);
        }

        // GET: Customers
        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("CustomerId") != null)
            {
                return RedirectToAction(nameof(Dashboard));
            }
            return RedirectToAction(nameof(Login));
        }

        // POST: Customers/DeleteAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null || customerId != id)
            {
                return RedirectToAction(nameof(Login));
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var customer = await _context.Customers
                        .Include(c => c.Orders)
                            .ThenInclude(o => o.Orderitems)
                        .FirstOrDefaultAsync(c => c.CustomerId == id);

                    if (customer != null)
                    {
                        // Delete all order items for each order
                        foreach (var order in customer.Orders)
                        {
                            _context.Orderitems.RemoveRange(order.Orderitems);
                        }

                        // Delete all orders
                        _context.Orders.RemoveRange(customer.Orders);

                        // Delete the customer
                        _context.Customers.Remove(customer);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        // Clear the session
                        HttpContext.Session.Clear();
                        return RedirectToAction(nameof(Login));
                    }
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "An error occurred while deleting your account. Please try again.";
                }
            }

            return RedirectToAction(nameof(Dashboard));
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.CustomerId == id);
        }
    }
}