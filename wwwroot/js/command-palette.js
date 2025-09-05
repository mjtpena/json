// Command Palette Implementation
window.commandPalette = {
    dotNetRef: null,
    isOpen: false,
    currentQuery: '',
    selectedIndex: 0,
    commands: [],
    filteredCommands: [],
    paletteElement: null,

    init(dotNetObjectReference) {
        this.dotNetRef = dotNetObjectReference;
        this.createPalette();
        this.bindEvents();
        console.log('Command palette initialized');
    },

    createPalette() {
        const palette = document.createElement('div');
        palette.className = 'command-palette-overlay';
        palette.style.display = 'none';
        palette.innerHTML = `
            <div class="command-palette-backdrop" onclick="window.commandPalette.hide()"></div>
            <div class="command-palette">
                <div class="command-palette-header">
                    <div class="command-palette-search">
                        <svg class="search-icon" viewBox="0 0 24 24" width="20" height="20">
                            <path fill="currentColor" d="M15.5 14h-.79l-.28-.27A6.471 6.471 0 0 0 16 9.5 6.5 6.5 0 1 0 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z"/>
                        </svg>
                        <input 
                            type="text" 
                            class="command-search-input" 
                            placeholder="Type a command or search..."
                            autocomplete="off"
                            spellcheck="false">
                    </div>
                    <button class="command-palette-close" onclick="window.commandPalette.hide()">
                        <svg viewBox="0 0 24 24" width="16" height="16">
                            <path fill="currentColor" d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/>
                        </svg>
                    </button>
                </div>
                <div class="command-palette-body">
                    <div class="command-list"></div>
                </div>
                <div class="command-palette-footer">
                    <div class="command-palette-tips">
                        <span class="tip">↑↓ Navigate</span>
                        <span class="tip">↵ Execute</span>
                        <span class="tip">Esc Close</span>
                    </div>
                </div>
            </div>
        `;

        document.body.appendChild(palette);
        this.paletteElement = palette;
    },

    bindEvents() {
        const searchInput = this.paletteElement.querySelector('.command-search-input');
        
        searchInput.addEventListener('input', (e) => {
            this.currentQuery = e.target.value;
            this.search();
        });

        searchInput.addEventListener('keydown', (e) => {
            switch (e.key) {
                case 'ArrowDown':
                    e.preventDefault();
                    this.selectNext();
                    break;
                case 'ArrowUp':
                    e.preventDefault();
                    this.selectPrevious();
                    break;
                case 'Enter':
                    e.preventDefault();
                    this.executeSelected();
                    break;
                case 'Escape':
                    e.preventDefault();
                    this.hide();
                    break;
                case 'Tab':
                    e.preventDefault();
                    if (this.filteredCommands.length > 0) {
                        const command = this.filteredCommands[this.selectedIndex];
                        searchInput.value = command.title;
                        this.currentQuery = command.title;
                        this.search();
                    }
                    break;
            }
        });

        // Click handling for command items
        this.paletteElement.addEventListener('click', (e) => {
            const commandItem = e.target.closest('.command-item');
            if (commandItem) {
                const index = parseInt(commandItem.dataset.index);
                this.selectedIndex = index;
                this.executeSelected();
            }
        });
    },

    async show() {
        this.isOpen = true;
        this.paletteElement.style.display = 'flex';
        
        // Focus search input
        const searchInput = this.paletteElement.querySelector('.command-search-input');
        searchInput.value = '';
        searchInput.focus();
        
        // Reset state
        this.currentQuery = '';
        this.selectedIndex = 0;
        
        // Load initial commands
        await this.search();
        
        // Add animation class
        this.paletteElement.classList.add('command-palette-show');
    },

    hide() {
        this.isOpen = false;
        this.paletteElement.classList.remove('command-palette-show');
        
        // Delay hiding to allow animation
        setTimeout(() => {
            this.paletteElement.style.display = 'none';
        }, 200);
    },

    async search() {
        if (!this.dotNetRef) return;

        try {
            this.filteredCommands = await this.dotNetRef.invokeMethodAsync('SearchCommands', this.currentQuery);
            this.renderCommands();
            this.updateSelection();
        } catch (error) {
            console.error('Error searching commands:', error);
        }
    },

    renderCommands() {
        const commandList = this.paletteElement.querySelector('.command-list');
        
        if (this.filteredCommands.length === 0) {
            commandList.innerHTML = `
                <div class="no-commands">
                    <div class="no-commands-icon">🔍</div>
                    <div class="no-commands-text">No commands found</div>
                    <div class="no-commands-subtext">Try a different search term</div>
                </div>
            `;
            return;
        }

        let currentCategory = '';
        let html = '';

        this.filteredCommands.forEach((command, index) => {
            if (command.category !== currentCategory) {
                currentCategory = command.category;
                html += `<div class="command-category">${currentCategory}</div>`;
            }

            const iconHtml = command.icon 
                ? `<span class="command-icon material-icons">${command.icon}</span>`
                : '<span class="command-icon">⚡</span>';

            const shortcutHtml = command.shortcut 
                ? `<span class="command-shortcut">${this.formatShortcut(command.shortcut)}</span>`
                : '';

            html += `
                <div class="command-item" data-index="${index}">
                    <div class="command-main">
                        ${iconHtml}
                        <div class="command-content">
                            <div class="command-title">${this.highlightQuery(command.title)}</div>
                            <div class="command-description">${command.description}</div>
                        </div>
                    </div>
                    ${shortcutHtml}
                </div>
            `;
        });

        commandList.innerHTML = html;
    },

    highlightQuery(text) {
        if (!this.currentQuery) return text;
        
        const regex = new RegExp(`(${this.escapeRegex(this.currentQuery)})`, 'gi');
        return text.replace(regex, '<mark>$1</mark>');
    },

    escapeRegex(text) {
        return text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    },

    formatShortcut(shortcut) {
        return shortcut.split('+')
                      .map(key => `<kbd>${key}</kbd>`)
                      .join('<span class="shortcut-plus">+</span>');
    },

    selectNext() {
        if (this.filteredCommands.length === 0) return;
        this.selectedIndex = (this.selectedIndex + 1) % this.filteredCommands.length;
        this.updateSelection();
        this.scrollToSelected();
    },

    selectPrevious() {
        if (this.filteredCommands.length === 0) return;
        this.selectedIndex = this.selectedIndex === 0 
            ? this.filteredCommands.length - 1 
            : this.selectedIndex - 1;
        this.updateSelection();
        this.scrollToSelected();
    },

    updateSelection() {
        const items = this.paletteElement.querySelectorAll('.command-item');
        items.forEach((item, index) => {
            item.classList.toggle('selected', index === this.selectedIndex);
        });
    },

    scrollToSelected() {
        const selectedItem = this.paletteElement.querySelector('.command-item.selected');
        if (selectedItem) {
            selectedItem.scrollIntoView({ 
                behavior: 'smooth', 
                block: 'nearest' 
            });
        }
    },

    async executeSelected() {
        if (this.filteredCommands.length === 0 || !this.dotNetRef) return;
        
        const selectedCommand = this.filteredCommands[this.selectedIndex];
        if (selectedCommand) {
            this.hide();
            try {
                await this.dotNetRef.invokeMethodAsync('ExecuteCommand', selectedCommand.id);
            } catch (error) {
                console.error('Error executing command:', error);
            }
        }
    }
};

