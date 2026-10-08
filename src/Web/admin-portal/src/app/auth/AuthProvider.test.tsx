import { fireEvent, render, screen } from '@testing-library/react';
import { MantineProvider } from '@mantine/core';
import { MemoryRouter } from 'react-router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { UiLanguageProvider } from '@/app/localization/UiLanguage';
import { getApiAuthHeaders, setApiAuthHeadersProvider } from './apiAuth';
import { AuthBoundary, AuthenticationGate } from './AuthProvider';

describe('local authentication provider', () => {
  beforeEach(() => {
    setApiAuthHeadersProvider(async () => ({}));
    vi.stubGlobal(
      'matchMedia',
      vi.fn().mockImplementation((media: string) => ({
        matches: false,
        media,
        onchange: null,
        addListener: vi.fn(),
        removeListener: vi.fn(),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(),
      })),
    );
  });
  afterEach(() => setApiAuthHeadersProvider(async () => ({})));

  it('keeps routes gated until a local role is selected and supplies matching dev headers', async () => {
    render(
      <MantineProvider>
        <MemoryRouter>
          <UiLanguageProvider>
            <AuthBoundary>
              <AuthenticationGate>
                <p>Protected portal</p>
              </AuthenticationGate>
            </AuthBoundary>
          </UiLanguageProvider>
        </MemoryRouter>
      </MantineProvider>,
    );

    expect(screen.queryByText('Protected portal')).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Continue as local Order Creator' }));

    expect(await screen.findByText('Protected portal')).toBeInTheDocument();
    await expect(getApiAuthHeaders()).resolves.toMatchObject({
      'X-HappyBoxx-Dev-Subject': 'local:order-creator@example.test',
      'X-HappyBoxx-Dev-Email': 'order-creator@example.test',
      'X-HappyBoxx-Dev-Role': 'Order Creator',
    });
  });

  it('does not allow a customer identity into the staff portal', () => {
    render(
      <MantineProvider>
        <MemoryRouter>
          <UiLanguageProvider>
            <AuthBoundary>
              <AuthenticationGate>
                <p>Protected portal</p>
              </AuthenticationGate>
            </AuthBoundary>
          </UiLanguageProvider>
        </MemoryRouter>
      </MantineProvider>,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Continue as local Customer' }));

    expect(screen.queryByText('Protected portal')).not.toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Sign in to HappyBoxx' })).toBeInTheDocument();
  });
});
