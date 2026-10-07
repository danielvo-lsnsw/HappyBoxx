import { Anchor, Breadcrumbs, Group, Stack, Text, Title } from '@mantine/core';
import type { ReactNode } from 'react';
import { Link } from 'react-router';
import { useActiveNavigation } from '@/app/navigation';

interface PageHeaderProps {
  title: string;
  description?: ReactNode;
  /** Page-level actions; at most one filled (primary) button. */
  actions?: ReactNode;
}

export function PageHeader({ title, description, actions }: PageHeaderProps) {
  const { trail } = useActiveNavigation();

  return (
    <Stack gap={6} mb="lg">
      {trail.length > 1 && (
        <Breadcrumbs separatorMargin={6} fz="sm">
          {trail.map((item, index) =>
            item.path && index < trail.length - 1 ? (
              <Anchor key={item.label} component={Link} to={item.path} size="sm" c="dimmed">
                {item.label}
              </Anchor>
            ) : (
              <Text key={item.label} size="sm" c="dimmed">
                {item.label}
              </Text>
            ),
          )}
        </Breadcrumbs>
      )}
      <Group justify="space-between" align="flex-end" gap="md" wrap="wrap">
        <Stack gap={2}>
          <Title order={2}>{title}</Title>
          {description && <Text c="dimmed">{description}</Text>}
        </Stack>
        {actions && <Group gap="sm">{actions}</Group>}
      </Group>
    </Stack>
  );
}
