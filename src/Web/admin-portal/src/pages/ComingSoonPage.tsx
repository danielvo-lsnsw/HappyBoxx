import { Badge, Button, Paper } from '@mantine/core';
import { IconArrowLeft, IconRocket } from '@tabler/icons-react';
import { Link } from 'react-router';
import { useActiveNavigation } from '@/app/navigation';
import { EmptyState } from '@/components/data/EmptyState';
import { PageHeader } from '@/components/page/PageHeader';

export function ComingSoonPage() {
  const { active } = useActiveNavigation();
  const title = active?.label ?? 'Coming soon';

  return (
    <>
      <PageHeader
        title={title}
        description={active?.description}
        actions={
          <Badge size="lg" variant="light" color="gray">
            Planned
          </Badge>
        }
      />
      <Paper withBorder radius="md">
        <EmptyState
          icon={active?.icon ?? IconRocket}
          title={`${title} is on the roadmap`}
          description="This module isn't available yet. The navigation is in place so you can see where it will live."
          action={
            <Button
              component={Link}
              to="/"
              variant="light"
              leftSection={<IconArrowLeft size={18} />}
            >
              Back to dashboard
            </Button>
          }
        />
      </Paper>
    </>
  );
}
