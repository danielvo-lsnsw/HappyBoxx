import { convertibleUnits, convertQuantity } from './units';

describe('units', () => {
  it('converts between weight units', () => {
    expect(convertQuantity(2.5, 'Kilogram', 'Gram')).toBe(2500);
    expect(convertQuantity(750, 'Gram', 'Kilogram')).toBe(0.75);
  });

  it('only offers conversions that are known', () => {
    expect(convertibleUnits('Kilogram')).toEqual(['Kilogram', 'Gram']);
    expect(convertibleUnits('Carton')).toEqual(['Carton']);
    expect(() => convertQuantity(1, 'Carton', 'Piece')).toThrow();
  });
});
