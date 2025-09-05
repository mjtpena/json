// Auto-completion and IntelliSense System
window.autoCompletion = {
    dotNetRef: null,
    editors: new Map(),
    isInitialized: false,
    
    init(dotNetObjectReference) {
        if (this.isInitialized) return;
        
        this.dotNetRef = dotNetObjectReference;
        this.isInitialized = true;
        console.log('Auto-completion system initialized');
    },

    registerEditor(editorId, options) {
        const editor = document.getElementById(editorId);
        if (!editor) {
            console.error('Editor not found:', editorId);
            return;
        }

        const autoCompleteEditor = new AutoCompleteEditor(editor, options, this.dotNetRef);
        this.editors.set(editorId, autoCompleteEditor);
        
        console.log('Registered auto-completion for editor:', editorId);
    },

    unregisterEditor(editorId) {
        const editor = this.editors.get(editorId);
        if (editor) {
            editor.destroy();
            this.editors.delete(editorId);
        }
    }
};

class AutoCompleteEditor {
    constructor(element, options, dotNetRef) {
        this.element = element;
        this.options = {
            enableSmartCompletion: true,
            enableSchemaValidation: true,
            enableContextualHelp: true,
            maxSuggestions: 50,
            triggerCharacters: ['"', ':', ',', '{', '['],
            completionDelay: 300,
            ...options
        };
        this.dotNetRef = dotNetRef;
        
        this.completionPopup = null;
        this.documentationPanel = null;
        this.completionTimeout = null;
        this.isShowingCompletions = false;
        this.selectedIndex = -1;
        this.completions = [];
        this.currentQuery = '';
        
        this.init();
    }

    init() {
        this.createCompletionPopup();
        this.createDocumentationPanel();
        this.bindEvents();
    }

    createCompletionPopup() {
        this.completionPopup = document.createElement('div');
        this.completionPopup.className = 'auto-completion-popup';
        this.completionPopup.style.display = 'none';
        document.body.appendChild(this.completionPopup);
    }

    createDocumentationPanel() {
        this.documentationPanel = document.createElement('div');
        this.documentationPanel.className = 'completion-documentation-panel';
        this.documentationPanel.style.display = 'none';
        document.body.appendChild(this.documentationPanel);
    }

    bindEvents() {
        // Input events for triggering completions
        this.element.addEventListener('input', this.handleInput.bind(this));
        this.element.addEventListener('keydown', this.handleKeyDown.bind(this));
        this.element.addEventListener('blur', this.hideCompletions.bind(this));
        this.element.addEventListener('scroll', this.updatePopupPosition.bind(this));
        
        // Window resize to reposition popup
        window.addEventListener('resize', this.updatePopupPosition.bind(this));
        
        // Click outside to hide
        document.addEventListener('click', (e) => {
            if (!this.completionPopup.contains(e.target) && e.target !== this.element) {
                this.hideCompletions();
            }
        });
    }

    handleInput(event) {
        if (this.completionTimeout) {
            clearTimeout(this.completionTimeout);
        }

        this.completionTimeout = setTimeout(() => {
            this.triggerCompletion(event);
        }, this.options.completionDelay);
        
        // Validate JSON in real-time
        this.validateJson();
    }

    handleKeyDown(event) {
        if (this.isShowingCompletions) {
            switch (event.key) {
                case 'ArrowDown':
                    event.preventDefault();
                    this.selectNext();
                    break;
                case 'ArrowUp':
                    event.preventDefault();
                    this.selectPrevious();
                    break;
                case 'Enter':
                case 'Tab':
                    if (this.selectedIndex >= 0) {
                        event.preventDefault();
                        this.insertCompletion();
                    }
                    break;
                case 'Escape':
                    event.preventDefault();
                    this.hideCompletions();
                    break;
                default:
                    // Check if it's a trigger character
                    if (this.options.triggerCharacters.includes(event.key)) {
                        setTimeout(() => this.triggerCompletion(event), 10);
                    }
                    break;
            }
        } else {
            // Trigger completion with Ctrl+Space
            if (event.ctrlKey && event.code === 'Space') {
                event.preventDefault();
                this.triggerCompletion(event);
            }
        }
    }

