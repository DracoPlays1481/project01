// Auth Service
const AuthService = {
    token: localStorage.getItem('token'),
    username: localStorage.getItem('username'),

    isAuthenticated() {
        return !!this.token;
    },

    async login(username, password) {
        try {
            const response = await fetch('/api/Auth/login', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ username, password })
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Login failed');
            }

            const data = await response.json();
            this.token = data.token;
            this.username = username;

            localStorage.setItem('token', data.token);
            localStorage.setItem('username', username);

            return { success: true };
        } catch (error) {
            console.error('Login error:', error);
            return { success: false, error: error.message };
        }
    },

    async register(userData) {
        try {
            const response = await fetch('/api/Auth/register', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(userData)
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Registration failed');
            }

            return { success: true };
        } catch (error) {
            console.error('Registration error:', error);
            return { success: false, error: error.message };
        }
    },

    logout() {
        this.token = null;
        this.username = null;
        localStorage.removeItem('token');
        localStorage.removeItem('username');
    },

    getAuthHeader() {
        return { 'Authorization': `Bearer ${this.token}` };
    }
};

// DOM Elements
const loginForm = document.getElementById('login-form');
const registerForm = document.getElementById('register-form');
const tabButtons = document.querySelectorAll('.tab-btn');
const tabContents = document.querySelectorAll('.tab-content');
const logoutBtn = document.getElementById('logout-btn');
const usernameDisplay = document.getElementById('username');
const authContainer = document.getElementById('auth-container');
const bookingContainer = document.getElementById('booking-container');

// Tab Switching
tabButtons.forEach(button => {
    button.addEventListener('click', () => {
        // Remove active class from all tabs
        tabButtons.forEach(btn => btn.classList.remove('active'));
        tabContents.forEach(content => content.classList.remove('active'));

        // Add active class to current tab
        button.classList.add('active');
        document.getElementById(button.dataset.tab).classList.add('active');
    });
});

// Login Form Submission
loginForm.addEventListener('submit', async (e) => {
    e.preventDefault();

    const username = document.getElementById('login-username').value;
    const password = document.getElementById('login-password').value;

    const result = await AuthService.login(username, password);

    if (result.success) {
        showToast('Login successful!', 'success');
        updateAuthUI();
    } else {
        showToast(result.error || 'Login failed. Please check your credentials.', 'error');
    }
});

// Register Form Submission
registerForm.addEventListener('submit', async (e) => {
    e.preventDefault();

    const userData = {
        username: document.getElementById('register-username').value,
        email: document.getElementById('register-email').value,
        firstName: document.getElementById('register-firstname').value,
        lastName: document.getElementById('register-lastname').value,
        password: document.getElementById('register-password').value
    };

    const result = await AuthService.register(userData);

    if (result.success) {
        showToast('Registration successful! You can now log in.', 'success');
        // Switch to login tab
        tabButtons[0].click();
        // Clear register form
        registerForm.reset();
    } else {
        showToast(result.error || 'Registration failed. Please try again.', 'error');
    }
});

// Logout Button
logoutBtn.addEventListener('click', () => {
    AuthService.logout();
    updateAuthUI();
    showToast('You have been logged out.', 'info');
});

// Update UI based on authentication state
function updateAuthUI() {
    if (AuthService.isAuthenticated()) {
        authContainer.style.display = 'none';
        bookingContainer.style.display = 'block';
        logoutBtn.style.display = 'block';
        usernameDisplay.textContent = AuthService.username;

        // Load bookings when authenticated
        if (window.BookingService && typeof window.BookingService.loadBookings === 'function') {
            window.BookingService.loadBookings();
        } else {
            console.error('BookingService.loadBookings is not available');
        }
    } else {
        authContainer.style.display = 'block';
        bookingContainer.style.display = 'none';
        logoutBtn.style.display = 'none';
        usernameDisplay.textContent = 'Not logged in';
    }
}

// Check authentication status on page load
document.addEventListener('DOMContentLoaded', () => {
    console.log('DOM loaded, checking auth status');
    updateAuthUI();
});