import {
  ActionIcon,
  Anchor,
  Divider,
  Group,
  Indicator,
  Popover,
  ScrollArea,
  Stack,
  Text,
  ThemeIcon,
  UnstyledButton,
} from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { IconAlertTriangle, IconBell, IconCircleX } from '@tabler/icons-react';
import { Link } from 'react-router';
import { useStockQuery } from '@/features/inventory/api';
import { formatNumber } from '@/lib/format';

/** Time-sensitive alerts. Currently low/out-of-stock; expiry & delivery alerts join later. */
export function NotificationBell() {
  const [opened, { close, toggle }] = useDisclosure(false);
  const lowStock = useStockQuery({ lowStockOnly: true, pageSize: 8 });
  const count = lowStock.data?.totalCount ?? 0;

  return (
    <Popover opened={opened} onChange={toggle} position="bottom-end" width={360} shadow="md">
      <Popover.Target>
        <Indicator
          label={count > 99 ? '99+' : count}
          size={18}
          color="red"
          disabled={count === 0}
          offset={6}
        >
          <ActionIcon
            onClick={toggle}
            aria-label={`Notifications${count ? ` (${count} alerts)` : ''}`}
          >
            <IconBell size={22} stroke={1.75} />
          </ActionIcon>
        </Indicator>
      </Popover.Target>
      <Popover.Dropdown p={0}>
        <Group justify="space-between" px="md" py="sm">
          <Text fw={600}>Alerts</Text>
          <Anchor
            component={Link}
            to="/inventory/stock?lowStockOnly=true"
            size="sm"
            onClick={close}
          >
            View all
          </Anchor>
        </Group>
        <Divider />
        <ScrollArea.Autosize mah={360}>
          {count === 0 ? (
            <Text c="dimmed" size="sm" p="md">
              You're all caught up.
            </Text>
          ) : (
            <Stack gap={0}>
              {lowStock.data?.items.map((item) => {
                const out = item.quantityOnHand <= 0;
                return (
                  <UnstyledButton
                    key={item.id}
                    component={Link}
                    to={`/inventory/products?productId=${item.productId}`}
                    onClick={close}
                    className="alert-item"
                  >
                    <Group wrap="nowrap" align="flex-start" gap="sm">
                      <ThemeIcon color={out ? 'red' : 'yellow'} variant="light" size="lg">
                        {out ? <IconCircleX size={20} /> : <IconAlertTriangle size={20} />}
                      </ThemeIcon>
                      <div>
                        <Text size="sm" fw={600}>
                          {out ? 'Out of stock' : 'Low stock'}: {item.sku}
                        </Text>
                        <Text size="xs" c="dimmed">
                          {formatNumber(item.quantityOnHand)} on hand · reorder at{' '}
                          {formatNumber(item.reorderLevel)}
                        </Text>
                      </div>
                    </Group>
                  </UnstyledButton>
                );
              })}
            </Stack>
          )}
        </ScrollArea.Autosize>
        <Divider />
        <Text size="xs" c="dimmed" px="md" py="xs">
          Expiry and delivery alerts will appear here once batch tracking and logistics go live.
        </Text>
      </Popover.Dropdown>
    </Popover>
  );
}
