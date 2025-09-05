// Accessibility Enhancement System
window.accessibility = {
    dotNetRef: null,
    announcementRegion: null,
    focusTraps: new Map(),
    isInitialized: false,
    userPreferences: {
        prefersReducedMotion: false,
        prefersHighContrast: false,
        usesScreenReader: false,
        colorScheme: 'auto',
        fontScale: 1.0
    },

    init(dotNetObjectReference) {
        if (this.isInitialized) return;
        
        this.dotNetRef = dotNetObjectReference;
        this.setupAnnouncementRegion();
        this.detectUserPreferences();
        this.setupEventListeners();
        this.addAccessibilityStyles();
        this.createSkipLinks();
        this.isInitialized = true;
        console.log('Accessibility system initialized');
    },

    setupAnnouncementRegion() {
        // Create screen reader announcement region
        this.announcementRegion = document.createElement('div');
        this.announcementRegion.setAttribute('aria-live', 'polite');
        this.announcementRegion.setAttribute('aria-atomic', 'true');
        this.announcementRegion.setAttribute('role', 'status');
        this.announcementRegion.style.cssText = `
            position: absolute;
            left: -10000px;
            width: 1px;
            height: 1px;
            overflow: hidden;
        `;
        document.body.appendChild(this.announcementRegion);
    },

    detectUserPreferences() {
        // Detect reduced motion preference
        this.userPreferences.prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        
        // Detect high contrast preference
        this.userPreferences.prefersHighContrast = window.matchMedia('(prefers-contrast: high)').matches ||
                                                   window.matchMedia('(-ms-high-contrast: active)').matches;
        
        // Detect color scheme preference
        if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
            this.userPreferences.colorScheme = 'dark';
        } else if (window.matchMedia('(prefers-color-scheme: light)').matches) {
            this.userPreferences.colorScheme = 'light';
        }

        // Detect screen reader usage (heuristic)
        this.userPreferences.usesScreenReader = this.detectScreenReader();

        // Apply preferences
        this.applyUserPreferences();
    },

    detectScreenReader() {
        // Heuristic detection of screen reader usage
        return navigator.userAgent.includes('NVDA') ||
               navigator.userAgent.includes('JAWS') ||
               navigator.userAgent.includes('VoiceOver') ||
               window.speechSynthesis?.speaking ||
               document.documentElement.getAttribute('aria-hidden') === 'false';
    },

    applyUserPreferences() {
        const root = document.documentElement;

        if (this.userPreferences.prefersReducedMotion) {
            root.style.setProperty('--animation-duration', '0.01ms');
            root.style.setProperty('--transition-duration', '0.01ms');
            root.setAttribute('data-reduced-motion', 'true');
        }

        if (this.userPreferences.prefersHighContrast) {
            root.setAttribute('data-high-contrast', 'true');
        }

        root.setAttribute('data-color-scheme', this.userPreferences.colorScheme);
    },

    setupEventListeners() {
        // Listen for preference changes
        window.matchMedia('(prefers-reduced-motion: reduce)').addEventListener('change', (e) => {
            this.userPreferences.prefersReducedMotion = e.matches;
            this.applyUserPreferences();
        });

        window.matchMedia('(prefers-contrast: high)').addEventListener('change', (e) => {
            this.userPreferences.prefersHighContrast = e.matches;
            this.applyUserPreferences();
        });

        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
            this.userPreferences.colorScheme = e.matches ? 'dark' : 'light';
            this.applyUserPreferences();
        });

        // Focus management
        document.addEventListener('focusin', (e) => {
            this.onFocusChange(e.target, true);
        });

        document.addEventListener('focusout', (e) => {
            this.onFocusChange(e.target, false);
        });

        // Keyboard navigation
        document.addEventListener('keydown', (e) => {
            this.handleKeyboardNavigation(e);
        });
    },

    onFocusChange(element, hasFocus) {
        // Add visual focus indicators
        if (hasFocus) {
            element.classList.add('accessibility-focus');
            this.ensureElementVisible(element);
        } else {
            element.classList.remove('accessibility-focus');
        }

        // Notify .NET of focus changes
        if (this.dotNetRef && element.id) {
            this.dotNetRef.invokeMethodAsync('OnFocusChanged', element.id, hasFocus);
        }
    },

    handleKeyboardNavigation(event) {
        // Skip to main content with Alt+M
        if (event.altKey && event.key.toLowerCase() === 'm') {
            event.preventDefault();
            const main = document.querySelector('main, [role="main"], #main-content');
            if (main) {
                main.focus();
                this.announceToScreenReader('Navigated to main content');
            }
        }

        // Skip to navigation with Alt+N
        if (event.altKey && event.key.toLowerCase() === 'n') {
            event.preventDefault();
            const nav = document.querySelector('nav, [role="navigation"], .navigation');
            if (nav) {
                const firstLink = nav.querySelector('a, button');
                if (firstLink) {
                    firstLink.focus();
                    this.announceToScreenReader('Navigated to navigation');
                }
            }
        }

        // Toggle high contrast with Ctrl+Alt+H
        if (event.ctrlKey && event.altKey && event.key.toLowerCase() === 'h') {
            event.preventDefault();
            this.toggleHighContrast();
        }
    },

    ensureElementVisible(element) {
        // Ensure focused element is visible
        if (element.scrollIntoViewIfNeeded) {
            element.scrollIntoViewIfNeeded();
        } else {
            element.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        }
    },

    announceToScreenReader(message, priority = 'polite') {
        if (!this.announcementRegion) return;

        // Clear previous message
        this.announcementRegion.textContent = '';
        
        // Set priority
        this.announcementRegion.setAttribute('aria-live', priority);
        
        // Announce message after a brief delay
        setTimeout(() => {
            this.announcementRegion.textContent = message;
        }, 100);

        console.log('Screen reader announcement:', message);
    },

    setAriaLabel(elementId, label) {
        const element = document.getElementById(elementId);
        if (element) {
            element.setAttribute('aria-label', label);
        }
    },

    setAriaDescribedBy(elementId, describedById) {
        const element = document.getElementById(elementId);
        if (element) {
            element.setAttribute('aria-describedby', describedById);
        }
    },

    setAriaExpanded(elementId, expanded) {
        const element = document.getElementById(elementId);
        if (element) {
            element.setAttribute('aria-expanded', expanded.toString());
        }
    },

    setElementRole(elementId, role) {
        const element = document.getElementById(elementId);
        if (element) {
            element.setAttribute('role', role);
        }
    },

    setTabIndex(elementId, tabIndex) {
        const element = document.getElementById(elementId);
        if (element) {
            element.setAttribute('tabindex', tabIndex.toString());
        }
    },

    focusElement(elementId) {
        const element = document.getElementById(elementId);
        if (element) {
            element.focus();
        }
    },

    focusElementWithOptions(elementId, options) {
        const element = document.getElementById(elementId);
        if (element) {
            element.focus({ preventScroll: options.preventScroll });
            if (options.selectText && element.select) {
                element.select();
            }
        }
    },

    createSkipLinks() {
        const skipLinksContainer = document.createElement('div');
        skipLinksContainer.className = 'skip-links';
        skipLinksContainer.innerHTML = `
            <a href="#main-content" class="skip-link">Skip to main content</a>
            <a href="#navigation" class="skip-link">Skip to navigation</a>
        `;
        document.body.insertBefore(skipLinksContainer, document.body.firstChild);
    },

    createSkipLink(targetId, text, position) {
        const skipLink = document.createElement('a');
        skipLink.href = `#${targetId}`;
        skipLink.className = `skip-link skip-link-${position}`;
        skipLink.textContent = text;
        skipLink.addEventListener('click', (e) => {
            e.preventDefault();
            const target = document.getElementById(targetId);
            if (target) {
                target.focus();
                this.announceToScreenReader(`Navigated to ${text}`);
            }
        });
        document.body.appendChild(skipLink);
    },

    addLiveRegion(elementId, type) {
        const element = document.getElementById(elementId);
        if (element) {
            element.setAttribute('aria-live', type);
            element.setAttribute('aria-atomic', 'true');
        }
    },

    updateLiveRegion(elementId, content) {
        const element = document.getElementById(elementId);
        if (element) {
            element.textContent = content;
        }
    },

    setupFocusTrap(containerId) {
        const container = document.getElementById(containerId);
        if (!container) return;

        const focusableElements = container.querySelectorAll(
            'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
        );

        if (focusableElements.length === 0) return;

        const firstFocusable = focusableElements[0];
        const lastFocusable = focusableElements[focusableElements.length - 1];

        const trapFunction = (e) => {
            if (e.key === 'Tab') {
                if (e.shiftKey) {
                    if (document.activeElement === firstFocusable) {
                        lastFocusable.focus();
                        e.preventDefault();
                    }
                } else {
                    if (document.activeElement === lastFocusable) {
                        firstFocusable.focus();
                        e.preventDefault();
                    }
                }
            }
            
            if (e.key === 'Escape') {
                this.removeFocusTrap(containerId);
            }
        };

        container.addEventListener('keydown', trapFunction);
        this.focusTraps.set(containerId, { container, trapFunction, firstFocusable });
        
        // Focus first element
        firstFocusable.focus();
    },

    removeFocusTrap(containerId) {
        const trap = this.focusTraps.get(containerId);
        if (trap) {
            trap.container.removeEventListener('keydown', trap.trapFunction);
            this.focusTraps.delete(containerId);
        }
    },

    enableHighContrastMode(enable) {
        const root = document.documentElement;
        if (enable) {
            root.setAttribute('data-high-contrast', 'true');
            this.announceToScreenReader('High contrast mode enabled');
        } else {
            root.removeAttribute('data-high-contrast');
            this.announceToScreenReader('High contrast mode disabled');
        }
    },

    enableReducedMotion(enable) {
        const root = document.documentElement;
        if (enable) {
            root.setAttribute('data-reduced-motion', 'true');
            this.announceToScreenReader('Reduced motion enabled');
        } else {
            root.removeAttribute('data-reduced-motion');
            this.announceToScreenReader('Reduced motion disabled');
        }
    },

    toggleHighContrast() {
        const root = document.documentElement;
        const isEnabled = root.getAttribute('data-high-contrast') === 'true';
        this.enableHighContrastMode(!isEnabled);
    },

    getUserAccessibilityPreferences() {
        return this.userPreferences;
    },

    validateAccessibility(elementId) {
        const element = document.getElementById(elementId);
        if (!element) return;

        const issues = [];

        // Check for missing alt text on images
        if (element.tagName === 'IMG' && !element.alt) {
            issues.push({
                elementId,
                issueType: 'missing-alt-text',
                description: 'Image missing alt attribute',
                severity: 'error',
                recommendation: 'Add descriptive alt text'
            });
        }

        // Check for low contrast
        const styles = window.getComputedStyle(element);
        const color = styles.color;
        const backgroundColor = styles.backgroundColor;
        
        if (this.hasLowContrast(color, backgroundColor)) {
            issues.push({
                elementId,
                issueType: 'low-contrast',
                description: 'Text has insufficient contrast ratio',
                severity: 'warning',
                recommendation: 'Increase color contrast'
            });
        }

        // Check for missing labels on form elements
        if (['INPUT', 'SELECT', 'TEXTAREA'].includes(element.tagName)) {
            const hasLabel = element.labels?.length > 0 || 
                           element.getAttribute('aria-label') ||
                           element.getAttribute('aria-labelledby');
            
            if (!hasLabel) {
                issues.push({
                    elementId,
                    issueType: 'missing-label',
                    description: 'Form element missing accessible label',
                    severity: 'error',
                    recommendation: 'Add a label or aria-label'
                });
            }
        }

        // Report issues to .NET
        if (this.dotNetRef) {
            issues.forEach(issue => {
                this.dotNetRef.invokeMethodAsync('OnAccessibilityIssueDetected', issue);
            });
        }
    },

    hasLowContrast(foreground, background) {
        // Simplified contrast checking
        // In a real implementation, you'd calculate the actual contrast ratio
        return false; // Placeholder
    },

    addAccessibilityStyles() {
        const styles = document.createElement('style');
        styles.textContent = `
            /* Skip links */
            .skip-links {
                position: absolute;
                top: -100px;
                left: 0;
                z-index: 10000;
            }
            
            .skip-link {
                position: absolute;
                top: -100px;
                left: 0;
                background: var(--primary-color, #3b82f6);
                color: white;
                padding: 8px 16px;
                text-decoration: none;
                border-radius: 4px;
                font-weight: 600;
                z-index: 10000;
                transition: top 0.2s ease;
            }
            
            .skip-link:focus {
                top: 10px;
                left: 10px;
            }
            
            /* Focus indicators */
            .accessibility-focus {
                outline: 3px solid var(--primary-color, #3b82f6) !important;
                outline-offset: 2px !important;
                box-shadow: 0 0 0 1px rgba(59, 130, 246, 0.3) !important;
            }
            
            /* High contrast mode */
            [data-high-contrast="true"] {
                --dark-bg: #000000 !important;
                --dark-bg-secondary: #1a1a1a !important;
                --dark-surface: #2a2a2a !important;
                --dark-text: #ffffff !important;
                --dark-text-secondary: #cccccc !important;
                --dark-border: #555555 !important;
                --primary-color: #66aaff !important;
                --json-string: #66ff66 !important;
                --json-number: #ff9999 !important;
                --json-boolean: #ffff66 !important;
                --json-null: #cccccc !important;
            }
            
            [data-high-contrast="true"] * {
                border-color: #555555 !important;
            }
            
            [data-high-contrast="true"] button,
            [data-high-contrast="true"] input,
            [data-high-contrast="true"] select,
            [data-high-contrast="true"] textarea {
                border: 2px solid #ffffff !important;
                background: #000000 !important;
                color: #ffffff !important;
            }
            
            /* Reduced motion */
            [data-reduced-motion="true"],
            [data-reduced-motion="true"] * {
                animation-duration: 0.01ms !important;
                animation-iteration-count: 1 !important;
                transition-duration: 0.01ms !important;
                scroll-behavior: auto !important;
            }
            
            /* Screen reader only content */
            .sr-only {
                position: absolute;
                width: 1px;
                height: 1px;
                padding: 0;
                margin: -1px;
                overflow: hidden;
                clip: rect(0, 0, 0, 0);
                white-space: nowrap;
                border: 0;
            }
            
            /* Focus visible for keyboard users */
            .js-focus-visible :focus:not(.focus-visible) {
                outline: none;
            }
            
            .focus-visible {
                outline: 3px solid var(--primary-color, #3b82f6);
                outline-offset: 2px;
            }
            
            /* Ensure interactive elements are large enough */
            button, input[type="button"], input[type="submit"], 
            input[type="reset"], .btn, a[role="button"] {
                min-height: 44px;
                min-width: 44px;
            }
            
            /* Improve color contrast for links */
            a {
                text-decoration: underline;
            }
            
            a:hover, a:focus {
                text-decoration: none;
                background: rgba(59, 130, 246, 0.1);
                outline: 2px solid var(--primary-color, #3b82f6);
                outline-offset: 2px;
            }
            
            /* Ensure form labels are clearly associated */
            label {
                font-weight: 600;
                margin-bottom: 4px;
                display: block;
            }
            
            /* Error states */
            [aria-invalid="true"] {
                border-color: #dc3545 !important;
                box-shadow: 0 0 0 1px #dc3545 !important;
            }
            
            .error-message {
                color: #dc3545;
                font-size: 0.875rem;
                margin-top: 4px;
            }
            
            /* Loading states */
            [aria-busy="true"] {
                pointer-events: none;
                opacity: 0.7;
            }
            
            /* Responsive text sizing */
            @media (max-width: 768px) {
                body {
                    font-size: 18px;
                    line-height: 1.6;
                }
                
                button, input, select, textarea {
                    font-size: 18px;
                    min-height: 44px;
                }
            }
        `;
        document.head.appendChild(styles);
    }
};

