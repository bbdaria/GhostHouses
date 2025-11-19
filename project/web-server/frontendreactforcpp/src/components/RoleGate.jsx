import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../context/AuthContext.jsx';

const roleRank = {
  Viewer: 0,
  Editor: 1,
  Admin: 2
};

export default function RoleGate({ minRole = 'Viewer', redirectTo = '/' }) {
  const { user } = useAuth();
  if (!user) {
    return <Navigate to="/login" replace />;
  }
  if (roleRank[user.role] < roleRank[minRole]) {
    return <Navigate to={redirectTo} replace />;
  }
  return <Outlet />;
}
