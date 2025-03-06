// Member Module
const MemberModule = (() => {
    // DOM Elements
    const bookingList = document.getElementById('booking-list');
    const noBookingsMessage = document.getElementById('no-bookings');
    const newBookingBtn = document.getElementById('new-booking-btn');
    const bookingFormContainer = document.getElementById('booking-form-container');
    const bookingForm = document.getElementById('booking-form');
    const formTitle = document.getElementById('form-title');
    const closeBtn = document.querySelector('.close-btn');
    const cancelFormBtn = document.getElementById('cancel-form');
    const searchInput = document.getElementById('search-bookings');
    const statusFilter = document.getElementById('status-filter');
    const confirmationModal = document.getElementById('confirmation-modal');
    const confirmDeleteBtn = document.getElementById('confirm-delete');
    const cancelDeleteBtn = document.getElementById('cancel-delete');

    // Form fields
    const bookingIdField = document.getElementById('booking-id');
    const facilityDescriptionField = document.getElementById('facility-description');
    const bookingDateFromField = document.getElementById('booking-date-from');
    const bookingDateToField = document.getElementById('booking-date-to');

    // API Endpoints
    const API_URL = {
        BOOKINGS: '/api/Bookings'
    };

    // State
    let bookings = [];
    let currentBookingId = null;

    // Initialize
    const init = () => {
        if (!AuthModule.isAuthenticated()) {
            window.location.href = '/login.html';
            return;
        }

        // Event Listeners
        newBookingBtn.addEventListener('click', showNewBookingForm);
        bookingForm.addEventListener('submit', handleSaveBooking);
        closeBtn.addEventListener('click', closeBookingForm);
        cancelFormBtn.addEventListener('click', closeBookingForm);
        searchInput.addEventListener('input', filterBookings);
        statusFilter.addEventListener('change', filterBookings);
        confirmDeleteBtn.addEventListener('click', confirmDelete);
        cancelDeleteBtn.addEventListener('click', closeConfirmationModal);

        // Load initial data
        loadBookings();
    };

    // Load all bookings for the current user
    const loadBookings = async () => {
        try {
            const response = await fetch(API_URL.BOOKINGS, {
                headers: {
                    'Authorization': `Bearer ${AuthModule.getToken()}`
                }
            });

            if (response.ok) {
                const allBookings = await response.json();
                // Filter bookings for current user
                bookings = allBookings.filter(b => b.bookedBy === localStorage.getItem('username'));
                renderBookings(bookings);
            } else if (response.status === 401) {
                window.location.href = '/login.html';
            } else {
                AppModule.showNotification('Failed to load bookings.', 'error');
            }
        } catch (error) {
            console.error('Error loading bookings:', error);
            AppModule.showNotification('Network error while loading bookings.', 'error');
        }
    };

    // Render bookings to the table
    const renderBookings = (bookingsToRender) => {
        bookingList.innerHTML = '';

        if (bookingsToRender.length === 0) {
            noBookingsMessage.classList.remove('hidden');
            return;
        }

        noBookingsMessage.classList.add('hidden');

        bookingsToRender.forEach(booking => {
            const row = document.createElement('tr');

            // Format dates for display
            const fromDate = new Date(booking.bookingDateFrom).toLocaleString();
            const toDate = new Date(booking.bookingDateTo).toLocaleString();

            // Create status badge class
            const statusClass = `status-${booking.bookingStatus.toLowerCase()}`;

            row.innerHTML = `
                <td>${booking.bookingID}</td>
                <td>${booking.facilityDescription}</td>
                <td>${fromDate}</td>
                <td>${toDate}</td>
                <td><span class="status-badge ${statusClass}">${booking.bookingStatus}</span></td>
                <td class="action-buttons">
                    ${booking.bookingStatus === 'Pending' ? `
                        <button class="action-btn delete-btn" data-id="${booking.bookingID}" title="Delete">
                            <i class="fas fa-trash"></i>
                        </button>
                    ` : ''}
                </td>
            `;

            bookingList.appendChild(row);
        });

        // Add event listeners to delete buttons
        document.querySelectorAll('.delete-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                const id = parseInt(btn.getAttribute('data-id'));
                showDeleteConfirmation(id);
            });
        });
    };

    // Filter bookings based on search input and status filter
    const filterBookings = () => {
        const searchTerm = searchInput.value.toLowerCase();
        const statusValue = statusFilter.value;

        const filteredBookings = bookings.filter(booking => {
            const matchesSearch =
                booking.facilityDescription.toLowerCase().includes(searchTerm) ||
                booking.bookingID.toString().includes(searchTerm);

            const matchesStatus = statusValue === '' || booking.bookingStatus === statusValue;

            return matchesSearch && matchesStatus;
        });

        renderBookings(filteredBookings);
    };

    // Show form for new booking
    const showNewBookingForm = () => {
        formTitle.textContent = 'New Booking';
        bookingForm.reset();
        bookingIdField.value = '';

        // Set default dates
        const now = new Date();
        const tomorrow = new Date(now);
        tomorrow.setDate(tomorrow.getDate() + 1);

        bookingDateFromField.value = formatDateForInput(now);
        bookingDateToField.value = formatDateForInput(tomorrow);

        bookingFormContainer.classList.remove('hidden');
    };

    // Close booking form
    const closeBookingForm = () => {
        bookingFormContainer.classList.add('hidden');
    };

    // Handle save booking (create only for members)
    const handleSaveBooking = async (e) => {
        e.preventDefault();

        const bookingData = {
            facilityDescription: facilityDescriptionField.value,
            bookingDateFrom: new Date(bookingDateFromField.value).toISOString(),
            bookingDateTo: new Date(bookingDateToField.value).toISOString(),
            bookingStatus: 'Pending',
            bookedBy: localStorage.getItem('username')
        };

        try {
            const response = await fetch(API_URL.BOOKINGS, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${AuthModule.getToken()}`
                },
                body: JSON.stringify(bookingData)
            });

            if (response.ok) {
                closeBookingForm();
                await loadBookings();
                AppModule.showNotification('Booking created successfully!', 'success');
            } else if (response.status === 401) {
                window.location.href = '/login.html';
            } else {
                const errorData = await response.json();
                AppModule.showNotification(`Failed to save booking: ${errorData.message || 'Unknown error'}`, 'error');
            }
        } catch (error) {
            console.error('Error saving booking:', error);
            AppModule.showNotification('Network error while saving booking.', 'error');
        }
    };

    // Show delete confirmation modal
    const showDeleteConfirmation = (id) => {
        const booking = bookings.find(b => b.bookingID === id);
        if (!booking || booking.bookingStatus !== 'Pending') {
            AppModule.showNotification('Only pending bookings can be deleted.', 'error');
            return;
        }

        currentBookingId = id;
        confirmationModal.classList.remove('hidden');
    };

    // Close confirmation modal
    const closeConfirmationModal = () => {
        confirmationModal.classList.add('hidden');
        currentBookingId = null;
    };

    // Confirm delete booking
    const confirmDelete = async () => {
        if (!currentBookingId) return;

        try {
            const response = await fetch(`${API_URL.BOOKINGS}/${currentBookingId}`, {
                method: 'DELETE',
                headers: {
                    'Authorization': `Bearer ${AuthModule.getToken()}`
                }
            });

            if (response.ok) {
                closeConfirmationModal();
                await loadBookings();
                AppModule.showNotification('Booking deleted successfully!', 'success');
            } else if (response.status === 401) {
                window.location.href = '/login.html';
            } else {
                const errorData = await response.json();
                AppModule.showNotification(`Failed to delete booking: ${errorData.message || 'Unknown error'}`, 'error');
            }
        } catch (error) {
            console.error('Error deleting booking:', error);
            AppModule.showNotification('Network error while deleting booking.', 'error');
        }
    };

    // Helper function to format date for datetime-local input
    const formatDateForInput = (date) => {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        const hours = String(date.getHours()).padStart(2, '0');
        const minutes = String(date.getMinutes()).padStart(2, '0');

        return `${year}-${month}-${day}T${hours}:${minutes}`;
    };

    // Public methods
    return {
        init
    };
})();

// Initialize module when DOM is loaded
document.addEventListener('DOMContentLoaded', MemberModule.init);