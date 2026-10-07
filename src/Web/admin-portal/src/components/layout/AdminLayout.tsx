import { NavLink, Outlet } from 'react-router';

const navItems = [
  { to: '/', label: 'Dashboard', end: true },
  { to: '/categories', label: 'Categories' },
  { to: '/products', label: 'Products' },
  { to: '/stock', label: 'Stock' },
];

/** Placeholder shell – the final layout and UI kit are still to be decided. */
export function AdminLayout() {
  return (
    <div className="admin-layout">
      <header>
        <strong>HappyBoxx Admin</strong>
      </header>
      <nav aria-label="Main">
        <ul>
          {navItems.map((item) => (
            <li key={item.to}>
              <NavLink to={item.to} end={item.end}>
                {item.label}
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>
      <main>
        <Outlet />
      </main>
    </div>
  );
}
