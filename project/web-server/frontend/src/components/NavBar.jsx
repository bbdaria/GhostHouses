import { Link } from 'react-router-dom';
import { useAuth } from '../authContext.jsx';

export default function NavBar() {
  const { user, logout } = useAuth();

  if (!user) {
    return null;
  }

  return (
    <header className="navbar">
      <div>
        <h1>GhostHouses</h1>
        <p>Haifa Abandoned Buildings Stage A</p>
      </div>
      <nav>
        <Link to="/dashboard">Dashboard</Link>
        <Link to="/buildings">Buildings</Link>
        <Link to="/logs">Logs</Link>
        {(user.role === 'Editor' || user.role === 'Admin') && <Link to="/buildings/add">Add Building</Link>}
        {user.role === 'Admin' && <Link to="/users">Users</Link>}
        <button
          type="button"
          onClick={() => {
            logout();
            window.location.href = '/login';
          }}
          className="link-button"
        >
          Sign out
        </button>
      </nav>
      <div className="user-pill">
        <strong>{user.username}</strong>
        <span>{user.role}</span>
      </div>
    </header>
  );
}
