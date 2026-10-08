import { ActionIcon, Box, Burger, Group, Text, ThemeIcon } from '@mantine/core';
import { spotlight } from '@mantine/spotlight';
import { IconLeaf, IconSearch } from '@tabler/icons-react';
import { Link } from 'react-router';
import { useUiLanguage } from '@/app/localization/UiLanguageContext';
import { GlobalSearch, GlobalSearchTrigger } from './GlobalSearch';
import { NotificationBell } from './NotificationBell';
import { QuickActions } from './QuickActions';
import { UserMenu } from './UserMenu';

interface HeaderProps {
  mobileNavOpened: boolean;
  onToggleMobileNav: () => void;
}

export function Header({ mobileNavOpened, onToggleMobileNav }: HeaderProps) {
  const { t } = useUiLanguage();

  return (
    <Group h="100%" px="md" gap="md" wrap="nowrap" justify="space-between">
      <Group gap="sm" wrap="nowrap">
        <Burger
          opened={mobileNavOpened}
          onClick={onToggleMobileNav}
          hiddenFrom="sm"
          size="sm"
          aria-label={t('Toggle navigation')}
        />
        <Group
          renderRoot={(props) => <Link to="/" aria-label={t('HappyBoxx home')} {...props} />}
          gap={10}
          wrap="nowrap"
          className="brand"
        >
          <ThemeIcon size={36} radius="md" variant="filled">
            <IconLeaf size={22} />
          </ThemeIcon>
          <Box visibleFrom="xs">
            <Text fw={800} size="lg" lh={1}>
              HappyBoxx
            </Text>
            <Text size="xs" c="dimmed" lh={1.2}>
              {t('Warehouse Admin')}
            </Text>
          </Box>
        </Group>
      </Group>

      <Box flex={1} maw={560} visibleFrom="sm">
        <GlobalSearchTrigger />
      </Box>

      <Group gap="xs" wrap="nowrap">
        <ActionIcon hiddenFrom="sm" onClick={spotlight.open} aria-label={t('Search')}>
          <IconSearch size={22} stroke={1.75} />
        </ActionIcon>
        <QuickActions />
        <NotificationBell />
        <UserMenu />
      </Group>

      <GlobalSearch />
    </Group>
  );
}
