import {
  Alert,
  Badge,
  Button,
  Checkbox,
  FileInput,
  Group,
  Modal,
  NumberInput,
  SegmentedControl,
  Select,
  Stack,
  Text,
  TextInput,
  Tooltip,
  UnstyledButton,
} from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';
import {
  IconChevronDown,
  IconChevronUp,
  IconDownload,
  IconPackageOff,
  IconPlus,
  IconSearch,
  IconSelector,
} from '@tabler/icons-react';
import { DataTable, type DataTableColumn } from '@/components/data/DataTable';
import { EmptyState } from '@/components/data/EmptyState';
import { QueryState } from '@/components/data/QueryState';
import { ActiveBadge } from '@/components/data/StatusBadge';
import { TablePagination } from '@/components/data/TablePagination';
import { DataPanel } from '@/components/page/DataPanel';
import { FilterBar } from '@/components/page/FilterBar';
import { PageHeader } from '@/components/page/PageHeader';
import { useAllCategoriesQuery } from '@/features/categories/api';
import { downloadCsv, toCsv } from '@/lib/csv';
import { formatCurrency } from '@/lib/format';
import { UNITS_OF_MEASURE, unitLabels } from '@/lib/units';
import { useUrlParams } from '@/lib/useUrlParams';
import {
  useProductsQuery,
  useBulkAdjustPricesMutation,
  useBulkUpdateProductsMutation,
  useCsvUpsertProductsMutation,
} from '../api';
import { ProductDrawer } from '../components/ProductDrawer';
import type { Product, UnitOfMeasure, UpsertProductsFromCsvResponse } from '../types';
import { useUiLanguage } from '@/app/localization/UiLanguageContext';
import { useAuth } from '@/app/auth/AuthContext';
import { notifyError, notifySuccess } from '@/lib/forms';
import { useState } from 'react';

type ProductSortBy = 'sku' | 'name' | 'category' | 'unit' | 'near' | 'far' | 'status';
type ProductSortDir = 'asc' | 'desc';

const DEFAULT_SORT_BY: ProductSortBy = 'sku';
const DEFAULT_SORT_DIR: ProductSortDir = 'asc';

