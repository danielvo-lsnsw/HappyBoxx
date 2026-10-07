import { NumberInput, Select, SimpleGrid, Stack, Switch, Textarea, TextInput } from '@mantine/core';
import { useForm } from '@mantine/form';
import type { Category } from '@/features/categories/types';
import { currencySymbol } from '@/lib/format';
import { handleSubmitError } from '@/lib/forms';
import { UNITS_OF_MEASURE, unitLabels, type UnitOfMeasure } from '@/lib/units';

export interface ProductFormValues {
  sku: string;
  name: string;
  description: string;
  categoryId: string | null;
  unit: UnitOfMeasure;
  price: number | string;
  isActive: boolean;
}

interface ProductFormProps {
  id: string;
  mode: 'create' | 'edit';
  initialValues: ProductFormValues;
  categories: Category[];
  onSubmit: (values: ProductFormValues) => Promise<unknown>;
}

const SKU_PATTERN = /^[A-Za-z0-9-]+$/;

export function ProductForm({ id, mode, initialValues, categories, onSubmit }: ProductFormProps) {
  const form = useForm<ProductFormValues>({
    mode: 'controlled',
    initialValues,
    validate: {
      sku: (value) =>
        mode === 'edit'
          ? null
          : !value.trim()
            ? 'SKU is required'
            : !SKU_PATTERN.test(value.trim())
              ? 'Letters, digits and dashes only'
              : null,
      name: (value) => (value.trim() ? null : 'Name is required'),
      categoryId: (value) => (value ? null : 'Choose a category'),
      price: (value) => (value === '' || Number(value) < 0 ? 'Enter a price of 0 or more' : null),
    },
  });

  const submit = form.onSubmit(async (values) => {
    try {
      await onSubmit(values);
    } catch (error) {
      handleSubmitError(form, error, {
        'Product.SkuAlreadyExists': 'sku',
        'Product.CategoryNotFound': 'categoryId',
      });
    }
  });

  return (
    <form id={id} onSubmit={submit} noValidate>
      <Stack gap="md">
        <SimpleGrid cols={{ base: 1, xs: 2 }}>
          <TextInput
            label="SKU"
            placeholder="e.g. TOM-001"
            required={mode === 'create'}
            disabled={mode === 'edit'}
            description={mode === 'edit' ? 'SKU cannot be changed' : undefined}
            {...form.getInputProps('sku')}
          />
          <Select
            label="Category"
            placeholder="Select category"
            required
            searchable
            data={categories.map((c) => ({ value: c.id, label: c.name }))}
            {...form.getInputProps('categoryId')}
          />
        </SimpleGrid>
        <TextInput
          label="Product name"
          placeholder="e.g. Roma tomatoes"
          required
          {...form.getInputProps('name')}
        />
        <Textarea
          label="Description"
          placeholder="Variety, grade, packaging notes…"
          autosize
          minRows={2}
          maxRows={5}
          {...form.getInputProps('description')}
        />
        <SimpleGrid cols={{ base: 1, xs: 2 }}>
          <Select
            label="Unit of measure"
            required
            allowDeselect={false}
            data={UNITS_OF_MEASURE.map((u) => ({ value: u, label: unitLabels[u].long }))}
            {...form.getInputProps('unit')}
          />
          <NumberInput
            label={`Price per ${unitLabels[form.values.unit].short}`}
            required
            min={0}
            decimalScale={2}
            fixedDecimalScale
            prefix={currencySymbol()}
            thousandSeparator=","
            {...form.getInputProps('price')}
          />
        </SimpleGrid>
        {mode === 'edit' && (
          <Switch
            size="md"
            label="Active"
            description="Inactive products are hidden from buyers"
            {...form.getInputProps('isActive', { type: 'checkbox' })}
          />
        )}
      </Stack>
    </form>
  );
}
