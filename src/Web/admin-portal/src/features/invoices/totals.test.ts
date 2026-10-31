import { describe, expect, it } from 'vitest';
import { calculateInvoiceTotals } from './totals';

describe('calculateInvoiceTotals', () => {
  it('extracts 10% GST from GST-inclusive line prices', () => {
    expect(
      calculateInvoiceTotals([
        { quantity: 3, unitPriceInclusive: 28 },
        { quantity: 5, unitPriceInclusive: 1.99 },
      ]),
    ).toEqual({ subtotal: 85.41, gstAmount: 8.54, total: 93.95 });
  });

  it('rounds each line amount to cents before summing', () => {
    expect(calculateInvoiceTotals([{ quantity: 1.25, unitPriceInclusive: 2.99 }])).toEqual({
      subtotal: 3.4,
      gstAmount: 0.34,
      total: 3.74,
    });
  });
});
