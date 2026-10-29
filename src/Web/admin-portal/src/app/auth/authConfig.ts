import { LogLevel, PublicClientApplication, type Configuration } from '@azure/msal-browser';

const requestedMode = import.meta.env.VITE_AUTH_MODE;
if (requestedMode === 'development' && !import.meta.env.DEV) {
  throw new Error('Development authentication can only run with the Vite development server.');
}

export const authMode =
  requestedMode === 'development' || (import.meta.env.DEV && !requestedMode)
    ? 'development'
    : 'entra';

const clientId = import.meta.env.VITE_ENTRA_CLIENT_ID;
const tenantId = import.meta.env.VITE_ENTRA_TENANT_ID;
const authority = import.meta.env.VITE_ENTRA_AUTHORITY;
export const apiScope = import.meta.env.VITE_ENTRA_API_SCOPE;

function createMsalConfig(): Configuration {
  if (!clientId || !tenantId || !authority || !apiScope) {
    throw new Error(
      'Entra authentication requires VITE_ENTRA_CLIENT_ID, VITE_ENTRA_TENANT_ID, VITE_ENTRA_AUTHORITY, and VITE_ENTRA_API_SCOPE.',
    );
  }

  const authorityUrl = new URL(authority);
  if (authorityUrl.protocol !== 'https:') {
    throw new Error('The Entra authority must use HTTPS.');
  }

  return {
    auth: {
      clientId,
      authority,
      knownAuthorities: [authorityUrl.host, `${tenantId}.ciamlogin.com`],
      redirectUri: import.meta.env.VITE_ENTRA_REDIRECT_URI || window.location.origin,
      postLogoutRedirectUri: window.location.origin,
    },
    cache: {
      cacheLocation: 'sessionStorage',
    },
    system: {
      loggerOptions: {
        loggerCallback: (level, message, containsPii) => {
          if (containsPii) {
            return;
          }
          if (level === LogLevel.Error) {
            console.error(message);
          } else if (level === LogLevel.Warning) {
            console.warn(message);
          } else if (import.meta.env.DEV && level === LogLevel.Verbose) {
            console.debug(message);
          }
        },
        logLevel: import.meta.env.DEV ? LogLevel.Verbose : LogLevel.Error,
        piiLoggingEnabled: false,
      },
    },
  };
}

export const msalInstance =
  authMode === 'entra' ? new PublicClientApplication(createMsalConfig()) : null;
