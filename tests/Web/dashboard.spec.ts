import { test, expect } from '@playwright/test';

test('overview is customer-grade and stays fixture-free when disconnected', async ({ page, request }) => {
  await page.goto('/');
  await expect(page.locator('h1')).toContainText('Overview');
  await expect(page.locator('body')).toContainText('Copying');
  await expect(page.locator('body')).toContainText('Disabled');
  await expect(page.locator('body')).toContainText('Disconnected');
  await expect(page.locator('body')).toContainText('SIM / Demo Only');
  await expect(page.locator('body')).toContainText('Copying starts disabled');
  await expect(page.locator('#status-pill')).not.toContainText('UNKNOWN');
  await expect(page.locator('body')).not.toContainText('live-certified');
  await expect(page.locator('body')).not.toContainText('SIM-LEADER-01');
  await expect(page.locator('body')).not.toContainText('SIM-FOLLOWER-03');
  await expect(page.locator('body')).not.toContainText('DemoPaper');
  await expect(page.locator('body')).not.toContainText('Selectable = true');
  await expect(page.locator('#alerts .critical')).toHaveCount(0);

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
});

test('copy groups uses Save & Activate and hides internal keys', async ({ page, request }) => {
  await page.goto('/');
  await page.locator('button[data-route="groups"]').click();
  await expect(page.locator('h1')).toContainText('Copy Groups');
  await expect(page.locator('body')).toContainText('NinjaTrader engine disconnected');
  await expect(page.locator('body')).not.toContainText('SIM-LEADER-01');
  await expect(page.locator('body')).not.toContainText('DemoPaper');
  await expect(page.locator('body')).not.toContainText('Provider31|');

  const js = await request.get('/app.js');
  const text = await js.text();
  expect(text).toContain('Save &amp; Activate');
  expect(text).toContain('Enable Non-Live Copying');
  expect(text).toContain('Pause New Entries');
  expect(text).toContain('Disable Copying');
  expect(text).toContain('1 : 1');
  expect(text).not.toContain('UNKNOWN');
});
