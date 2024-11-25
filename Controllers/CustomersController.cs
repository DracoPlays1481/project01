﻿using System;
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

        // GET: Customers/Create
        public IActionResult Create()
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId != 1)
            {
                return RedirectToAction(nameof(Dashboard));
            }
            return View();
        }

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Password,Address,Phone,Email")] Customer customer)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId != 1)
            {
                return RedirectToAction(nameof(Dashboard));
            }

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

                return RedirectToAction(nameof(CustomersList));
            }
            return View(customer);
        }

        // GET: Customers/CustomersList
        public async Task<IActionResult> CustomersList()
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId != 1)
            {
                return RedirectToAction(nameof(Dashboard));
            }

            return View(await _context.Customers.ToListAsync());
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

        // GET: Customers
        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("CustomerId") != null)
            {
                return RedirectToAction(nameof(Dashboard));
            }
            return RedirectToAction(nameof(Login));
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
            if (customerId != id && customerId != 1)
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
            if (customerId != id && customerId != 1)
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

                    // Update session if name changed and it's the current user
                    if (customerId == customer.CustomerId && HttpContext.Session.GetString("CustomerName") != customer.Name)
                    {
                        HttpContext.Session.SetString("CustomerName", customer.Name);
                    }

                    if (customerId == 1)
                    {
                        return RedirectToAction(nameof(CustomersList));
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

        // GET: Customers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId != id && customerId != 1)
            {
                return RedirectToAction(nameof(Dashboard));
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
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId != id && customerId != 1)
            {
                return RedirectToAction(nameof(Dashboard));
            }

            var customer = await _context.Customers.FindAsync(id);
            if (customer != null)
            {
                if (customer.CustomerId == 1)
                {
                    return RedirectToAction(nameof(Dashboard));
                }

                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();

                if (customerId == id)
                {
                    HttpContext.Session.Clear();
                    return RedirectToAction(nameof(Login));
                }
            }

            return RedirectToAction(nameof(CustomersList));
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.CustomerId == id);
        }
    }
}