    async triggerCompletion(event) {
        if (!this.dotNetRef || !this.options.enableSmartCompletion) return;

        try {
            const text = this.element.value;
            const position = this.element.selectionStart || 0;
            
            const context = {
                triggerCharacter: event.data || '',
                triggerKind: event.type === 'input' ? 2 : 1 // TriggerCharacter : Invoked
            };

            this.completions = await this.dotNetRef.invokeMethodAsync(
                'GetCompletions', 
                this.element.id, 
                text, 
                position, 
                context
            );

            if (this.completions && this.completions.length > 0) {
                this.showCompletions();
            } else {
                this.hideCompletions();
            }
        } catch (error) {
            console.error('Error getting completions:', error);
            this.hideCompletions();
        }
    }

    showCompletions() {
        if (!this.completions || this.completions.length === 0) return;

        this.renderCompletions();
        this.updatePopupPosition();
        this.completionPopup.style.display = 'block';
        this.isShowingCompletions = true;
        this.selectedIndex = 0;
        this.updateSelection();
    }

    hideCompletions() {
        this.completionPopup.style.display = 'none';
        this.documentationPanel.style.display = 'none';
        this.isShowingCompletions = false;
        this.selectedIndex = -1;
    }

    renderCompletions() {
        let html = '';
        
        this.completions.forEach((completion, index) => {
            const iconClass = this.getCompletionIcon(completion.kind);
            const isSelected = index === this.selectedIndex;
            
            html += `
                <div class="completion-item ${isSelected ? 'selected' : ''}" 
                     data-index="${index}"
                     onclick="window.autoCompletion.editors.get('${this.element.id}').selectCompletion(${index})">
                    <div class="completion-icon">
                        <i class="material-icons">${iconClass}</i>
                    </div>
                    <div class="completion-content">
                        <div class="completion-label">${this.highlightQuery(completion.label)}</div>
                        ${completion.detail ? `<div class="completion-detail">${completion.detail}</div>` : ''}
                    </div>
                    <div class="completion-kind">${this.getKindLabel(completion.kind)}</div>
                </div>
            `;
        });

        this.completionPopup.innerHTML = `
            <div class="completion-header">
                <span>Suggestions</span>
                <span class="completion-count">${this.completions.length}</span>
            </div>
            <div class="completion-list">
                ${html}
            </div>
        `;
    }

    highlightQuery(label) {
        if (!this.currentQuery) return label;
        
        const regex = new RegExp(`(${this.escapeRegex(this.currentQuery)})`, 'gi');
        return label.replace(regex, '<mark>$1</mark>');
    }

    escapeRegex(text) {
        return text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    }

    getCompletionIcon(kind) {
        const iconMap = {
            1: 'text_fields',      // Text
            2: 'functions',        // Method
            3: 'functions',        // Function
            4: 'build',           // Constructor
            5: 'input',           // Field
            6: 'data_object',     // Variable
            7: 'class',           // Class
            8: 'interface',       // Interface
            9: 'folder',          // Module
            10: 'label',          // Property
            11: 'straighten',     // Unit
            12: 'data_object',    // Value
            13: 'list',           // Enum
            14: 'code',           // Keyword
            15: 'integration_instructions', // Snippet
            16: 'palette',        // Color
            17: 'description',    // File
            18: 'link'            // Reference
        };
        
        return iconMap[kind] || 'help';
    }

    getKindLabel(kind) {
        const kindMap = {
            1: 'Text', 2: 'Method', 3: 'Function', 4: 'Constructor',
            5: 'Field', 6: 'Variable', 7: 'Class', 8: 'Interface',
            9: 'Module', 10: 'Property', 11: 'Unit', 12: 'Value',
            13: 'Enum', 14: 'Keyword', 15: 'Snippet', 16: 'Color',
            17: 'File', 18: 'Reference'
        };
        
        return kindMap[kind] || 'Text';
    }

