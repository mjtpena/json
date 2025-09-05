// Theme management and interactive enhancements
window.themeManager = {
    currentTheme: 'light',
    
    init() {
        // Get saved theme or detect system preference
        const savedTheme = localStorage.getItem('json-tool-theme');
        if (savedTheme) {
            this.currentTheme = savedTheme;
        } else {
            // Detect system preference
            this.currentTheme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
        }
        
        this.applyTheme(this.currentTheme);
        this.initializeInteractions();
        this.initializeAnimations();
    },
    
    toggle() {
        this.currentTheme = this.currentTheme === 'light' ? 'dark' : 'light';
        this.applyTheme(this.currentTheme);
        localStorage.setItem('json-tool-theme', this.currentTheme);
        
        // Dispatch custom event for components to listen to
        window.dispatchEvent(new CustomEvent('themeChanged', { 
            detail: { theme: this.currentTheme, isDark: this.currentTheme === 'dark' }
        }));
    },
    
    applyTheme(theme) {
        const html = document.documentElement;
        const body = document.body;
        
        // Remove existing theme classes
        html.classList.remove('light-theme', 'dark-theme');
        body.classList.remove('light-theme', 'dark-theme');
        
        // Add new theme class
        const themeClass = `${theme}-theme`;
        html.classList.add(themeClass);
        body.classList.add(themeClass);
        
        // Update meta theme color for mobile browsers
        const metaThemeColor = document.querySelector('meta[name="theme-color"]');
        if (metaThemeColor) {
            metaThemeColor.content = theme === 'dark' ? '#0f172a' : '#3b82f6';
        }
        
        // Update favicon for dark mode
        this.updateFavicon(theme);
    },
    
    updateFavicon(theme) {
        const favicon = document.querySelector('link[rel="icon"]');
        if (favicon) {
            // You could have different favicons for light/dark mode
            // favicon.href = theme === 'dark' ? '/favicon-dark.svg' : '/favicon.svg';
        }
    },
    
    initializeInteractions() {
        // Add hover effects to interactive elements
        const interactiveElements = document.querySelectorAll('.interactive-hover, .stats-card, .mud-paper');
        
        interactiveElements.forEach(el => {
            el.addEventListener('mouseenter', function() {
                this.style.transform = 'translateY(-2px)';
            });
            
            el.addEventListener('mouseleave', function() {
                this.style.transform = 'translateY(0)';
            });
        });
        
        // Add click ripple effect
        this.initializeRippleEffect();
    },
    
    initializeRippleEffect() {
        const buttons = document.querySelectorAll('.mud-button');
        
        buttons.forEach(button => {
            button.addEventListener('click', function(e) {
                const ripple = document.createElement('span');
                const rect = this.getBoundingClientRect();
                const size = Math.max(rect.width, rect.height);
                const x = e.clientX - rect.left - size / 2;
                const y = e.clientY - rect.top - size / 2;
                
                ripple.style.cssText = `
                    position: absolute;
                    width: ${size}px;
                    height: ${size}px;
                    left: ${x}px;
                    top: ${y}px;
                    background: rgba(255, 255, 255, 0.5);
                    border-radius: 50%;
                    transform: scale(0);
                    animation: ripple 0.6s linear;
                    pointer-events: none;
                `;
                
                this.style.position = 'relative';
                this.style.overflow = 'hidden';
                this.appendChild(ripple);
                
                setTimeout(() => ripple.remove(), 600);
            });
        });
        
        // Add ripple animation CSS if not exists
        if (!document.querySelector('#ripple-styles')) {
            const style = document.createElement('style');
            style.id = 'ripple-styles';
            style.textContent = `
                @keyframes ripple {
                    to {
                        transform: scale(4);
                        opacity: 0;
                    }
                }
            `;
            document.head.appendChild(style);
        }
    },
    
    initializeAnimations() {
        // Scroll animations
        const observerOptions = {
            threshold: 0.1,
            rootMargin: '0px 0px -50px 0px'
        };
        
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('visible');
                }
            });
        }, observerOptions);
        
        // Observe elements for scroll animations
        document.querySelectorAll('.animate-on-scroll').forEach(el => {
            observer.observe(el);
        });
        
        // Add smooth scrolling
        document.documentElement.style.scrollBehavior = 'smooth';
    },
    
    // JSON syntax highlighting helper
    highlightJson(jsonString, isDark = null) {
        if (isDark === null) {
            isDark = this.currentTheme === 'dark';
        }
        
        try {
            const parsed = JSON.parse(jsonString);
            const formatted = JSON.stringify(parsed, null, 2);
            return this.syntaxHighlight(formatted, isDark);
        } catch {
            return this.syntaxHighlight(jsonString, isDark);
        }
    },
    
    syntaxHighlight(json, isDark) {
        const colors = isDark ? {
            key: '#9CDCFE',
            string: '#CE9178',
            number: '#B5CEA8',
            boolean: '#569CD6',
            null: '#808080',
            punctuation: '#D4D4D4'
        } : {
            key: '#0451A5',
            string: '#A31515',
            number: '#098658',
            boolean: '#0000FF',
            null: '#808080',
            punctuation: '#000000'
        };
        
        return json.replace(
            /("(\\u[a-zA-Z0-9]{4}|\\[^u]|[^\\"])*"(\s*:)?|\b(true|false|null)\b|-?\d+(?:\.\d*)?(?:[eE][+\-]?\d+)?)/g,
            function (match) {
                let cls = '';
                let color = colors.punctuation;
                
                if (/^"/.test(match)) {
                    if (/:$/.test(match)) {
                        cls = 'json-key';
                        color = colors.key;
                    } else {
                        cls = 'json-string';
                        color = colors.string;
                    }
                } else if (/true|false/.test(match)) {
                    cls = 'json-boolean';
                    color = colors.boolean;
                } else if (/null/.test(match)) {
                    cls = 'json-null';
                    color = colors.null;
                } else {
                    cls = 'json-number';
                    color = colors.number;
                }
                
                return `<span class="${cls}" style="color: ${color}; font-weight: ${cls === 'json-key' || cls === 'json-boolean' ? '600' : 'normal'};">${match}</span>`;
            }
        );
    },
    
    // Copy to clipboard with visual feedback
    async copyToClipboard(text, sourceElement = null) {
        try {
            await navigator.clipboard.writeText(text);
            this.showCopyFeedback(sourceElement);
            return true;
        } catch (err) {
            // Fallback for older browsers
            const textArea = document.createElement('textarea');
            textArea.value = text;
            document.body.appendChild(textArea);
            textArea.select();
            document.execCommand('copy');
            document.body.removeChild(textArea);
            this.showCopyFeedback(sourceElement);
            return true;
        }
    },
    
    showCopyFeedback(element) {
        if (element) {
            const originalText = element.textContent || element.title;
            element.textContent = 'Copied!';
            element.style.color = '#10b981';
            
            setTimeout(() => {
                element.textContent = originalText;
                element.style.color = '';
            }, 1000);
        }
        
        // Show toast notification
        this.showToast('Copied to clipboard!', 'success');
    },
    
    showToast(message, type = 'info') {
        const toast = document.createElement('div');
        toast.className = `toast toast-${type}`;
        toast.textContent = message;
        
        toast.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            background: ${type === 'success' ? '#10b981' : type === 'error' ? '#ef4444' : '#3b82f6'};
            color: white;
            padding: 12px 24px;
            border-radius: 8px;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
            z-index: 10000;
            animation: slideInRight 0.3s ease;
            font-weight: 500;
        `;
        
        document.body.appendChild(toast);
        
        setTimeout(() => {
            toast.style.animation = 'slideOutRight 0.3s ease';
            setTimeout(() => toast.remove(), 300);
        }, 3000);
        
        // Add animation CSS if not exists
        if (!document.querySelector('#toast-styles')) {
            const style = document.createElement('style');
            style.id = 'toast-styles';
            style.textContent = `
                @keyframes slideInRight {
                    from { transform: translateX(100%); opacity: 0; }
                    to { transform: translateX(0); opacity: 1; }
                }
                @keyframes slideOutRight {
                    from { transform: translateX(0); opacity: 1; }
                    to { transform: translateX(100%); opacity: 0; }
                }
            `;
            document.head.appendChild(style);
        }
    }
};

// Global functions for Blazor interop
window.setTheme = function(isDark) {
    themeManager.applyTheme(isDark ? 'dark' : 'light');
};

window.toggleTheme = function() {
    themeManager.toggle();
    return themeManager.currentTheme === 'dark';
};

window.copyToClipboard = async function(text) {
    return await themeManager.copyToClipboard(text);
};

window.highlightJson = function(json, isDark) {
    return themeManager.highlightJson(json, isDark);
};

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    themeManager.init();
});

// Initialize on Blazor app start
window.addEventListener('load', () => {
    setTimeout(() => themeManager.init(), 100);
});