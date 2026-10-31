import {
  Alert,
  Button,
  Divider,
  Group,
  Modal,
  NumberInput,
  Paper,
  ScrollArea,
  Select,
  SimpleGrid,
  Stack,
  Text,
  TextInput,
  Textarea,
} from '@mantine/core';
import { useDebouncedValue, useDisclosure } from '@mantine/hooks';
import { useForm, type UseFormReturnType } from '@mantine/form';
import { IconPlus, IconTrash } from '@tabler/icons-react';
import { Fragment, useEffect, useState } from 'react';
import { DetailDrawer } from '@/components/page/DetailDrawer';
import { useUiLanguage } from '@/app/localization/UiLanguageContext';
import { useProductsQuery } from '@/features/products/api';
import type { Product } from '@/features/products/types';
import { useCreateCustomerMutation, useCustomersQuery } from '@/features/customers/api';
import { customerTypeOptions, type CreateCustomerRequest, type CustomerType } from '@/features/customers/types';
import { unitLabels } from '@/lib/units';
import { formatCurrency } from '@/lib/format';
import { handleSubmitError, notifySuccess } from '@/lib/forms';
import { useCreateInvoiceMutation } from '../api';
import { calculateInvoiceTotals } from '../totals';
import type { CreateInvoiceRequest, DuplicateInvoiceDraft } from '../types';

interface InvoiceFormDrawerProps {
  opened: boolean;
  onClose: () => void;
  initialDraft?: DuplicateInvoiceDraft | null;
}

interface InvoiceLineFormValues {
  productId: string | null;
  productCode: string;
  priceTier: 'Near' | 'Far';
  description: string;
  unit: string;
  quantity: number | string;
  unitPriceInclusive: number | string;
  nearPrice: number;
  farPrice: number;
}

interface InvoiceFormValues {
  invoiceNumber: string;
  invoiceDate: string;
  customerId: string | null;
  lines: InvoiceLineFormValues[];
}

interface CustomerFormValues {
  name: string;
  address: string;
  phone: string;
  email: string;
  website: string;
  abn: string;
  customerType: CustomerType;
}

const itemGridTemplateColumns = 'minmax(320px, 1fr) 90px 120px 140px 110px 40px';

function stripTrailingCode(name: string) {
  return name.replace(/\s*\([^)]+\)\s*$/, '').trim();
}

function extractTrailingCode(name: string) {
  const match = name.match(/\(([^)]+)\)\s*$/);
  return match?.[1]?.trim() ?? '';
}

function roundCurrency(value: number) {
  return Math.round((value + Number.EPSILON) * 100) / 100;
}

function todayAsLocalDate() {
  const today = new Date();
  const month = String(today.getMonth() + 1).padStart(2, '0');
  const day = String(today.getDate()).padStart(2, '0');
  return `${today.getFullYear()}-${month}-${day}`;
}

function createLine(): InvoiceLineFormValues {
  return {
    productId: null,
    productCode: '',
    priceTier: 'Near',
    description: '',
    unit: '',
    quantity: 1,
    unitPriceInclusive: '',
    nearPrice: 0,
    farPrice: 0,
  };
}

function createInitialValues(): InvoiceFormValues {
  return {
    invoiceNumber: '',
    invoiceDate: todayAsLocalDate(),
    customerId: null,
    lines: [createLine()],
  };
}

