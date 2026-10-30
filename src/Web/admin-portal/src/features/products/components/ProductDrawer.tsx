import { Alert, Badge, Button, Divider, Group, Skeleton, Stack, Text, Title } from '@mantine/core';
import { IconInfoCircle } from '@tabler/icons-react';
import { ActiveBadge } from '@/components/data/StatusBadge';
import { DetailDrawer } from '@/components/page/DetailDrawer';
import { useAllCategoriesQuery } from '@/features/categories/api';
import type { Category } from '@/features/categories/types';
import { formatCurrency, formatDate, formatDateTime } from '@/lib/format';
import { notifySuccess } from '@/lib/forms';
import {
  useCreateProductMutation,
  useProductChangeHistoryQuery,
  useProductQuery,
  useUpdateProductMutation,
} from '../api';
import type { Product } from '../types';
import { useUiLanguage } from '@/app/localization/UiLanguageContext';
import { useAuth } from '@/app/auth/AuthContext';
import { ProductForm, type ProductFormValues } from './ProductForm';
import { ProductStockSection } from './ProductStockSection';

const FORM_ID = 'product-form';

const emptyProductForm = (categoryId?: string): ProductFormValues => ({
  sku: '',
  legacyCode: '',
  name: '',
  description: '',
  categoryId: categoryId ?? null,
  unit: 'Carton',
  packDetails: '',
  sellPriceNear: '',
  sellPriceFar: '',
  buyPrice: '',
  priceEffectiveDate: '',
  supplierNames: [],
  defaultSupplierName: null,
  isActive: true,
});

interface ProductDrawerProps {
  productId?: string;
  creating: boolean;
  defaultCategoryId?: string;
  onClose: () => void;
  onCreated: (product: Product) => void;
}

