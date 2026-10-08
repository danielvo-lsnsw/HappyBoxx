import {
  Avatar,
  Badge,
  Group,
  Menu,
  Text,
  UnstyledButton,
  useComputedColorScheme,
  useMantineColorScheme,
} from '@mantine/core';
import {
  IconBuildingWarehouse,
  IconCheck,
  IconChevronDown,
  IconClockPlay,
  IconLanguage,
  IconLogout,
  IconMoon,
  IconSettings,
  IconSun,
} from '@tabler/icons-react';
import { useUiLanguage } from '@/app/localization/UiLanguageContext';

function SoonBadge() {
  const { t } = useUiLanguage();
  return (
    <Badge size="xs" variant="light" color="gray">
      {t('Soon')}
    </Badge>
  );
}

// Placeholder identity until authentication is added.
const currentUser = { name: 'Admin User', role: 'Warehouse manager', initials: 'AU' };
const currentWarehouse = 'Main Warehouse';

export function UserMenu() {
  const { setColorScheme } = useMantineColorScheme();
  const { language, setLanguage, t } = useUiLanguage();
  const isDark = useComputedColorScheme('light') === 'dark';

  return (
    <Menu position="bottom-end" width={260} shadow="md">
      <Menu.Target>
        <UnstyledButton className="user-trigger" aria-label={t('Account menu')}>
          <Group gap="xs" wrap="nowrap">
            <Avatar color="green" radius="xl" size={36}>
              {currentUser.initials}
            </Avatar>
            <div className="user-trigger-text">
              <Text size="sm" fw={600} lh={1.2}>
                {currentUser.name}
              </Text>
              <Text size="xs" c="dimmed" lh={1.2}>
                {currentWarehouse}
              </Text>
            </div>
            <IconChevronDown size={16} />
          </Group>
        </UnstyledButton>
      </Menu.Target>
      <Menu.Dropdown>
        <Menu.Label>
          {currentUser.name} · {t(currentUser.role)}
        </Menu.Label>
        <Menu.Divider />
        <Menu.Label>{t('Warehouse')}</Menu.Label>
        <Menu.Item
          leftSection={<IconBuildingWarehouse size={18} />}
          rightSection={<IconCheck size={16} />}
        >
          {currentWarehouse}
        </Menu.Item>
        <Menu.Item disabled rightSection={<SoonBadge />}>
          {t('Switch warehouse')}
        </Menu.Item>
        <Menu.Divider />
        <Menu.Item leftSection={<IconClockPlay size={18} />} rightSection={<SoonBadge />} disabled>
          {t('Clock in / out')}
        </Menu.Item>
        <Menu.Item
          leftSection={isDark ? <IconSun size={18} /> : <IconMoon size={18} />}
          onClick={() => setColorScheme(isDark ? 'light' : 'dark')}
        >
          {t(isDark ? 'Light mode' : 'Dark mode')}
        </Menu.Item>
        <Menu.Item
          leftSection={<IconLanguage size={18} />}
          rightSection={
            <Text size="xs" c="dimmed">
              {language === 'en' ? 'Tiếng Việt' : 'English'}
            </Text>
          }
          onClick={() => setLanguage(language === 'en' ? 'vi' : 'en')}
        >
          {language === 'en' ? 'Language' : 'Ngôn ngữ'}
        </Menu.Item>
        <Menu.Item leftSection={<IconSettings size={18} />} rightSection={<SoonBadge />} disabled>
          {t('Account settings')}
        </Menu.Item>
        <Menu.Divider />
        <Menu.Item leftSection={<IconLogout size={18} />} color="red" disabled>
          {t('Sign out')}
        </Menu.Item>
      </Menu.Dropdown>
    </Menu>
  );
}