export function InvoiceFormDrawer({ opened, onClose, initialDraft }: InvoiceFormDrawerProps) {
  const { t } = useUiLanguage();
  const [customerModalOpened, customerModal] = useDisclosure(false);
  const customersQuery = useCustomersQuery({ page: 1, pageSize: 100, isActive: true });
  const createInvoice = useCreateInvoiceMutation();
  const createCustomer = useCreateCustomerMutation();

  const form = useForm<InvoiceFormValues>({
    mode: 'controlled',
    initialValues: createInitialValues(),
    validate: {
      invoiceNumber: (value) => (value.trim() ? null : t('Invoice number is required')),
      invoiceDate: (value) => (value ? null : t('Invoice date is required')),
      customerId: (value) => (value ? null : t('Customer is required')),
      lines: {
        productId: (value) => (value ? null : t('Item is required')),
        quantity: (value) => (Number(value) > 0 ? null : t('Enter a quantity greater than 0')),
        unitPriceInclusive: (value) =>
          value !== '' && Number(value) >= 0 ? null : t('Enter a price of 0 or more'),
      },
    },
  });

  const customerForm = useForm<CustomerFormValues>({
    mode: 'controlled',
    initialValues: {
      name: '',
      address: '',
      phone: '',
      email: '',
      website: '',
      abn: '',
      customerType: 'Restaurant',
    },
    validate: {
      name: (value) => (value.trim() ? null : t('Customer name is required')),
      address: (value) => (value.trim() ? null : t('Customer address is required')),
      phone: (value) => (value.trim() ? null : t('Customer phone is required')),
      email: (value) =>
        value.trim() && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.trim())
          ? t('Enter a valid email')
          : null,
      website: (value) =>
        value.trim() && !/^(https?:\/\/)?[\w.-]+\.[a-z]{2,}(\/.*)?$/i.test(value.trim())
          ? t('Enter a valid website URL')
          : null,
      abn: (value) =>
        value.trim() && !/^\d{11}$/.test(value.trim()) ? t('ABN must be 11 digits') : null,
    },
  });

  const totals = calculateInvoiceTotals(form.values.lines);
  const customers = customersQuery.data?.items ?? [];
  const selectedCustomer = customers.find((item) => item.id === form.values.customerId);

  useEffect(() => {
    if (!opened) {
      return;
    }

    if (!initialDraft) {
      form.setValues(createInitialValues());
      return;
    }

    form.setValues({
      invoiceNumber: '',
      invoiceDate: todayAsLocalDate(),
      customerId: initialDraft.customerId,
      lines: initialDraft.lines.length > 0
        ? initialDraft.lines.map((line) => ({
            productId: line.productId,
            productCode: '',
            priceTier: line.priceTier,
            description: line.description,
            unit: line.unit,
            quantity: line.quantity,
            unitPriceInclusive: roundCurrency(line.unitPriceInclusive),
            nearPrice: roundCurrency(line.unitPriceInclusive),
            farPrice: roundCurrency(line.unitPriceInclusive),
          }))
        : [createLine()],
    });
  }, [opened, initialDraft]);

  const submit = form.onSubmit(async (values) => {
    if (!values.customerId) {
      form.setFieldError('customerId', t('Customer is required'));
      return;
    }

    const hasMissingProduct = values.lines.some((line) => !line.productId);
    if (hasMissingProduct) {
      form.setFieldError('lines', t('Item is required'));
      return;
    }

    const request: CreateInvoiceRequest = {
      invoiceNumber: values.invoiceNumber.trim(),
      invoiceDate: values.invoiceDate,
      customerId: values.customerId,
      lines: values.lines.map((line) => {
        if (!line.productId) {
          throw new Error('Product is required');
        }

        return {
          productId: line.productId,
          priceTier: line.priceTier,
          description: line.description,
          unit: line.unit,
          quantity: Number(line.quantity),
          unitPriceInclusive: Number(line.unitPriceInclusive),
        };
      }),
    };

    try {
      await createInvoice.mutateAsync(request);
      notifySuccess(t('Invoice saved'));
      form.setValues(createInitialValues());
      onClose();
    } catch (error) {
      handleSubmitError(form, error, { 'Invoice.NumberAlreadyExists': 'invoiceNumber' });
    }
  });

  const submitCustomer = customerForm.onSubmit(async (values) => {
    const request: CreateCustomerRequest = {
      name: values.name.trim(),
      address: values.address.trim(),
      phone: values.phone.trim(),
      email: values.email.trim() || null,
      website: values.website.trim() || null,
      abn: values.abn.trim() || null,
      customerType: values.customerType,
    };

    try {
      const customer = await createCustomer.mutateAsync(request);
      notifySuccess(t('Customer created'));
      form.setFieldValue('customerId', customer.id);
      customerForm.reset();
      customerModal.close();
    } catch (error) {
      handleSubmitError(customerForm, error, { 'Customer.NameAlreadyExists': 'name' });
    }
  });

  return (
    <>
      <DetailDrawer
        opened={opened}
        onClose={onClose}
        title={t('New invoice')}
        subtitle={t('Prices include 10% GST.')}
        drawerSize={1080}
        footer={
          <>
            <Button variant="default" onClick={onClose}>
              {t('Cancel')}
            </Button>
            <Button type="submit" form="invoice-create-form" loading={createInvoice.isPending}>
              {t('Save invoice')}
            </Button>
          </>
        }
      >
        <form id="invoice-create-form" onSubmit={submit} noValidate>
          <Stack gap="lg">
            <SimpleGrid cols={{ base: 1, sm: 2 }}>
              <TextInput
                label={t('Invoice number')}
                required
                {...form.getInputProps('invoiceNumber')}
              />
              <TextInput
                label={t('Invoice date')}
                type="date"
                required
                {...form.getInputProps('invoiceDate')}
              />
            </SimpleGrid>

            <Stack gap="sm">
              <Group justify="space-between" align="flex-end">
                <Select
                  label={t('Customer')}
                  placeholder={t('Search customer')}
                  searchable
                  required
                  w="100%"
                  data={customers.map((customer) => ({
                    value: customer.id,
                    label: customer.phone ? `${customer.name} · ${customer.phone}` : customer.name,
                  }))}
                  value={form.values.customerId}
                  onChange={(value) => form.setFieldValue('customerId', value)}
                  error={form.errors.customerId}
                  nothingFoundMessage={t('No active customers found')}
                />
                <Button
                  type="button"
                  variant="light"
                  leftSection={<IconPlus size={16} />}
                  onClick={customerModal.open}
                >
                  {t('New customer')}
                </Button>
              </Group>
              {form.values.customerId && (
                <Alert color="gray" variant="light">
                  <Stack gap={2}>
                    <Text fw={600}>{selectedCustomer?.name}</Text>
                    <Text size="sm">{selectedCustomer?.address}</Text>
                    <Group gap="xl">
                      <Text size="sm">{t('Phone')}: {selectedCustomer?.phone ?? '—'}</Text>
                      <Text size="sm">{t('ABN')}: {selectedCustomer?.abn ?? '—'}</Text>
                    </Group>
                  </Stack>
                </Alert>
              )}
            </Stack>

            <Stack gap="sm">
              <Group justify="space-between">
                <Text fw={600}>{t('Items')}</Text>
                <Button
                  type="button"
                  size="compact-sm"
                  variant="light"
                  leftSection={<IconPlus size={16} />}
                  onClick={() => form.insertListItem('lines', createLine())}
                >
                  {t('Add item')}
                </Button>
              </Group>
              <Paper withBorder radius="md" p="xs">
                <div
                  style={{
                    display: 'grid',
                    gridTemplateColumns: itemGridTemplateColumns,
                    columnGap: 8,
                    alignItems: 'center',
                    padding: '0 8px 8px 8px',
                  }}
                >
                  <Text size="xs" c="dimmed">
                    {t('Item')}
                  </Text>
                  <Text size="xs" c="dimmed" ta="right">
                    {t('Quantity')}
                  </Text>
                  <Text size="xs" c="dimmed" ta="right">
                    {t('Price tier')}
                  </Text>
                  <Text size="xs" c="dimmed" ta="right">
                    {t('Unit price (incl. GST)')}
                  </Text>
                  <Text size="xs" c="dimmed" ta="right">
                    {t('Amount')}
                  </Text>
                  <Text size="xs" c="dimmed" ta="center">
                    •
                  </Text>
                </div>
                <ScrollArea.Autosize mah={360}>
                  <Stack gap={0}>
                    {form.values.lines.map((line, index) => (
                      <Fragment key={`line-${index}`}>
                        <InvoiceLineEditor form={form} line={line} index={index} />
                        {index < form.values.lines.length - 1 && <Divider />}
                      </Fragment>
                    ))}
                  </Stack>
                </ScrollArea.Autosize>
              </Paper>
            </Stack>

            <Alert color="blue" variant="light">
              <Stack gap={4}>
                <Text size="sm">
                  {t('Subtotal (ex GST)')}: {formatCurrency(totals.subtotal)}
                </Text>
                <Text size="sm">
                  {t('GST included (10%)')}: {formatCurrency(totals.gstAmount)}
                </Text>
                <Text fw={700}>
                  {t('Total (incl. GST)')}: {formatCurrency(totals.total)}
                </Text>
              </Stack>
            </Alert>
          </Stack>
        </form>
      </DetailDrawer>

      <Modal opened={customerModalOpened} onClose={customerModal.close} title={t('New customer')} centered>
        <form onSubmit={submitCustomer} noValidate>
          <Stack gap="sm">
            <TextInput label={t('Customer')} required {...customerForm.getInputProps('name')} />
            <Textarea
              label={t('Customer address')}
              required
              minRows={2}
              autosize
              {...customerForm.getInputProps('address')}
            />
            <TextInput label={t('Phone')} required {...customerForm.getInputProps('phone')} />
            <Select
              label={t('Customer type')}
              allowDeselect={false}
              data={customerTypeOptions.map((type) => ({
                value: type,
                label: type === 'Other' ? t('Other (*)') : t(type),
              }))}
              {...customerForm.getInputProps('customerType')}
            />
            <TextInput label={t('Email')} type="email" {...customerForm.getInputProps('email')} />
            <TextInput
              label={t('Website')}
              placeholder="https://example.com"
              {...customerForm.getInputProps('website')}
            />
            <TextInput label={t('ABN')} placeholder="12345678901" {...customerForm.getInputProps('abn')} />
            <Group justify="flex-end">
              <Button variant="default" onClick={customerModal.close}>
                {t('Cancel')}
              </Button>
              <Button type="submit" loading={createCustomer.isPending}>
                {t('Create customer')}
              </Button>
            </Group>
          </Stack>
        </form>
      </Modal>
    </>
  );
}

