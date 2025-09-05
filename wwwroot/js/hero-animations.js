// Hero Section Animations and Particles
window.createHeroParticles = () => {
    const heroSection = document.querySelector('.hero-enhanced');
    const particlesContainer = document.getElementById('hero-particles');
    
    if (!heroSection || !particlesContainer) return;
    
    // Create floating particles
    const particleCount = 30;
    const particles = [];
    
    for (let i = 0; i < particleCount; i++) {
        const particle = document.createElement('div');
        particle.className = 'hero-particle';
        
        // Random positioning
        const x = Math.random() * 100;
        const y = Math.random() * 100;
        const delay = Math.random() * 6;
        const duration = 4 + Math.random() * 4;
        
        particle.style.left = x + '%';
        particle.style.top = y + '%';
        particle.style.animationDelay = delay + 's';
        particle.style.animationDuration = duration + 's';
        
        // Random color variation
        const opacity = 0.3 + Math.random() * 0.4;
        const hue = 220 + Math.random() * 60; // Blue to purple range
        particle.style.background = `hsla(${hue}, 70%, 60%, ${opacity})`;
        
        // Random size variation
        const size = 2 + Math.random() * 6;
        particle.style.width = size + 'px';
        particle.style.height = size + 'px';
        
        particlesContainer.appendChild(particle);
        particles.push(particle);
    }
    
    // Cleanup on navigation
    window.addEventListener('beforeunload', () => {
        particles.forEach(particle => particle.remove());
    });
    
    // Interactive mouse movement effect
    let mouseX = 0, mouseY = 0;
    let targetX = 0, targetY = 0;
    
    heroSection.addEventListener('mousemove', (e) => {
        const rect = heroSection.getBoundingClientRect();
        mouseX = (e.clientX - rect.left) / rect.width;
        mouseY = (e.clientY - rect.top) / rect.height;
        
        targetX = (mouseX - 0.5) * 20;
        targetY = (mouseY - 0.5) * 20;
        
        // Apply parallax effect to particles
        particles.forEach((particle, index) => {
            const factor = (index % 3 + 1) * 0.5;
            const translateX = targetX * factor;
            const translateY = targetY * factor;
            
            particle.style.transform = `translate(${translateX}px, ${translateY}px)`;
        });
    });
    
    heroSection.addEventListener('mouseleave', () => {
        particles.forEach(particle => {
            particle.style.transform = 'translate(0, 0)';
        });
    });
};

window.animateHeroElements = () => {
    // Animate hero elements on load
    const heroTitle = document.getElementById('hero-title');
    const heroDescription = document.querySelector('.hero-description');
    const heroStats = document.querySelector('.hero-stats');
    const heroActions = document.querySelector('.hero-actions');
    
    const elements = [heroTitle, heroDescription, heroStats, heroActions].filter(Boolean);
    
    elements.forEach((element, index) => {
        if (element) {
            element.style.opacity = '0';
            element.style.transform = 'translateY(30px)';
            element.style.transition = 'all 0.8s cubic-bezier(0.25, 0.46, 0.45, 0.94)';
            
            setTimeout(() => {
                element.style.opacity = '1';
                element.style.transform = 'translateY(0)';
            }, index * 200 + 500);
        }
    });
    
    // Animate typing effect for title
    if (heroTitle) {
        setTimeout(() => {
            heroTitle.classList.add('typing-complete');
        }, 2000);
    }
    
    // Animate stats counters
    animateStatsCounters();
    
    // Setup intersection observer for scroll animations
    setupScrollAnimations();
};