export function ProductDrawer({
  productId,
  creating,
  defaultCategoryId,
  onClose,
  onCreated,
}: ProductDrawerProps) {
  const { t } = useUiLanguage();
  const { user } = useAuth();
  const isAdmin = user?.role === 'Admin';
  const opened = creating || !!productId;
  const product = useProductQuery(creating ? undefined : productId);
  const changeHistory = useProductChangeHistoryQuery(creating ? undefined : productId, 100);
  const categories = useAllCategoriesQuery();
  const categoryList = categories.data?.items ?? [];
  const historyFieldLabels: Record<string, string> = {
    Name: t('Product name'),
    CategoryId: t('Category'),
    IsActive: t('Active'),
    SellPriceNear: t('Sell price (Near)'),
    SellPriceFar: t('Sell price (Far)'),
    BuyPrice: t('Buy price'),
    PriceEffectiveDate: t('Price effective date'),
    Suppliers: t('Suppliers'),
    DefaultSupplier: t('Default supplier'),
  };

  if (creating && isAdmin) {
    return (
      <DetailDrawer
        opened={opened}
        onClose={onClose}
        drawerSize={1080}
        title={t('New product')}
        subtitle={t('Add an item to the catalog')}
        footer={<FormFooter onCancel={onClose} label={t('Create product')} />}
      >
        <CreateProduct
          categories={categoryList}
          defaultCategoryId={defaultCategoryId}
          onCreated={onCreated}
        />
      </DetailDrawer>
    );
  }

  const data = product.data;
  const supplierNameById = new Map(data?.suppliers.map((supplier) => [supplier.id, supplier.name]) ?? []);
  const categoryNameById = new Map(categoryList.map((category) => [category.id, category.name]));
  const isGuidLike = (value: string) =>
    /^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value);
  const isOpaqueActorId = (value: string) => isGuidLike(value) || (!value.includes('@') && value.length > 24);

  const formatHistoryValue = (fieldName: string, value: string | null) => {
    if (!value) {
      return '(empty)';
    }

    if (fieldName === 'Suppliers') {
      return value
        .split(',')
        .map((part) => part.trim())
        .filter((part) => part.length > 0)
        .map((part) => supplierNameById.get(part) ?? (isGuidLike(part) ? t('Unknown supplier') : part))
        .join(', ');
    }

    if (fieldName === 'DefaultSupplier') {
      return supplierNameById.get(value) ?? (isGuidLike(value) ? t('Unknown supplier') : value);
    }

    if (fieldName === 'CategoryId') {
      return categoryNameById.get(value) ?? (isGuidLike(value) ? t('Unknown category') : value);
    }

    if (fieldName === 'PriceEffectiveDate') {
      return formatDate(value);
    }

    return value;
  };

  const formatChangedBy = (changedBy: string) =>
    isOpaqueActorId(changedBy) ? (user?.name ?? t('Unknown user')) : changedBy;

  return (
    <DetailDrawer
      opened={opened}
      onClose={onClose}
      drawerSize={1080}
      title={data?.name ?? t('Product')}
      subtitle={data && `${data.sku} · ${data.categoryName}`}
      badge={data && <ActiveBadge isActive={data.isActive} />}
      footer={data && isAdmin && <FormFooter onCancel={onClose} label={t('Save changes')} />}
    >
      {product.isPending && <Skeleton height={320} radius="md" />}
      {product.isError && (
        <Alert color="red" title={t("Couldn't load product")}>
          {product.error.message}
        </Alert>
      )}
      {data && (
        <Stack gap="lg">
          <ProductStockSection product={data} />
          {isAdmin ? (
            <>
              <Divider label={t('Details')} labelPosition="left" />
              <EditProduct key={data.id} product={data} categories={categoryList} />
            </>
          ) : (
            <Text>{data.description}</Text>
          )}
          <Divider label={t('Supplier intelligence')} labelPosition="left" />
          {data.suppliers.length === 0 ? (
            <Alert variant="light" color="gray" icon={<IconInfoCircle />}>
              <Text size="sm">{t('No suppliers linked to this product yet.')}</Text>
            </Alert>
          ) : (
            <Stack gap="xs">
              {data.suppliers.map((supplier) => (
                <Group key={supplier.id} justify="space-between" align="flex-start">
                  <Group gap="xs">
                    <Text size="sm" fw={500}>
                      {supplier.name}
                    </Text>
                    {supplier.isDefault && <Badge size="xs">{t('Default')}</Badge>}
                  </Group>
                  <Text size="sm" c="dimmed">
                    {supplier.lastBuyPrice == null
                      ? t('No buy history')
                      : `${t('Last buy')}: ${formatCurrency(supplier.lastBuyPrice)}${supplier.lastBuyPriceAtUtc ? ` · ${formatDate(supplier.lastBuyPriceAtUtc)}` : ''}`}
                  </Text>
                </Group>
              ))}
            </Stack>
          )}

          <Divider label={t('Product change history')} labelPosition="left" />
          {changeHistory.isPending ? (
            <Skeleton height={120} radius="md" />
          ) : changeHistory.isError ? (
            <Alert color="red">{changeHistory.error.message}</Alert>
          ) : (changeHistory.data?.length ?? 0) === 0 ? (
            <Alert variant="light" color="gray" icon={<IconInfoCircle />}>
              <Text size="sm">{t('No changes have been recorded yet.')}</Text>
            </Alert>
          ) : (
            <Stack gap="xs">
              {changeHistory.data?.map((item) => (
                <Stack key={item.id} gap={2}>
                  <Text size="sm" fw={500}>
                    {historyFieldLabels[item.fieldName] ?? item.fieldName}
                  </Text>
                  <Text size="xs" c="dimmed">
                    {formatHistoryValue(item.fieldName, item.oldValue)} {'->'}{' '}
                    {formatHistoryValue(item.fieldName, item.newValue)}
                  </Text>
                  <Text size="xs" c="dimmed">
                    {formatChangedBy(item.changedBy)} · {formatDateTime(item.changedAtUtc)}
                  </Text>
                </Stack>
              ))}
            </Stack>
          )}
        </Stack>
      )}
    </DetailDrawer>
  );
}

function FormFooter({ onCancel, label }: { onCancel: () => void; label: string }) {
  const { t } = useUiLanguage();
  return (
    <>
      <Button variant="default" onClick={onCancel}>
        {t('Cancel')}
      </Button>
      <Button type="submit" form={FORM_ID}>
        {label}
      </Button>
    </>
  );
}

