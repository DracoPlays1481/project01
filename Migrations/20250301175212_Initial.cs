using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ESD_PROJECT.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    BookingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BookingFrom = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BookingTo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BookedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BookingStatus = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.BookingID);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bookings");
        }
    }
}
