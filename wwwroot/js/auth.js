// Auth Module
const AuthModule = (() => {
    // DOM Elements
    const authContainer = document.getElementById('auth-container');
    const loginForm = document.getElementById('login');
    const registerForm = document.getElementById('register');
    const tabBtns = document.querySelectorAll('.tab-btn');
    const authForms = document.querySelectorAll('.auth-form');
    const userInfo = document.getElementById('user-info');
    const usernameDisplay = document.getElementById('username-display');
    const logoutBtn = document.getElementById('logout-btn');
    const bookingContainer = document.getElementById('booking-container');

    // API Endpoints
    const API_URL = {
        LOGIN: '/api/Auth/login',
        REGISTER: '/api/Auth/register'
    };

    // Initialize
    const init = () => {
        // Check if user is already logged in
        const token = localStorage.getItem('token');
        const username = localStorage.getItem('username');

        if (token && username) {
            showLoggedInUI(username);
        }

        // Event Listeners
        loginForm.addEventListener('submit', handleLogin);
        registerForm.addEventListener('submit', handleRegister);
        logoutBtn.addEventListener('click', handleLogout);

        // Tab switching
        tabBtns.forEach(btn => {
            btn.addEventListener('click', () => {
                const tabName = btn.getAttribute('data-tab');
                switchTab(tabName);
            });
        });
    };

    // Switch between login and register tabs
    const switchTab = (tabName) => {
        tabBtns.forEach(btn => {
            btn.classList.remove('active');
            if (btn.getAttribute('data-tab') === tabName) {
                btn.classList.add('active');
            }
        });

        authForms.forEach(form => {
            form.classList.remove('active');
            if (form.id === `${tabName}-form`) {
                form.classList.add('active');
            }
        });
    };

    // Handle Login
    const handleLogin = async (e) => {
        e.preventDefault();

        const username = document.getElementById('login-username').value;
        const password = document.getElementById('login-password').value;

        try {
            const response = await fetch(API_URL.LOGIN, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ username, password })
            });

            const data = await response.json();

            if (response.ok) {
                // Store token and user info
                localStorage.setItem('token', data.token);
                localStorage.setItem('username', username);

                // Update UI
                showLoggedInUI(username);

                // Show success notification
                AppModule.showNotification('Login successful!', 'success');

                // Load bookings
                BookingModule.loadBookings();
            } else {
                AppModule.showNotification('Login failed: ' + (data.message || 'Invalid credentials'), 'error');
            }
        } catch (error) {
            console.error('Login error:', error);
            AppModule.showNotification('Login failed: Network error', 'error');
        }
    };

    // Handle Register
    const handleRegister = async (e) => {
        e.preventDefault();

        const username = document.getElementById('register-username').value;
        const email = document.getElementById('register-email').value;
        const firstName = document.getElementById('register-firstname').value;
        const lastName = document.getElementById('register-lastname').value;
        const password = document.getElementById('register-password').value;

        try {
            const response = await fetch(API_URL.REGISTER, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    username,
                    email,
                    firstName,
                    lastName,
                    password
                })
            });

            const data = await response.json();

            if (response.ok) {
                // Show success notification
                AppModule.showNotification('Registration successful! You can now login.', 'success');

                // Clear form and switch to login tab
                registerForm.reset();
                switchTab('login');
            } else {
                const errorMessage = data.errors
                    ? `Registration failed: ${data.errors.join(', ')}`
                    : `Registration failed: ${data.message || 'Unknown error'}`;
                AppModule.showNotification(errorMessage, 'error');
            }
        } catch (error) {
            console.error('Registration error:', error);
            AppModule.showNotification('Registration failed: Network error', 'error');
        }
    };

    // Handle Logout
    const handleLogout = () => {
        // Clear local storage
        localStorage.removeItem('token');
        localStorage.removeItem('username');

        // Update UI
        showLoggedOutUI();

        // Show notification
        AppModule.showNotification('Logged out successfully!', 'success');
    };

    // Show logged in UI
    const showLoggedInUI = (username) => {
        authContainer.classList.add('hidden');
        bookingContainer.classList.remove('hidden');
        userInfo.classList.remove('hidden');
        usernameDisplay.textContent = username;
    };

    // Show logged out UI
    const showLoggedOutUI = () => {
        authContainer.classList.remove('hidden');
        bookingContainer.classList.add('hidden');
        userInfo.classList.add('hidden');
        usernameDisplay.textContent = '';
    };

    // Check if user is authenticated
    const isAuthenticated = () => {
        return localStorage.getItem('token') !== null;
    };

    // Get auth token
    const getToken = () => {
        return localStorage.getItem('token');
    };

    // Public methods
    return {
        init,
        isAuthenticated,
        getToken,
        showLoggedOutUI
    };
})();