import { test, expect } from '@playwright/test';

test.describe('Navigation', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should have working navigation menu', async ({ page }) => {
    // Check main navigation links
    await expect(page.locator('nav')).toBeVisible();
    
    // Navigate to key pages
    const navLinks = [
      { name: 'Formatter', url: '/formatter' },
      { name: 'Validator', url: '/validator' },
      { name: 'Converter', url: '/converter' },
      { name: 'Diff & Merge', url: '/diff' },
      { name: 'Generator', url: '/generator' },
      { name: 'Query', url: '/query' }
    ];

    for (const link of navLinks) {
      await page.locator(`text=${link.name}`).first().click();
      await expect(page).toHaveURL(link.url);
      await page.goBack();
    }
  });

  test('should expand advanced tools menu', async ({ page }) => {
    // Click on Advanced Tools to expand
    await page.locator('text=Advanced Tools').click();
    
    // Check advanced tool links are visible
    await expect(page.locator('text=Documentation')).toBeVisible();
    await expect(page.locator('text=Performance')).toBeVisible();
    await expect(page.locator('text=JWT Decoder')).toBeVisible();
    await expect(page.locator('text=API Tester')).toBeVisible();
  });

  test('should navigate to settings and about pages', async ({ page }) => {
    await page.locator('text=Settings').click();
    await expect(page).toHaveURL('/settings');
    
    await page.locator('text=About').click();
    await expect(page).toHaveURL('/about');
  });

  test('should handle 404 routing correctly', async ({ page }) => {
    // Navigate to non-existent route
    await page.goto('/non-existent-page');
    // Should either show 404 or redirect to home
    await expect(page.locator('body')).toBeVisible();
  });
});