function animateStatsCounters() {
    const statNumbers = document.querySelectorAll('.stat-number');
    
    statNumbers.forEach((stat, index) => {
        const text = stat.textContent;
        
        // Skip animation for non-numeric values
        if (text === '∞' || text === '0ms') return;
        
        const targetNumber = parseInt(text.replace('+', ''));
        if (isNaN(targetNumber)) return;
        
        let currentNumber = 0;
        const duration = 2000;
        const increment = targetNumber / (duration / 16);
        
        const timer = setInterval(() => {
            currentNumber += increment;
            
            if (currentNumber >= targetNumber) {
                stat.textContent = text; // Restore original text with +
                clearInterval(timer);
            } else {
                stat.textContent = Math.floor(currentNumber) + (text.includes('+') ? '+' : '');
            }
        }, 16);
    });
}

function setupScrollAnimations() {
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -10% 0px'
    };
    
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('animate-in');
            }
        });
    }, observerOptions);
    
    // Observe elements for scroll animations
    const animatedElements = document.querySelectorAll(
        '.tool-card, .feature-card, .update-item, .section-header'
    );
    
    animatedElements.forEach(el => {
        el.classList.add('animate-on-scroll');
        observer.observe(el);
    });
}

// Demo functionality
window.showDemo = () => {
    const demoData = {
        "user": {
            "id": 12345,
            "name": "John Developer",
            "email": "john@example.com",
            "skills": ["JavaScript", "Python", "JSON"],
            "isActive": true,
            "lastLogin": "2023-12-01T10:30:00Z",
            "preferences": {
                "theme": "dark",
                "notifications": true,
                "language": "en"
            },
            "projects": [
                {
                    "name": "Ultimate JSON Tool",
                    "status": "active",
                    "stars": 1337
                }
            ]
        }
    };
    
    // Show formatted JSON in a modal or navigate to formatter
    const jsonString = JSON.stringify(demoData, null, 2);
    
    // Create demo modal
    const modal = document.createElement('div');
    modal.className = 'demo-modal';
    modal.innerHTML = `
        <div class="demo-overlay" onclick="this.parentElement.remove()">
            <div class="demo-content" onclick="event.stopPropagation()">
                <div class="demo-header">
                    <h3>JSON Formatter Demo</h3>
                    <button onclick="this.closest('.demo-modal').remove()" class="close-btn">&times;</button>
                </div>
                <div class="demo-body">
                    <div class="demo-description">
                        <p>This is how your JSON will look after formatting:</p>
                    </div>
                    <div class="demo-json json-enhanced">
                        <pre><code>${highlightJSON(jsonString)}</code></pre>
                    </div>
                    <div class="demo-actions">
                        <button onclick="copyDemoJSON('${jsonString.replace(/'/g, "\\'")})" class="btn-gradient-primary">
                            Copy JSON
                        </button>
                        <button onclick="goToFormatter('${jsonString.replace(/'/g, "\\'")}'); this.closest('.demo-modal').remove();" class="btn-gradient-secondary">
                            Try in Formatter
                        </button>
                    </div>
                </div>
            </div>
        </div>
    `;
    
    document.body.appendChild(modal);
    
    // Focus trap and keyboard handling
    const focusableElements = modal.querySelectorAll('button');
    const firstFocusable = focusableElements[0];
    const lastFocusable = focusableElements[focusableElements.length - 1];
    
    firstFocusable?.focus();
    
    modal.addEventListener('keydown', (e) => {
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
            modal.remove();
        }
    });
};

function highlightJSON(jsonString) {
    return jsonString
        .replace(/"([^"]+)":/g, '<span class="json-key">"$1"</span>:')
        .replace(/: "([^"]*)"/g, ': <span class="json-string">"$1"</span>')
        .replace(/: (\d+(?:\.\d+)?)/g, ': <span class="json-number">$1</span>')
        .replace(/: (true|false)/g, ': <span class="json-boolean">$1</span>')
        .replace(/: null/g, ': <span class="json-null">null</span>')
        .replace(/([{}[\]])/g, '<span class="json-bracket">$1</span>');
}

