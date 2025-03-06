// Auth Module
const AuthModule = (() => {
    // DOM Elements
    const loginForm = document.getElementById('login');
    const registerForm = document.getElementById('register');
    const tabBtns = document.querySelectorAll('.tab-btn');
    const authForms = document.querySelectorAll('.auth-form');
    const userInfo = document.getElementById('user-info');
    const usernameDisplay = document.getElementById('username-display');
    const logoutBtn = document.getElementById('logout-btn');

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
        const userRole = localStorage.getItem('userRole');

        if (token && username) {
            if (window.location.pathname === '/login.html') {
                redirectBasedOnRole(userRole);
            } else {
                showLoggedInUI(username);
            }
        } else if (window.location.pathname !== '/login.html') {
            window.location.href = '/login.html';
        }

        // Event Listeners
        if (loginForm) loginForm.addEventListener('submit', handleLogin);
        if (registerForm) registerForm.addEventListener('submit', handleRegister);
        if (logoutBtn) logoutBtn.addEventListener('click', handleLogout);

        // Tab switching
        if (tabBtns) {
            tabBtns.forEach(btn => {
                btn.addEventListener('click', () => {
                    const tabName = btn.getAttribute('data-tab');
                    switchTab(tabName);
                });
            });
        }
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
                // Parse JWT token to get user role
                const tokenPayload = JSON.parse(atob(data.token.split('.')[1]));
                const userRole = tokenPayload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

                // Store token and user info
                localStorage.setItem('token', data.token);
                localStorage.setItem('username', username);
                localStorage.setItem('userRole', userRole);

                // Redirect based on role
                redirectBasedOnRole(userRole);

                AppModule.showNotification('Login successful!', 'success');
            } else {
                AppModule.showNotification('Login failed: ' + (data.message || 'Invalid credentials'), 'error');
            }
        } catch (error) {
            console.error('Login error:', error);
            AppModule.showNotification('Login failed: Network error', 'error');
        }
    };

    // Redirect based on user role
    const redirectBasedOnRole = (role) => {
        if (role === 'Admin') {
            window.location.href = '/admin.html';
        } else {
            window.location.href = '/member.html';
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
                AppModule.showNotification('Registration successful! You can now login.', 'success');
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
        localStorage.removeItem('userRole');

        // Redirect to login page
        window.location.href = '/login.html';
    };

    // Show logged in UI
    const showLoggedInUI = (username) => {
        if (userInfo) userInfo.classList.remove('hidden');
        if (usernameDisplay) usernameDisplay.textContent = username;
    };

    // Check if user is authenticated
    const isAuthenticated = () => {
        return localStorage.getItem('token') !== null;
    };

    // Get auth token
    const getToken = () => {
        return localStorage.getItem('token');
    };

    // Get user role
    const getUserRole = () => {
        return localStorage.getItem('userRole');
    };

    // Public methods
    return {
        init,
        isAuthenticated,
        getToken,
        getUserRole
    };
})();