using System.ComponentModel.DataAnnotations;

namespace ESD_PROJECT.Models
{
    public class Booking
    {
        [Key]
        public int BookingID { get; set; }

        public string? BookingDescription { get; set; }

        public string? BookingFrom { get; set; }

        public string? BookingTo { get; set; }

        public string? BookedBy { get; set; }

        public string? BookingStatus { get; set; }
    }
}
