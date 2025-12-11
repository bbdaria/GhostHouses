import { NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../context/AuthContext.jsx';
import { ROLE_LABELS } from '../i18n.js';

const NAV_LINKS = [
  { to: '/buildings', label: 'מאגר מבנים', roles: ['Viewer', 'Editor', 'Admin'] },
  { to: '/streets', label: 'מאגר רחובות', roles: ['Viewer', 'Editor', 'Admin'] },
  { to: '/logs', label: 'יומן פעילויות', roles: ['Viewer', 'Editor', 'Admin'] },
  { to: '/settings', label: 'הגדרות אישיות', roles: ['Viewer', 'Editor', 'Admin'] },
  { to: '/users', label: 'ניהול משתמשים', roles: ['Admin'] }
];

export default function AppLayout() {
  const { user, logout } = useAuth();
  const allowedLinks = NAV_LINKS.filter((link) => !link.roles || link.roles.includes(user?.role));
  const roleLabel = ROLE_LABELS[user?.role] || user?.role || '—';

  return (
    <div className="app-shell">
      <header className="top-bar">
        <div className="brand">
          <p className="eyebrow">עירייה</p>
          <h2>מוקד ניהול מבנים</h2>
        </div>
        <nav className="nav-links">
          {allowedLinks.map((link) => (
            <NavLink
              key={link.to}
              to={link.to}
              className={({ isActive }) => (isActive ? 'nav-link active' : 'nav-link')}
            >
              {link.label}
            </NavLink>
          ))}
        </nav>
        <div className="user-info">
          <div className="user-pill">
            <p className="muted">מחובר כ</p>
            <strong>{user?.username}</strong>
          </div>
          <div className="user-pill">
            <p className="muted">תפקיד</p>
            <strong>{roleLabel}</strong>
          </div>
          <button type="button" className="ghost logout-btn" onClick={logout}>
            התנתקות
          </button>
        </div>
      </header>
      <section className="main-content">
        <Outlet />
      </section>
    </div>
  );
}
