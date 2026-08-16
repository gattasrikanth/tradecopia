import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: '.',
  timeout: 30000,
  use: {
    baseURL: 'http://127.0.0.1:17841',
    ignoreHTTPSErrors: true
  },
  webServer: {
    command: 'dotnet run --project ../../src/ControlPlane/TradeCopia.ControlPlane/TradeCopia.ControlPlane.csproj -- --port=17841',
    url: 'http://127.0.0.1:17841/api/v1/system/health',
    reuseExistingServer: !process.env.CI,
    timeout: 60000
  }
});