// Global functions for .NET interop
window.initializeAccessibility = (dotNetObjectReference) => {
    window.accessibility.init(dotNetObjectReference);
};

window.announceToScreenReader = (message, priority) => {
    window.accessibility.announceToScreenReader(message, priority);
};

window.setAriaLabel = (elementId, label) => {
    window.accessibility.setAriaLabel(elementId, label);
};

window.setAriaDescribedBy = (elementId, describedById) => {
    window.accessibility.setAriaDescribedBy(elementId, describedById);
};

window.setAriaExpanded = (elementId, expanded) => {
    window.accessibility.setAriaExpanded(elementId, expanded);
};

window.setElementRole = (elementId, role) => {
    window.accessibility.setElementRole(elementId, role);
};

window.setTabIndex = (elementId, tabIndex) => {
    window.accessibility.setTabIndex(elementId, tabIndex);
};

window.focusElement = (elementId) => {
    window.accessibility.focusElement(elementId);
};

window.focusElementWithOptions = (elementId, options) => {
    window.accessibility.focusElementWithOptions(elementId, options);
};

window.createSkipLink = (targetId, text, position) => {
    window.accessibility.createSkipLink(targetId, text, position);
};

window.addLiveRegion = (elementId, type) => {
    window.accessibility.addLiveRegion(elementId, type);
};

window.updateLiveRegion = (elementId, content) => {
    window.accessibility.updateLiveRegion(elementId, content);
};

window.setupFocusTrap = (containerId) => {
    window.accessibility.setupFocusTrap(containerId);
};

window.removeFocusTrap = (containerId) => {
    window.accessibility.removeFocusTrap(containerId);
};

window.enableHighContrastMode = (enable) => {
    window.accessibility.enableHighContrastMode(enable);
};

window.enableReducedMotion = (enable) => {
    window.accessibility.enableReducedMotion(enable);
};

window.getUserAccessibilityPreferences = () => {
    return window.accessibility.getUserAccessibilityPreferences();
};

window.validateAccessibility = (elementId) => {
    window.accessibility.validateAccessibility(elementId);
};

console.log('Accessibility system loaded');