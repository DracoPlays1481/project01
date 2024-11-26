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
    public class BooksController : Controller
    {
        private readonly KardContext _context;

        public BooksController(KardContext context)
        {
            _context = context;
        }

        // GET: Books
        public async Task<IActionResult> Index()
        {
            var books = await _context.Books
                .FromSqlRaw("SELECT BookID, Title, Author, Genre, Price, Stock FROM BOOK")
                .ToListAsync();
            return View(books);
        }

        // GET: Books/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .FromSqlRaw("SELECT BookID, Title, Author, Genre, Price, Stock FROM BOOK WHERE BookID = {0}", id)
                .FirstOrDefaultAsync();

            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // GET: Books/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Books/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BookId,Title,Author,Genre,Price,Stock")] Book book)
        {
            if (ModelState.IsValid)
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO BOOK (BookID, Title, Author, Genre, Price, Stock) VALUES ({0}, {1}, {2}, {3}, {4}, {5})",
                    book.BookId, book.Title, book.Author, book.Genre, book.Price, book.Stock);
                return RedirectToAction(nameof(Index));
            }
            return View(book);
        }

        // GET: Books/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .FromSqlRaw("SELECT BookID, Title, Author, Genre, Price, Stock FROM BOOK WHERE BookID = {0}", id)
                .FirstOrDefaultAsync();

            if (book == null)
            {
                return NotFound();
            }
            return View(book);
        }

        // POST: Books/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BookId,Title,Author,Genre,Price,Stock")] Book book)
        {
            if (id != book.BookId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE BOOK SET Title = {1}, Author = {2}, Genre = {3}, Price = {4}, Stock = {5} WHERE BookID = {0}",
                        book.BookId, book.Title, book.Author, book.Genre, book.Price, book.Stock);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookExists(book.BookId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(book);
        }

        // GET: Books/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .FromSqlRaw("SELECT BookID, Title, Author, Genre, Price, Stock FROM BOOK WHERE BookID = {0}", id)
                .FirstOrDefaultAsync();

            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // POST: Books/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM BOOK WHERE BookID = {0}", id);
            return RedirectToAction(nameof(Index));
        }

        private bool BookExists(int id)
        {
            var book = _context.Books
                .FromSqlRaw("SELECT BookID, Title, Author, Genre, Price, Stock FROM BOOK WHERE BookID = {0}", id)
                .FirstOrDefault();
            return book != null;
        }
    }
}