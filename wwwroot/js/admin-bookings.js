// Admin Bookings Management
document.addEventListener('DOMContentLoaded', () => {
    // Initialize admin bookings tab
    const adminBookingsTab = document.getElementById('bookings-management');
    const adminBookingsTable = document.getElementById('admin-bookings-table');

    if (adminBookingsTab && adminBookingsTable) {
        // Add tab switching event for admin bookings
        const adminTabButtons = document.querySelectorAll('#admin-panel .tab-btn');
        adminTabButtons.forEach(button => {
            button.addEventListener('click', () => {
                if (button.dataset.tab === 'bookings-management') {
                    loadAdminBookings();
                }
            });
        });
    }
});

// Load bookings for admin panel
async function loadAdminBookings() {
    console.log('Loading bookings for admin panel...');
    const tableBody = document.querySelector('#admin-bookings-table tbody');
    if (!tableBody) return;

    tableBody.innerHTML = '<tr><td colspan="7" class="loading">Loading bookings...</td></tr>';

    try {
        const bookings = await BookingService.loadBookings();
        console.log('Admin bookings data received:', bookings);

        // Clear the loading message before rendering bookings
        tableBody.innerHTML = '';

        if (!bookings || bookings.length === 0) {
            tableBody.innerHTML = '<tr><td colspan="7">No bookings found.</td></tr>';
            return;
        }

        bookings.forEach(booking => {
            const row = document.createElement('tr');
            row.innerHTML = `
                < td >${booking.bookingID}</ td >
                < td >${booking.facilityDescription || 'N/A'}</ td >
                < td >${formatDate(booking.bookingDateFrom)}</ td >
                < td >${formatDate(booking.bookingDateTo)}</ td >
                < td >${booking.bookedBy || 'N/A'}</ td >
                < td >${booking.bookingStatus || 'N/A'}</ td >
                < td >
                    < button class= "action-btn admin-edit-btn" data - id = "${booking.bookingID}" >
                        < i class= "fas fa-edit" ></ i >
                    </ button >
                    < button class= "action-btn admin-delete-btn" data - id = "${booking.bookingID}" >
                        < i class= "fas fa-trash" ></ i >
                    </ button >
                </ td >
            `;
            tableBody.appendChild(row);
        });

        // Add event listeners to edit and delete buttons
        document.querySelectorAll('.admin-edit-btn').forEach(btn => {
            btn.addEventListener('click', () => editBooking(btn.dataset.id));
        });

        document.querySelectorAll('.admin-delete-btn').forEach(btn => {
            btn.addEventListener('click', () => deleteBooking(btn.dataset.id));
        });
    } catch (error) {
        console.error('Error rendering admin bookings:', error);
        tableBody.innerHTML = `< tr >< td colspan = "7" > Error loading bookings: ${error.message}</ td ></ tr >`;
    }
}

// Format date for display (reused from bookings.js)
function formatDate(dateString) {
    const options = {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    };
    return new Date(dateString).toLocaleDateString(undefined, options);
}