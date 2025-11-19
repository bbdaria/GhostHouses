import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { AuthApi } from '../api.js';
import { useAuth } from '../authContext.jsx';

export default function LoginPage() {
  const navigate = useNavigate();
  const { setUser } = useAuth();
  const [step, setStep] = useState(1);
  const [form, setForm] = useState({ username: '', password: '', code: '' });
  const [challenge, setChallenge] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const handleChange = (event) => {
    const { name, value } = event.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  };

  const handleLogin = async (event) => {
    event.preventDefault();
    setLoading(true);
    setError('');
    try {
      const response = await AuthApi.login({ username: form.username, password: form.password });
      setChallenge(response);
      setStep(2);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleVerify = async (event) => {
    event.preventDefault();
    if (!challenge) return;
    setLoading(true);
    setError('');
    try {
      const result = await AuthApi.verify2fa({
        userId: challenge.userId,
        challengeToken: challenge.challengeToken,
        code: form.code,
      });
      localStorage.setItem('gh_token', result.token);
      setUser(result);
      navigate('/dashboard');
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <main className="auth-layout">
      <form className="card" onSubmit={step === 1 ? handleLogin : handleVerify}>
        <h1>GhostHouses Login</h1>
        {error ? <p className="error">{error}</p> : null}
        {step === 1 ? (
          <>
            <label>
              Username
              <input name="username" value={form.username} onChange={handleChange} required disabled={loading} />
            </label>
            <label>
              Password
              <input
                type="password"
                name="password"
                value={form.password}
                onChange={handleChange}
                required
                disabled={loading}
              />
            </label>
            <button type="submit" disabled={loading}>
              {loading ? 'Checking…' : 'Next'}
            </button>
          </>
        ) : (
          <>
            <p>Enter the verification code sent to your secure channel.</p>
            <label>
              2FA Code
              <input name="code" value={form.code} onChange={handleChange} required disabled={loading} />
            </label>
            {challenge?.devTwoFactorCode ? (
              <p className="muted">Developer mode code: {challenge.devTwoFactorCode}</p>
            ) : null}
            <button type="submit" disabled={loading}>
              {loading ? 'Verifying…' : 'Sign in'}
            </button>
          </>
        )}
      </form>
    </main>
  );
}
