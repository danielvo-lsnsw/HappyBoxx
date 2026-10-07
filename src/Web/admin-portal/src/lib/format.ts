// Single place to change locale/currency for the whole portal.
const CURRENCY = 'AUD';

const currencyFormatter = new Intl.NumberFormat(undefined, {
  style: 'currency',
  currency: CURRENCY,
});
const numberFormatter = new Intl.NumberFormat(undefined, { maximumFractionDigits: 3 });
const dateTimeFormatter = new Intl.DateTimeFormat(undefined, {
  dateStyle: 'medium',
  timeStyle: 'short',
});

export function formatCurrency(value: number): string {
  return currencyFormatter.format(value);
}

export function formatNumber(value: number): string {
  return numberFormatter.format(value);
}

export function formatDateTime(isoDate: string): string {
  return dateTimeFormatter.format(new Date(isoDate));
}

export function currencySymbol(): string {
  return currencyFormatter.formatToParts(0).find((p) => p.type === 'currency')?.value ?? '$';
}
