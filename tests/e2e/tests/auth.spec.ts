import { test, expect } from '../fixtures';

test.describe('Discovery endpoint', () => {
  test('returns expected scopes and endpoints', async ({ request }) => {
    const res = await request.get('/.well-known/openid-configuration');

    expect(res.status()).toBe(200);
    const data = await res.json() as { scopes_supported: string[]; token_endpoint: string };
    expect(data.scopes_supported).toContain('openid');
    expect(data.scopes_supported).toContain('profile');
    expect(data.scopes_supported).toContain('email');
    expect(data.scopes_supported).toContain('duende:manage');
    expect(data.scopes_supported).toContain('duende:read');
    expect(data.token_endpoint).toContain('/connect/token');
  });
});

test.describe('Token issuance', () => {
  test('admin-client issues duende:manage token', async ({ request }) => {
    const res = await request.post('/connect/token', {
      form: {
        grant_type: 'client_credentials',
        client_id: 'admin-client',
        client_secret: process.env.ADMIN_CLIENT_SECRET!,
        scope: 'duende:manage',
      },
    });

    expect(res.status()).toBe(200);
    const data = await res.json() as { access_token: string; token_type: string };
    expect(data.access_token).toBeTruthy();
    expect(data.token_type.toLowerCase()).toBe('bearer');
  });

  test('watcher-client issues duende:read token', async ({ request }) => {
    const res = await request.post('/connect/token', {
      form: {
        grant_type: 'client_credentials',
        client_id: 'watcher-client',
        client_secret: process.env.WATCHER_CLIENT_SECRET!,
        scope: 'duende:read',
      },
    });

    expect(res.status()).toBe(200);
    const data = await res.json() as { access_token: string };
    expect(data.access_token).toBeTruthy();
  });

  test('wrong secret returns 400', async ({ request }) => {
    const res = await request.post('/connect/token', {
      form: {
        grant_type: 'client_credentials',
        client_id: 'admin-client',
        client_secret: 'wrong-secret',
        scope: 'duende:manage',
      },
    });

    expect(res.status()).toBe(400);
  });
});

test.describe('Authorization enforcement', () => {
  test('no token returns 401 on GET /users', async ({ unauthRequest }) => {
    const res = await unauthRequest.get('/api/v1/users');
    expect(res.status()).toBe(401);
  });

  test('no token returns 401 on GET /clients', async ({ unauthRequest }) => {
    const res = await unauthRequest.get('/api/v1/clients');
    expect(res.status()).toBe(401);
  });

  test('duende:read token returns 200 on GET /users', async ({ watcherRequest }) => {
    const res = await watcherRequest.get('/api/v1/users');
    expect(res.status()).toBe(200);
  });

  test('duende:read token returns 200 on GET /clients', async ({ watcherRequest }) => {
    const res = await watcherRequest.get('/api/v1/clients');
    expect(res.status()).toBe(200);
  });

  test('duende:read token returns 403 on POST /users', async ({ watcherRequest }) => {
    const res = await watcherRequest.post('/api/v1/users', {
      data: { userName: 'forbidden-user', email: 'forbidden@test.com', password: 'Test123!' },
    });
    expect(res.status()).toBe(403);
  });

  test('duende:read token returns 403 on POST /clients', async ({ watcherRequest }) => {
    const res = await watcherRequest.post('/api/v1/clients', {
      data: { clientId: 'forbidden-client', secret: 'secret', allowedScopes: ['scalar-api'] },
    });
    expect(res.status()).toBe(403);
  });

  test('duende:read token returns 403 on DELETE /users/:id', async ({ watcherRequest }) => {
    const res = await watcherRequest.delete('/api/v1/users/some-id');
    expect(res.status()).toBe(403);
  });
});

test.describe('Security headers', () => {
  test('responses include required security headers', async ({ request }) => {
    const res = await request.get('/.well-known/openid-configuration');
    const headers = res.headers();

    expect(headers['x-content-type-options']).toBe('nosniff');
    expect(headers['x-frame-options']).toBe('DENY');
    expect(headers['referrer-policy']).toBe('strict-origin-when-cross-origin');
  });
});
