import { test, expect } from '@playwright/test';

test.describe('Home Page', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should display hero section with correct branding', async ({ page }) => {
    // Check hero title
    await expect(page.locator('#hero-title')).toContainText('Ultimate JSON');
    
    // Check hero description
    await expect(page.locator('.hero-description')).toContainText('most powerful JSON toolkit');
    
    // Check CTA buttons
    await expect(page.getByRole('button', { name: 'Start Editing' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'View Demo' })).toBeVisible();
  });

  test('should display stats section', async ({ page }) => {
    await expect(page.locator('.hero-stats')).toBeVisible();
    await expect(page.locator('.stat-number').first()).toContainText('10+');
    await expect(page.locator('.stat-label').first()).toContainText('Tools');
  });

  test('should show quick tools grid', async ({ page }) => {
    await expect(page.locator('.tools-grid')).toBeVisible();
    
    // Check for key tools
    await expect(page.locator('.tool-card').filter({ hasText: 'JSON Formatter' })).toBeVisible();
    await expect(page.locator('.tool-card').filter({ hasText: 'JSON Validator' })).toBeVisible();
    await expect(page.locator('.tool-card').filter({ hasText: 'Format Converter' })).toBeVisible();
  });

  test('should navigate to formatter on CTA click', async ({ page }) => {
    await page.getByRole('button', { name: 'Start Editing' }).click();
    await expect(page).toHaveURL('/formatter');
  });

  test('should navigate to tools on card click', async ({ page }) => {
    await page.locator('.tool-card').filter({ hasText: 'JSON Formatter' }).click();
    await expect(page).toHaveURL('/formatter');
  });

  test('should show keyboard shortcuts hint', async ({ page }) => {
    await expect(page.locator('.keyboard-hint')).toBeVisible();
    await expect(page.locator('.keyboard-hint')).toContainText('F1');
  });
});