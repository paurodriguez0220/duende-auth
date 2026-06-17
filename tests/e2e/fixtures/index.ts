import { test as base, APIRequestContext } from '@playwright/test';

type AuthFixtures = {
  adminRequest: APIRequestContext;
  watcherRequest: APIRequestContext;
  unauthRequest: APIRequestContext;
};

async function fetchToken(
  baseURL: string,
  clientId: string,
  clientSecret: string,
  scope: string,
): Promise<string> {
  const res = await fetch(`${baseURL}/connect/token`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams({
      grant_type: 'client_credentials',
      client_id: clientId,
      client_secret: clientSecret,
      scope,
    }).toString(),
  });
  if (!res.ok) throw new Error(`Token request failed: ${res.status} ${await res.text()}`);
  const data = await res.json() as { access_token: string };
  return data.access_token;
}

export const test = base.extend<AuthFixtures>({
  adminRequest: async ({ playwright }, use) => {
    const baseURL = process.env.BASE_URL!;
    const token = await fetchToken(baseURL, 'admin-client', process.env.ADMIN_CLIENT_SECRET!, 'duende:manage');
    const ctx = await playwright.request.newContext({
      baseURL,
      extraHTTPHeaders: { Authorization: `Bearer ${token}`, Accept: 'application/json' },
    });
    await use(ctx);
    await ctx.dispose();
  },

  watcherRequest: async ({ playwright }, use) => {
    const baseURL = process.env.BASE_URL!;
    const token = await fetchToken(baseURL, 'watcher-client', process.env.WATCHER_CLIENT_SECRET!, 'duende:read');
    const ctx = await playwright.request.newContext({
      baseURL,
      extraHTTPHeaders: { Authorization: `Bearer ${token}`, Accept: 'application/json' },
    });
    await use(ctx);
    await ctx.dispose();
  },

  unauthRequest: async ({ playwright }, use) => {
    const ctx = await playwright.request.newContext({
      baseURL: process.env.BASE_URL!,
      extraHTTPHeaders: { Accept: 'application/json' },
    });
    await use(ctx);
    await ctx.dispose();
  },
});

export { expect } from '@playwright/test';
