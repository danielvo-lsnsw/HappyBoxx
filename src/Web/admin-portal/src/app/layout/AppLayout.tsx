import { AppShell } from '@mantine/core';
import { useDisclosure, useLocalStorage } from '@mantine/hooks';
import { Outlet } from 'react-router';
import { Header } from './Header';
import { Sidebar } from './Sidebar';

const SIDEBAR_WIDTH = 264;
const RAIL_WIDTH = 76;

export function AppLayout() {
  const [mobileOpened, { toggle: toggleMobile, close: closeMobile }] = useDisclosure(false);
  const [collapsed, setCollapsed] = useLocalStorage({
    key: 'happyboxx:sidebar-collapsed',
    defaultValue: false,
  });
  const rail = collapsed && !mobileOpened;

  return (
    <AppShell
      header={{ height: 64 }}
      navbar={{
        width: rail ? RAIL_WIDTH : SIDEBAR_WIDTH,
        breakpoint: 'sm',
        collapsed: { mobile: !mobileOpened },
      }}
      padding="lg"
    >
      <AppShell.Header>
        <Header mobileNavOpened={mobileOpened} onToggleMobileNav={toggleMobile} />
      </AppShell.Header>
      <AppShell.Navbar>
        <Sidebar
          collapsed={rail}
          onToggleCollapsed={() => setCollapsed((value) => !value)}
          onNavigate={closeMobile}
        />
      </AppShell.Navbar>
      <AppShell.Main className="app-main">
        <Outlet />
      </AppShell.Main>
    </AppShell>
  );
}
