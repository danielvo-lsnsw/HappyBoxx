import { Table, Text } from '@mantine/core';
import type { KeyboardEvent, ReactNode } from 'react';

export interface DataTableColumn<T> {
  key: string;
  header: ReactNode;
  render: (row: T) => ReactNode;
  align?: 'left' | 'right' | 'center';
  width?: number | string;
}

interface DataTableProps<T> {
  columns: DataTableColumn<T>[];
  rows: T[];
  getRowId: (row: T) => string;
  onRowClick?: (row: T) => void;
  selectedId?: string;
  minWidth?: number;
  caption?: string;
}

export function DataTable<T>({
  columns,
  rows,
  getRowId,
  onRowClick,
  selectedId,
  minWidth = 720,
  caption,
}: DataTableProps<T>) {
  const handleKeyDown = (event: KeyboardEvent, row: T) => {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      onRowClick?.(row);
    }
  };

  return (
    <Table.ScrollContainer minWidth={minWidth}>
      <Table
        striped
        highlightOnHover={!!onRowClick}
        stickyHeader
        verticalSpacing="sm"
        horizontalSpacing="md"
        className="data-table"
      >
        {caption && (
          <Table.Caption>
            <Text size="sm" c="dimmed">
              {caption}
            </Text>
          </Table.Caption>
        )}
        <Table.Thead>
          <Table.Tr>
            {columns.map((column) => (
              <Table.Th key={column.key} scope="col" ta={column.align} w={column.width}>
                {column.header}
              </Table.Th>
            ))}
          </Table.Tr>
        </Table.Thead>
        <Table.Tbody>
          {rows.map((row) => {
            const id = getRowId(row);
            return (
              <Table.Tr
                key={id}
                bg={id === selectedId ? 'var(--mantine-primary-color-light)' : undefined}
                style={onRowClick ? { cursor: 'pointer' } : undefined}
                tabIndex={onRowClick ? 0 : undefined}
                onClick={onRowClick ? () => onRowClick(row) : undefined}
                onKeyDown={onRowClick ? (event) => handleKeyDown(event, row) : undefined}
              >
                {columns.map((column) => (
                  <Table.Td key={column.key} ta={column.align}>
                    {column.render(row)}
                  </Table.Td>
                ))}
              </Table.Tr>
            );
          })}
        </Table.Tbody>
      </Table>
    </Table.ScrollContainer>
  );
}
