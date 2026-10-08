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
  IconChevronDown,
  IconClockPlay,
  IconLanguage,
  IconLogout,
  IconMoon,
  IconSettings,
  IconSun,
  IconUsers,
} from '@tabler/icons-react';
import { Link } from 'react-router';
import { useUiLanguage } from '@/app/localization/UiLanguageContext';
import { useAuth } from '@/app/auth/AuthContext';

function SoonBadge() {
  const { t } = useUiLanguage();
  return (
    <Badge size="xs" variant="light" color="gray">
      {t('Soon')}
    </Badge>
  );
}

export function UserMenu() {
  const { setColorScheme } = useMantineColorScheme();
  const { language, setLanguage, t } = useUiLanguage();
  const { user, signOut } = useAuth();
  const isDark = useComputedColorScheme('light') === 'dark';
  const name = user?.name ?? '';
  const initials = name
    .split(/\s+/)
    .map((part) => part[0])
    .slice(0, 2)
    .join('')
    .toUpperCase();

  return (
    <Menu position="bottom-end" width={260} shadow="md">
      <Menu.Target>
        <UnstyledButton className="user-trigger" aria-label={t('Account menu')}>
          <Group gap="xs" wrap="nowrap">
            <Avatar color="green" radius="xl" size={36}>
              {initials}
            </Avatar>
            <div className="user-trigger-text">
              <Text size="sm" fw={600} lh={1.2}>
                {name}
              </Text>
              <Text size="xs" c="dimmed" lh={1.2}>
                {user ? t(user.role) : ''}
              </Text>
            </div>
            <IconChevronDown size={16} />
          </Group>
        </UnstyledButton>
      </Menu.Target>
      <Menu.Dropdown>
        <Menu.Label>
          {name} · {user?.email}
        </Menu.Label>
        <Menu.Divider />
        {user?.role === 'Admin' && (
          <Menu.Item component={Link} to="/admin/staff" leftSection={<IconUsers size={18} />}>
            {t('Staff access')}
          </Menu.Item>
        )}
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
        <Menu.Item leftSection={<IconLogout size={18} />} color="red" onClick={signOut}>
          {t('Sign out')}
        </Menu.Item>
      </Menu.Dropdown>
    </Menu>
  );
}
