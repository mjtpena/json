// Keyboard Shortcuts Management
window.keyboardShortcuts = {
    dotNetRef: null,
    shortcuts: new Map(),
    isInitialized: false,

    init(dotNetObjectReference) {
        if (this.isInitialized) return;
        
        this.dotNetRef = dotNetObjectReference;
        this.bindEvents();
        this.isInitialized = true;
        console.log('Keyboard shortcuts initialized');
    },

    bindEvents() {
        document.addEventListener('keydown', this.handleKeyDown.bind(this));
        
        // Prevent default browser shortcuts for our custom ones
        document.addEventListener('keydown', (e) => {
            const key = this.getKeyString(e);
            
            // Prevent browser defaults for our shortcuts
            const preventDefaults = [
                'ctrl+enter', 'ctrl+shift+m', 'ctrl+shift+v', 'ctrl+shift+c',
                'ctrl+shift+f', 'ctrl+shift+d', 'ctrl+shift+g', 'ctrl+shift+q',
                'ctrl+k', 'alt+1', 'alt+2', 'alt+3', 'alt+4', 'alt+5',
                'alt+6', 'alt+7', 'alt+8', 'f11'
            ];
            
            if (preventDefaults.includes(key.toLowerCase())) {
                e.preventDefault();
                e.stopPropagation();
            }
        });
    },

    handleKeyDown(event) {
        // Don't handle shortcuts when typing in input fields (unless specifically allowed)
        const activeElement = document.activeElement;
        const isInputField = activeElement && (
            activeElement.tagName === 'INPUT' || 
            activeElement.tagName === 'TEXTAREA' ||
            activeElement.contentEditable === 'true'
        );

        const keyString = this.getKeyString(event);
        
        // Allow certain shortcuts in input fields
        const allowedInInput = [
            'ctrl+enter', 'ctrl+shift+v', 'ctrl+shift+c', 'ctrl+shift+f',
            'escape', 'f1', 'f11', 'ctrl+k'
        ];
        
        if (isInputField && !allowedInInput.includes(keyString.toLowerCase())) {
            return;
        }

        // Send to .NET for handling
        if (this.dotNetRef) {
            this.dotNetRef.invokeMethodAsync('HandleShortcut', keyString);
        }
    },

    getKeyString(event) {
        const parts = [];
        
        if (event.ctrlKey || event.metaKey) parts.push('ctrl');
        if (event.altKey) parts.push('alt');
        if (event.shiftKey) parts.push('shift');
        
        // Handle special keys
        let key = event.key.toLowerCase();
        
        const specialKeys = {
            ' ': 'space',
            'enter': 'enter',
            'escape': 'escape',
            'tab': 'tab',
            'backspace': 'backspace',
            'delete': 'delete',
            'arrowup': 'up',
            'arrowdown': 'down',
            'arrowleft': 'left',
            'arrowright': 'right',
            'f1': 'f1', 'f2': 'f2', 'f3': 'f3', 'f4': 'f4',
            'f5': 'f5', 'f6': 'f6', 'f7': 'f7', 'f8': 'f8',
            'f9': 'f9', 'f10': 'f10', 'f11': 'f11', 'f12': 'f12'
        };
        
        if (specialKeys[key]) {
            key = specialKeys[key];
        } else if (key.length === 1) {
            key = key.toLowerCase();
        }
        
        parts.push(key);
        
        return parts.join('+');
    },

    showHelp() {
        const helpModal = document.createElement('div');
        helpModal.className = 'keyboard-help-modal';
        helpModal.innerHTML = `
            <div class="keyboard-help-overlay" onclick="this.parentElement.remove()">
                <div class="keyboard-help-content" onclick="event.stopPropagation()">
                    <div class="keyboard-help-header">
                        <h2>Keyboard Shortcuts</h2>
                        <button onclick="this.closest('.keyboard-help-modal').remove()" class="close-btn">&times;</button>
                    </div>
                    <div class="keyboard-help-body">
                        <div class="shortcut-category">
                            <h3>JSON Operations</h3>
                            <div class="shortcut-item">
                                <kbd>Ctrl</kbd> + <kbd>Enter</kbd>
                                <span>Format JSON</span>
                            </div>
                            <div class="shortcut-item">
                                <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>M</kbd>
                                <span>Minify JSON</span>
                            </div>
                            <div class="shortcut-item">
                                <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>V</kbd>
                                <span>Validate JSON</span>
                            </div>
                            <div class="shortcut-item">
                                <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>C</kbd>
                                <span>Copy to Clipboard</span>
                            </div>
                        </div>
                        
                        <div class="shortcut-category">
                            <h3>Navigation</h3>
                            <div class="shortcut-item">
                                <kbd>Alt</kbd> + <kbd>1-8</kbd>
                                <span>Switch between tools</span>
                            </div>
                            <div class="shortcut-item">
                                <kbd>Ctrl</kbd> + <kbd>K</kbd>
                                <span>Command Palette</span>
                            </div>
                            <div class="shortcut-item">
                                <kbd>Escape</kbd>
                                <span>Close dialogs</span>
                            </div>
                        </div>
                        
                        <div class="shortcut-category">
                            <h3>Advanced</h3>
                            <div class="shortcut-item">
                                <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>D</kbd>
                                <span>JSON Diff</span>
                            </div>
                            <div class="shortcut-item">
                                <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>G</kbd>
                                <span>Generate Data</span>
                            </div>
                            <div class="shortcut-item">
                                <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>Q</kbd>
                                <span>Execute Query</span>
                            </div>
                            <div class="shortcut-item">
                                <kbd>F11</kbd>
                                <span>Fullscreen</span>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `;
        
        document.body.appendChild(helpModal);
        
        // Focus trap
        const focusableElements = helpModal.querySelectorAll('button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])');
        const firstFocusable = focusableElements[0];
        const lastFocusable = focusableElements[focusableElements.length - 1];
        
        firstFocusable?.focus();
        
        helpModal.addEventListener('keydown', (e) => {
            if (e.key === 'Tab') {
                if (e.shiftKey) {
                    if (document.activeElement === firstFocusable) {
                        lastFocusable?.focus();
                        e.preventDefault();
                    }
                } else {
                    if (document.activeElement === lastFocusable) {
                        firstFocusable?.focus();
                        e.preventDefault();
                    }
                }
            } else if (e.key === 'Escape') {
                helpModal.remove();
            }
        });
    },

    toggleFullscreen() {
        if (!document.fullscreenElement) {
            document.documentElement.requestFullscreen().catch(err => {
                console.log('Error attempting to enable fullscreen:', err.message);
            });
        } else {
            if (document.exitFullscreen) {
                document.exitFullscreen();
            }
        }
    }
};

