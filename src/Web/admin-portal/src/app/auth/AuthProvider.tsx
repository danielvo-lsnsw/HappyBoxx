import { InteractionRequiredAuthError, type AccountInfo } from '@azure/msal-browser';
import { MsalProvider, useMsal } from '@azure/msal-react';
import { Center, Loader } from '@mantine/core';
import { useEffect, useEffectEvent, useMemo, useState, type ReactNode } from 'react';
import { useUiLanguage } from '@/app/localization/UiLanguageContext';
import { apiScope, msalInstance } from './authConfig';
import { setApiAuthHeadersProvider } from './apiAuth';
import {
  AuthContext,
  type AuthContextValue,
  type AuthenticatedUser,
  type HappyBoxxRole,
  useAuth,
} from './AuthContext';
import { SignInPage } from './SignInPage';

export function AuthBoundary({ children }: { children: ReactNode }) {
  if (!msalInstance) {
    return <DevelopmentAuthProvider>{children}</DevelopmentAuthProvider>;
  }

  return (
    <MsalProvider instance={msalInstance}>
      <EntraAuthProvider>{children}</EntraAuthProvider>
    </MsalProvider>
  );
}

export function AuthenticationGate({ children }: { children: ReactNode }) {
  const { isReady, user, signIn, signInAsDevelopmentRole } = useAuth();
  const { t } = useUiLanguage();

  if (!isReady) {
    return (
      <Center h="100dvh" aria-label={t('Checking sign-in')}>
        <Loader />
      </Center>
    );
  }

  if (!user || (user.role !== 'Admin' && user.role !== 'Order Creator')) {
    return <SignInPage onSignIn={signIn} onDevelopmentSignIn={signInAsDevelopmentRole} />;
  }

  return <>{children}</>;
}

function DevelopmentAuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthenticatedUser | null>(null);

  const value = useMemo<AuthContextValue>(
    () => ({
      isReady: true,
      user,
      signIn: () => undefined,
      signUp: () => signInWithDevelopmentRole('Customer', setUser),
      signOut: () => {
        setApiAuthHeadersProvider(async () => ({}));
        setUser(null);
      },
      signInAsDevelopmentRole: (role, email) => signInWithDevelopmentRole(role, setUser, email),
    }),
    [user],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

function signInWithDevelopmentRole(
  role: Exclude<HappyBoxxRole, 'Unassigned'>,
  setUser: (user: AuthenticatedUser) => void,
  requestedEmail?: string,
) {
  const normalizedRole = role.toLowerCase().replaceAll(' ', '-');
  const email = requestedEmail?.trim().toLowerCase() || `${normalizedRole}@example.test`;
  const user: AuthenticatedUser = {
    subject: `local:${email}`,
    email,
    name: `Local ${role}`,
    role,
    isDevelopmentIdentity: true,
  };
  setApiAuthHeadersProvider(async () => ({
    'X-HappyBoxx-Dev-Subject': user.subject,
    'X-HappyBoxx-Dev-Email': user.email,
    'X-HappyBoxx-Dev-Role': user.role,
  }));
  setUser(user);
}

function EntraAuthProvider({ children }: { children: ReactNode }) {
  const { accounts, inProgress, instance } = useMsal();
  const { t } = useUiLanguage();
  const account = instance.getActiveAccount() ?? accounts[0] ?? null;
  const [userState, setUserState] = useState<{
    subject: string;
    user: AuthenticatedUser;
  } | null>(null);
  const user = account && userState?.subject === account.homeAccountId ? userState.user : null;
  const isReady = inProgress === 'none' && (!account || user !== null);
  const accountSubject = account?.homeAccountId;

  useEffect(() => {
    if (!instance.getActiveAccount() && accounts[0]) {
      instance.setActiveAccount(accounts[0]);
    }
  }, [accounts, instance]);

  const loadAccountAccess = useEffectEvent(async (subject: string, isCurrent: () => boolean) => {
    const activeAccount =
      instance.getActiveAccount() ??
      accounts.find((candidate) => candidate.homeAccountId === subject);
    if (!activeAccount || activeAccount.homeAccountId !== subject) {
      return;
    }

    let accessToken: string;
    try {
      const result = await instance.acquireTokenSilent({
        account: activeAccount,
        scopes: [apiScope],
      });
      accessToken = result.accessToken;
    } catch (error) {
      if (import.meta.env.DEV) {
        console.error('Could not acquire a HappyBoxx API access token.', error);
      }
      if (isCurrent()) {
        setUserState({
          subject,
          user: toAuthenticatedUser(activeAccount, 'Unassigned'),
        });
      }
      return;
    }

    try {
      const response = await fetch('/api/v1/identity/access/current', {
        headers: { Authorization: `Bearer ${accessToken}` },
      });
      if (!response.ok && import.meta.env.DEV) {
        console.error(`HappyBoxx access check returned HTTP ${response.status}.`);
      }
      const access = response.ok
        ? ((await response.json()) as { isActive: boolean; role: string })
        : { isActive: false, role: 'Unassigned' };
      const role: HappyBoxxRole =
        access.isActive &&
        (access.role === 'Admin' || access.role === 'Order Creator' || access.role === 'Customer')
          ? access.role
          : 'Unassigned';
      if (isCurrent()) {
        setUserState({ subject, user: toAuthenticatedUser(activeAccount, role) });
      }
    } catch (error) {
      if (import.meta.env.DEV) {
        console.error('Could not check HappyBoxx account access.', error);
      }
      if (isCurrent()) {
        setUserState({
          subject,
          user: toAuthenticatedUser(activeAccount, 'Unassigned'),
        });
      }
    }
  });

  useEffect(() => {
    if (!accountSubject || inProgress !== 'none') {
      return;
    }

    let current = true;
    void Promise.resolve().then(() => loadAccountAccess(accountSubject, () => current));
    return () => {
      current = false;
    };
  }, [accountSubject, inProgress, instance]);

  useEffect(() => {
    setApiAuthHeadersProvider(async (): Promise<Record<string, string>> => {
      const activeAccount = instance.getActiveAccount() ?? accounts[0];
      if (!activeAccount) {
        return {};
      }

      try {
        const result = await instance.acquireTokenSilent({
          account: activeAccount,
          scopes: [apiScope],
        });
        return { Authorization: `Bearer ${result.accessToken}` };
      } catch (error) {
        if (error instanceof InteractionRequiredAuthError) {
          await instance.acquireTokenRedirect({ account: activeAccount, scopes: [apiScope] });
          return {};
        }
        throw error;
      }
    });
    return () => setApiAuthHeadersProvider(async () => ({}));
  }, [accounts, instance]);

  const value = useMemo<AuthContextValue>(
    () => ({
      isReady,
      user,
      signIn: () => {
        void instance.loginRedirect({
          scopes: [apiScope],
          prompt: 'login',
        });
      },
      signOut: () => {
        void instance.logoutRedirect({ account: account ?? undefined });
      },
      signUp: () => {
        void instance.loginRedirect({
          scopes: [apiScope],
          prompt: 'create',
        });
      },
      signInAsDevelopmentRole: () => undefined,
    }),
    [account, instance, isReady, user],
  );

  if (!isReady && !user) {
    return (
      <Center h="100dvh" aria-label={t('Checking sign-in')}>
        <Loader />
      </Center>
    );
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

function toAuthenticatedUser(account: AccountInfo, role: HappyBoxxRole): AuthenticatedUser {
  const claims = account.idTokenClaims as Record<string, unknown> | undefined;
  const emails = Array.isArray(claims?.emails) ? claims.emails : [];
  const email =
    typeof claims?.email === 'string'
      ? claims.email
      : typeof emails[0] === 'string'
        ? emails[0]
        : account.username;

  return {
    subject: account.homeAccountId,
    email,
    name: account.name ?? email,
    role,
    isDevelopmentIdentity: false,
  };
}
