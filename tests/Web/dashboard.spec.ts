import { test, expect } from '@playwright/test';

test('dashboard loads synthetic demo and requires CSRF for flatten', async ({ page, request }) => {
  await page.goto('/');
  await expect(page.locator('h1')).toContainText('Overview');
  await expect(page.locator('body')).toContainText('disabled');
  await expect(page.locator('body')).toContainText('Disconnected');
  await expect(page.locator('body')).not.toContainText('live-certified');
  await expect(page.locator('body')).toContainText('SIM-');

  const noCsrf = await request.post('/api/v1/flatten/prepare', { data: {} });
  expect(noCsrf.status()).toBe(403);

  const boot = await request.get('/api/v1/system/bootstrap');
  const { csrfToken } = await boot.json();
  const forbidden = await request.post('/api/v1/orders', {
    headers: { 'X-CSRF-Token': csrfToken },
    data: { qty: 1 }
  });
  expect(forbidden.status()).toBe(404);
  const forbiddenBody = await forbidden.json();
  expect(forbiddenBody.error).toBe('no-generic-order-entry');

  const prepared = await request.post('/api/v1/flatten/prepare', {
    headers: { 'X-CSRF-Token': csrfToken },
    data: {}
  });
  expect(prepared.ok()).toBeTruthy();
  const body = await prepared.json();
  expect(body.confirmationId).toBeTruthy();
});
