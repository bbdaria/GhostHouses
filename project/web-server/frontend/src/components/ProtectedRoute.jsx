import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../authContext.jsx';

export default function ProtectedRoute({ allowedRoles }) {
  const { user, loading } = useAuth();

  if (loading) {
    return <div className="card">Loading session…</div>;
  }

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  if (allowedRoles && !allowedRoles.includes(user.role)) {
    return (
      <div className="card danger">
        <h2>Access denied</h2>
        <p>You do not have permissions to see this page.</p>
      </div>
    );
  }

  return <Outlet />;
}
