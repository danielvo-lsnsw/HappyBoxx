import { Box, Divider, Drawer, Group, ScrollArea, Stack, Text, Title } from '@mantine/core';
import type { ReactNode } from 'react';
import { useUiLanguage } from '@/app/localization/UiLanguageContext';

interface DetailDrawerProps {
  opened: boolean;
  onClose: () => void;
  title: ReactNode;
  subtitle?: ReactNode;
  badge?: ReactNode;
  children: ReactNode;
  /** Sticky action bar (Cancel / Save). */
  footer?: ReactNode;
}

/** Contextual right panel – see docs/UI_GUIDELINES.md §4. */
export function DetailDrawer({
  opened,
  onClose,
  title,
  subtitle,
  badge,
  children,
  footer,
}: DetailDrawerProps) {
  const { t } = useUiLanguage();
  return (
    <Drawer.Root opened={opened} onClose={onClose} position="right" size={540}>
      <Drawer.Overlay backgroundOpacity={0.25} blur={1} />
      <Drawer.Content>
        <Stack gap={0} h="100%">
          <Drawer.Header>
            <Stack gap={4}>
              <Group gap="sm">
                <Drawer.Title component="div">
                  <Title order={3}>{title}</Title>
                </Drawer.Title>
                {badge}
              </Group>
              {subtitle && (
                <Text size="sm" c="dimmed">
                  {subtitle}
                </Text>
              )}
            </Stack>
            <Drawer.CloseButton size="lg" aria-label={t('Close panel')} />
          </Drawer.Header>
          <Divider />
          <ScrollArea flex={1} type="auto">
            <Box p="md">{children}</Box>
          </ScrollArea>
          {footer && (
            <>
              <Divider />
              <Group justify="flex-end" p="md" gap="sm">
                {footer}
              </Group>
            </>
          )}
        </Stack>
      </Drawer.Content>
    </Drawer.Root>
  );
}
