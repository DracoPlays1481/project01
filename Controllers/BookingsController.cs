using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ESD_PROJECT.Data;

namespace ESD_PROJECT.Controllers
{
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
        public IActionResult GetAll()
        {
            return Ok(_context.Bookings);
        }

        [HttpGet("{BookingID}")]

        public IActionResult GetById(int? BookingID)
        {
            var bookings = _context.Bookings.FirstOrDefault(e => e.BookingID == BookingID);
            if (bookings == null)
                return Problem(detail: "Booking with id " + BookingID + " is not found.", statusCode: 404);

            return Ok(bookings);
        }
    }
}