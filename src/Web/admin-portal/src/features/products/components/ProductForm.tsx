import {
  NumberInput,
  Select,
  SimpleGrid,
  Stack,
  Switch,
  TagsInput,
  Textarea,
  TextInput,
} from '@mantine/core';
import { useForm } from '@mantine/form';
import type { Category } from '@/features/categories/types';
import { currencySymbol } from '@/lib/format';
import { handleSubmitError } from '@/lib/forms';
import { UNITS_OF_MEASURE, unitLabels, type UnitOfMeasure } from '@/lib/units';
import { useUiLanguage } from '@/app/localization/UiLanguageContext';

export interface ProductFormValues {
  sku: string;
  legacyCode: string;
  name: string;
  description: string;
  categoryId: string | null;
  unit: UnitOfMeasure;
  packDetails: string;
  sellPriceNear: number | string;
  sellPriceFar: number | string;
  buyPrice: number | string;
  priceEffectiveDate: string;
  supplierNames: string[];
  defaultSupplierName: string | null;
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
  const { t } = useUiLanguage();
  const form = useForm<ProductFormValues>({
    mode: 'controlled',
    initialValues,
    validate: {
      sku: (value) =>
        mode === 'edit'
          ? null
          : !value.trim()
            ? t('SKU is required')
            : !SKU_PATTERN.test(value.trim())
              ? t('Letters, digits and dashes only')
              : null,
      name: (value) => (value.trim() ? null : t('Name is required')),
      categoryId: (value) => (value ? null : t('Choose a category')),
      sellPriceNear: (value) =>
        value !== '' && Number(value) < 0 ? t('Enter a price of 0 or more') : null,
      sellPriceFar: (value) =>
        value !== '' && Number(value) < 0 ? t('Enter a price of 0 or more') : null,
      buyPrice: (value) =>
        value !== '' && Number(value) < 0 ? t('Enter a price of 0 or more') : null,
      defaultSupplierName: (value, values) =>
        value && !values.supplierNames.includes(value)
          ? t('Default supplier must be in suppliers list')
          : null,
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
            label={t('SKU')}
            placeholder={t('e.g. TOM-001')}
            required={mode === 'create'}
            disabled={mode === 'edit'}
            // description={mode === 'edit' ? t('SKU cannot be changed') : undefined}
            {...form.getInputProps('sku')}
          />
          <TextInput
            label={t('Legacy code')}
            placeholder={t('Spreadsheet code')}
            {...form.getInputProps('legacyCode')}
          />
        </SimpleGrid>
        <SimpleGrid cols={{ base: 1, xs: 2 }}>
          <Select
            label={t('Category')}
            placeholder={t('Select category')}
            required
            searchable
            data={categories.map((c) => ({ value: c.id, label: c.name }))}
            {...form.getInputProps('categoryId')}
          />
          <TextInput
            label={t('Pack details')}
            placeholder={t('e.g. 50 pcs x 10 slv')}
            {...form.getInputProps('packDetails')}
          />
        </SimpleGrid>
        <TextInput
          label={t('Product name')}
          placeholder={t('e.g. Roma tomatoes')}
          required
          {...form.getInputProps('name')}
        />
        <Textarea
          label={t('Description')}
          placeholder={t('Variety, grade, packaging notes…')}
          autosize
          minRows={2}
          maxRows={5}
          {...form.getInputProps('description')}
        />
        <SimpleGrid cols={{ base: 1, xs: 2 }}>
          <Stack gap="md">
            <Select
              label={t('Unit of measure')}
              required
              allowDeselect={false}
              data={UNITS_OF_MEASURE.map((u) => ({ value: u, label: t(unitLabels[u].long) }))}
              {...form.getInputProps('unit')}
            />
            <TextInput
              label={t('Price effective date')}
              type="date"
              {...form.getInputProps('priceEffectiveDate')}
            />
          </Stack>
          <Stack gap="md">
            <NumberInput
              label={t('Sell price (Near)')}
              min={0}
              decimalScale={3}
              fixedDecimalScale
              prefix={currencySymbol()}
              thousandSeparator=","
              {...form.getInputProps('sellPriceNear')}
            />
            <NumberInput
              label={t('Sell price (Far)')}
              min={0}
              decimalScale={3}
              fixedDecimalScale
              prefix={currencySymbol()}
              thousandSeparator=","
              {...form.getInputProps('sellPriceFar')}
            />
            <NumberInput
              label={t('Buy price')}
              min={0}
              decimalScale={3}
              fixedDecimalScale
              prefix={currencySymbol()}
              thousandSeparator=","
              {...form.getInputProps('buyPrice')}
            />
          </Stack>
        </SimpleGrid>
        <SimpleGrid cols={{ base: 1, xs: 2 }}>
          <TagsInput
            label={t('Suppliers')}
            placeholder={t('Add suppliers')}
            value={form.values.supplierNames}
            onChange={(value) => {
              const defaultSupplierName = value.includes(form.values.defaultSupplierName ?? '')
                ? form.values.defaultSupplierName
                : (value[0] ?? null);
              form.setFieldValue('supplierNames', value);
              form.setFieldValue('defaultSupplierName', defaultSupplierName);
            }}
          />
          <Select
            label={t('Default supplier')}
            placeholder={t('Select default supplier')}
            data={form.values.supplierNames.map((s) => ({ value: s, label: s }))}
            clearable
            {...form.getInputProps('defaultSupplierName')}
          />
        </SimpleGrid>
        {mode === 'edit' && (
          <Switch
            size="md"
            label={t('Active')}
            description={t('Inactive products are hidden from buyers')}
            {...form.getInputProps('isActive', { type: 'checkbox' })}
          />
        )}
      </Stack>
    </form>
  );
}
