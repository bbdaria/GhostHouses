import { createContext, useContext, useEffect, useMemo, useState } from 'react';
import api, { clearAuthToken, setAuthToken } from '../api/client.js';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [token, setToken] = useState(() => {
    const stored = localStorage.getItem('authToken');
    if (stored) {
      setAuthToken(stored);
    }
    return stored;
  });
  const [user, setUser] = useState(() => {
    const raw = localStorage.getItem('authUser');
    return raw ? JSON.parse(raw) : null;
  });
  const [otpChallenge, setOtpChallenge] = useState(null);
  const [loadingProfile, setLoadingProfile] = useState(false);

  useEffect(() => {
    if (token) {
      setAuthToken(token);
      localStorage.setItem('authToken', token);
    } else {
      clearAuthToken();
      localStorage.removeItem('authToken');
    }
  }, [token]);

  const refreshProfile = async () => {
    if (!token) {
      return null;
    }
    try {
      setLoadingProfile(true);
      const profile = await api.me();
      setUser(profile);
      localStorage.setItem('authUser', JSON.stringify(profile));
      return profile;
    } finally {
      setLoadingProfile(false);
    }
  };

  const login = async (username, password) => {
    const result = await api.login(username, password);
    const challenge = {
      username,
      userId: result.userId,
      challengeToken: result.challengeToken,
      demoCode: result.demoCode
    };
    setOtpChallenge(challenge);
    return challenge;
  };

  const verifyOtp = async (code) => {
    if (!otpChallenge) {
      throw new Error('No challenge to verify');
    }
    const result = await api.verifyOtp({
      userId: otpChallenge.userId,
      challengeToken: otpChallenge.challengeToken,
      code
    });
    setToken(result.token);
    setUser(result.profile);
    localStorage.setItem('authUser', JSON.stringify(result.profile));
    setOtpChallenge(null);
    return result;
  };

  const logout = async () => {
    setToken(null);
    setUser(null);
    setOtpChallenge(null);
    localStorage.removeItem('authUser');
    localStorage.removeItem('authToken');
  };

  const value = useMemo(
    () => ({
      token,
      user,
      otpChallenge,
      loadingProfile,
      login,
      verifyOtp,
      refreshProfile,
      logout,
      setOtpChallenge
    }),
    [token, user, otpChallenge, loadingProfile]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error('useAuth must be used inside AuthProvider');
  }
  return ctx;
}
