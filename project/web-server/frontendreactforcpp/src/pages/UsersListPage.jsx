import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import api from '../api/client.js';
import { ROLE_LABELS } from '../i18n.js';
import useDocumentTitle from '../hooks/useDocumentTitle.js';

const newUserDefaults = {
  username: '',
  password: '',
  role: 'Viewer',
  department: ''
};

export default function UsersListPage() {
  const [users, setUsers] = useState([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [form, setForm] = useState(newUserDefaults);
  const [message, setMessage] = useState('');
  useDocumentTitle('ניהול משתמשים - מוקד המבנים העירוני');

  useEffect(() => {
    loadUsers();
  }, []);

  const loadUsers = async () => {
    setLoading(true);
    setError('');
    try {
      const data = await api.fetchUsers();
      setUsers(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleChange = (event) => {
    const { name, value } = event.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    setMessage('');
    try {
      await api.createUser(form);
      setForm(newUserDefaults);
      loadUsers();
      setMessage('המשתמש נוצר בהצלחה.');
    } catch (err) {
      setMessage(err.message);
    }
  };

  const handleDelete = async (id) => {
    const confirmed = window.confirm('האם למחוק משתמש זה?');
    if (!confirmed) return;
    try {
      await api.deleteUser(id);
      loadUsers();
      setMessage('המשתמש הוסר.');
    } catch (err) {
      setMessage(err.message);
    }
  };

  return (
    <main className="app users-app">
      <header className="page-header">
        <div>
          <p className="eyebrow">ניהול</p>
          <h1>ניהול משתמשים</h1>
          <p className="subtitle">יצירה, עדכון ומחיקה של חשבונות מערכת.</p>
        </div>
      </header>
      <section className="panel">
        <h3>הוספת משתמש</h3>
        <form className="form-grid" onSubmit={handleSubmit}>
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
          <label>
            תפקיד
            <select name="role" value={form.role} onChange={handleChange}>
              <option value="Viewer">{ROLE_LABELS.Viewer}</option>
              <option value="Editor">{ROLE_LABELS.Editor}</option>
              <option value="Admin">{ROLE_LABELS.Admin}</option>
            </select>
          </label>
          <label>
            מחלקה
            <input name="department" value={form.department} onChange={handleChange} />
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
        {loading && <p className="muted">טוען משתמשים…</p>}
        {error && <p className="error">שגיאה: {error}</p>}
        <div className="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>שם משתמש</th>
                <th>תפקיד</th>
                <th>מחלקה</th>
                <th>סטטוס</th>
                <th>פעולות</th>
              </tr>
            </thead>
            <tbody>
              {users.map((u) => (
                <tr key={u.id}>
                  <td>
                    <Link to={`/users/${u.id}`}>{u.username}</Link>
                  </td>
                  <td>{ROLE_LABELS[u.role] || u.role}</td>
                  <td>{u.department || '—'}</td>
                  <td>{u.isActive ? 'פעיל' : 'מנוטרל'}</td>
                  <td>
                    <button className="ghost danger" onClick={() => handleDelete(u.id)}>
                      מחיקה
                    </button>
                  </td>
                </tr>
              ))}
              {users.length === 0 && !loading && (
                <tr>
                  <td colSpan="5" className="muted">
                    אין משתמשים להצגה.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </section>
    </main>
  );
}