function InvoiceLineEditor({
  form,
  line,
  index,
}: {
  form: UseFormReturnType<InvoiceFormValues>;
  line: InvoiceLineFormValues;
  index: number;
}) {
  const { t } = useUiLanguage();
  const [productSearch, setProductSearch] = useState('');
  const [debouncedProductSearch] = useDebouncedValue(productSearch, 250);
  const productsQuery = useProductsQuery(
    {
      page: 1,
      pageSize: 100,
      search: debouncedProductSearch || undefined,
      isActive: true,
      sortBy: 'name',
      sortDir: 'asc',
    },
    { enabled: true },
  );

  const products = productsQuery.data?.items ?? [];
  const productById = new Map(products.map((product) => [product.id, product]));
  const productField = form.getInputProps(`lines.${index}.productId`);
  const lineTotal = Number(line.quantity || 0) * Number(line.unitPriceInclusive || 0);

  const applyProduct = (product: Product | undefined, priceTier: 'Near' | 'Far') => {
    if (!product) {
      return;
    }

    const fallbackCode = extractTrailingCode(product.name);
    const productCode = product.sku?.trim() || fallbackCode;
    const nearPrice = roundCurrency(product.sellPriceNear ?? 0);
    const farPrice = roundCurrency(product.sellPriceFar ?? nearPrice);

    form.setFieldValue(`lines.${index}.description`, product.name);
    form.setFieldValue(`lines.${index}.productCode`, productCode);
    form.setFieldValue(`lines.${index}.unit`, unitLabels[product.unit].long);
    form.setFieldValue(`lines.${index}.nearPrice`, nearPrice);
    form.setFieldValue(`lines.${index}.farPrice`, farPrice);
    form.setFieldValue(
      `lines.${index}.unitPriceInclusive`,
      roundCurrency(priceTier === 'Near' ? nearPrice : farPrice),
    );
  };

  return (
    <div style={{ padding: '8px' }}>
      <Stack gap={4}>
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: itemGridTemplateColumns,
            columnGap: 8,
            alignItems: 'start',
          }}
        >
          <Select
            aria-label={t('Item')}
            placeholder={t('Search active products')}
            searchable
            required
            style={{ minWidth: 0 }}
            comboboxProps={{
              width: 760,
              position: 'bottom-start',
              middlewares: { flip: false, shift: false },
            }}
            filter={({ options }) => options}
            renderOption={({ option }) => {
              const product = productById.get(option.value);
              const code = product?.sku?.trim() || extractTrailingCode(product?.name ?? '');
              return (
                <Group gap="sm" wrap="nowrap" align="flex-start">
                  {code && (
                    <Text size="xs" c="dimmed" fw={700}>
                      {code}
                    </Text>
                  )}
                  <Text size="sm" style={{ whiteSpace: 'normal', lineHeight: 1.25 }}>
                    {option.label}
                  </Text>
                </Group>
              );
            }}
            data={products.map((product) => ({
              value: product.id,
              label: (() => {
                const cleanName = stripTrailingCode(product.name);
                return product.description ? `${cleanName} — ${product.description}` : cleanName;
              })(),
            }))}
            value={line.productId}
            onChange={(value) => {
              form.setFieldValue(`lines.${index}.productId`, value);
              const product = products.find((item) => item.id === value);
              applyProduct(product, line.priceTier);
            }}
            searchValue={productSearch}
            onSearchChange={setProductSearch}
            error={productField.error}
            nothingFoundMessage={t('No active products found')}
          />
          <NumberInput
            aria-label={t('Quantity')}
            min={0.001}
            decimalScale={3}
            required
            w="100%"
            {...form.getInputProps(`lines.${index}.quantity`)}
          />
          <Select
            aria-label={t('Price tier')}
            allowDeselect={false}
            w="100%"
            data={[
              { value: 'Near', label: t('Near') },
              { value: 'Far', label: t('Far') },
            ]}
            value={line.priceTier}
            onChange={(value) => {
              const priceTier = (value as 'Near' | 'Far') ?? 'Near';
              form.setFieldValue(`lines.${index}.priceTier`, priceTier);
              form.setFieldValue(
                `lines.${index}.unitPriceInclusive`,
                roundCurrency(priceTier === 'Near' ? line.nearPrice : line.farPrice),
              );
            }}
          />
          <NumberInput
            aria-label={t('Unit price (incl. GST)')}
            min={0}
            decimalScale={2}
            fixedDecimalScale
            required
            w="100%"
            {...form.getInputProps(`lines.${index}.unitPriceInclusive`)}
          />
          <Text size="sm" fw={600} ta="right" mt={9}>
            {formatCurrency(lineTotal)}
          </Text>
          <Button
            type="button"
            variant="subtle"
            color="red"
            aria-label={t('Remove item')}
            disabled={form.values.lines.length === 1}
            px={0}
            onClick={() => form.removeListItem('lines', index)}
          >
            <IconTrash size={18} />
          </Button>
        </div>
        {line.productId && (
          <Text size="xs" c="dimmed" px="xs">
            {t('Code')}: {line.productCode || '—'} ·{' '}
            <span style={{ color: 'var(--mantine-color-text)' }}>
              {t('Unit')}: {line.unit || '—'}
            </span>
          </Text>
        )}
      </Stack>
    </div>
  );
}
