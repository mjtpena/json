// Virtual Scroll Implementation for Large JSON Data
window.virtualScroll = {
    containers: new Map(),
    dotNetRef: null,

    init(dotNetObjectReference) {
        this.dotNetRef = dotNetObjectReference;
        console.log('Virtual scroll initialized');
    },

    createContainer(containerId, options) {
        const container = document.getElementById(containerId);
        if (!container) {
            console.error('Container not found:', containerId);
            return null;
        }

        const virtualContainer = new VirtualScrollContainer(containerId, options, this.dotNetRef);
        this.containers.set(containerId, virtualContainer);

        return virtualContainer.render();
    },

    updateData(containerId, data) {
        const virtualContainer = this.containers.get(containerId);
        if (virtualContainer) {
            virtualContainer.updateData(data);
        }
    },

    scrollToIndex(containerId, index) {
        const virtualContainer = this.containers.get(containerId);
        if (virtualContainer) {
            virtualContainer.scrollToIndex(index);
        }
    },

    scrollToItem(containerId, itemId) {
        const virtualContainer = this.containers.get(containerId);
        if (virtualContainer) {
            virtualContainer.scrollToItem(itemId);
        }
    },

    refresh(containerId) {
        const virtualContainer = this.containers.get(containerId);
        if (virtualContainer) {
            virtualContainer.refresh();
        }
    },

    destroy(containerId) {
        const virtualContainer = this.containers.get(containerId);
        if (virtualContainer) {
            virtualContainer.destroy();
            this.containers.delete(containerId);
        }
    }
};

class VirtualScrollContainer {
    constructor(containerId, options, dotNetRef) {
        this.containerId = containerId;
        this.options = {
            itemHeight: 30,
            containerHeight: 400,
            bufferSize: 5,
            enableLazyLoading: true,
            loadingThreshold: 10,
            overScan: 3,
            ...options
        };
        this.dotNetRef = dotNetRef;
        
        this.data = [];
        this.scrollTop = 0;
        this.startIndex = 0;
        this.endIndex = 0;
        this.visibleItems = [];
        
        this.container = document.getElementById(containerId);
        this.viewport = null;
        this.content = null;
        this.spacerBefore = null;
        this.spacerAfter = null;
        
        this.isScrolling = false;
        this.scrollTimeout = null;
    }

    render() {
        if (!this.container) return null;

        this.container.innerHTML = `
            <div class="virtual-scroll-viewport" style="
                height: ${this.options.containerHeight}px;
                overflow-y: auto;
                position: relative;
                border: 1px solid var(--dark-border, #3D4451);
                border-radius: 6px;
                background: var(--dark-surface, #21262D);
            ">
                <div class="virtual-scroll-spacer-before"></div>
                <div class="virtual-scroll-content"></div>
                <div class="virtual-scroll-spacer-after"></div>
                <div class="virtual-scroll-loading" style="
                    display: none;
                    padding: 1rem;
                    text-align: center;
                    color: var(--dark-text-secondary, #8B949E);
                ">
                    <div class="loading-spinner"></div>
                    Loading more items...
                </div>
            </div>
        `;

        this.viewport = this.container.querySelector('.virtual-scroll-viewport');
        this.content = this.container.querySelector('.virtual-scroll-content');
        this.spacerBefore = this.container.querySelector('.virtual-scroll-spacer-before');
        this.spacerAfter = this.container.querySelector('.virtual-scroll-spacer-after');
        this.loadingIndicator = this.container.querySelector('.virtual-scroll-loading');

        this.bindEvents();
        this.updateDisplay();

        return this.container.innerHTML;
    }

    bindEvents() {
        if (!this.viewport) return;

        this.viewport.addEventListener('scroll', this.handleScroll.bind(this));
        
        // Intersection Observer for lazy loading
        if (this.options.enableLazyLoading) {
            this.intersectionObserver = new IntersectionObserver(
                this.handleIntersection.bind(this),
                { 
                    root: this.viewport,
                    rootMargin: '100px',
                    threshold: 0.1
                }
            );
        }
    }

    handleScroll(event) {
        this.scrollTop = event.target.scrollTop;
        
        if (this.scrollTimeout) {
            clearTimeout(this.scrollTimeout);
        }

        if (!this.isScrolling) {
            this.isScrolling = true;
            this.onScrollStart();
        }

        this.updateVisibleRange();
        this.updateDisplay();

        // Debounce scroll end
        this.scrollTimeout = setTimeout(() => {
            this.isScrolling = false;
            this.onScrollEnd();
        }, 150);

        // Check for lazy loading
        if (this.options.enableLazyLoading) {
            this.checkLazyLoad();
        }

        // Notify .NET of scroll changes
        if (this.dotNetRef) {
            this.dotNetRef.invokeMethodAsync('OnScroll', this.containerId, {
                scrollTop: this.scrollTop,
                scrollHeight: event.target.scrollHeight,
                clientHeight: event.target.clientHeight,
                startIndex: this.startIndex,
                endIndex: this.endIndex,
                visibleCount: this.endIndex - this.startIndex,
                totalCount: this.data.length
            });
        }
    }

