// Admin Service
const AdminService = {
    async getUsers() {
        try {
            console.log('Fetching users from API...');
            const response = await fetch('/api/Users', {
                headers: {
                    ...AuthService.getAuthHeader()
                }
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Failed to load users');
            }

            const users = await response.json();
            console.log('Users fetched successfully:', users);
            return users;
        } catch (error) {
            console.error('Error loading users:', error);
            showToast(error.message, 'error');
            return [];
        }
    },

    async getUser(id) {
        try {
            const response = await fetch(`/api/Users/${id}`, {
                headers: {
                    ...AuthService.getAuthHeader()
                }
            });

            if (!response.ok) {
                throw new Error('Failed to load user details');
            }

            return await response.json();
        } catch (error) {
            showToast(error.message, 'error');
            return null;
        }
    },

    async updateUser(id, userData) {
        try {
            const response = await fetch(`/api/Users/${id}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    ...AuthService.getAuthHeader()
                },
                body: JSON.stringify(userData)
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Failed to update user');
            }

            return { success: true };
        } catch (error) {
            return { success: false, error: error.message };
        }
    },

    async deleteUser(id) {
        try {
            const response = await fetch(`/api/Users/${id}`, {
                method: 'DELETE',
                headers: {
                    ...AuthService.getAuthHeader()
                }
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Failed to delete user');
            }

            return { success: true };
        } catch (error) {
            return { success: false, error: error.message };
        }
    },

    async getRoles() {
        try {
            const response = await fetch('/api/Users/roles', {
                headers: {
                    ...AuthService.getAuthHeader()
                }
            });

            if (!response.ok) {
                throw new Error('Failed to load roles');
            }

            return await response.json();
        } catch (error) {
            showToast(error.message, 'error');
            return [];
        }
    },

    async registerAdmin(userData) {
        try {
            const response = await fetch('/api/Auth/register-admin', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    ...AuthService.getAuthHeader()
                },
                body: JSON.stringify(userData)
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Failed to register admin');
            }

            return { success: true };
        } catch (error) {
            return { success: false, error: error.message };
        }
    },

    // Load bookings for admin panel
    async loadAdminBookings() {
        try {
            console.log('Loading bookings for admin panel...');
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
            console.log('Admin bookings loaded:', bookings);
            return bookings;
        } catch (error) {
            console.error('Error loading admin bookings:', error);
            showToast(error.message, 'error');
            return [];
        }
    }
};

// DOM Elements
const adminPanel = document.getElementById('admin-panel');
const usersTable = document.getElementById('users-table');
const userFormContainer = document.getElementById('user-form-container');
const userForm = document.getElementById('user-form');
const closeUserModalBtn = document.querySelector('#user-form-container .close');
const newAdminBtn = document.getElementById('new-admin-btn');
const adminFormContainer = document.getElementById('admin-form-container');
const adminForm = document.getElementById('admin-form');
const closeAdminModalBtn = document.querySelector('#admin-form-container .close');
const adminNewBookingBtn = document.getElementById('admin-new-booking-btn');

// Render users table
async function renderUsers() {
    console.log('Rendering users table...');
    const tableBody = usersTable.querySelector('tbody');
    tableBody.innerHTML = '<tr><td colspan="5">Loading users...</td></tr>';

    try {
        const users = await AdminService.getUsers();
        console.log('Users data received:', users);

        // Clear the loading message before rendering users
        tableBody.innerHTML = '';

        if (!users || users.length === 0) {
            tableBody.innerHTML = '<tr><td colspan="5">No users found.</td></tr>';
            return;
        }

        users.forEach(user => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td>${user.userName}</td>
                <td>${user.email}</td>
                <td>${user.firstName} ${user.lastName}</td>
                <td>${user.roles.join(', ')}</td>
                <td>
                    <button class="action-btn edit-user-btn" data-id="${user.id}">
                        <i class="fas fa-edit"></i>
                    </button>
                    <button class="action-btn delete-user-btn" data-id="${user.id}">
                        <i class="fas fa-trash"></i>
                    </button>
                </td>
            `;
            tableBody.appendChild(row);
        });

        // Add event listeners to edit and delete buttons
        document.querySelectorAll('.edit-user-btn').forEach(btn => {
            btn.addEventListener('click', () => editUser(btn.dataset.id));
        });

        document.querySelectorAll('.delete-user-btn').forEach(btn => {
            btn.addEventListener('click', () => deleteUser(btn.dataset.id));
        });
    } catch (error) {
        console.error('Error rendering users:', error);
        tableBody.innerHTML = `<tr><td colspan="5">Error loading users: ${error.message}</td></tr>`;
    }
}

// Render admin bookings table
async function renderAdminBookings() {
    console.log('Rendering admin bookings table...');
    const tableBody = document.querySelector('#admin-bookings-table tbody');
    if (!tableBody) {
        console.error('Admin bookings table body not found');
        return;
    }

    tableBody.innerHTML = '<tr><td colspan="7" class="loading">Loading bookings...</td></tr>';

    try {
        const bookings = await AdminService.loadAdminBookings();
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
                <td>${booking.bookingID}</td>
                <td>${booking.facilityDescription || 'N/A'}</td>
                <td>${formatDate(booking.bookingDateFrom)}</td>
                <td>${formatDate(booking.bookingDateTo)}</td>
                <td>${booking.bookedBy || 'N/A'}</td>
                <td>${booking.bookingStatus || 'N/A'}</td>
                <td>
                    <button class="action-btn admin-edit-btn" data-id="${booking.bookingID}">
                        <i class="fas fa-edit"></i>
                    </button>
                    <button class="action-btn admin-delete-btn" data-id="${booking.bookingID}">
                        <i class="fas fa-trash"></i>
                    </button>
                </td>
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
        tableBody.innerHTML = `<tr><td colspan="7">Error loading bookings: ${error.message}</td></tr>`;
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

// Open form for editing user
async function editUser(id) {
    const user = await AdminService.getUser(id);

    if (!user) {
        showToast('Failed to load user details', 'error');
        return;
    }

    document.getElementById('user-id').value = user.id;
    document.getElementById('user-email').value = user.email;
    document.getElementById('user-firstname').value = user.firstName;
    document.getElementById('user-lastname').value = user.lastName;

    // Populate roles dropdown
    const roleSelect = document.getElementById('user-role');
    roleSelect.innerHTML = '';

    const roles = await AdminService.getRoles();
    roles.forEach(role => {
        const option = document.createElement('option');
        option.value = role;
        option.textContent = role;
        if (user.roles.includes(role)) {
            option.selected = true;
        }
        roleSelect.appendChild(option);
    });

    userFormContainer.style.display = 'block';
}

// Delete user
async function deleteUser(id) {
    if (!confirm('Are you sure you want to delete this user?')) {
        return;
    }

    const result = await AdminService.deleteUser(id);

    if (result.success) {
        showToast('User deleted successfully', 'success');
        renderUsers();
    } else {
        showToast(result.error || 'Failed to delete user', 'error');
    }
}

// Edit booking (reuse from bookings.js)
async function editBooking(id) {
    // Get the booking form from the main page
    const bookingFormContainer = document.getElementById('booking-form-container');
    const formTitle = document.getElementById('form-title');

    if (!bookingFormContainer || !formTitle) {
        showToast('Booking form not found', 'error');
        return;
    }

    formTitle.textContent = 'Edit Booking';

    try {
        const response = await fetch(`/api/Bookings/${id}`, {
            headers: {
                ...AuthService.getAuthHeader()
            }
        });

        if (!response.ok) {
            throw new Error('Failed to load booking details');
        }

        const booking = await response.json();

        document.getElementById('booking-id').value = booking.bookingID;
        document.getElementById('facility').value = booking.facilityDescription || '';
        document.getElementById('date-from').value = formatDateForInput(booking.bookingDateFrom);
        document.getElementById('date-to').value = formatDateForInput(booking.bookingDateTo);
        document.getElementById('status').value = booking.bookingStatus || 'Pending';

        bookingFormContainer.style.display = 'block';
    } catch (error) {
        showToast(error.message, 'error');
    }
}

// Format date for input fields (reused from bookings.js)
function formatDateForInput(dateString) {
    const date = new Date(dateString);
    return date.toISOString().slice(0, 16);
}

// Delete booking
async function deleteBooking(id) {
    if (!confirm('Are you sure you want to delete this booking?')) {
        return;
    }

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

        showToast('Booking deleted successfully', 'success');
        renderAdminBookings();
    } catch (error) {
        showToast(error.message, 'error');
    }
}

// Open form for new admin
async function openNewAdminForm() {
    adminForm.reset();
    adminFormContainer.style.display = 'block';
}

// Open form for new booking (admin)
function openNewBookingForm() {
    const bookingFormContainer = document.getElementById('booking-form-container');
    const formTitle = document.getElementById('form-title');
    const bookingForm = document.getElementById('booking-form');

    if (!bookingFormContainer || !formTitle || !bookingForm) {
        showToast('Booking form not found', 'error');
        return;
    }

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

// Event Listeners
if (newAdminBtn) {
    newAdminBtn.addEventListener('click', openNewAdminForm);
}

if (adminNewBookingBtn) {
    adminNewBookingBtn.addEventListener('click', openNewBookingForm);
}

if (closeUserModalBtn) {
    closeUserModalBtn.addEventListener('click', () => {
        userFormContainer.style.display = 'none';
    });
}

if (closeAdminModalBtn) {
    closeAdminModalBtn.addEventListener('click', () => {
        adminFormContainer.style.display = 'none';
    });
}

// Close modals when clicking outside
window.addEventListener('click', (e) => {
    if (e.target === userFormContainer) {
        userFormContainer.style.display = 'none';
    }
    if (e.target === adminFormContainer) {
        adminFormContainer.style.display = 'none';
    }

    // Also close booking form modal if clicked outside
    const bookingFormContainer = document.getElementById('booking-form-container');
    if (e.target === bookingFormContainer) {
        bookingFormContainer.style.display = 'none';
    }
});

// User form submission
if (userForm) {
    userForm.addEventListener('submit', async (e) => {
        e.preventDefault();

        const userId = document.getElementById('user-id').value;
        const userData = {
            email: document.getElementById('user-email').value,
            firstName: document.getElementById('user-firstname').value,
            lastName: document.getElementById('user-lastname').value,
            role: document.getElementById('user-role').value
        };

        const result = await AdminService.updateUser(userId, userData);

        if (result.success) {
            showToast('User updated successfully', 'success');
            userFormContainer.style.display = 'none';
            renderUsers();
        } else {
            showToast(result.error || 'Failed to update user', 'error');
        }
    });
}

// Admin form submission
if (adminForm) {
    adminForm.addEventListener('submit', async (e) => {
        e.preventDefault();

        const adminData = {
            username: document.getElementById('admin-username').value,
            email: document.getElementById('admin-email').value,
            firstName: document.getElementById('admin-firstname').value,
            lastName: document.getElementById('admin-lastname').value,
            password: document.getElementById('admin-password').value
        };

        const result = await AdminService.registerAdmin(adminData);

        if (result.success) {
            showToast('Admin registered successfully', 'success');
            adminFormContainer.style.display = 'none';
            renderUsers();
        } else {
            showToast(result.error || 'Failed to register admin', 'error');
        }
    });
}

// Booking form submission handler
document.addEventListener('DOMContentLoaded', () => {
    const bookingForm = document.getElementById('booking-form');

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

            try {
                let response;

                if (bookingId) {
                    // Update existing booking
                    response = await fetch(`/api/Bookings/${bookingId}`, {
                        method: 'PUT',
                        headers: {
                            'Content-Type': 'application/json',
                            ...AuthService.getAuthHeader()
                        },
                        body: JSON.stringify(bookingData)
                    });
                } else {
                    // Create new booking
                    response = await fetch('/api/Bookings', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            ...AuthService.getAuthHeader()
                        },
                        body: JSON.stringify(bookingData)
                    });
                }

                if (!response.ok) {
                    const errorData = await response.json();
                    throw new Error(errorData.message || `Failed to ${bookingId ? 'update' : 'create'} booking`);
                }

                showToast(`Booking ${bookingId ? 'updated' : 'created'} successfully`, 'success');
                document.getElementById('booking-form-container').style.display = 'none';

                // Refresh the appropriate booking list
                if (AuthService.hasRole('Admin')) {
                    renderAdminBookings();
                } else {
                    if (window.BookingService && typeof window.BookingService.loadBookings === 'function') {
                        window.BookingService.loadBookings();
                    }
                }
            } catch (error) {
                showToast(error.message, 'error');
            }
        });
    }
});