    selectNext() {
        if (this.selectedIndex < this.completions.length - 1) {
            this.selectedIndex++;
        } else {
            this.selectedIndex = 0;
        }
        this.updateSelection();
    }

    selectPrevious() {
        if (this.selectedIndex > 0) {
            this.selectedIndex--;
        } else {
            this.selectedIndex = this.completions.length - 1;
        }
        this.updateSelection();
    }

    updateSelection() {
        const items = this.completionPopup.querySelectorAll('.completion-item');
        items.forEach((item, index) => {
            item.classList.toggle('selected', index === this.selectedIndex);
        });

        // Scroll selected item into view
        const selectedItem = this.completionPopup.querySelector('.completion-item.selected');
        if (selectedItem) {
            selectedItem.scrollIntoView({ block: 'nearest' });
        }

        // Show documentation for selected item
        if (this.selectedIndex >= 0) {
            this.showDocumentation(this.completions[this.selectedIndex]);
        }
    }

    selectCompletion(index) {
        this.selectedIndex = index;
        this.insertCompletion();
    }

    insertCompletion() {
        if (this.selectedIndex < 0 || this.selectedIndex >= this.completions.length) return;

        const completion = this.completions[this.selectedIndex];
        const insertText = completion.insertText || completion.label;
        
        // Get current cursor position
        const start = this.element.selectionStart || 0;
        const end = this.element.selectionEnd || start;
        
        // Find the start of the current word to replace
        const text = this.element.value;
        let wordStart = start;
        while (wordStart > 0 && /\w/.test(text[wordStart - 1])) {
            wordStart--;
        }
        
        // Replace the current word with the completion
        const before = text.substring(0, wordStart);
        const after = text.substring(end);
        const newText = before + insertText + after;
        
        this.element.value = newText;
        this.element.selectionStart = this.element.selectionEnd = wordStart + insertText.length;
        
        // Trigger input event to update any bindings
        this.element.dispatchEvent(new Event('input', { bubbles: true }));
        
        this.hideCompletions();
        this.element.focus();
    }

    async showDocumentation(completion) {
        if (!this.dotNetRef || !completion.label) return;

        try {
            const documentation = await this.dotNetRef.invokeMethodAsync(
                'GetCompletionDocumentation', 
                completion.label
            );

            if (documentation) {
                this.renderDocumentation(documentation);
                this.positionDocumentationPanel();
                this.documentationPanel.style.display = 'block';
            }
        } catch (error) {
            console.error('Error getting documentation:', error);
        }
    }

