// Ultimate JSON Tool Service Worker
const CACHE_NAME = 'ultimate-json-tool-v1';
const OFFLINE_URL = '/';

// Essential files to cache for offline functionality
const CACHE_FILES = [
    '/',
    '/index.html',
    '/css/app.css',
    '/css/enhanced.css',
    '/css/visual-effects.css',
    '/css/enhanced-design.css',
    '/js/theme.js',
    '/js/clipboard.js',
    '/js/keyboard-shortcuts.js',
    '/js/hero-animations.js',
    '/js/command-palette.js',
    '/js/virtual-scroll.js',
    '/js/accessibility.js',
    '/js/auto-completion.js',
    '/manifest.webmanifest',
    '/icon-192.png',
    '/icon-512.png'
];

// Install event - cache essential files
self.addEventListener('install', (event) => {
    console.log('Service Worker installing...');
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then((cache) => {
                console.log('Caching essential files');
                return cache.addAll(CACHE_FILES);
            })
            .then(() => {
                // Force activation of new service worker
                return self.skipWaiting();
            })
            .catch((error) => {
                console.error('Failed to cache files during install:', error);
            })
    );
});

// Activate event - clean up old caches
self.addEventListener('activate', (event) => {
    console.log('Service Worker activating...');
    event.waitUntil(
        caches.keys()
            .then((cacheNames) => {
                return Promise.all(
                    cacheNames.map((cacheName) => {
                        if (cacheName !== CACHE_NAME) {
                            console.log('Deleting old cache:', cacheName);
                            return caches.delete(cacheName);
                        }
                    })
                );
            })
            .then(() => {
                // Claim all clients immediately
                return self.clients.claim();
            })
    );
});

// Fetch event - implement cache-first strategy for static assets
self.addEventListener('fetch', (event) => {
    // Skip non-GET requests
    if (event.request.method !== 'GET') {
        return;
    }

    // Skip external requests
    if (!event.request.url.startsWith(self.location.origin)) {
        return;
    }

    event.respondWith(
        caches.match(event.request)
            .then((cachedResponse) => {
                // Return cached version if available
                if (cachedResponse) {
                    return cachedResponse;
                }

                // Otherwise fetch from network
                return fetch(event.request)
                    .then((response) => {
                        // Don't cache if not successful
                        if (!response || response.status !== 200 || response.type !== 'basic') {
                            return response;
                        }

                        // Clone response for caching
                        const responseToCache = response.clone();

                        // Cache static assets
                        if (shouldCache(event.request.url)) {
                            caches.open(CACHE_NAME)
                                .then((cache) => {
                                    cache.put(event.request, responseToCache);
                                });
                        }

                        return response;
                    })
                    .catch(() => {
                        // Return offline page for navigation requests
                        if (event.request.mode === 'navigate') {
                            return caches.match(OFFLINE_URL);
                        }
                    });
            })
    );
});

// Helper function to determine if a URL should be cached
function shouldCache(url) {
    // Cache static assets
    return url.includes('/css/') ||
           url.includes('/js/') ||
           url.includes('/fonts/') ||
           url.includes('/images/') ||
           url.includes('.png') ||
           url.includes('.jpg') ||
           url.includes('.jpeg') ||
           url.includes('.svg') ||
           url.includes('.ico') ||
           url.includes('.webp') ||
           url.includes('.woff') ||
           url.includes('.woff2');
}

// Message event - handle commands from the main thread
self.addEventListener('message', (event) => {
    if (event.data && event.data.type === 'SKIP_WAITING') {
        self.skipWaiting();
    }
    
    if (event.data && event.data.type === 'CACHE_UPDATE') {
        // Force update cache
        caches.delete(CACHE_NAME).then(() => {
            caches.open(CACHE_NAME).then((cache) => {
                cache.addAll(CACHE_FILES);
            });
        });
    }
});

// Background sync for offline actions
self.addEventListener('sync', (event) => {
    if (event.tag === 'json-data-sync') {
        event.waitUntil(syncJsonData());
    }
});

// Sync JSON data when back online
async function syncJsonData() {
    try {
        // Get any stored offline data
        const cache = await caches.open('json-offline-data');
        const requests = await cache.keys();
        
        for (const request of requests) {
            if (request.url.includes('offline-json-data')) {
                const response = await cache.match(request);
                const data = await response.json();
                
                // Process offline data
                console.log('Syncing offline JSON data:', data);
                
                // Remove from cache after processing
                await cache.delete(request);
            }
        }
    } catch (error) {
        console.error('Failed to sync JSON data:', error);
    }
}

// Notification click event
self.addEventListener('notificationclick', (event) => {
    event.notification.close();
    
    event.waitUntil(
        clients.openWindow('/')
    );
});

console.log('Ultimate JSON Tool Service Worker loaded');
