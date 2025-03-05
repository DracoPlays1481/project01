// Main App Module
const AppModule = (() => {
    // DOM Elements
    const notification = document.getElementById('notification');
    const notificationMessage = document.getElementById('notification-message');
    const closeNotification = document.querySelector('.close-notification');

    // Initialize
    const init = () => {
        // Initialize modules
        AuthModule.init();
        BookingModule.init();

        // Event listeners
        closeNotification.addEventListener('click', hideNotification);

        // Check if user is authenticated and load bookings
        if (AuthModule.isAuthenticated()) {
            BookingModule.loadBookings();
        }
    };

    // Show notification
    const showNotification = (message, type = 'default') => {
        notificationMessage.textContent = message;
        notification.className = 'notification';
        notification.classList.add(type);
        notification.classList.remove('hidden');

        // Auto-hide after 5 seconds
        setTimeout(hideNotification, 5000);
    };

    // Hide notification
    const hideNotification = () => {
        notification.classList.add('hidden');
    };

    // Public methods
    return {
        init,
        showNotification
    };
})();

// Initialize app when DOM is loaded
document.addEventListener('DOMContentLoaded', AppModule.init);