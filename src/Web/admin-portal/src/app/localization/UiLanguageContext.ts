import { createContext, useContext } from 'react';

export type UiLanguage = 'en' | 'vi';

interface UiLanguageContextValue {
  language: UiLanguage;
  setLanguage: (language: UiLanguage) => void;
  t: (englishText: string) => string;
}

export const UiLanguageContext = createContext<UiLanguageContextValue | null>(null);

export function useUiLanguage() {
  const context = useContext(UiLanguageContext);
  if (!context) {
    throw new Error('useUiLanguage must be used within UiLanguageProvider');
  }
  return context;
}
