// Development environment — points directly at the local CMS.API (no proxy).
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5000',
  // Public course site root the QR codes point at (same public site even in dev). Keep in sync
  // with the API's PublicSite:BaseUrl (appsettings.json) — see the note in environment.ts.
  publicSiteBaseUrl: 'https://www.uuu.com.tw',
};
