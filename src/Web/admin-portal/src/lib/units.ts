import { formatNumber } from './format';

export const UNITS_OF_MEASURE = [
  'Piece',
  'Kilogram',
  'Gram',
  'Bunch',
  'Pack',
  'Box',
  'Carton',
] as const;

export type UnitOfMeasure = (typeof UNITS_OF_MEASURE)[number];

export const unitLabels: Record<UnitOfMeasure, { short: string; long: string }> = {
  Piece: { short: 'pcs', long: 'Pieces' },
  Kilogram: { short: 'kg', long: 'Kilograms' },
  Gram: { short: 'g', long: 'Grams' },
  Bunch: { short: 'bunch', long: 'Bunches' },
  Pack: { short: 'pack', long: 'Packs' },
  Box: { short: 'box', long: 'Boxes' },
  Carton: { short: 'ctn', long: 'Cartons' },
};

// Factors to a common base per dimension. Pack-based units need per-product pack sizes (future).
const weightInGrams: Partial<Record<UnitOfMeasure, number>> = { Kilogram: 1000, Gram: 1 };

export function convertibleUnits(unit: UnitOfMeasure): UnitOfMeasure[] {
  return unit in weightInGrams ? (Object.keys(weightInGrams) as UnitOfMeasure[]) : [unit];
}

export function convertQuantity(value: number, from: UnitOfMeasure, to: UnitOfMeasure): number {
  if (from === to) {
    return value;
  }
  const fromFactor = weightInGrams[from];
  const toFactor = weightInGrams[to];
  if (fromFactor === undefined || toFactor === undefined) {
    throw new Error(`Cannot convert ${from} to ${to}`);
  }
  return (value * fromFactor) / toFactor;
}

export function formatQuantity(value: number, unit: UnitOfMeasure): string {
  return `${formatNumber(value)} ${unitLabels[unit].short}`;
}