    renderDocumentation(doc) {
        let examplesHtml = '';
        if (doc.examples && doc.examples.length > 0) {
            examplesHtml = `
                <div class="doc-examples">
                    <h4>Examples:</h4>
                    ${doc.examples.map(example => 
                        `<code class="doc-example">${this.escapeHtml(example)}</code>`
                    ).join('')}
                </div>
            `;
        }

        this.documentationPanel.innerHTML = `
            <div class="doc-header">
                <h3>${doc.summary}</h3>
            </div>
            <div class="doc-content">
                <p>${doc.description}</p>
                ${examplesHtml}
            </div>
        `;
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    updatePopupPosition() {
        if (!this.isShowingCompletions) return;

        const rect = this.element.getBoundingClientRect();
        const popupHeight = this.completionPopup.offsetHeight;
        const windowHeight = window.innerHeight;
        
        let top = rect.bottom + window.scrollY;
        
        // If popup would go below viewport, show above the element
        if (rect.bottom + popupHeight > windowHeight) {
            top = rect.top + window.scrollY - popupHeight;
        }
        
        this.completionPopup.style.left = rect.left + 'px';
        this.completionPopup.style.top = top + 'px';
        this.completionPopup.style.minWidth = Math.min(rect.width, 300) + 'px';
    }

    positionDocumentationPanel() {
        const popupRect = this.completionPopup.getBoundingClientRect();
        
        this.documentationPanel.style.left = (popupRect.right + 10) + 'px';
        this.documentationPanel.style.top = popupRect.top + 'px';
    }

    async validateJson() {
        if (!this.dotNetRef || !this.options.enableSchemaValidation) return;

        try {
            const text = this.element.value;
            if (!text.trim()) return;

            const diagnostics = await this.dotNetRef.invokeMethodAsync('ValidateJson', text);
            this.displayDiagnostics(diagnostics);
        } catch (error) {
            console.error('Error validating JSON:', error);
        }
    }

    displayDiagnostics(diagnostics) {
        // Remove existing error indicators
        this.clearDiagnostics();

        if (!diagnostics || diagnostics.length === 0) return;

        // Add error indicators
        diagnostics.forEach(diagnostic => {
            this.addDiagnosticIndicator(diagnostic);
        });
    }

    clearDiagnostics() {
        // Remove any existing diagnostic indicators
        const indicators = document.querySelectorAll('.json-diagnostic');
        indicators.forEach(indicator => indicator.remove());
        
        this.element.classList.remove('has-errors', 'has-warnings');
    }

    addDiagnosticIndicator(diagnostic) {
        const severity = diagnostic.severity;
        
        if (severity === 1) { // Error
            this.element.classList.add('has-errors');
        } else if (severity === 2) { // Warning
            this.element.classList.add('has-warnings');
        }
        
        // Could add line-specific indicators here for more advanced editors
        console.log(`JSON ${this.getSeverityLabel(severity)}: ${diagnostic.message}`);
    }

    getSeverityLabel(severity) {
        return ['', 'Error', 'Warning', 'Information', 'Hint'][severity] || 'Unknown';
    }

    destroy() {
        if (this.completionTimeout) {
            clearTimeout(this.completionTimeout);
        }
        
        this.completionPopup?.remove();
        this.documentationPanel?.remove();
        
        // Remove event listeners
        this.element.removeEventListener('input', this.handleInput);
        this.element.removeEventListener('keydown', this.handleKeyDown);
        this.element.removeEventListener('blur', this.hideCompletions);
        this.element.removeEventListener('scroll', this.updatePopupPosition);
    }
}

// Global functions for .NET interop
window.initializeAutoCompletion = (dotNetObjectReference) => {
    window.autoCompletion.init(dotNetObjectReference);
};

window.registerAutoCompletionEditor = (editorId, options) => {
    window.autoCompletion.registerEditor(editorId, options);
};

// Add CSS styles
const autoCompletionStyles = document.createElement('style');
autoCompletionStyles.textContent = `
    .auto-completion-popup {
        position: absolute;
        z-index: 10000;
        background: var(--dark-bg-secondary, #161B22);
        border: 1px solid var(--dark-border, #3D4451);
        border-radius: 8px;
        box-shadow: 0 8px 32px rgba(0, 0, 0, 0.4);
        max-height: 300px;
        overflow: hidden;
        min-width: 250px;
        font-family: var(--font-system, system-ui);
    }
    
    .completion-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 8px 12px;
        background: var(--dark-surface, #21262D);
        border-bottom: 1px solid var(--dark-border, #3D4451);
        font-size: 0.8rem;
        font-weight: 600;
        color: var(--dark-text, #F0F6FC);
    }
    
    .completion-count {
        color: var(--dark-text-secondary, #8B949E);
        font-weight: normal;
    }
    
    .completion-list {
        max-height: 260px;
        overflow-y: auto;
    }
    
    .completion-item {
        display: flex;
        align-items: center;
        padding: 8px 12px;
        cursor: pointer;
        border-bottom: 1px solid rgba(255, 255, 255, 0.05);
        transition: background-color 0.15s ease;
    }
    
    .completion-item:hover,
    .completion-item.selected {
        background: var(--primary-color, #3b82f6);
        color: white;
    }
    
    .completion-item.selected .completion-detail,
    .completion-item.selected .completion-kind {
        color: rgba(255, 255, 255, 0.8);
    }
    
    .completion-icon {
        width: 20px;
        height: 20px;
        display: flex;
        align-items: center;
        justify-content: center;
        margin-right: 8px;
        flex-shrink: 0;
    }
    
    .completion-icon i {
        font-size: 16px;
        color: var(--primary-color, #3b82f6);
    }
    
    .completion-item.selected .completion-icon i {
        color: white;
    }
    
    .completion-content {
        flex: 1;
        min-width: 0;
    }
    
    .completion-label {
        font-weight: 500;
        color: var(--dark-text, #F0F6FC);
        font-size: 0.9rem;
        line-height: 1.2;
    }
    
    .completion-label mark {
        background: var(--primary-color, #3b82f6);
        color: white;
        border-radius: 2px;
        padding: 0 2px;
    }
    
    .completion-detail {
        font-size: 0.8rem;
        color: var(--dark-text-secondary, #8B949E);
        line-height: 1.3;
        margin-top: 2px;
    }
    
    .completion-kind {
        font-size: 0.7rem;
        color: var(--dark-text-secondary, #8B949E);
        text-transform: uppercase;
        letter-spacing: 0.05em;
        margin-left: 8px;
        flex-shrink: 0;
    }
    
    .completion-documentation-panel {
        position: absolute;
        z-index: 10001;
        background: var(--dark-bg-secondary, #161B22);
        border: 1px solid var(--dark-border, #3D4451);
        border-radius: 8px;
        box-shadow: 0 8px 32px rgba(0, 0, 0, 0.4);
        max-width: 400px;
        max-height: 300px;
        overflow-y: auto;
        font-family: var(--font-system, system-ui);
    }
    
    .doc-header {
        padding: 12px;
        border-bottom: 1px solid var(--dark-border, #3D4451);
        background: var(--dark-surface, #21262D);
    }
    
    .doc-header h3 {
        margin: 0;
        font-size: 0.95rem;
        font-weight: 600;
        color: var(--dark-text, #F0F6FC);
    }
    
    .doc-content {
        padding: 12px;
    }
    
    .doc-content p {
        margin: 0 0 12px 0;
        font-size: 0.85rem;
        line-height: 1.5;
        color: var(--dark-text-secondary, #8B949E);
    }
    
    .doc-examples h4 {
        margin: 0 0 8px 0;
        font-size: 0.8rem;
        font-weight: 600;
        color: var(--dark-text, #F0F6FC);
    }
    
    .doc-example {
        display: block;
        background: var(--dark-surface, #21262D);
        border: 1px solid var(--dark-border, #3D4451);
        border-radius: 4px;
        padding: 6px 8px;
        margin-bottom: 4px;
        font-family: var(--font-mono, monospace);
        font-size: 0.8rem;
        color: var(--json-string, #50C878);
    }
    
    /* Input validation styles */
    .has-errors {
        border-color: #dc3545 !important;
        box-shadow: 0 0 0 1px rgba(220, 53, 69, 0.3) !important;
    }
    
    .has-warnings {
        border-color: #ffc107 !important;
        box-shadow: 0 0 0 1px rgba(255, 193, 7, 0.3) !important;
    }
    
    /* Scrollbar styling */
    .completion-list::-webkit-scrollbar,
    .completion-documentation-panel::-webkit-scrollbar {
        width: 6px;
    }
    
    .completion-list::-webkit-scrollbar-track,
    .completion-documentation-panel::-webkit-scrollbar-track {
        background: rgba(255, 255, 255, 0.05);
    }
    
    .completion-list::-webkit-scrollbar-thumb,
    .completion-documentation-panel::-webkit-scrollbar-thumb {
        background: var(--primary-color, #3b82f6);
        border-radius: 3px;
    }
    
    /* Mobile responsive */
    @media (max-width: 768px) {
        .auto-completion-popup {
            max-width: calc(100vw - 20px);
        }
        
        .completion-documentation-panel {
            display: none !important; /* Hide on mobile to save space */
        }
        
        .completion-item {
            padding: 12px;
        }
        
        .completion-kind {
            display: none; /* Hide kind on mobile */
        }
    }
`;

document.head.appendChild(autoCompletionStyles);

console.log('Auto-completion system loaded');