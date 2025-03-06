// Admin Module
const AdminModule = (() => {
    // DOM Elements
    const bookingList = document.getElementById('booking-list');
    const noBookingsMessage = document.getElementById('no-bookings');
    const newBookingBtn = document.getElementById('new-booking-btn');
    const createAdminBtn = document.getElementById('create-admin-btn');
    const bookingFormContainer = document.getElementById('booking-form-container');
    const adminFormContainer = document.getElementById('admin-form-container');
    const bookingForm = document.getElementById('booking-form');
    const adminForm = document.getElementById('admin-form');
    const formTitle = document.getElementById('form-title');
    const closeBtns = document.querySelectorAll('.close-btn');
    const cancelFormBtn = document.getElementById('cancel-form');
    const cancelModalBtns = document.querySelectorAll('.cancel-modal');
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
    const bookingStatusField = document.getElementById('booking-status');

    // API Endpoints
    const API_URL = {
        BOOKINGS: '/api/Bookings',
        REGISTER_ADMIN: '/api/Auth/register-admin'
    };

    // State
    let bookings = [];
    let currentBookingId = null;

    // Initialize
    const init = () => {
        if (!AuthModule.isAuthenticated() || AuthModule.getUserRole() !== 'Admin') {
            window.location.href = '/login.html';
            return;
        }

        // Event Listeners
        newBookingBtn.addEventListener('click', showNewBookingForm);
        createAdminBtn.addEventListener('click', showAdminForm);
        bookingForm.addEventListener('submit', handleSaveBooking);
        adminForm.addEventListener('submit', handleCreateAdmin);
        closeBtns.forEach(btn => btn.addEventListener('click', closeModals));
        cancelFormBtn.addEventListener('click', closeModals);
        cancelModalBtns.forEach(btn => btn.addEventListener('click', closeModals));
        searchInput.addEventListener('input', filterBookings);
        statusFilter.addEventListener('change', filterBookings);
        confirmDeleteBtn.addEventListener('click', confirmDelete);
        cancelDeleteBtn.addEventListener('click', closeConfirmationModal);

        // Load initial data
        loadBookings();
    };

    // Load all bookings
    const loadBookings = async () => {
        try {
            const response = await fetch(API_URL.BOOKINGS, {
                headers: {
                    'Authorization': `Bearer ${AuthModule.getToken()}`
                }
            });

            if (response.ok) {
                bookings = await response.json();
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
                <td>${booking.bookedBy}</td>
                <td><span class="status-badge ${statusClass}">${booking.bookingStatus}</span></td>
                <td class="action-buttons">
                    <button class="action-btn edit-btn" data-id="${booking.bookingID}" title="Edit">
                        <i class="fas fa-edit"></i>
                    </button>
                    <button class="action-btn delete-btn" data-id="${booking.bookingID}" title="Delete">
                        <i class="fas fa-trash"></i>
                    </button>
                </td>
            `;

            bookingList.appendChild(row);
        });

        // Add event listeners to action buttons
        document.querySelectorAll('.edit-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                const id = parseInt(btn.getAttribute('data-id'));
                showEditBookingForm(id);
            });
        });

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
                booking.bookedBy.toLowerCase().includes(searchTerm) ||
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

    // Show form for editing booking
    const showEditBookingForm = (id) => {
        const booking = bookings.find(b => b.bookingID === id);
        if (!booking) return;

        formTitle.textContent = 'Edit Booking';

        bookingIdField.value = booking.bookingID;
        facilityDescriptionField.value = booking.facilityDescription;
        bookingDateFromField.value = formatDateForInput(new Date(booking.bookingDateFrom));
        bookingDateToField.value = formatDateForInput(new Date(booking.bookingDateTo));
        bookingStatusField.value = booking.bookingStatus;

        bookingFormContainer.classList.remove('hidden');
    };

    // Show admin creation form
    const showAdminForm = () => {
        adminFormContainer.classList.remove('hidden');
    };

    // Close all modals
    const closeModals = () => {
        bookingFormContainer.classList.add('hidden');
        adminFormContainer.classList.add('hidden');
        confirmationModal.classList.add('hidden');
    };

    // Handle save booking (create or update)
    const handleSaveBooking = async (e) => {
        e.preventDefault();

        const bookingData = {
            facilityDescription: facilityDescriptionField.value,
            bookingDateFrom: new Date(bookingDateFromField.value).toISOString(),
            bookingDateTo: new Date(bookingDateToField.value).toISOString(),
            bookingStatus: bookingStatusField.value,
            bookedBy: localStorage.getItem('username')
        };

        const id = bookingIdField.value;
        let url = API_URL.BOOKINGS;
        let method = 'POST';

        if (id) {
            url = `${API_URL.BOOKINGS}/${id}`;
            method = 'PUT';
            bookingData.bookingID = parseInt(id);
        }

        try {
            const response = await fetch(url, {
                method: method,
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${AuthModule.getToken()}`
                },
                body: JSON.stringify(bookingData)
            });

            if (response.ok) {
                closeModals();
                loadBookings();
                AppModule.showNotification(
                    id ? 'Booking updated successfully!' : 'Booking created successfully!',
                    'success'
                );
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

    // Handle create admin
    const handleCreateAdmin = async (e) => {
        e.preventDefault();

        const adminData = {
            username: document.getElementById('admin-username').value,
            email: document.getElementById('admin-email').value,
            firstName: document.getElementById('admin-firstname').value,
            lastName: document.getElementById('admin-lastname').value,
            password: document.getElementById('admin-password').value
        };

        try {
            const response = await fetch(API_URL.REGISTER_ADMIN, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${AuthModule.getToken()}`
                },
                body: JSON.stringify(adminData)
            });

            if (response.ok) {
                closeModals();
                adminForm.reset();
                AppModule.showNotification('Admin account created successfully!', 'success');
            } else {
                const errorData = await response.json();
                AppModule.showNotification(`Failed to create admin: ${errorData.message || 'Unknown error'}`, 'error');
            }
        } catch (error) {
            console.error('Error creating admin:', error);
            AppModule.showNotification('Network error while creating admin account.', 'error');
        }
    };

    // Show delete confirmation modal
    const showDeleteConfirmation = (id) => {
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
                loadBookings();
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
document.addEventListener('DOMContentLoaded', AdminModule.init);