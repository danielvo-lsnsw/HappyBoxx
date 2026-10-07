const currencyFormatter = new Intl.NumberFormat(undefined, {
  style: 'currency',
  currency: 'AUD',
});

export function formatCurrency(value: number): string {
  return currencyFormatter.format(value);
}
