// Booking Service
const BookingService = {
    async loadBookings() {
        try {
            console.log('Loading bookings...');
            const response = await fetch('/api/Bookings', {
                headers: {
                    ...AuthService.getAuthHeader()
                }
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Failed to load bookings');
            }

            const bookings = await response.json();
            console.log('Bookings loaded:', bookings);
            return bookings;
        } catch (error) {
            console.error('Error loading bookings:', error);
            showToast(error.message, 'error');
            return [];
        }
    },

    async getBooking(id) {
        try {
            const response = await fetch(`/api/Bookings/${id}`, {
                headers: {
                    ...AuthService.getAuthHeader()
                }
            });

            if (!response.ok) {
                throw new Error('Failed to load booking details');
            }

            return await response.json();
        } catch (error) {
            showToast(error.message, 'error');
            return null;
        }
    },

    async createBooking(bookingData) {
        try {
            const response = await fetch('/api/Bookings', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    ...AuthService.getAuthHeader()
                },
                body: JSON.stringify(bookingData)
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Failed to create booking');
            }

            return { success: true, data: await response.json() };
        } catch (error) {
            return { success: false, error: error.message };
        }
    },

    async updateBooking(id, bookingData) {
        try {
            const response = await fetch(`/api/Bookings/${id}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    ...AuthService.getAuthHeader()
                },
                body: JSON.stringify(bookingData)
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Failed to update booking');
            }

            return { success: true };
        } catch (error) {
            return { success: false, error: error.message };
        }
    },

    async deleteBooking(id) {
        try {
            const response = await fetch(`/api/Bookings/${id}`, {
                method: 'DELETE',
                headers: {
                    ...AuthService.getAuthHeader()
                }
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Failed to delete booking');
            }

            return { success: true };
        } catch (error) {
            return { success: false, error: error.message };
        }
    }
};

// DOM Elements
const bookingsTable = document.getElementById('bookings-table');
const bookingForm = document.getElementById('booking-form');
const bookingFormContainer = document.getElementById('booking-form-container');
const newBookingBtn = document.getElementById('new-booking-btn');
const closeModalBtn = document.querySelector('.close');
const formTitle = document.getElementById('form-title');

// Format date for display
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

// Format date for input fields
function formatDateForInput(dateString) {
    const date = new Date(dateString);
    return date.toISOString().slice(0, 16);
}

// Render bookings table
async function renderBookings() {
    console.log('Rendering bookings table...');
    const tableBody = bookingsTable.querySelector('tbody');
    tableBody.innerHTML = '<tr><td colspan="7" class="loading">Loading bookings...</td></tr>';

    try {
        const bookings = await BookingService.loadBookings();
        console.log('Bookings data received:', bookings);

        // Clear the loading message before rendering bookings
        tableBody.innerHTML = '';

        if (!bookings || bookings.length === 0) {
            tableBody.innerHTML = '<tr><td colspan="7">No bookings found.</td></tr>';
            return;
        }

        bookings.forEach(booking => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td>${booking.bookingID}</td>
                <td>${booking.facilityDescription || 'N/A'}</td>
                <td>${formatDate(booking.bookingDateFrom)}</td>
                <td>${formatDate(booking.bookingDateTo)}</td>
                <td>${booking.bookedBy || 'N/A'}</td>
                <td>${booking.bookingStatus || 'N/A'}</td>
                <td>
                    <button class="action-btn edit-btn" data-id="${booking.bookingID}">
                        <i class="fas fa-edit"></i>
                    </button>
                    <button class="action-btn delete-btn" data-id="${booking.bookingID}">
                        <i class="fas fa-trash"></i>
                    </button>
                </td>
            `;
            tableBody.appendChild(row);
        });

        // Add event listeners to edit and delete buttons
        document.querySelectorAll('.edit-btn').forEach(btn => {
            btn.addEventListener('click', () => editBooking(btn.dataset.id));
        });

        document.querySelectorAll('.delete-btn').forEach(btn => {
            btn.addEventListener('click', () => deleteBooking(btn.dataset.id));
        });
    } catch (error) {
        console.error('Error rendering bookings:', error);
        tableBody.innerHTML = `<tr><td colspan="7">Error loading bookings: ${error.message}</td></tr>`;
    }
}

// Open form for new booking
function openNewBookingForm() {
    formTitle.textContent = 'New Booking';
    bookingForm.reset();
    document.getElementById('booking-id').value = '';

    // Set default dates
    const now = new Date();
    const tomorrow = new Date(now);
    tomorrow.setDate(tomorrow.getDate() + 1);

    document.getElementById('date-from').value = formatDateForInput(now);
    document.getElementById('date-to').value = formatDateForInput(tomorrow);

    bookingFormContainer.style.display = 'block';
}

// Open form for editing booking
async function editBooking(id) {
    formTitle.textContent = 'Edit Booking';
    const booking = await BookingService.getBooking(id);

    if (!booking) {
        showToast('Failed to load booking details', 'error');
        return;
    }

    document.getElementById('booking-id').value = booking.bookingID;
    document.getElementById('facility').value = booking.facilityDescription || '';
    document.getElementById('date-from').value = formatDateForInput(booking.bookingDateFrom);
    document.getElementById('date-to').value = formatDateForInput(booking.bookingDateTo);
    document.getElementById('status').value = booking.bookingStatus || 'Pending';

    bookingFormContainer.style.display = 'block';
}

// Delete booking
async function deleteBooking(id) {
    if (!confirm('Are you sure you want to delete this booking?')) {
        return;
    }

    const result = await BookingService.deleteBooking(id);

    if (result.success) {
        showToast('Booking deleted successfully', 'success');
        renderBookings();
    } else {
        showToast(result.error || 'Failed to delete booking', 'error');
    }
}

// Event Listeners
if (newBookingBtn) {
    newBookingBtn.addEventListener('click', openNewBookingForm);
}

if (closeModalBtn) {
    closeModalBtn.addEventListener('click', () => {
        bookingFormContainer.style.display = 'none';
    });
}

// Close modal when clicking outside
window.addEventListener('click', (e) => {
    if (e.target === bookingFormContainer) {
        bookingFormContainer.style.display = 'none';
    }
});

// Form submission
if (bookingForm) {
    bookingForm.addEventListener('submit', async (e) => {
        e.preventDefault();

        const bookingId = document.getElementById('booking-id').value;
        const bookingData = {
            facilityDescription: document.getElementById('facility').value,
            bookingDateFrom: document.getElementById('date-from').value,
            bookingDateTo: document.getElementById('date-to').value,
            bookingStatus: document.getElementById('status').value,
            bookedBy: AuthService.username
        };

        if (bookingId) {
            // Update existing booking
            bookingData.bookingID = parseInt(bookingId);
        }

        let result;

        if (bookingId) {
            // Update existing booking
            result = await BookingService.updateBooking(bookingId, bookingData);
        } else {
            // Create new booking
            result = await BookingService.createBooking(bookingData);
        }

        if (result.success) {
            showToast(`Booking ${bookingId ? 'updated' : 'created'} successfully`, 'success');
            bookingFormContainer.style.display = 'none';
            renderBookings();
        } else {
            showToast(result.error || `Failed to ${bookingId ? 'update' : 'create'} booking`, 'error');
        }
    });
}

// Export the renderBookings function to be called from auth.js
window.BookingService = {
    loadBookings: renderBookings
};