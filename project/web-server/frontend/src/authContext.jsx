import { createContext, useContext, useEffect, useState } from 'react';
import { AuthApi } from './api.js';

const AuthContext = createContext();

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const token = localStorage.getItem('gh_token');
    if (!token) {
      setLoading(false);
      return;
    }

    AuthApi.me()
      .then((profile) => setUser(profile))
      .catch(() => {
        localStorage.removeItem('gh_token');
        setUser(null);
      })
      .finally(() => setLoading(false));
  }, []);

  const logout = () => {
    localStorage.removeItem('gh_token');
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ user, setUser, loading, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export const useAuth = () => useContext(AuthContext);
