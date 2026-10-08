import { Button, Paper } from '@mantine/core';
import { IconArrowLeft, IconMapOff } from '@tabler/icons-react';
import { Link } from 'react-router';
import { EmptyState } from '@/components/data/EmptyState';
import { useUiLanguage } from '@/app/localization/UiLanguageContext';

export function NotFoundPage() {
  const { t } = useUiLanguage();
  return (
    <Paper withBorder radius="md" mt="xl">
      <EmptyState
        icon={IconMapOff}
        title={t('Page not found')}
        description={t("The page you're looking for doesn't exist or has moved.")}
        action={
          <Button component={Link} to="/" leftSection={<IconArrowLeft size={18} />}>
            {t('Back to dashboard')}
          </Button>
        }
      />
    </Paper>
  );
}
