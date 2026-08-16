import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: '.',
  timeout: 30000,
  use: {
    baseURL: 'http://127.0.0.1:17842',
    ignoreHTTPSErrors: true
  },
  webServer: {
    command: 'dotnet run --project ../../src/ControlPlane/TradeCopia.ControlPlane/TradeCopia.ControlPlane.csproj -- --port=17842 --pipe=TradeCopia.E2E.isolated --data=C:/Users/monug/AppData/Local/Temp/tradecopia-e2e-data',
    url: 'http://127.0.0.1:17842/api/v1/system/health',
    reuseExistingServer: false,
    timeout: 60000
  }
});
