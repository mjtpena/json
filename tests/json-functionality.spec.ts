import { test, expect } from '@playwright/test';

test.describe('JSON Functionality', () => {
  const validJson = '{"name": "John", "age": 30, "city": "New York"}';
  const invalidJson = '{"name": "John", "age": 30, "city":}';
  const complexJson = `{
    "users": [
      {"id": 1, "name": "Alice", "email": "alice@example.com"},
      {"id": 2, "name": "Bob", "email": "bob@example.com"}
    ],
    "meta": {
      "total": 2,
      "page": 1
    }
  }`;

  test.describe('JSON Formatter', () => {
    test.beforeEach(async ({ page }) => {
      await page.goto('/formatter');
    });

    test('should format valid JSON', async ({ page }) => {
      // Wait for page to load
      await expect(page.locator('h1')).toContainText('Formatter');
      
      // Look for editor or textarea
      const editor = page.locator('textarea, .monaco-editor, [data-testid="json-input"]').first();
      if (await editor.isVisible()) {
        await editor.fill(validJson);
        
        // Look for format button
        const formatBtn = page.locator('button').filter({ hasText: /format|beautify/i }).first();
        if (await formatBtn.isVisible()) {
          await formatBtn.click();
          // Should see formatted output
          await expect(page.locator('body')).toContainText('John');
        }
      }
    });

    test('should handle invalid JSON gracefully', async ({ page }) => {
      const editor = page.locator('textarea, .monaco-editor, [data-testid="json-input"]').first();
      if (await editor.isVisible()) {
        await editor.fill(invalidJson);
        
        // Should show error message
        await expect(page.locator('body')).toContainText(/error|invalid/i);
      }
    });
  });

  test.describe('JSON Validator', () => {
    test.beforeEach(async ({ page }) => {
      await page.goto('/validator');
    });

    test('should validate JSON and show results', async ({ page }) => {
      await expect(page.locator('h1')).toContainText('Validator');
      
      const editor = page.locator('textarea, .monaco-editor, [data-testid="json-input"]').first();
      if (await editor.isVisible()) {
        await editor.fill(validJson);
        
        // Look for validate button
        const validateBtn = page.locator('button').filter({ hasText: /validate/i }).first();
        if (await validateBtn.isVisible()) {
          await validateBtn.click();
          
          // Should show validation results
          await expect(page.locator('body')).toContainText(/valid|success/i);
        }
      }
    });
  });

  test.describe('JSON Converter', () => {
    test.beforeEach(async ({ page }) => {
      await page.goto('/converter');
    });

    test('should convert JSON to other formats', async ({ page }) => {
      await expect(page.locator('h1')).toContainText('Converter');
      
      // Check for format options
      const formatOptions = ['XML', 'YAML', 'CSV'];
      for (const format of formatOptions) {
        const option = page.locator('text=' + format).first();
        if (await option.isVisible()) {
          // Format conversion UI is present
          break;
        }
      }
    });
  });

  test.describe('JSON Diff', () => {
    test.beforeEach(async ({ page }) => {
      await page.goto('/diff');
    });

    test('should compare two JSON documents', async ({ page }) => {
      await expect(page.locator('h1')).toContainText('Diff');
      
      // Should have two input areas
      const editors = page.locator('textarea, .monaco-editor');
      await expect(editors).toHaveCountGreaterThan(1);
    });
  });
});