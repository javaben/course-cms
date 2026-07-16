// Default (production) environment.
// Overridden in development by environment.development.ts via angular.json fileReplacements.
export const environment = {
  production: true,
  apiBaseUrl: '',
  // Public course site root the QR codes point at. Keep in sync with the API's PublicSite:BaseUrl
  // (appsettings.json) — the on-screen QR (this value) and the printed flyer QR (server value) must
  // match, and a printed sheet can't be hotfixed.
  publicSiteBaseUrl: 'https://www.uuu.com.tw',
};
