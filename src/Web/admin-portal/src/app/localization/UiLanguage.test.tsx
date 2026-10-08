import { fireEvent, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { UiLanguageProvider } from './UiLanguage';
import { useUiLanguage } from './UiLanguageContext';

function LanguageProbe() {
  const { language, setLanguage, t } = useUiLanguage();

  return (
    <button onClick={() => setLanguage(language === 'en' ? 'vi' : 'en')}>
      {language} · {t('Dark mode')} · {t('Product Tomato')}
    </button>
  );
}

describe('UiLanguageProvider', () => {
  beforeEach(() => localStorage.clear());
  afterEach(() => {
    document.documentElement.lang = 'en';
  });

  it('defaults to English and preserves unknown business text', () => {
    render(
      <UiLanguageProvider>
        <LanguageProbe />
      </UiLanguageProvider>,
    );

    expect(screen.getByRole('button')).toHaveTextContent('en · Dark mode · Product Tomato');
  });

  it('loads and persists Vietnamese UI language', () => {
    localStorage.setItem('happyboxx.ui-language', 'vi');

    render(
      <UiLanguageProvider>
        <LanguageProbe />
      </UiLanguageProvider>,
    );

    expect(screen.getByRole('button')).toHaveTextContent('vi · Chế độ tối · Product Tomato');
    expect(document.documentElement.lang).toBe('vi');

    fireEvent.click(screen.getByRole('button'));

    expect(screen.getByRole('button')).toHaveTextContent('en · Dark mode · Product Tomato');
    expect(localStorage.getItem('happyboxx.ui-language')).toBe('en');
  });
});
