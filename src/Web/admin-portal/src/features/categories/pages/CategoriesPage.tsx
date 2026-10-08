import { Button, SegmentedControl, Text, TextInput } from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';
import { IconCategoryPlus, IconFolderOff, IconSearch } from '@tabler/icons-react';
import { DataTable, type DataTableColumn } from '@/components/data/DataTable';
import { EmptyState } from '@/components/data/EmptyState';
import { QueryState } from '@/components/data/QueryState';
import { ActiveBadge } from '@/components/data/StatusBadge';
import { TablePagination } from '@/components/data/TablePagination';
import { DataPanel } from '@/components/page/DataPanel';
import { FilterBar } from '@/components/page/FilterBar';
import { PageHeader } from '@/components/page/PageHeader';
import { formatDateTime } from '@/lib/format';
import { useUrlParams } from '@/lib/useUrlParams';
import { useCategoriesQuery } from '../api';
import { CategoryDrawer } from '../components/CategoryDrawer';
import type { Category } from '../types';
import { useUiLanguage } from '@/app/localization/UiLanguageContext';
import { useAuth } from '@/app/auth/AuthContext';

const columns: DataTableColumn<Category>[] = [
  {
    key: 'name',
    header: 'Name',
    render: (c) => (
      <Text fw={600} size="sm">
        {c.name}
      </Text>
    ),
  },
  {
    key: 'description',
    header: 'Description',
    render: (c) => (
      <Text size="sm" c="dimmed" lineClamp={1}>
        {c.description ?? '—'}
      </Text>
    ),
  },
  {
    key: 'status',
    header: 'Status',
    width: 120,
    render: (c) => <ActiveBadge isActive={c.isActive} />,
  },
  {
    key: 'updated',
    header: 'Last updated',
    width: 200,
    render: (c) => (
      <Text size="sm" c="dimmed">
        {formatDateTime(c.updatedAtUtc)}
      </Text>
    ),
  },
];

export function CategoriesPage() {
  const { t } = useUiLanguage();
  const { user } = useAuth();
  const isAdmin = user?.role === 'Admin';
  const url = useUrlParams();
  const page = url.getNumber('page', 1);
  const pageSize = url.getNumber('pageSize', 20);
  const search = url.get('search') ?? '';
  const status = url.get('status') ?? 'all';
  const categoryId = url.get('categoryId');
  const creating = url.get('create') === '1';
  const [debouncedSearch] = useDebouncedValue(search.trim(), 300);

  const categories = useCategoriesQuery({
    page,
    pageSize,
    search: debouncedSearch || undefined,
    isActive: status === 'all' ? undefined : status === 'active',
  });

  const hasActiveFilters = !!search || status !== 'all';
  const newCategory = () => url.set({ create: 1, categoryId: undefined }, { push: true });

  return (
    <>
      <PageHeader
        title={t('Categories')}
        description={t('Group products so staff and buyers can find them quickly.')}
        actions={
          isAdmin && (
            <Button leftSection={<IconCategoryPlus size={18} />} onClick={newCategory}>
              {t('New category')}
            </Button>
          )
        }
      />

      <FilterBar
        hasActiveFilters={hasActiveFilters}
        onReset={() => url.set({ search: undefined, status: undefined, page: undefined })}
      >
        <TextInput
          aria-label={t('Search categories')}
          placeholder={t('Search categories')}
          leftSection={<IconSearch size={18} />}
          value={search}
          onChange={(e) => url.set({ search: e.currentTarget.value, page: undefined })}
          w={260}
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

      <DataPanel>
        <QueryState
          query={categories}
          isEmpty={(data) => data.items.length === 0}
          empty={
            <EmptyState
              icon={IconFolderOff}
              title={t(hasActiveFilters ? 'No categories match your filters' : 'No categories yet')}
              action={
                !hasActiveFilters &&
                isAdmin && (
                  <Button onClick={newCategory} leftSection={<IconCategoryPlus size={18} />}>
                    {t('New category')}
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
                  header: typeof column.header === 'string' ? t(column.header) : column.header,
                }))}
                rows={data.items}
                getRowId={(c) => c.id}
                selectedId={categoryId}
                onRowClick={(c) => url.set({ categoryId: c.id, create: undefined }, { push: true })}
                minWidth={640}
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

      <CategoryDrawer
        categoryId={categoryId}
        creating={creating && isAdmin}
        canEdit={isAdmin}
        onClose={() => url.set({ categoryId: undefined, create: undefined })}
      />
    </>
  );
}
