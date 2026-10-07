import { getStockStatus } from './stockStatus';

describe('getStockStatus', () => {
  it.each([
    [0, true, 'danger', 'Out of stock'],
    [3, true, 'warning', 'Low stock'],
    [40, false, 'success', 'In stock'],
  ] as const)('qty %d (low=%s) → %s', (quantityOnHand, isLowStock, tone, label) => {
    expect(getStockStatus({ quantityOnHand, isLowStock })).toEqual({ tone, label });
  });
});