// Global function to initialize from .NET
window.initializeKeyboardShortcuts = (dotNetObjectReference) => {
    window.keyboardShortcuts.init(dotNetObjectReference);
};

// Add CSS for help modal
const style = document.createElement('style');
style.textContent = `
    .keyboard-help-modal {
        position: fixed;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        z-index: 10000;
        background: rgba(0, 0, 0, 0.8);
        display: flex;
        align-items: center;
        justify-content: center;
        animation: fadeIn 0.2s ease-out;
    }
    
    .keyboard-help-overlay {
        width: 100%;
        height: 100%;
        display: flex;
        align-items: center;
        justify-content: center;
    }
    
    .keyboard-help-content {
        background: var(--dark-bg-secondary, #161B22);
        border: 1px solid var(--dark-border, #3D4451);
        border-radius: 12px;
        max-width: 800px;
        max-height: 80vh;
        overflow-y: auto;
        animation: slideUp 0.3s ease-out;
    }
    
    .keyboard-help-header {
        display: flex;
        justify-content: between;
        align-items: center;
        padding: 1.5rem 2rem;
        border-bottom: 1px solid var(--dark-border, #3D4451);
    }
    
    .keyboard-help-header h2 {
        margin: 0;
        color: var(--dark-text, #F0F6FC);
        font-size: 1.5rem;
    }
    
    .close-btn {
        background: none;
        border: none;
        color: var(--dark-text-secondary, #8B949E);
        font-size: 2rem;
        cursor: pointer;
        padding: 0;
        width: 2rem;
        height: 2rem;
        display: flex;
        align-items: center;
        justify-content: center;
        border-radius: 50%;
        transition: all 0.2s ease;
    }
    
    .close-btn:hover {
        background: rgba(255, 255, 255, 0.1);
        color: var(--dark-text, #F0F6FC);
    }
    
    .keyboard-help-body {
        padding: 2rem;
        display: grid;
        gap: 2rem;
        grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
    }
    
    .shortcut-category {
        
    }
    
    .shortcut-category h3 {
        margin: 0 0 1rem 0;
        color: var(--dark-text, #F0F6FC);
        font-size: 1.1rem;
        font-weight: 600;
    }
    
    .shortcut-item {
        display: flex;
        justify-content: between;
        align-items: center;
        padding: 0.75rem 0;
        border-bottom: 1px solid rgba(255, 255, 255, 0.1);
    }
    
    .shortcut-item:last-child {
        border-bottom: none;
    }
    
    .shortcut-item kbd {
        background: var(--dark-surface, #30363D);
        color: var(--dark-text, #F0F6FC);
        padding: 0.25rem 0.5rem;
        border-radius: 4px;
        font-size: 0.875rem;
        font-family: var(--font-mono, monospace);
        border: 1px solid var(--dark-border, #3D4451);
        margin-right: 0.25rem;
    }
    
    .shortcut-item span {
        color: var(--dark-text-secondary, #8B949E);
        margin-left: auto;
    }
    
    @keyframes fadeIn {
        from { opacity: 0; }
        to { opacity: 1; }
    }
    
    @keyframes slideUp {
        from {
            opacity: 0;
            transform: translateY(20px) scale(0.95);
        }
        to {
            opacity: 1;
            transform: translateY(0) scale(1);
        }
    }
    
    @media (max-width: 768px) {
        .keyboard-help-content {
            margin: 1rem;
            max-width: none;
        }
        
        .keyboard-help-body {
            grid-template-columns: 1fr;
            padding: 1rem;
        }
        
        .keyboard-help-header {
            padding: 1rem;
        }
    }
`;

document.head.appendChild(style);