// Check if user is admin and initialize admin panel
async function initializeAdminPanel() {
    console.log('Initializing admin panel');
    if (!AuthService.isAuthenticated()) {
        console.log('User not authenticated, skipping admin panel initialization');
        return;
    }

    try {
        // Check if user has Admin role
        if (AuthService.hasRole('Admin')) {
            console.log('User has Admin role, showing admin panel');
            // Show admin panel
            if (adminPanel) {
                adminPanel.style.display = 'block';

                // Initialize tabs
                const adminTabButtons = document.querySelectorAll('#admin-panel .tab-btn');
                adminTabButtons.forEach(button => {
                    button.addEventListener('click', () => {
                        // Find the parent container of this tab button
                        const parentContainer = button.closest('.tabs').parentElement;

                        // Get all tab buttons and contents within this container
                        const containerTabButtons = parentContainer.querySelectorAll('.tab-btn');
                        const containerTabContents = parentContainer.querySelectorAll('.tab-content');

                        // Remove active class from all tabs in this container
                        containerTabButtons.forEach(btn => btn.classList.remove('active'));
                        containerTabContents.forEach(content => content.classList.remove('active'));

                        // Add active class to current tab
                        button.classList.add('active');
                        const tabId = button.dataset.tab;
                        const tabContent = parentContainer.querySelector(`#${tabId}`);
                        if (tabContent) {
                            tabContent.classList.add('active');
                        }

                        // Load data based on the selected tab
                        if (tabId === 'users-management') {
                            renderUsers();
                        } else if (tabId === 'bookings-management') {
                            renderAdminBookings();
                        }
                    });
                });

                // Render users by default (first tab)
                renderUsers();

                // Also load bookings data for the bookings tab
                renderAdminBookings();

                // Hide the regular booking container for admin users
                const bookingContainer = document.getElementById('booking-container');
                if (bookingContainer) {
                    bookingContainer.style.display = 'none';
                }
            }
        } else {
            console.log('User does not have Admin role, hiding admin panel');
            if (adminPanel) {
                adminPanel.style.display = 'none';
            }
        }
    } catch (error) {
        console.error('Error initializing admin panel:', error);
    }
}

// Initialize admin panel when authenticated
document.addEventListener('DOMContentLoaded', () => {
    // This will be called after auth.js checks authentication
    if (AuthService.isAuthenticated()) {
        initializeAdminPanel();
    }
});

// Export for use in auth.js
window.AdminService = {
    initializeAdminPanel
};