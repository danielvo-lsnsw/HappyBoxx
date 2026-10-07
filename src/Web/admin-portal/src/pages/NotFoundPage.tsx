import { Button, Paper } from '@mantine/core';
import { IconArrowLeft, IconMapOff } from '@tabler/icons-react';
import { Link } from 'react-router';
import { EmptyState } from '@/components/data/EmptyState';

export function NotFoundPage() {
  return (
    <Paper withBorder radius="md" mt="xl">
      <EmptyState
        icon={IconMapOff}
        title="Page not found"
        description="The page you're looking for doesn't exist or has moved."
        action={
          <Button component={Link} to="/" leftSection={<IconArrowLeft size={18} />}>
            Back to dashboard
          </Button>
        }
      />
    </Paper>
  );
}
