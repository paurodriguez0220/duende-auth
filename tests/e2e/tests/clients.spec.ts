import { test, expect } from '../fixtures';

const uid = () => Date.now().toString(36) + Math.random().toString(36).slice(2, 5);

interface ClientSummaryDto {
  clientId: string;
  allowedScopes: string[];
  allowedGrantTypes: string[];
}

test.describe('Client lifecycle', () => {
  test('registers and deletes a client_credentials client', async ({ adminRequest }) => {
    const clientId = `e2e-cc-${uid()}`;

    const createRes = await adminRequest.post('/api/v1/clients', {
      data: {
        clientId,
        secret: 'e2e-test-secret',
        allowedScopes: ['scalar-api'],
        grantType: 'client_credentials',
      },
    });
    expect(createRes.status()).toBe(201);
    const client = await createRes.json() as ClientSummaryDto;
    expect(client.clientId).toBe(clientId);
    expect(client.allowedGrantTypes).toContain('client_credentials');
    expect(client.allowedScopes).toContain('scalar-api');

    const listRes = await adminRequest.get('/api/v1/clients');
    expect(listRes.status()).toBe(200);
    const list = await listRes.json() as ClientSummaryDto[];
    expect(list.some(c => c.clientId === clientId)).toBe(true);

    const deleteRes = await adminRequest.delete(`/api/v1/clients/${clientId}`);
    expect(deleteRes.status()).toBe(204);

    const afterList = await adminRequest.get('/api/v1/clients');
    const after = await afterList.json() as ClientSummaryDto[];
    expect(after.some(c => c.clientId === clientId)).toBe(false);
  });

  test('registers an authorization_code client with redirect URIs', async ({ adminRequest }) => {
    const clientId = `e2e-ac-${uid()}`;

    const createRes = await adminRequest.post('/api/v1/clients', {
      data: {
        clientId,
        secret: 'e2e-test-secret',
        allowedScopes: ['openid', 'profile', 'email'],
        grantType: 'authorization_code',
        redirectUris: ['https://app.example.com/callback'],
        postLogoutRedirectUris: ['https://app.example.com/logout'],
      },
    });
    expect(createRes.status()).toBe(201);
    const client = await createRes.json() as ClientSummaryDto;
    expect(client.allowedGrantTypes).toContain('authorization_code');

    await adminRequest.delete(`/api/v1/clients/${clientId}`);
  });

  test('duplicate clientId returns 409', async ({ adminRequest }) => {
    const clientId = `e2e-dup-${uid()}`;

    const first = await adminRequest.post('/api/v1/clients', {
      data: { clientId, secret: 'secret', allowedScopes: ['scalar-api'], grantType: 'client_credentials' },
    });
    expect(first.status()).toBe(201);

    const second = await adminRequest.post('/api/v1/clients', {
      data: { clientId, secret: 'secret', allowedScopes: ['scalar-api'], grantType: 'client_credentials' },
    });
    expect(second.status()).toBe(409);

    await adminRequest.delete(`/api/v1/clients/${clientId}`);
  });

  test('delete admin-client returns 409', async ({ adminRequest }) => {
    const res = await adminRequest.delete('/api/v1/clients/admin-client');
    expect(res.status()).toBe(409);
  });

  test('delete non-existent client returns 404', async ({ adminRequest }) => {
    const res = await adminRequest.delete(`/api/v1/clients/does-not-exist-${uid()}`);
    expect(res.status()).toBe(404);
  });
});

test.describe('Seeded clients', () => {
  test('GET /clients contains all seeded clients', async ({ adminRequest }) => {
    const res = await adminRequest.get('/api/v1/clients');
    expect(res.status()).toBe(200);
    const clients = await res.json() as ClientSummaryDto[];
    const clientIds = clients.map(c => c.clientId);

    expect(clientIds).toContain('scalar-client');
    expect(clientIds).toContain('admin-client');
    expect(clientIds).toContain('watcher-client');
  });
});

test.describe('Client validation', () => {
  test('authorization_code without redirectUris returns 400', async ({ adminRequest }) => {
    const res = await adminRequest.post('/api/v1/clients', {
      data: {
        clientId: `e2e-invalid-${uid()}`,
        secret: 'secret',
        allowedScopes: ['openid'],
        grantType: 'authorization_code',
      },
    });
    expect(res.status()).toBe(400);
  });

  test('invalid grantType returns 400', async ({ adminRequest }) => {
    const res = await adminRequest.post('/api/v1/clients', {
      data: {
        clientId: `e2e-invalid-${uid()}`,
        secret: 'secret',
        allowedScopes: ['scalar-api'],
        grantType: 'implicit',
      },
    });
    expect(res.status()).toBe(400);
  });

  test('missing clientId returns 400', async ({ adminRequest }) => {
    const res = await adminRequest.post('/api/v1/clients', {
      data: { secret: 'secret', allowedScopes: ['scalar-api'] },
    });
    expect(res.status()).toBe(400);
  });

  test('missing allowedScopes returns 400', async ({ adminRequest }) => {
    const res = await adminRequest.post('/api/v1/clients', {
      data: { clientId: `e2e-invalid-${uid()}`, secret: 'secret' },
    });
    expect(res.status()).toBe(400);
  });
});
