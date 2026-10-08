export type ApiAuthHeadersProvider = () => Promise<Record<string, string>>;

let apiAuthHeadersProvider: ApiAuthHeadersProvider = async () => ({});

export function setApiAuthHeadersProvider(provider: ApiAuthHeadersProvider) {
  apiAuthHeadersProvider = provider;
}

export function getApiAuthHeaders() {
  return apiAuthHeadersProvider();
}