    handleIntersection(entries) {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const index = parseInt(entry.target.dataset.index);
                const itemId = entry.target.dataset.itemId;
                
                if (this.dotNetRef) {
                    this.dotNetRef.invokeMethodAsync('OnItemVisible', this.containerId, index, itemId);
                }
            }
        });
    }

    updateData(data) {
        this.data = Array.isArray(data) ? data : [];
        this.updateVisibleRange();
        this.updateDisplay();
    }

    updateVisibleRange() {
        if (this.data.length === 0) {
            this.startIndex = 0;
            this.endIndex = 0;
            return;
        }

        const { itemHeight, bufferSize, overScan } = this.options;
        const containerHeight = this.viewport?.clientHeight || this.options.containerHeight;

        // Calculate visible range
        const visibleStart = Math.floor(this.scrollTop / itemHeight);
        const visibleEnd = Math.min(
            this.data.length,
            Math.ceil((this.scrollTop + containerHeight) / itemHeight)
        );

        // Add buffer and overscan
        this.startIndex = Math.max(0, visibleStart - bufferSize - overScan);
        this.endIndex = Math.min(this.data.length, visibleEnd + bufferSize + overScan);
    }

    updateDisplay() {
        if (!this.content || !this.spacerBefore || !this.spacerAfter) return;

        const { itemHeight } = this.options;
        
        // Update spacers
        const beforeHeight = this.startIndex * itemHeight;
        const afterHeight = (this.data.length - this.endIndex) * itemHeight;
        
        this.spacerBefore.style.height = `${beforeHeight}px`;
        this.spacerAfter.style.height = `${afterHeight}px`;

        // Render visible items
        this.renderVisibleItems();
    }

    renderVisibleItems() {
        const fragment = document.createDocumentFragment();
        
        for (let i = this.startIndex; i < this.endIndex; i++) {
            const item = this.data[i];
            const itemElement = this.createItemElement(item, i);
            fragment.appendChild(itemElement);
        }

        this.content.innerHTML = '';
        this.content.appendChild(fragment);

        // Observe items for lazy loading
        if (this.intersectionObserver) {
            this.content.querySelectorAll('.virtual-scroll-item').forEach(item => {
                this.intersectionObserver.observe(item);
            });
        }
    }

    createItemElement(item, index) {
        const element = document.createElement('div');
        element.className = 'virtual-scroll-item';
        element.dataset.index = index;
        element.dataset.itemId = item.id || `item-${index}`;
        element.style.cssText = `
            height: ${this.options.itemHeight}px;
            display: flex;
            align-items: center;
            padding: 0 12px;
            border-bottom: 1px solid rgba(255, 255, 255, 0.05);
            color: var(--dark-text, #F0F6FC);
            font-family: var(--font-mono, monospace);
            font-size: 0.875rem;
            line-height: 1.5;
        `;

        // Format JSON item display
        if (typeof item === 'object') {
            element.innerHTML = this.formatJsonItem(item, index);
        } else {
            element.innerHTML = `
                <span class="item-index" style="
                    color: var(--dark-text-secondary, #8B949E);
                    width: 40px;
                    flex-shrink: 0;
                ">${index}:</span>
                <span class="item-content">${this.escapeHtml(String(item))}</span>
            `;
        }

        return element;
    }

    formatJsonItem(item, index) {
        if (Array.isArray(item)) {
            return `
                <span class="item-index" style="color: var(--dark-text-secondary, #8B949E); width: 40px;">${index}:</span>
                <span class="json-bracket">[</span>
                <span class="item-summary">${item.length} items</span>
                <span class="json-bracket">]</span>
            `;
        }

        if (item && typeof item === 'object') {
            const keys = Object.keys(item);
            const firstKey = keys[0];
            const preview = firstKey ? `${firstKey}: ${this.formatValue(item[firstKey])}` : 'empty';
            
            return `
                <span class="item-index" style="color: var(--dark-text-secondary, #8B949E); width: 40px;">${index}:</span>
                <span class="json-bracket">{</span>
                <span class="item-preview" style="color: var(--dark-text-secondary, #8B949E);">${preview}</span>
                ${keys.length > 1 ? `<span class="item-more">... +${keys.length - 1}</span>` : ''}
                <span class="json-bracket">}</span>
            `;
        }

        return `
            <span class="item-index" style="color: var(--dark-text-secondary, #8B949E); width: 40px;">${index}:</span>
            <span class="item-content">${this.formatValue(item)}</span>
        `;
    }

    formatValue(value) {
        if (value === null) return '<span class="json-null">null</span>';
        if (typeof value === 'boolean') return `<span class="json-boolean">${value}</span>`;
        if (typeof value === 'number') return `<span class="json-number">${value}</span>`;
        if (typeof value === 'string') {
            const truncated = value.length > 50 ? value.substring(0, 50) + '...' : value;
            return `<span class="json-string">"${this.escapeHtml(truncated)}"</span>`;
        }
        return this.escapeHtml(String(value));
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    scrollToIndex(index) {
        if (index < 0 || index >= this.data.length) return;

        const scrollTop = index * this.options.itemHeight;
        if (this.viewport) {
            this.viewport.scrollTop = scrollTop;
        }
    }

    scrollToItem(itemId) {
        const index = this.data.findIndex((item, idx) => 
            (item && item.id === itemId) || `item-${idx}` === itemId
        );
        
        if (index !== -1) {
            this.scrollToIndex(index);
        }
    }

    checkLazyLoad() {
        if (!this.options.enableLazyLoading || !this.viewport) return;

        const { scrollTop, scrollHeight, clientHeight } = this.viewport;
        const isNearBottom = scrollTop + clientHeight >= scrollHeight - this.options.loadingThreshold;

        if (isNearBottom && this.data.length > 0) {
            this.showLoading();
            if (this.dotNetRef) {
                this.dotNetRef.invokeMethodAsync('OnLoadMore', this.containerId, this.data.length);
            }
        }
    }

    showLoading() {
        if (this.loadingIndicator) {
            this.loadingIndicator.style.display = 'block';
        }
    }

    hideLoading() {
        if (this.loadingIndicator) {
            this.loadingIndicator.style.display = 'none';
        }
    }

    onScrollStart() {
        this.container?.classList.add('virtual-scroll-scrolling');
    }

    onScrollEnd() {
        this.container?.classList.remove('virtual-scroll-scrolling');
        this.hideLoading();
    }

    refresh() {
        this.updateVisibleRange();
        this.updateDisplay();
    }

    destroy() {
        if (this.intersectionObserver) {
            this.intersectionObserver.disconnect();
        }
        if (this.scrollTimeout) {
            clearTimeout(this.scrollTimeout);
        }
        if (this.viewport) {
            this.viewport.removeEventListener('scroll', this.handleScroll);
        }
    }
}

