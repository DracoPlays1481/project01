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
    public class CustomersController : Controller
    {
        private readonly KardContext _context;

        public CustomersController(KardContext context)
        {
            _context = context;
        }

        // GET: Customers
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetInt32("CustomerId") != null)
            {
                return RedirectToAction(nameof(CustomersList));
            }
            return View("Login");
        }

        // GET: CustomersList
        public async Task<IActionResult> CustomersList()
        {
            var customers = await _context.Customers.ToListAsync();
            return View(customers);
        }

        // GET: Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([Bind("Name,Password,Address,Phone,Email")] Customer customer)
        {
            if (ModelState.IsValid)
            {
                try
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

                    TempData["SuccessMessage"] = "Registration successful! Please login with your credentials.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred during registration. Please try again.");
                }
            }
            return View(customer);
        }

        // POST: Login
        [HttpPost]
        public async Task<IActionResult> Login(Customer login)
        {
            try
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.CustomerId == login.CustomerId &&
                                            c.Password == login.Password);

                if (customer != null)
                {
                    HttpContext.Session.SetInt32("CustomerId", customer.CustomerId);
                    HttpContext.Session.SetString("CustomerName", customer.Name);
                    return RedirectToAction(nameof(CustomersList));
                }

                ModelState.AddModelError("", "Invalid login credentials");
                return View("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred during login");
                return View("Login");
            }
        }

        // POST: Customers/Logout
        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        // GET: Customers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(m => m.CustomerId == id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // GET: Customers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CustomerId,Password,Name,Address,Phone,Email")] Customer customer)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.Customers.AnyAsync(c => c.CustomerId == customer.CustomerId))
                    {
                        ModelState.AddModelError("CustomerId", "This Customer ID is already in use");
                        return View(customer);
                    }

                    _context.Add(customer);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(CustomersList));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while creating the customer.");
                }
            }
            return View(customer);
        }

        // GET: Customers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
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
            if (ModelState.IsValid)
            {
                try
                {
                    // Load the current state of the customer from the database
                    var existingCustomer = await _context.Customers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.CustomerId == id);

                    if (existingCustomer == null)
                    {
                        return NotFound();
                    }

                    // Check if the new CustomerId already exists (only if it's different from the current one)
                    if (customer.CustomerId != id && await _context.Customers.AnyAsync(c => c.CustomerId == customer.CustomerId))
                    {
                        ModelState.AddModelError("CustomerId", "This Customer ID is already in use");
                        return View(customer);
                    }

                    // Update session if the logged-in user is updating their own ID
                    if (HttpContext.Session.GetInt32("CustomerId") == id)
                    {
                        HttpContext.Session.SetInt32("CustomerId", customer.CustomerId);
                    }

                    // Update the customer
                    _context.Entry(customer).State = EntityState.Modified;
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(CustomersList));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CustomerExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        // Handle concurrency conflict
                        ModelState.AddModelError("", "The record has been modified by another user. Please refresh and try again.");
                        return View(customer);
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while updating the customer.");
                }
            }
            return View(customer);
        }

        // GET: Customers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(m => m.CustomerId == id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // POST: Customers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(id);
                if (customer != null)
                {
                    // Check if the user is trying to delete their own account
                    if (HttpContext.Session.GetInt32("CustomerId") == id)
                    {
                        HttpContext.Session.Clear();
                    }

                    _context.Customers.Remove(customer);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(CustomersList));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while deleting the customer.");
                return View("Delete", await _context.Customers.FindAsync(id));
            }
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.CustomerId == id);
        }
    }
}