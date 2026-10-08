import { createContext, useContext } from 'react';

export type HappyBoxxRole = 'Admin' | 'Order Creator' | 'Customer' | 'Unassigned';

export interface AuthenticatedUser {
  subject: string;
  email: string;
  name: string;
  role: HappyBoxxRole;
  isDevelopmentIdentity: boolean;
}

export interface AuthContextValue {
  isReady: boolean;
  user: AuthenticatedUser | null;
  signIn: () => void;
  signUp: () => void;
  signOut: () => void;
  signInAsDevelopmentRole: (role: Exclude<HappyBoxxRole, 'Unassigned'>, email?: string) => void;
}

export const AuthContext = createContext<AuthContextValue | null>(null);

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthBoundary');
  }
  return context;
}