function CreateProduct({
  categories,
  defaultCategoryId,
  onCreated,
}: {
  categories: Category[];
  defaultCategoryId?: string;
  onCreated: (product: Product) => void;
}) {
  const { t } = useUiLanguage();
  const createProduct = useCreateProductMutation();

  const handleSubmit = async (values: ProductFormValues) => {
    const created = await createProduct.mutateAsync({
      sku: values.sku.trim(),
      legacyCode: values.legacyCode.trim() || null,
      name: values.name.trim(),
      description: values.description.trim() || null,
      categoryId: values.categoryId ?? '',
      unit: values.unit,
      packDetails: values.packDetails.trim() || null,
      sellPriceNear: values.sellPriceNear === '' ? null : Number(values.sellPriceNear),
      sellPriceFar: values.sellPriceFar === '' ? null : Number(values.sellPriceFar),
      buyPrice: values.buyPrice === '' ? null : Number(values.buyPrice),
      priceEffectiveDate: values.priceEffectiveDate.trim() || null,
      supplierNames: values.supplierNames,
      defaultSupplierName: values.defaultSupplierName,
    });
    notifySuccess(`${created.name} ${t('was added to the catalog')}`, t('Product created'));
    onCreated(created);
  };

  return (
    <Stack gap="md">
      <Title order={5}>{t('Product details')}</Title>
      <ProductForm
        id={FORM_ID}
        mode="create"
        initialValues={emptyProductForm(defaultCategoryId)}
        categories={categories}
        onSubmit={handleSubmit}
      />
    </Stack>
  );
}

function EditProduct({ product, categories }: { product: Product; categories: Category[] }) {
  const { t } = useUiLanguage();
  const updateProduct = useUpdateProductMutation(product.id);

  const handleSubmit = async (values: ProductFormValues) => {
    const updated = await updateProduct.mutateAsync({
      name: values.name.trim(),
      description: values.description.trim() || null,
      legacyCode: values.legacyCode.trim() || null,
      categoryId: values.categoryId ?? '',
      unit: values.unit,
      packDetails: values.packDetails.trim() || null,
      sellPriceNear: values.sellPriceNear === '' ? null : Number(values.sellPriceNear),
      sellPriceFar: values.sellPriceFar === '' ? null : Number(values.sellPriceFar),
      buyPrice: values.buyPrice === '' ? null : Number(values.buyPrice),
      priceEffectiveDate: values.priceEffectiveDate.trim() || null,
      supplierNames: values.supplierNames,
      defaultSupplierName: values.defaultSupplierName,
      isActive: values.isActive,
    });
    notifySuccess(`${updated.name} ${t('was updated')}`);
  };

  return (
    <ProductForm
      id={FORM_ID}
      mode="edit"
      categories={categories}
      initialValues={{
        sku: product.sku,
        legacyCode: product.legacyCode ?? '',
        name: product.name,
        description: product.description ?? '',
        categoryId: product.categoryId,
        unit: product.unit,
        packDetails: product.packDetails ?? '',
        sellPriceNear: product.sellPriceNear ?? '',
        sellPriceFar: product.sellPriceFar ?? '',
        buyPrice: product.buyPrice ?? '',
        priceEffectiveDate: toInputDate(product.priceEffectiveDate),
        supplierNames: product.suppliers.map((supplier) => supplier.name),
        defaultSupplierName: product.suppliers.find((supplier) => supplier.isDefault)?.name ?? null,
        isActive: product.isActive,
      }}
      onSubmit={handleSubmit}
    />
  );
}

function toInputDate(value: string | null): string {
  if (!value) {
    return '';
  }

  const isoDatePrefixMatch = value.match(/^(\d{4}-\d{2}-\d{2})/);
  if (isoDatePrefixMatch?.[1]) {
    return isoDatePrefixMatch[1];
  }

  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return '';
  }

  const year = parsed.getFullYear();
  const month = String(parsed.getMonth() + 1).padStart(2, '0');
  const day = String(parsed.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}
