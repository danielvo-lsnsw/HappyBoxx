import { Badge, Button, Paper } from '@mantine/core';
import { IconArrowLeft, IconRocket } from '@tabler/icons-react';
import { Link } from 'react-router';
import { useActiveNavigation } from '@/app/navigation';
import { EmptyState } from '@/components/data/EmptyState';
import { PageHeader } from '@/components/page/PageHeader';
import { useUiLanguage } from '@/app/localization/UiLanguageContext';

export function ComingSoonPage() {
  const { t } = useUiLanguage();
  const { active } = useActiveNavigation();
  const title = t(active?.label ?? 'Coming soon');

  return (
    <>
      <PageHeader
        title={title}
        description={active?.description ? t(active.description) : undefined}
        actions={
          <Badge size="lg" variant="light" color="gray">
            {t('Planned')}
          </Badge>
        }
      />
      <Paper withBorder radius="md">
        <EmptyState
          icon={active?.icon ?? IconRocket}
          title={`${title}${t(' is on the roadmap')}`}
          description={t(
            "This module isn't available yet. The navigation is in place so you can see where it will live.",
          )}
          action={
            <Button
              component={Link}
              to="/"
              variant="light"
              leftSection={<IconArrowLeft size={18} />}
            >
              {t('Back to dashboard')}
            </Button>
          }
        />
      </Paper>
    </>
  );
}
