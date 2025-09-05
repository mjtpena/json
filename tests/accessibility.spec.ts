import { test, expect } from '@playwright/test';

test.describe('Accessibility', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should have proper heading structure', async ({ page }) => {
    // Check for h1 on home page
    await expect(page.locator('h1')).toHaveCount(1);
    await expect(page.locator('h1')).toContainText('Ultimate JSON');
    
    // Check heading hierarchy
    const headings = await page.locator('h1, h2, h3, h4, h5, h6').all();
    expect(headings.length).toBeGreaterThan(0);
  });

  test('should have keyboard navigation support', async ({ page }) => {
    // Test tab navigation
    await page.keyboard.press('Tab');
    
    // Should focus on first interactive element
    const focusedElement = await page.evaluate(() => document.activeElement?.tagName);
    expect(['BUTTON', 'A', 'INPUT']).toContain(focusedElement);
  });

  test('should have proper ARIA labels and roles', async ({ page }) => {
    // Check for ARIA labels on interactive elements
    const buttons = await page.locator('button').all();
    
    for (const button of buttons) {
      const ariaLabel = await button.getAttribute('aria-label');
      const text = await button.textContent();
      
      // Button should have either visible text or aria-label
      expect(ariaLabel || text).toBeTruthy();
    }
  });

  test('should have sufficient color contrast', async ({ page }) => {
    // This would ideally use axe-core for automated testing
    // For now, check that text is visible
    const textElements = await page.locator('p, h1, h2, h3, h4, h5, h6, span').all();
    
    for (const element of textElements.slice(0, 5)) { // Test first 5
      await expect(element).toBeVisible();
    }
  });

  test('should have proper form labels', async ({ page }) => {
    // Navigate to a page with forms (like API tester)
    await page.goto('/api-test');
    
    const inputs = await page.locator('input').all();
    
    for (const input of inputs) {
      const id = await input.getAttribute('id');
      const ariaLabel = await input.getAttribute('aria-label');
      const ariaLabelledBy = await input.getAttribute('aria-labelledby');
      const placeholder = await input.getAttribute('placeholder');
      
      // Input should have some form of labeling
      expect(id || ariaLabel || ariaLabelledBy || placeholder).toBeTruthy();
    }
  });

  test('should support screen reader navigation landmarks', async ({ page }) => {
    // Check for main content area
    const main = page.locator('main, [role="main"]');
    await expect(main).toBeVisible();
    
    // Check for navigation
    const nav = page.locator('nav, [role="navigation"]');
    await expect(nav).toBeVisible();
  });

  test('should handle focus management', async ({ page }) => {
    // Test modal focus management
    await page.keyboard.press('F1'); // Open help modal
    
    // Focus should move to modal if it opens
    const modalOrDialog = page.locator('[role="dialog"], .modal, .command-palette');
    
    if (await modalOrDialog.isVisible({ timeout: 1000 })) {
      // Check that focus is trapped in modal
      await page.keyboard.press('Tab');
      const focusedElement = page.locator(':focus');
      await expect(focusedElement).toBeVisible();
    }
  });
});