// Global functions for .NET interop
window.initializeCommandPalette = (dotNetObjectReference) => {
    window.commandPalette.init(dotNetObjectReference);
};

window.showCommandPalette = () => {
    window.commandPalette.show();
};

window.hideCommandPalette = () => {
    window.commandPalette.hide();
};

window.showCommandError = (commandId, error) => {
    console.error(`Command ${commandId} failed:`, error);
    // You could show a toast notification here
};

// Add CSS styles
const commandPaletteStyles = document.createElement('style');
commandPaletteStyles.textContent = `
    .command-palette-overlay {
        position: fixed;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        z-index: 10000;
        display: flex;
        align-items: flex-start;
        justify-content: center;
        padding-top: 10vh;
        opacity: 0;
        transition: opacity 0.2s ease-out;
    }
    
    .command-palette-overlay.command-palette-show {
        opacity: 1;
    }
    
    .command-palette-backdrop {
        position: absolute;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        background: rgba(0, 0, 0, 0.5);
        backdrop-filter: blur(4px);
    }
    
    .command-palette {
        position: relative;
        width: 90%;
        max-width: 600px;
        background: var(--dark-bg-secondary, #161B22);
        border: 1px solid var(--dark-border, #3D4451);
        border-radius: 12px;
        box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5);
        overflow: hidden;
        transform: translateY(-20px) scale(0.95);
        transition: transform 0.2s ease-out;
        max-height: 70vh;
        display: flex;
        flex-direction: column;
    }
    
    .command-palette-overlay.command-palette-show .command-palette {
        transform: translateY(0) scale(1);
    }
    
    .command-palette-header {
        display: flex;
        align-items: center;
        padding: 1rem;
        border-bottom: 1px solid var(--dark-border, #3D4451);
        background: var(--dark-surface, #0D1117);
    }
    
    .command-palette-search {
        flex: 1;
        display: flex;
        align-items: center;
        gap: 0.75rem;
    }
    
    .search-icon {
        color: var(--dark-text-secondary, #8B949E);
        flex-shrink: 0;
    }
    
    .command-search-input {
        flex: 1;
        background: transparent;
        border: none;
        outline: none;
        color: var(--dark-text, #F0F6FC);
        font-size: 1.125rem;
        font-family: var(--font-system, system-ui);
        font-weight: 400;
        placeholder-color: var(--dark-text-secondary, #8B949E);
    }
    
    .command-search-input::placeholder {
        color: var(--dark-text-secondary, #8B949E);
    }
    
    .command-palette-close {
        background: transparent;
        border: none;
        color: var(--dark-text-secondary, #8B949E);
        cursor: pointer;
        padding: 0.5rem;
        border-radius: 6px;
        display: flex;
        align-items: center;
        justify-content: center;
        transition: all 0.15s ease;
    }
    
    .command-palette-close:hover {
        background: var(--dark-surface, #21262D);
        color: var(--dark-text, #F0F6FC);
    }
    
    .command-palette-body {
        flex: 1;
        overflow-y: auto;
        padding: 0;
    }
    
    .command-list {
        padding: 0.5rem;
    }
    
    .command-category {
        font-size: 0.75rem;
        font-weight: 600;
        color: var(--dark-text-secondary, #8B949E);
        text-transform: uppercase;
        letter-spacing: 0.05em;
        padding: 0.75rem 0.75rem 0.5rem;
        margin-top: 1rem;
        position: sticky;
        top: 0;
        background: var(--dark-bg-secondary, #161B22);
        border-bottom: 1px solid rgba(255, 255, 255, 0.05);
    }
    
    .command-category:first-child {
        margin-top: 0;
    }
    
    .command-item {
        display: flex;
        align-items: center;
        padding: 0.75rem;
        border-radius: 8px;
        cursor: pointer;
        transition: all 0.15s ease;
        margin-bottom: 2px;
        border: 1px solid transparent;
    }
    
    .command-item:hover,
    .command-item.selected {
        background: var(--dark-surface, #21262D);
        border-color: var(--primary-color, #3b82f6);
    }
    
    .command-main {
        flex: 1;
        display: flex;
        align-items: center;
        gap: 0.75rem;
        min-width: 0;
    }
    
    .command-icon {
        font-size: 1.125rem;
        color: var(--primary-color, #3b82f6);
        flex-shrink: 0;
        width: 20px;
        height: 20px;
        display: flex;
        align-items: center;
        justify-content: center;
    }
    
    .command-content {
        flex: 1;
        min-width: 0;
    }
    
    .command-title {
        font-weight: 500;
        color: var(--dark-text, #F0F6FC);
        margin-bottom: 0.125rem;
        font-size: 0.9rem;
    }
    
    .command-title mark {
        background: var(--primary-color, #3b82f6);
        color: white;
        border-radius: 3px;
        padding: 0 2px;
    }
    
    .command-description {
        font-size: 0.8rem;
        color: var(--dark-text-secondary, #8B949E);
        line-height: 1.3;
    }
    
    .command-shortcut {
        font-size: 0.75rem;
        color: var(--dark-text-secondary, #8B949E);
        display: flex;
        align-items: center;
        gap: 2px;
        flex-shrink: 0;
    }
    
    .command-shortcut kbd {
        background: var(--dark-surface, #30363D);
        color: var(--dark-text, #F0F6FC);
        padding: 0.125rem 0.375rem;
        border-radius: 4px;
        font-size: 0.7rem;
        font-family: var(--font-mono, monospace);
        border: 1px solid var(--dark-border, #3D4451);
        line-height: 1;
        min-width: 20px;
        text-align: center;
    }
    
    .shortcut-plus {
        margin: 0 0.125rem;
        color: var(--dark-text-secondary, #8B949E);
    }
    
    .command-palette-footer {
        border-top: 1px solid var(--dark-border, #3D4451);
        padding: 0.75rem 1rem;
        background: var(--dark-surface, #0D1117);
    }
    
    .command-palette-tips {
        display: flex;
        gap: 1rem;
        justify-content: center;
    }
    
    .tip {
        font-size: 0.75rem;
        color: var(--dark-text-secondary, #8B949E);
        display: flex;
        align-items: center;
        gap: 0.25rem;
    }
    
    .no-commands {
        text-align: center;
        padding: 3rem 1rem;
        color: var(--dark-text-secondary, #8B949E);
    }
    
    .no-commands-icon {
        font-size: 3rem;
        margin-bottom: 1rem;
        opacity: 0.5;
    }
    
    .no-commands-text {
        font-size: 1.1rem;
        font-weight: 500;
        margin-bottom: 0.5rem;
        color: var(--dark-text, #F0F6FC);
    }
    
    .no-commands-subtext {
        font-size: 0.9rem;
        opacity: 0.7;
    }
    
    @media (max-width: 768px) {
        .command-palette {
            width: 95%;
            margin: 0 auto;
            max-height: 80vh;
        }
        
        .command-palette-tips {
            flex-wrap: wrap;
            gap: 0.5rem;
        }
        
        .command-item {
            padding: 1rem 0.75rem;
        }
        
        .command-shortcut {
            display: none;
        }
    }
`;

document.head.appendChild(commandPaletteStyles);