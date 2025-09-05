// Clipboard functionality with fallback support
window.copyToClipboardFallback = (text) => {
    // Create a temporary textarea element
    const textArea = document.createElement('textarea');
    textArea.value = text;
    textArea.style.position = 'fixed';
    textArea.style.left = '-999999px';
    textArea.style.top = '-999999px';
    document.body.appendChild(textArea);
    textArea.focus();
    textArea.select();
    
    try {
        // Execute the copy command
        const successful = document.execCommand('copy');
        if (!successful) {
            throw new Error('Copy command failed');
        }
    } finally {
        document.body.removeChild(textArea);
    }
};

window.isClipboardSupported = () => {
    return !!(navigator.clipboard && window.isSecureContext);
};

// File download functionality
window.downloadFile = (fileName, contentType, data) => {
    const blob = new Blob([data], { type: contentType });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
};

// File upload functionality
window.readFileAsText = (inputElement) => {
    return new Promise((resolve, reject) => {
        if (!inputElement.files || inputElement.files.length === 0) {
            reject('No file selected');
            return;
        }
        
        const file = inputElement.files[0];
        const reader = new FileReader();
        
        reader.onload = (e) => {
            resolve({
                fileName: file.name,
                content: e.target.result,
                size: file.size,
                lastModified: file.lastModified
            });
        };
        
        reader.onerror = () => {
            reject('Error reading file');
        };
        
        reader.readAsText(file);
    });
};

// Local storage helpers
window.localStorageHelper = {
    setItem: (key, value) => {
        try {
            localStorage.setItem(key, JSON.stringify(value));
            return true;
        } catch (error) {
            console.error('Error saving to localStorage:', error);
            return false;
        }
    },
    
    getItem: (key) => {
        try {
            const item = localStorage.getItem(key);
            return item ? JSON.parse(item) : null;
        } catch (error) {
            console.error('Error reading from localStorage:', error);
            return null;
        }
    },
    
    removeItem: (key) => {
        try {
            localStorage.removeItem(key);
            return true;
        } catch (error) {
            console.error('Error removing from localStorage:', error);
            return false;
        }
    },
    
    clear: () => {
        try {
            localStorage.clear();
            return true;
        } catch (error) {
            console.error('Error clearing localStorage:', error);
            return false;
        }
    }
};

// Notification helper
window.showNotification = (message, type = 'info', duration = 3000) => {
    // Create notification element
    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.textContent = message;
    notification.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        background: var(--mud-palette-${type === 'error' ? 'error' : type === 'success' ? 'success' : 'info'});
        color: white;
        padding: 12px 20px;
        border-radius: 8px;
        box-shadow: 0 4px 12px rgba(0,0,0,0.15);
        z-index: 10000;
        transform: translateX(400px);
        transition: transform 0.3s ease;
    `;
    
    document.body.appendChild(notification);
    
    // Show notification
    setTimeout(() => {
        notification.style.transform = 'translateX(0)';
    }, 10);
    
    // Auto-hide notification
    setTimeout(() => {
        notification.style.transform = 'translateX(400px)';
        setTimeout(() => {
            if (notification.parentNode) {
                document.body.removeChild(notification);
            }
        }, 300);
    }, duration);
};