import { ActionIcon, Badge, Button, Menu } from '@mantine/core';
import {
  IconCategoryPlus,
  IconClipboardPlus,
  IconPackageImport,
  IconPlus,
  IconSquarePlus,
  IconStack2,
  IconTruckDelivery,
} from '@tabler/icons-react';
import { useNavigate } from 'react-router';

const soon = (
  <Badge size="xs" variant="light" color="gray">
    Soon
  </Badge>
);

/** Global "+" menu to start common tasks from anywhere. */
export function QuickActions() {
  const navigate = useNavigate();
  const go = (path: string) => void navigate(path);

  return (
    <Menu position="bottom-end" width={260} shadow="md">
      <Menu.Target>
        <div>
          <Button leftSection={<IconPlus size={18} />} visibleFrom="sm">
            New
          </Button>
          <ActionIcon variant="filled" hiddenFrom="sm" aria-label="Quick actions">
            <IconPlus size={20} />
          </ActionIcon>
        </div>
      </Menu.Target>
      <Menu.Dropdown>
        <Menu.Label>Catalog & stock</Menu.Label>
        <Menu.Item
          leftSection={<IconSquarePlus size={18} />}
          onClick={() => go('/inventory/products?create=1')}
        >
          New product
        </Menu.Item>
        <Menu.Item
          leftSection={<IconCategoryPlus size={18} />}
          onClick={() => go('/inventory/categories?create=1')}
        >
          New category
        </Menu.Item>
        <Menu.Item leftSection={<IconStack2 size={18} />} onClick={() => go('/inventory/stock')}>
          Adjust stock
        </Menu.Item>
        <Menu.Divider />
        <Menu.Label>Operations</Menu.Label>
        <Menu.Item leftSection={<IconClipboardPlus size={18} />} rightSection={soon} disabled>
          Create order
        </Menu.Item>
        <Menu.Item leftSection={<IconTruckDelivery size={18} />} rightSection={soon} disabled>
          Log delivery
        </Menu.Item>
        <Menu.Item leftSection={<IconPackageImport size={18} />} rightSection={soon} disabled>
          Receive shipment
        </Menu.Item>
      </Menu.Dropdown>
    </Menu>
  );
}
