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

// Open form for new admin
async function openNewAdminForm() {
    adminForm.reset();
    adminFormContainer.style.display = 'block';
}

// Event Listeners
if (newAdminBtn) {
    newAdminBtn.addEventListener('click', openNewAdminForm);
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
                renderUsers();

                // Initialize bookings management tab
                const bookingsManagementTab = document.getElementById('bookings-management');
                if (bookingsManagementTab) {
                    // Clone the booking list from the main booking container
                    const originalBookingList = document.getElementById('booking-list');
                    if (originalBookingList) {
                        const clonedBookingList = originalBookingList.cloneNode(true);
                        bookingsManagementTab.appendChild(clonedBookingList);
                    }
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