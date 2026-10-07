import { Button, SegmentedControl, Select, Stack, Text, TextInput, Tooltip } from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';
import { IconDownload, IconPackageOff, IconPlus, IconSearch } from '@tabler/icons-react';
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
import { unitLabels } from '@/lib/units';
import { useUrlParams } from '@/lib/useUrlParams';
import { useProductsQuery } from '../api';
import { ProductDrawer } from '../components/ProductDrawer';
import type { Product } from '../types';

const columns: DataTableColumn<Product>[] = [
  {
    key: 'sku',
    header: 'SKU',
    width: 130,
    render: (p) => (
      <Text ff="monospace" fw={600} size="sm">
        {p.sku}
      </Text>
    ),
  },
  {
    key: 'name',
    header: 'Product',
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
  { key: 'category', header: 'Category', render: (p) => p.categoryName },
  { key: 'unit', header: 'Unit', width: 110, render: (p) => unitLabels[p.unit].long },
  {
    key: 'price',
    header: 'Price',
    align: 'right',
    width: 120,
    render: (p) => <span className="tabular">{formatCurrency(p.price)}</span>,
  },
  {
    key: 'status',
    header: 'Status',
    width: 120,
    render: (p) => <ActiveBadge isActive={p.isActive} />,
  },
];

export function ProductsPage() {
  const url = useUrlParams();
  const page = url.getNumber('page', 1);
  const pageSize = url.getNumber('pageSize', 20);
  const search = url.get('search') ?? '';
  const status = url.get('status') ?? 'all';
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
    },
    { enabled: !resolvingCategory },
  );

  const hasActiveFilters = !!search || !!categoryId || status !== 'all';
  const title = categoryFromName?.name ?? 'Products';

  const exportCsv = () => {
    const rows = products.data?.items ?? [];
    downloadCsv(
      'products.csv',
      toCsv(rows, [
        { header: 'SKU', value: (p) => p.sku },
        { header: 'Name', value: (p) => p.name },
        { header: 'Category', value: (p) => p.categoryName },
        { header: 'Unit', value: (p) => p.unit },
        { header: 'Price', value: (p) => p.price },
        { header: 'Active', value: (p) => p.isActive },
      ]),
    );
  };

  const newProduct = () => url.set({ create: 1, productId: undefined }, { push: true });

  return (
    <>
      <PageHeader
        title={title}
        description="Manage the catalog: pricing, units and availability."
        actions={
          <>
            <Tooltip label="Exports the rows currently shown">
              <Button
                variant="default"
                leftSection={<IconDownload size={18} />}
                onClick={exportCsv}
                disabled={!products.data?.items.length}
              >
                Export
              </Button>
            </Tooltip>
            <Button leftSection={<IconPlus size={18} />} onClick={newProduct}>
              New product
            </Button>
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
            page: undefined,
          })
        }
      >
        <TextInput
          aria-label="Search products"
          placeholder="Search name or SKU"
          leftSection={<IconSearch size={18} />}
          value={search}
          onChange={(e) => url.set({ search: e.currentTarget.value, page: undefined })}
          w={260}
        />
        <Select
          aria-label="Category"
          placeholder="All categories"
          clearable
          searchable
          data={(categories.data?.items ?? []).map((c) => ({ value: c.id, label: c.name }))}
          value={categoryId ?? null}
          onChange={(value) => url.set({ categoryId: value, category: undefined, page: undefined })}
          w={220}
        />
        <SegmentedControl
          aria-label="Status"
          value={status}
          onChange={(value) =>
            url.set({ status: value === 'all' ? undefined : value, page: undefined })
          }
          data={[
            { value: 'all', label: 'All' },
            { value: 'active', label: 'Active' },
            { value: 'inactive', label: 'Inactive' },
          ]}
        />
      </FilterBar>

      <DataPanel>
        <QueryState
          query={products}
          isEmpty={(data) => data.items.length === 0}
          empty={
            <EmptyState
              icon={IconPackageOff}
              title={hasActiveFilters ? 'No products match your filters' : 'No products yet'}
              description={
                hasActiveFilters
                  ? 'Try a different search or reset the filters.'
                  : 'Add your first vegetable, fruit or container to start selling.'
              }
              action={
                !hasActiveFilters && (
                  <Button leftSection={<IconPlus size={18} />} onClick={newProduct}>
                    New product
                  </Button>
                )
              }
            />
          }
        >
          {(data) => (
            <>
              <DataTable
                columns={columns}
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
        creating={creating}
        defaultCategoryId={categoryId}
        onClose={() => url.set({ productId: undefined, create: undefined })}
        onCreated={(product) => url.set({ productId: product.id, create: undefined })}
      />
    </>
  );
}
