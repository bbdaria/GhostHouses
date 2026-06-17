import { useState } from 'react';
import { Link, Navigate, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext.jsx';
import useDocumentTitle from '../hooks/useDocumentTitle.js';

export default function LoginPage() {
  const { login, token, user, otpChallenge } = useAuth();
  const navigate = useNavigate();
  const [form, setForm] = useState({ username: '', password: '' });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  useDocumentTitle('התחברות - מוקד המבנים העירוני');

  if (token && user) {
    return <Navigate to="/" replace />;
  }

  const handleChange = (event) => {
    const { name, value } = event.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    setError('');
    setLoading(true);
    try {
      await login(form.username, form.password);
      navigate('/otp');
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <main className="auth-page">
      <form className="auth-card" onSubmit={handleSubmit}>
        <h1>מערכת ניהול מבנים</h1>
        <p className="subtitle">הזינו שם משתמש וסיסמה לקבלת קוד אימות.</p>
        <label>
          שם משתמש
          <input name="username" value={form.username} onChange={handleChange} required />
        </label>
        <label>
          סיסמה
          <input
            type="password"
            name="password"
            value={form.password}
            onChange={handleChange}
            required
          />
        </label>
        <button type="submit" className="primary" disabled={loading}>
          {loading ? 'מחשב קוד…' : 'שליחת קוד אימות'}
        </button>
        {error && <p className="error">{error}</p>}
        {otpChallenge && (
          <p className="muted">
            קוד דמה עבור {otpChallenge.username}: <strong>{otpChallenge.demoCode}</strong>
          </p>
        )}
        <p className="muted">
          נתקלתם בקושי? צרו קשר עם מנהל המערכת או עברו ל{' '}
          <Link to="/otp">הזנת קוד OTP</Link>.
        </p>
      </form>
    </main>
  );
}
