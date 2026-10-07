import { SegmentedControl } from '@mantine/core';
import { convertibleUnits, unitLabels, type UnitOfMeasure } from '@/lib/units';

interface UnitToggleProps {
  baseUnit: UnitOfMeasure;
  value: UnitOfMeasure;
  onChange: (unit: UnitOfMeasure) => void;
}

/** Lets users switch the display unit; renders nothing when no conversion is known. */
export function UnitToggle({ baseUnit, value, onChange }: UnitToggleProps) {
  const units = convertibleUnits(baseUnit);
  if (units.length < 2) {
    return null;
  }

  return (
    <SegmentedControl
      aria-label="Display unit"
      size="sm"
      value={value}
      onChange={(unit) => onChange(unit as UnitOfMeasure)}
      data={units.map((unit) => ({ value: unit, label: unitLabels[unit].short }))}
    />
  );
}
