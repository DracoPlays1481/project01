using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ESD_PROJECT.Data;
using ESD_PROJECT.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace ESD_PROJECT.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public BookingsController(ApplicationDBContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Member")]
        public async Task<ActionResult<IEnumerable<Booking>>> GetAll()
        {
            return await _context.Bookings.ToListAsync();
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Member")]
        public async Task<ActionResult<Booking>> GetById(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);

            if (booking == null)
                return NotFound(new { message = $"Booking with ID {id} not found." });

            return booking;
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Member")]
        public async Task<ActionResult<Booking>> Create(Booking booking)
        {
            // Set the BookedBy field to the current user if not provided
            if (string.IsNullOrEmpty(booking.BookedBy))
            {
                booking.BookedBy = User.Identity.Name;
            }

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = booking.BookingID }, booking);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Member")]
        public async Task<IActionResult> Update(int id, Booking booking)
        {
            if (id != booking.BookingID)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            var existingBooking = await _context.Bookings.FindAsync(id);
            if (existingBooking == null)
            {
                return NotFound(new { message = $"Booking with ID {id} not found." });
            }

            // Update the booking properties
            existingBooking.FacilityDescription = booking.FacilityDescription;
            existingBooking.BookingDateFrom = booking.BookingDateFrom;
            existingBooking.BookingDateTo = booking.BookingDateTo;
            existingBooking.BookedBy = booking.BookedBy;
            existingBooking.BookingStatus = booking.BookingStatus;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BookingExists(id))
                {
                    return NotFound(new { message = $"Booking with ID {id} not found." });
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Member")]
        public async Task<IActionResult> Delete(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound(new { message = $"Booking with ID {id} not found." });
            }

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.BookingID == id);
        }
    }
}