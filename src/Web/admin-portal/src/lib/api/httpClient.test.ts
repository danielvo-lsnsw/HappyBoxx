import { ApiError, buildUrl, httpClient } from './httpClient';

describe('buildUrl', () => {
  it('prefixes the API base and skips empty params', () => {
    expect(buildUrl('/products', { page: 2, search: '', categoryId: undefined })).toBe(
      '/api/v1/products?page=2',
    );
  });
});

describe('httpClient', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('throws ApiError carrying the problem details on failure', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(JSON.stringify({ status: 409, detail: 'Duplicate', errorCode: 'X.Conflict' }), {
        status: 409,
      }),
    );

    const error = await httpClient.post('/categories', { name: 'Fruits' }).catch((e: unknown) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).status).toBe(409);
    expect((error as ApiError).problem.errorCode).toBe('X.Conflict');
  });
});
