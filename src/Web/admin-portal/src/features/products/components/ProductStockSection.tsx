import {
  Anchor,
  Button,
  Group,
  NumberInput,
  Paper,
  SimpleGrid,
  Skeleton,
  Stack,
  Text,
  Title,
} from '@mantine/core';
import { useForm } from '@mantine/form';
import { IconStack2 } from '@tabler/icons-react';
import { useState } from 'react';
import { Link } from 'react-router';
import { StatusBadge } from '@/components/data/StatusBadge';
import { UnitToggle } from '@/components/data/UnitToggle';
import { useCreateStockItemMutation, useStockByProductQuery } from '@/features/inventory/api';
import { getStockStatus } from '@/features/inventory/stockStatus';
import { formatDateTime } from '@/lib/format';
import { handleSubmitError, notifySuccess } from '@/lib/forms';
import { convertQuantity, formatQuantity, type UnitOfMeasure } from '@/lib/units';
import type { Product } from '../types';

export function ProductStockSection({ product }: { product: Product }) {
  const stock = useStockByProductQuery(product.id);
  const [displayUnit, setDisplayUnit] = useState<UnitOfMeasure>(product.unit);

  if (stock.isPending) {
    return <Skeleton height={120} radius="md" />;
  }

  if (stock.isError) {
    return (
      <Text c="red" size="sm">
        Couldn't load stock: {stock.error.message}
      </Text>
    );
  }

  if (!stock.data) {
    return <StartTrackingForm product={product} />;
  }

  const item = stock.data;
  const status = getStockStatus(item);
  const show = (value: number) =>
    formatQuantity(convertQuantity(value, product.unit, displayUnit), displayUnit);

  return (
    <Paper withBorder p="md" radius="md">
      <Group justify="space-between" mb="sm">
        <Group gap="xs">
          <Title order={5}>Stock</Title>
          <StatusBadge tone={status.tone} label={status.label} />
        </Group>
        <UnitToggle baseUnit={product.unit} value={displayUnit} onChange={setDisplayUnit} />
      </Group>
      <SimpleGrid cols={2}>
        <div>
          <Text size="xs" c="dimmed" tt="uppercase" fw={600}>
            On hand
          </Text>
          <Text size="xl" fw={700} className="tabular">
            {show(item.quantityOnHand)}
          </Text>
        </div>
        <div>
          <Text size="xs" c="dimmed" tt="uppercase" fw={600}>
            Reorder level
          </Text>
          <Text size="xl" fw={700} className="tabular">
            {show(item.reorderLevel)}
          </Text>
        </div>
      </SimpleGrid>
      <Group justify="space-between" mt="md">
        <Text size="xs" c="dimmed">
          Updated {formatDateTime(item.updatedAtUtc)}
        </Text>
        <Anchor component={Link} to={`/inventory/stock?productId=${product.id}`} size="sm">
          Adjust stock →
        </Anchor>
      </Group>
    </Paper>
  );
}

function StartTrackingForm({ product }: { product: Product }) {
  const createStock = useCreateStockItemMutation();
  const form = useForm({
    mode: 'controlled',
    initialValues: { initialQuantity: 0 as number | string, reorderLevel: 0 as number | string },
    validate: {
      initialQuantity: (v) => (v === '' || Number(v) < 0 ? 'Enter 0 or more' : null),
      reorderLevel: (v) => (v === '' || Number(v) < 0 ? 'Enter 0 or more' : null),
    },
  });

  const submit = form.onSubmit(async (values) => {
    try {
      await createStock.mutateAsync({
        productId: product.id,
        sku: product.sku,
        initialQuantity: Number(values.initialQuantity),
        reorderLevel: Number(values.reorderLevel),
      });
      notifySuccess(`${product.sku} is now tracked in inventory`, 'Stock tracking started');
    } catch (error) {
      handleSubmitError(form, error);
    }
  });

  return (
    <Paper withBorder p="md" radius="md">
      <form onSubmit={submit} noValidate>
        <Stack gap="sm">
          <Group gap="xs">
            <IconStack2 size={20} />
            <Title order={5}>Not tracked in inventory yet</Title>
          </Group>
          <Text size="sm" c="dimmed">
            Start tracking to record on-hand quantity and get low-stock alerts.
          </Text>
          <SimpleGrid cols={2}>
            <NumberInput
              label="Opening quantity"
              min={0}
              decimalScale={3}
              {...form.getInputProps('initialQuantity')}
            />
            <NumberInput
              label="Reorder level"
              min={0}
              decimalScale={3}
              {...form.getInputProps('reorderLevel')}
            />
          </SimpleGrid>
          <Group justify="flex-end">
            <Button type="submit" variant="light" loading={createStock.isPending}>
              Start tracking
            </Button>
          </Group>
        </Stack>
      </form>
    </Paper>
  );
}