export function ProductsPage() {
  const { t } = useUiLanguage();
  const { user } = useAuth();
  const isAdmin = user?.role === 'Admin';
  const url = useUrlParams();
  const page = url.getNumber('page', 1);
  const pageSize = url.getNumber('pageSize', 20);
  const search = url.get('search') ?? '';
  const status = url.get('status') ?? 'all';
  const sortBy = (url.get('sortBy') as ProductSortBy | undefined) ?? DEFAULT_SORT_BY;
  const sortDir = (url.get('sortDir') as ProductSortDir | undefined) ?? DEFAULT_SORT_DIR;
  const categoryName = url.get('category');
  const productId = url.get('productId');
  const creating = url.get('create') === '1';
  const [debouncedSearch] = useDebouncedValue(search.trim(), 300);

  const categories = useAllCategoriesQuery();
  const categoryFromName = categoryName
    ? categories.data?.items.find((c) => c.name.toLowerCase() === categoryName.toLowerCase())
    : undefined;
  const categoryId = url.get('categoryId') ?? categoryFromName?.id;
  const resolvingCategory = !!categoryName && !url.get('categoryId') && categories.isPending;

  const products = useProductsQuery(
    {
      page,
      pageSize,
      search: debouncedSearch || undefined,
      categoryId,
      isActive: status === 'all' ? undefined : status === 'active',
      sortBy,
      sortDir,
    },
    { enabled: !resolvingCategory },
  );
  const bulkUpdate = useBulkUpdateProductsMutation();
  const bulkAdjustPrices = useBulkAdjustPricesMutation();
  const csvUpsert = useCsvUpsertProductsMutation();

  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [bulkEditOpened, setBulkEditOpened] = useState(false);
  const [bulkPriceOpened, setBulkPriceOpened] = useState(false);
  const [csvImportOpened, setCsvImportOpened] = useState(false);

  const [bulkCategoryId, setBulkCategoryId] = useState<string | null>(null);
  const [bulkUnit, setBulkUnit] = useState<UnitOfMeasure | null>(null);
  const [bulkSupplierNames, setBulkSupplierNames] = useState<string[]>([]);
  const [bulkDefaultSupplierName, setBulkDefaultSupplierName] = useState<string | null>(null);
  const [bulkStatus, setBulkStatus] = useState<'active' | 'inactive' | null>(null);

  const [priceMode, setPriceMode] = useState<'Percentage' | 'Absolute'>('Percentage');
  const [nearChange, setNearChange] = useState<number | ''>('');
  const [farChange, setFarChange] = useState<number | ''>('');

  const [csvFile, setCsvFile] = useState<File | null>(null);
  const [csvResult, setCsvResult] = useState<UpsertProductsFromCsvResponse | null>(null);

  const hasActiveFilters = !!search || !!categoryId || status !== 'all';
  const title = categoryFromName?.name ?? t('Products');
  const supplierOptions = Array.from(
    new Set(
      (products.data?.items ?? []).flatMap((p) => p.suppliers.map((s) => s.name)).filter((x) => x),
    ),
  );
  const pageIds = products.data?.items.map((p) => p.id) ?? [];
  const selectedOnPage = pageIds.filter((id) => selectedIds.includes(id)).length;
  const allPageSelected = pageIds.length > 0 && selectedOnPage === pageIds.length;

  const toggleSelection = (id: string, checked: boolean) => {
    setSelectedIds((current) =>
      checked
        ? current.includes(id)
          ? current
          : [...current, id]
        : current.filter((x) => x !== id),
    );
  };

  const togglePageSelection = (checked: boolean) => {
    setSelectedIds((current) => {
      if (checked) {
        return Array.from(new Set([...current, ...pageIds]));
      }

      const pageSet = new Set(pageIds);
      return current.filter((id) => !pageSet.has(id));
    });
  };

  const applyBulkUpdate = async () => {
    if (selectedIds.length === 0) {
      return;
    }
    const request = {
      productIds: selectedIds,
      isActive: bulkStatus === null ? undefined : bulkStatus === 'active',
      categoryId: bulkCategoryId ?? undefined,
      unit: bulkUnit ?? undefined,
      supplierNames: bulkSupplierNames.length > 0 ? bulkSupplierNames : undefined,
      defaultSupplierName: bulkDefaultSupplierName,
    };
    if (
      request.isActive == null &&
      !request.categoryId &&
      !request.unit &&
      !request.supplierNames &&
      request.defaultSupplierName == null
    ) {
      notifyError(new Error('Select at least one field to update.'));
      return;
    }

    try {
      const result = await bulkUpdate.mutateAsync(request);
      notifySuccess(`Updated ${result.updatedCount} products.`, 'Bulk update complete');
      setSelectedIds([]);
      setBulkEditOpened(false);
    } catch (error) {
      notifyError(error);
    }
  };

  const applyPriceAdjust = async () => {
    if (selectedIds.length === 0) {
      return;
    }
    const request = {
      productIds: selectedIds,
      mode: priceMode,
      nearChange: nearChange === '' ? null : nearChange,
      farChange: farChange === '' ? null : farChange,
    };
    if (request.nearChange == null && request.farChange == null) {
      notifyError(new Error('Enter at least one price adjustment.'));
      return;
    }

    try {
      const result = await bulkAdjustPrices.mutateAsync(request);
      notifySuccess(`Adjusted ${result.updatedCount} products.`, 'Price adjustment complete');
      setSelectedIds([]);
      setBulkPriceOpened(false);
    } catch (error) {
      notifyError(error);
    }
  };

  const runCsvImport = async () => {
    if (!csvFile) {
      notifyError(new Error('Choose a CSV file first.'));
      return;
    }

    try {
      const result = await csvUpsert.mutateAsync(csvFile);
      setCsvResult(result);
      notifySuccess(
        `Created ${result.createdCount}, updated ${result.updatedCount}, errors ${result.errorCount}.`,
        'CSV import complete',
      );
      if (result.errorCount === 0) {
        setCsvImportOpened(false);
        setCsvFile(null);
        setCsvResult(null);
      }
    } catch (error) {
      notifyError(error);
    }
  };

  const downloadImportTemplate = () => {
    window.open('/templates/products-upsert-template.csv', '_blank', 'noopener,noreferrer');
  };

  const exportCsv = () => {
    const rows = products.data?.items ?? [];
    downloadCsv(
      'products.csv',
      toCsv(rows, [
        { header: 'SKU', value: (p) => p.sku },
        { header: 'Name', value: (p) => p.name },
        { header: 'Category', value: (p) => p.categoryName },
        { header: 'Unit', value: (p) => p.unit },
        { header: 'SellNear', value: (p) => p.sellPriceNear ?? '' },
        { header: 'SellFar', value: (p) => p.sellPriceFar ?? '' },
        { header: 'Buy', value: (p) => p.buyPrice ?? '' },
        {
          header: 'DefaultSupplier',
          value: (p) => p.suppliers.find((s) => s.isDefault)?.name ?? '',
        },
        { header: 'Active', value: (p) => p.isActive },
      ]),
    );
  };

  const newProduct = () => url.set({ create: 1, productId: undefined }, { push: true });

  const toggleSort = (field: ProductSortBy) => {
    const nextDir: ProductSortDir = sortBy !== field ? 'asc' : sortDir === 'asc' ? 'desc' : 'asc';
    url.set({ sortBy: field, sortDir: nextDir, page: undefined });
  };

  const renderSortHeader = (field: ProductSortBy, label: string) => {
    const isCurrent = sortBy === field;
    const Icon = !isCurrent ? IconSelector : sortDir === 'asc' ? IconChevronUp : IconChevronDown;
    return (
      <UnstyledButton onClick={() => toggleSort(field)} aria-label={`${label} ${t('sort')}`}>
        <Group gap={6} wrap="nowrap">
          <span>{label}</span>
          <Icon size={14} />
        </Group>
      </UnstyledButton>
    );
  };

  const columns: DataTableColumn<Product>[] = [
    {
      key: 'select',
      width: 48,
      header: (
        <Checkbox
          aria-label={t('Select page')}
          checked={allPageSelected}
          indeterminate={selectedOnPage > 0 && !allPageSelected}
          onChange={(event) => togglePageSelection(event.currentTarget.checked)}
        />
      ),
      render: (p) => (
        <Checkbox
          aria-label={t('Select row')}
          checked={selectedIds.includes(p.id)}
          onChange={(event) => toggleSelection(p.id, event.currentTarget.checked)}
          onClick={(event) => event.stopPropagation()}
        />
      ),
    },
    {
      key: 'sku',
      header: renderSortHeader('sku', t('SKU')),
      width: 130,
      render: (p) => (
        <Text ff="monospace" fw={600} size="sm">
          {p.sku}
        </Text>
      ),
    },
    {
      key: 'name',
      header: renderSortHeader('name', t('Product')),
      render: (p) => (
        <Stack gap={0}>
          <Text fw={500} size="sm">
            {p.name}
          </Text>
          {p.description && (
            <Text size="xs" c="dimmed" lineClamp={1}>
              {p.description}
            </Text>
          )}
        </Stack>
      ),
    },
    {
      key: 'category',
      header: renderSortHeader('category', t('Category')),
      render: (p) => p.categoryName,
    },
    {
      key: 'unit',
      header: renderSortHeader('unit', t('Unit')),
      width: 110,
      render: (p) => unitLabels[p.unit].long,
    },
    {
      key: 'price',
      header: renderSortHeader('near', t('Near')),
      align: 'right',
      width: 120,
      render: (p) => (
        <span className="tabular">
          {p.sellPriceNear == null ? '—' : formatCurrency(p.sellPriceNear)}
        </span>
      ),
    },
    {
      key: 'farPrice',
      header: renderSortHeader('far', t('Far')),
      align: 'right',
      width: 120,
      render: (p) => (
        <span className="tabular">
          {p.sellPriceFar == null ? '—' : formatCurrency(p.sellPriceFar)}
        </span>
      ),
    },
    {
      key: 'status',
      header: renderSortHeader('status', t('Status')),
      width: 120,
      render: (p) => <ActiveBadge isActive={p.isActive} />,
    },
  ];

  return (
    <>
      <PageHeader
        title={title}
        description={t('Manage the catalog: pricing, units and availability.')}
        actions={
          <>
            {isAdmin && (
              <Button
                variant="default"
                onClick={() => {
                  setCsvResult(null);
                  setCsvFile(null);
                  setCsvImportOpened(true);
                }}
              >
                {t('CSV upsert')}
              </Button>
            )}
            <Tooltip label={t('Exports the rows currently shown')}>
              <Button
                variant="default"
                leftSection={<IconDownload size={18} />}
                onClick={exportCsv}
                disabled={!products.data?.items.length}
              >
                {t('Export')}
              </Button>
            </Tooltip>
            {isAdmin && (
              <Button leftSection={<IconPlus size={18} />} onClick={newProduct}>
                {t('New product')}
              </Button>
            )}
          </>
        }
      />

      <FilterBar
        hasActiveFilters={hasActiveFilters}
        onReset={() =>
          url.set({
            search: undefined,
            categoryId: undefined,
            category: undefined,
            status: undefined,
            sortBy: undefined,
            sortDir: undefined,
            page: undefined,
          })
        }
      >
        <TextInput
          aria-label={t('Search products')}
          placeholder={t('Search name or SKU')}
          leftSection={<IconSearch size={18} />}
          value={search}
          onChange={(e) => url.set({ search: e.currentTarget.value, page: undefined })}
          w={260}
        />
        <Select
          aria-label={t('Category')}
          placeholder={t('All categories')}
          clearable
          searchable
          data={(categories.data?.items ?? []).map((c) => ({ value: c.id, label: c.name }))}
          value={categoryId ?? null}
          onChange={(value) => url.set({ categoryId: value, category: undefined, page: undefined })}
          w={220}
        />
        <SegmentedControl
          aria-label={t('Status')}
          value={status}
          onChange={(value) =>
            url.set({ status: value === 'all' ? undefined : value, page: undefined })
          }
          data={[
            { value: 'all', label: t('All') },
            { value: 'active', label: t('Active') },
            { value: 'inactive', label: t('Inactive') },
          ]}
        />
      </FilterBar>

      {isAdmin && selectedIds.length > 0 && (
        <Alert mb="md">
          <Group justify="space-between" align="center">
            <Group gap="xs">
              <Badge>{selectedIds.length}</Badge>
              <Text size="sm">{t('products selected')}</Text>
            </Group>
            <Group gap="xs">
              <Button size="xs" variant="default" onClick={() => setBulkEditOpened(true)}>
                {t('Bulk update fields')}
              </Button>
              <Button size="xs" variant="default" onClick={() => setBulkPriceOpened(true)}>
                {t('Bulk adjust prices')}
              </Button>
              <Button size="xs" variant="subtle" onClick={() => setSelectedIds([])}>
                {t('Clear selection')}
              </Button>
            </Group>
          </Group>
        </Alert>
      )}

      <DataPanel>
        <QueryState
          query={products}
          isEmpty={(data) => data.items.length === 0}
          empty={
            <EmptyState
              icon={IconPackageOff}
              title={t(hasActiveFilters ? 'No products match your filters' : 'No products yet')}
              description={
                hasActiveFilters
                  ? t('Try a different search or reset the filters.')
                  : t('Add your first vegetable, fruit or container to start selling.')
              }
              action={
                !hasActiveFilters &&
                isAdmin && (
                  <Button leftSection={<IconPlus size={18} />} onClick={newProduct}>
                    {t('New product')}
                  </Button>
                )
              }
            />
          }
        >
          {(data) => (
            <>
              <DataTable
                columns={columns.map((column) => ({
                  ...column,
                  render:
                    column.key === 'unit'
                      ? (product: Product) => t(unitLabels[product.unit].long)
                      : column.render,
                }))}
                rows={data.items}
                getRowId={(p) => p.id}
                selectedId={productId}
                onRowClick={(p) => url.set({ productId: p.id, create: undefined }, { push: true })}
              />
              <TablePagination
                page={data.page}
                pageSize={data.pageSize}
                totalCount={data.totalCount}
                totalPages={data.totalPages}
                onPageChange={(value) => url.set({ page: value })}
                onPageSizeChange={(value) => url.set({ pageSize: value, page: undefined })}
              />
            </>
          )}
        </QueryState>
      </DataPanel>

      <ProductDrawer
        productId={productId}
        creating={creating && isAdmin}
        defaultCategoryId={categoryId}
        onClose={() => url.set({ productId: undefined, create: undefined })}
        onCreated={(product) => url.set({ productId: product.id, create: undefined })}
      />

      <Modal
        opened={bulkEditOpened}
        onClose={() => setBulkEditOpened(false)}
        title={t('Bulk update products')}
      >
        <Stack>
          <SegmentedControl
            value={bulkStatus ?? 'none'}
            onChange={(value) =>
              setBulkStatus(value === 'none' ? null : (value as 'active' | 'inactive'))
            }
            data={[
              { value: 'none', label: t('Keep status') },
              { value: 'active', label: t('Active') },
              { value: 'inactive', label: t('Inactive') },
            ]}
          />
          <Select
            label={t('Category')}
            placeholder={t('Keep current category')}
            clearable
            searchable
            data={(categories.data?.items ?? []).map((c) => ({ value: c.id, label: c.name }))}
            value={bulkCategoryId}
            onChange={setBulkCategoryId}
          />
          <Select
            label={t('Unit')}
            placeholder={t('Keep current unit')}
            clearable
            data={UNITS_OF_MEASURE.map((unit) => ({
              value: unit,
              label: t(unitLabels[unit].long),
            }))}
            value={bulkUnit}
            onChange={(value) => setBulkUnit((value as UnitOfMeasure | null) ?? null)}
          />
          <Select
            label={t('Default supplier')}
            placeholder={t('Keep current default supplier')}
            clearable
            searchable
            data={supplierOptions.map((name) => ({ value: name, label: name }))}
            value={bulkDefaultSupplierName}
            onChange={setBulkDefaultSupplierName}
          />
          <TextInput
            label={t('Supplier names')}
            placeholder={t('Comma separated supplier names')}
            value={bulkSupplierNames.join(', ')}
            onChange={(event) =>
              setBulkSupplierNames(
                event.currentTarget.value
                  .split(',')
                  .map((value) => value.trim())
                  .filter((value) => value.length > 0),
              )
            }
          />
          <Group justify="flex-end">
            <Button variant="default" onClick={() => setBulkEditOpened(false)}>
              {t('Cancel')}
            </Button>
            <Button loading={bulkUpdate.isPending} onClick={applyBulkUpdate}>
              {t('Apply changes')}
            </Button>
          </Group>
        </Stack>
      </Modal>

      <Modal
        opened={bulkPriceOpened}
        onClose={() => setBulkPriceOpened(false)}
        title={t('Bulk adjust prices')}
      >
        <Stack>
          <SegmentedControl
            value={priceMode}
            onChange={(value) => setPriceMode(value as 'Percentage' | 'Absolute')}
            data={[
              { value: 'Percentage', label: t('Percentage') },
              { value: 'Absolute', label: t('Absolute') },
            ]}
          />
          <NumberInput
            label={t('Near price change')}
            placeholder={priceMode === 'Percentage' ? t('e.g. 5 for +5%') : t('e.g. 2.5 for +$2.5')}
            value={nearChange}
            onChange={(value) => setNearChange(typeof value === 'number' ? value : '')}
            decimalScale={2}
          />
          <NumberInput
            label={t('Far price change')}
            placeholder={
              priceMode === 'Percentage' ? t('e.g. 7.5 for +7.5%') : t('e.g. -0.4 for -$0.4')
            }
            value={farChange}
            onChange={(value) => setFarChange(typeof value === 'number' ? value : '')}
            decimalScale={2}
          />
          <Group justify="flex-end">
            <Button variant="default" onClick={() => setBulkPriceOpened(false)}>
              {t('Cancel')}
            </Button>
            <Button loading={bulkAdjustPrices.isPending} onClick={applyPriceAdjust}>
              {t('Apply changes')}
            </Button>
          </Group>
        </Stack>
      </Modal>

      <Modal
        opened={csvImportOpened}
        onClose={() => setCsvImportOpened(false)}
        title={t('CSV upsert')}
      >
        <Stack>
          <Text size="sm" c="dimmed">
            {t('Upserts products by SKU. Unknown columns are ignored.')}
          </Text>
          <Alert color="blue">
            <Stack gap={4}>
              <Text size="sm" fw={600}>
                {t('Quick checklist')}
              </Text>
              <Text size="xs">
                • {t('Use the template file below')}
              </Text>
              <Text size="xs">
                • {t('Required: sku (and name for new SKUs)')}
              </Text>
              <Text size="xs">
                • {t('Category: set categoryCode or categoryName')}
              </Text>
              <Text size="xs">
                • {t('Optional fields can be left blank; unknown columns are ignored')}
              </Text>
            </Stack>
          </Alert>
          <Group justify="flex-start">
            <Button
              variant="light"
              size="xs"
              leftSection={<IconDownload size={16} />}
              onClick={downloadImportTemplate}
            >
              {t('Download template CSV')}
            </Button>
          </Group>
          <FileInput
            label={t('CSV file')}
            placeholder={t('Choose a CSV file')}
            accept=".csv,text/csv"
            value={csvFile}
            onChange={setCsvFile}
          />
          {csvResult && (
            <Alert color={csvResult.errorCount > 0 ? 'yellow' : 'green'}>
              <Stack gap={4}>
                <Text size="sm">
                  {t('Created')}: {csvResult.createdCount} · {t('Updated')}:{' '}
                  {csvResult.updatedCount} · {t('Errors')}: {csvResult.errorCount}
                </Text>
                {csvResult.errors.slice(0, 5).map((error) => (
                  <Text key={`${error.rowNumber}-${error.message}`} size="xs">
                    Row {error.rowNumber}: {error.message}
                  </Text>
                ))}
              </Stack>
            </Alert>
          )}
          <Group justify="flex-end">
            <Button variant="default" onClick={() => setCsvImportOpened(false)}>
              {t('Cancel')}
            </Button>
            <Button loading={csvUpsert.isPending} onClick={runCsvImport}>
              {t('Import')}
            </Button>
          </Group>
        </Stack>
      </Modal>
    </>
  );
}
