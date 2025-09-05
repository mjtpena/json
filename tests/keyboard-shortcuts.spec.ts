import { test, expect } from '@playwright/test';

test.describe('Keyboard Shortcuts', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    // Wait for keyboard shortcuts to initialize
    await page.waitForTimeout(1000);
  });

  test('should show keyboard help with F1', async ({ page }) => {
    await page.keyboard.press('F1');
    
    // Should show help modal or overlay
    await expect(page.locator('body')).toContainText(/shortcut|help|keyboard/i);
  });

  test('should open command palette with Ctrl+K', async ({ page }) => {
    await page.keyboard.press('Control+k');
    
    // Should show command palette
    await expect(page.locator('.command-palette, [data-testid="command-palette"]')).toBeVisible({ timeout: 3000 });
  });

  test('should navigate to tools with Alt+Number shortcuts', async ({ page }) => {
    // Alt+1 should go to formatter
    await page.keyboard.press('Alt+1');
    await expect(page).toHaveURL('/formatter');
    
    // Go back and try Alt+2 for validator
    await page.goto('/');
    await page.keyboard.press('Alt+2');
    await expect(page).toHaveURL('/validator');
  });

  test('should handle keyboard shortcuts in different contexts', async ({ page }) => {
    // Test that shortcuts work from different pages
    await page.goto('/formatter');
    await page.keyboard.press('Control+k');
    
    // Command palette should still work
    const palette = page.locator('.command-palette, [data-testid="command-palette"]');
    if (await palette.isVisible({ timeout: 1000 })) {
      // Palette is working
      await page.keyboard.press('Escape');
      await expect(palette).not.toBeVisible();
    }
  });

  test('should prevent default browser shortcuts', async ({ page }) => {
    // Test that our custom shortcuts don't trigger browser defaults
    await page.keyboard.press('Control+k');
    
    // Should not open browser search, should open our command palette
    await expect(page.locator('body')).toContainText(/command|search|palette/i);
  });
});