function copyDemoJSON(jsonString) {
    if (navigator.clipboard) {
        navigator.clipboard.writeText(jsonString).then(() => {
            showToast('JSON copied to clipboard!', 'success');
        });
    } else {
        // Fallback for older browsers
        const textArea = document.createElement('textarea');
        textArea.value = jsonString;
        document.body.appendChild(textArea);
        textArea.select();
        document.execCommand('copy');
        document.body.removeChild(textArea);
        showToast('JSON copied to clipboard!', 'success');
    }
}

function goToFormatter(jsonString) {
    // Store in localStorage for the formatter to pick up
    localStorage.setItem('demo-json', jsonString);
    window.location.href = '/formatter';
}

function showToast(message, type = 'info') {
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.textContent = message;
    
    // Position toast
    toast.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        background: var(--primary-gradient);
        color: white;
        padding: 1rem 1.5rem;
        border-radius: 8px;
        box-shadow: var(--shadow-lg);
        z-index: 10000;
        font-weight: 500;
        animation: slideInRight 0.3s ease-out;
    `;
    
    document.body.appendChild(toast);
    
    setTimeout(() => {
        toast.style.animation = 'slideOutRight 0.3s ease-in forwards';
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

// Add CSS for demo modal and animations
const demoStyles = document.createElement('style');
demoStyles.textContent = `
    .demo-modal {
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
        animation: fadeIn 0.3s ease-out;
    }
    
    .demo-overlay {
        width: 100%;
        height: 100%;
        display: flex;
        align-items: center;
        justify-content: center;
        padding: 2rem;
    }
    
    .demo-content {
        background: var(--dark-bg-secondary);
        border: 1px solid var(--dark-border);
        border-radius: 12px;
        max-width: 800px;
        max-height: 80vh;
        overflow: hidden;
        animation: slideUp 0.4s cubic-bezier(0.25, 0.46, 0.45, 0.94);
        display: flex;
        flex-direction: column;
    }
    
    .demo-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 1.5rem 2rem;
        border-bottom: 1px solid var(--dark-border);
    }
    
    .demo-header h3 {
        margin: 0;
        color: var(--dark-text);
    }
    
    .demo-body {
        padding: 2rem;
        overflow-y: auto;
    }
    
    .demo-description {
        margin-bottom: 1.5rem;
    }
    
    .demo-description p {
        margin: 0;
        color: var(--dark-text-secondary);
    }
    
    .demo-json {
        margin-bottom: 2rem;
        max-height: 300px;
        overflow-y: auto;
    }
    
    .demo-json pre {
        margin: 0;
        font-family: var(--font-mono);
        line-height: 1.6;
    }
    
    .demo-actions {
        display: flex;
        gap: 1rem;
        justify-content: center;
    }
    
    .demo-actions button {
        padding: 0.75rem 1.5rem;
        border: none;
        border-radius: 6px;
        font-weight: 600;
        cursor: pointer;
        transition: all 0.2s ease;
    }
    
    /* Scroll animations */
    .animate-on-scroll {
        opacity: 0;
        transform: translateY(30px);
        transition: all 0.6s cubic-bezier(0.25, 0.46, 0.45, 0.94);
    }
    
    .animate-on-scroll.animate-in {
        opacity: 1;
        transform: translateY(0);
    }
    
    /* Toast animations */
    @keyframes slideInRight {
        from {
            transform: translateX(100%);
            opacity: 0;
        }
        to {
            transform: translateX(0);
            opacity: 1;
        }
    }
    
    @keyframes slideOutRight {
        from {
            transform: translateX(0);
            opacity: 1;
        }
        to {
            transform: translateX(100%);
            opacity: 0;
        }
    }
    
    /* Typing animation completion */
    .typing-animation.typing-complete {
        border-right: none;
    }
    
    @media (max-width: 768px) {
        .demo-overlay {
            padding: 1rem;
        }
        
        .demo-content {
            max-width: none;
            width: 100%;
        }
        
        .demo-actions {
            flex-direction: column;
        }
        
        .demo-actions button {
            width: 100%;
        }
    }
`;

document.head.appendChild(demoStyles);