// Global functions for .NET interop
window.initializeVirtualScroll = (dotNetObjectReference) => {
    window.virtualScroll.init(dotNetObjectReference);
};

window.createVirtualScrollContainer = (containerId, options) => {
    return window.virtualScroll.createContainer(containerId, options);
};

window.updateVirtualScrollData = (containerId, data) => {
    window.virtualScroll.updateData(containerId, data);
};

window.scrollVirtualScrollToIndex = (containerId, index) => {
    window.virtualScroll.scrollToIndex(containerId, index);
};

window.scrollVirtualScrollToItem = (containerId, itemId) => {
    window.virtualScroll.scrollToItem(containerId, itemId);
};

window.refreshVirtualScroll = (containerId) => {
    window.virtualScroll.refresh(containerId);
};

window.destroyVirtualScroll = (containerId) => {
    window.virtualScroll.destroy(containerId);
};

// Add CSS for virtual scroll components
const virtualScrollStyles = document.createElement('style');
virtualScrollStyles.textContent = `
    .virtual-scroll-viewport {
        position: relative;
        overflow-y: auto;
        scrollbar-width: thin;
        scrollbar-color: var(--primary-color, #3b82f6) transparent;
    }
    
    .virtual-scroll-viewport::-webkit-scrollbar {
        width: 8px;
    }
    
    .virtual-scroll-viewport::-webkit-scrollbar-track {
        background: rgba(255, 255, 255, 0.05);
        border-radius: 4px;
    }
    
    .virtual-scroll-viewport::-webkit-scrollbar-thumb {
        background: var(--primary-color, #3b82f6);
        border-radius: 4px;
    }
    
    .virtual-scroll-viewport::-webkit-scrollbar-thumb:hover {
        background: var(--primary-color-hover, #2563eb);
    }
    
    .virtual-scroll-item {
        transition: background-color 0.15s ease;
    }
    
    .virtual-scroll-item:hover {
        background: rgba(59, 130, 246, 0.1);
    }
    
    .virtual-scroll-scrolling .virtual-scroll-item {
        transition: none;
    }
    
    .json-bracket {
        color: var(--json-bracket, #8B949E);
        font-weight: 600;
    }
    
    .json-string {
        color: var(--json-string, #50C878);
    }
    
    .json-number {
        color: var(--json-number, #FF6B9D);
    }
    
    .json-boolean {
        color: var(--json-boolean, #FFD93D);
    }
    
    .json-null {
        color: var(--json-null, #8B949E);
        font-style: italic;
    }
    
    .item-preview {
        font-size: 0.8rem;
        opacity: 0.8;
    }
    
    .item-more {
        color: var(--dark-text-secondary, #8B949E);
        font-size: 0.75rem;
        margin-left: 4px;
    }
    
    .loading-spinner {
        width: 20px;
        height: 20px;
        border: 2px solid transparent;
        border-top: 2px solid var(--primary-color, #3b82f6);
        border-radius: 50%;
        display: inline-block;
        animation: spin 1s linear infinite;
        margin-bottom: 8px;
    }
    
    @keyframes spin {
        0% { transform: rotate(0deg); }
        100% { transform: rotate(360deg); }
    }
    
    @media (max-width: 768px) {
        .virtual-scroll-item {
            font-size: 0.8rem;
            padding: 0 8px;
        }
        
        .item-index {
            width: 30px !important;
        }
    }
`;

document.head.appendChild(virtualScrollStyles);