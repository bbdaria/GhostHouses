import { Link } from 'react-router-dom';
import { useAuth } from '../authContext.jsx';

export default function DashboardPage() {
  const { user } = useAuth();

  return (
    <section className="grid">
      <article className="card">
        <h2>Welcome, {user?.username}</h2>
        <p>Use the navigation to inspect buildings, coordinate remediation, and manage permissions.</p>
      </article>
      <article className="card">
        <h3>Quick links</h3>
        <ul>
          <li>
            <Link to="/buildings">Search Buildings</Link>
          </li>
          {(user?.role === 'Editor' || user?.role === 'Admin') && (
            <li>
              <Link to="/buildings/add">Add New Building</Link>
            </li>
          )}
          <li>
            <Link to="/logs">Investigate Logs</Link>
          </li>
          {user?.role === 'Admin' && (
            <li>
              <Link to="/users">Manage Users & Roles</Link>
            </li>
          )}
        </ul>
      </article>
    </section>
  );
}
