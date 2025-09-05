import { test, expect } from '@playwright/test';

test.describe('Performance', () => {
  test('should load home page quickly', async ({ page }) => {
    const startTime = Date.now();
    await page.goto('/');
    
    // Wait for hero title to be visible
    await expect(page.locator('#hero-title')).toBeVisible();
    
    const loadTime = Date.now() - startTime;
    console.log(`Page load time: ${loadTime}ms`);
    
    // Should load within reasonable time (adjust based on needs)
    expect(loadTime).toBeLessThan(5000);
  });

  test('should handle large JSON files efficiently', async ({ page }) => {
    await page.goto('/formatter');
    
    // Create a large JSON object
    const largeJson = JSON.stringify({
      data: Array.from({ length: 1000 }, (_, i) => ({
        id: i,
        name: `Item ${i}`,
        description: `Description for item ${i}`,
        metadata: {
          created: new Date().toISOString(),
          tags: [`tag${i}`, `category${i % 10}`]
        }
      }))
    });

    const startTime = Date.now();
    
    // Input large JSON
    const editor = page.locator('textarea, .monaco-editor').first();
    if (await editor.isVisible()) {
      await editor.fill(largeJson);
      
      const processTime = Date.now() - startTime;
      console.log(`Large JSON processing time: ${processTime}ms`);
      
      // Should handle large JSON within reasonable time
      expect(processTime).toBeLessThan(10000);
    }
  });

  test('should have good Core Web Vitals', async ({ page }) => {
    await page.goto('/');
    
    // Wait for page to fully load
    await page.waitForLoadState('networkidle');
    
    // Measure performance metrics
    const metrics = await page.evaluate(() => {
      return new Promise((resolve) => {
        new PerformanceObserver((list) => {
          const entries = list.getEntries();
          const vitals = {};
          
          entries.forEach((entry) => {
            if (entry.entryType === 'navigation') {
              vitals.loadTime = entry.loadEventEnd - entry.loadEventStart;
              vitals.domContentLoaded = entry.domContentLoadedEventEnd - entry.domContentLoadedEventStart;
            }
          });
          
          resolve(vitals);
        }).observe({ entryTypes: ['navigation'] });
        
        // Fallback timeout
        setTimeout(() => resolve({}), 3000);
      });
    });

    console.log('Performance metrics:', metrics);
  });

  test('should handle rapid user interactions', async ({ page }) => {
    await page.goto('/');
    
    // Rapid navigation clicks
    const tools = ['formatter', 'validator', 'converter', 'diff'];
    const startTime = Date.now();
    
    for (const tool of tools) {
      await page.locator(`text=${tool}`, { hasText: new RegExp(tool, 'i') }).first().click({ timeout: 1000 });
      await page.waitForTimeout(100);
      await page.goBack();
      await page.waitForTimeout(100);
    }
    
    const totalTime = Date.now() - startTime;
    console.log(`Rapid navigation time: ${totalTime}ms`);
    
    // Should handle rapid interactions smoothly
    expect(totalTime).toBeLessThan(15000);
  });

  test('should not have memory leaks', async ({ page }) => {
    await page.goto('/');
    
    // Navigate through multiple pages to check for memory leaks
    const pages = ['/', '/formatter', '/validator', '/converter', '/diff', '/generator', '/query'];
    
    for (let i = 0; i < 3; i++) { // Cycle 3 times
      for (const pagePath of pages) {
        await page.goto(pagePath);
        await page.waitForTimeout(200);
      }
    }
    
    // Check JavaScript heap size
    const heapUsage = await page.evaluate(() => {
      if ('memory' in performance) {
        return {
          used: performance.memory.usedJSHeapSize,
          total: performance.memory.totalJSHeapSize,
          limit: performance.memory.jsHeapSizeLimit
        };
      }
      return null;
    });
    
    if (heapUsage) {
      console.log('Memory usage:', heapUsage);
      
      // Should not use excessive memory
      const usageRatio = heapUsage.used / heapUsage.limit;
      expect(usageRatio).toBeLessThan(0.8); // Less than 80% of limit
    }
  });
});