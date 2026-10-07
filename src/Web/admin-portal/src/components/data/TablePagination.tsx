import { Group, Pagination, Select, Text } from '@mantine/core';

const PAGE_SIZES = ['20', '50', '100'];

interface TablePaginationProps {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
}

export function TablePagination({
  page,
  pageSize,
  totalCount,
  totalPages,
  onPageChange,
  onPageSizeChange,
}: TablePaginationProps) {
  const from = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const to = Math.min(page * pageSize, totalCount);

  return (
    <Group justify="space-between" px="md" py="sm" gap="md" wrap="wrap">
      <Text size="sm" c="dimmed" className="tabular">
        Showing {from}–{to} of {totalCount}
      </Text>
      <Group gap="md">
        <Group gap="xs">
          <Text size="sm" c="dimmed">
            Rows per page
          </Text>
          <Select
            aria-label="Rows per page"
            data={PAGE_SIZES}
            value={String(pageSize)}
            onChange={(value) => value && onPageSizeChange(Number(value))}
            allowDeselect={false}
            w={88}
            size="sm"
          />
        </Group>
        <Pagination
          total={Math.max(totalPages, 1)}
          value={page}
          onChange={onPageChange}
          size="md"
        />
      </Group>
    </Group>
  );
}
