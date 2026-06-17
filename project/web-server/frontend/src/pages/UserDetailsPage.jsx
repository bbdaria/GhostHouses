import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import api from '../api/client.js';
import { ROLE_LABELS } from '../i18n.js';
import useDocumentTitle from '../hooks/useDocumentTitle.js';
import { formatDateTime } from '../utils/formatDate.js';

export default function UserDetailsPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [user, setUser] = useState(null);
  const [logs, setLogs] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [form, setForm] = useState({ role: 'Viewer', email: '', twoFactorEnabled: true });
  const [message, setMessage] = useState('');
  useDocumentTitle(user ? `פרטי משתמש – ${user.username}` : 'פרטי משתמש');

  useEffect(() => {
    loadUser();
  }, [id]);

  const loadUser = async () => {
    setLoading(true);
    setError('');
    try {
      const data = await api.fetchUser(id);
      setUser(data);
      setForm({
        role: data.role,
        email: data.email || '',
        twoFactorEnabled: data.twoFactorEnabled
      });
      const history = await api.fetchLogs({ userId: data.id });
      setLogs(history);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleChange = (event) => {
    const { name, value, type, checked } = event.target;
    setForm((prev) => ({ ...prev, [name]: type === 'checkbox' ? checked : value }));
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    setMessage('');
    try {
      const data = await api.updateUser(id, form);
      setUser(data);
      setMessage('המשתמש עודכן.');
    } catch (err) {
      setMessage(err.message);
    }
  };

  if (loading) {
    return (
      <main className="app users-app">
        <p className="muted">טוען משתמש…</p>
      </main>
    );
  }

  if (error) {
    return (
      <main className="app users-app">
        <p className="error">שגיאה: {error}</p>
      </main>
    );
  }

  if (!user) {
    return (
      <main className="app users-app">
        <p className="muted">המשתמש לא נמצא.</p>
      </main>
    );
  }

  const roleLabel = ROLE_LABELS[user.role] || user.role;

  return (
    <main className="app users-app">
      <button className="ghost" onClick={() => navigate(-1)}>
        חזרה
      </button>
      <header className="page-header">
        <div>
          <p className="eyebrow">חשבון</p>
          <h1>{user.username}</h1>
          <p className="subtitle">{user.email || 'ללא דוא"ל'}</p>
        </div>
      </header>

      <section className="panel">
        <h3>עריכת משתמש</h3>
        <form className="form-grid" onSubmit={handleSubmit}>
          <label>
            תפקיד
            <select name="role" value={form.role} onChange={handleChange}>
              <option value="Viewer">{ROLE_LABELS.Viewer}</option>
              <option value="Editor">{ROLE_LABELS.Editor}</option>
              <option value="Admin">{ROLE_LABELS.Admin}</option>
            </select>
          </label>
          <label>
            דוא"ל
            <input name="email" value={form.email} onChange={handleChange} type="email" />
          </label>
          <label className="checkbox">
            <input
              type="checkbox"
              name="twoFactorEnabled"
              checked={form.twoFactorEnabled}
              onChange={handleChange}
            />
            דרוש OTP
          </label>
          <div className="filters-actions">
            <button type="submit" className="primary">
              שמירה
            </button>
          </div>
        </form>
        {message && <p className="muted">{message}</p>}
      </section>

      <section className="panel">
        <h3>יומן פעילות</h3>
        {logs.length === 0 && <p className="muted">אין רשומות למשתמש זה.</p>}
        <ul className="log-list">
          {logs.map((log) => (
            <li key={log.id}>
              <div>
                <strong>{log.actionType}</strong>
                <p>{log.description || '—'}</p>
                <small>
                  מבנה #{log.buildingId} • {formatDateTime(log.createdAt)}
                </small>
              </div>
            </li>
          ))}
        </ul>
      </section>
    </main>
  );
}
