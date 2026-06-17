import { test, expect } from '../fixtures';

const uid = () => Date.now().toString(36) + Math.random().toString(36).slice(2, 5);

interface UserDto {
  id: string;
  userName: string;
  email: string | null;
  emailConfirmed: boolean;
}

interface ClaimDto {
  type: string;
  value: string;
}

interface CursorResult<T> {
  data: T[];
  pagination: { nextCursor: string | null; hasMore: boolean };
}

test.describe('User lifecycle', () => {
  test('creates, lists, and deletes a user', async ({ adminRequest }) => {
    const id = uid();
    const userName = `e2e-${id}`;
    const email = `e2e-${id}@test.com`;

    const createRes = await adminRequest.post('/api/v1/users', {
      data: { userName, email, password: 'E2eTest123!' },
    });
    expect(createRes.status()).toBe(201);
    const user = await createRes.json() as UserDto;
    expect(user.userName).toBe(userName);
    expect(user.email).toBe(email);
    expect(user.id).toBeTruthy();

    const listRes = await adminRequest.get('/api/v1/users');
    expect(listRes.status()).toBe(200);
    const list = await listRes.json() as CursorResult<UserDto>;
    expect(list.data.some(u => u.userName === userName)).toBe(true);

    const deleteRes = await adminRequest.delete(`/api/v1/users/${user.id}`);
    expect(deleteRes.status()).toBe(204);
  });

  test('create user with weak password returns 400', async ({ adminRequest }) => {
    const res = await adminRequest.post('/api/v1/users', {
      data: { userName: `e2e-${uid()}`, email: `weak-${uid()}@test.com`, password: 'weak' },
    });
    expect(res.status()).toBe(400);
  });

  test('delete non-existent user returns 404', async ({ adminRequest }) => {
    const res = await adminRequest.delete('/api/v1/users/00000000-0000-0000-0000-000000000000');
    expect(res.status()).toBe(404);
  });
});

test.describe('Cursor pagination', () => {
  test('GET /users returns cursor-shaped response', async ({ adminRequest }) => {
    const res = await adminRequest.get('/api/v1/users');
    expect(res.status()).toBe(200);
    const data = await res.json() as CursorResult<UserDto>;

    expect(Array.isArray(data.data)).toBe(true);
    expect(data.pagination).toHaveProperty('hasMore');
    expect(data.pagination).toHaveProperty('nextCursor');
  });

  test('limit=1 returns hasMore=true and a nextCursor when multiple users exist', async ({ adminRequest }) => {
    const id1 = uid();
    const id2 = uid();

    const [r1, r2] = await Promise.all([
      adminRequest.post('/api/v1/users', {
        data: { userName: `aaa-${id1}`, email: `aaa-${id1}@test.com`, password: 'E2eTest123!' },
      }),
      adminRequest.post('/api/v1/users', {
        data: { userName: `aab-${id2}`, email: `aab-${id2}@test.com`, password: 'E2eTest123!' },
      }),
    ]);
    const u1 = await r1.json() as UserDto;
    const u2 = await r2.json() as UserDto;

    const res = await adminRequest.get('/api/v1/users?limit=1');
    expect(res.status()).toBe(200);
    const data = await res.json() as CursorResult<UserDto>;
    expect(data.pagination.hasMore).toBe(true);
    expect(data.pagination.nextCursor).toBeTruthy();

    await Promise.all([
      adminRequest.delete(`/api/v1/users/${u1.id}`),
      adminRequest.delete(`/api/v1/users/${u2.id}`),
    ]);
  });

  test('invalid cursor returns 400', async ({ adminRequest }) => {
    const res = await adminRequest.get('/api/v1/users?cursor=!!!not-valid-base64!!!');
    expect(res.status()).toBe(400);
  });
});

test.describe('Claims lifecycle', () => {
  let userId: string;

  test.beforeEach(async ({ adminRequest }) => {
    const id = uid();
    const res = await adminRequest.post('/api/v1/users', {
      data: { userName: `claims-${id}`, email: `claims-${id}@test.com`, password: 'E2eTest123!' },
    });
    expect(res.status()).toBe(201);
    const user = await res.json() as UserDto;
    userId = user.id;
  });

  test.afterEach(async ({ adminRequest }) => {
    await adminRequest.delete(`/api/v1/users/${userId}`);
  });

  test('adds, lists, and removes a claim', async ({ adminRequest }) => {
    const addRes = await adminRequest.post(`/api/v1/users/${userId}/claims`, {
      data: { type: 'role', value: 'viewer' },
    });
    expect(addRes.status()).toBe(201);

    const listRes = await adminRequest.get(`/api/v1/users/${userId}/claims`);
    expect(listRes.status()).toBe(200);
    const claims = await listRes.json() as ClaimDto[];
    expect(claims.some(c => c.type === 'role' && c.value === 'viewer')).toBe(true);

    const removeRes = await adminRequest.delete(`/api/v1/users/${userId}/claims/role`);
    expect(removeRes.status()).toBe(204);

    const afterRes = await adminRequest.get(`/api/v1/users/${userId}/claims`);
    const after = await afterRes.json() as ClaimDto[];
    expect(after.some(c => c.type === 'role')).toBe(false);
  });

  test('add claim to non-existent user returns 404', async ({ adminRequest }) => {
    const res = await adminRequest.post('/api/v1/users/00000000-0000-0000-0000-000000000000/claims', {
      data: { type: 'role', value: 'viewer' },
    });
    expect(res.status()).toBe(404);
  });

  test('remove non-existent claim returns 404', async ({ adminRequest }) => {
    const res = await adminRequest.delete(`/api/v1/users/${userId}/claims/nonexistent-claim-type`);
    expect(res.status()).toBe(404);
  });
});
