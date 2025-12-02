import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import api from '../api/client.js';
import { ROLE_LABELS } from '../i18n.js';
import useDocumentTitle from '../hooks/useDocumentTitle.js';

const todayIso = () => new Date().toISOString().slice(0, 10);
const todayIsoIsrael = () =>
  new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Asia/Jerusalem',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  })
    .format(new Date())
    .replace(/\//g, '-');

export default function UsersListPage() {
  const { id: selectedParamId } = useParams();
  const [filters, setFilters] = useState({
    username: '',
    email: '',
    role: '',
    startDate: todayIsoIsrael(),
    endDate: todayIsoIsrael()
  });
  const [users, setUsers] = useState([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [createForm, setCreateForm] = useState({
    username: '',
    email: '',
    password: '',
    role: 'Viewer'
  });
  const [message, setMessage] = useState('');
  const [selectedUser, setSelectedUser] = useState(null);
  const [selectedLogs, setSelectedLogs] = useState([]);
  const [detailLoading, setDetailLoading] = useState(false);
  const [detailError, setDetailError] = useState('');
  const [detailMessage, setDetailMessage] = useState('');
  const [detailForm, setDetailForm] = useState({ role: 'Viewer', email: '', twoFactorEnabled: true });
  const [showCreateForm, setShowCreateForm] = useState(false);
  useDocumentTitle('ניהול משתמשים - מוקד המבנים העירוני');

  useEffect(() => {
    loadUsers();
  }, []);

  useEffect(() => {
    if (selectedParamId) {
      handleSelectUser(selectedParamId);
    }
  }, [selectedParamId]);

  const loadUsers = async () => {
    setLoading(true);
    setError('');
    try {
      const query = {};
      if (filters.username) query.username = filters.username;
      if (filters.email) query.email = filters.email;
      if (filters.role) query.role = filters.role;
      if (filters.startDate && filters.endDate && filters.startDate === filters.endDate) {
        query.from = `${filters.startDate}T00:00:00Z`;
        query.to = `${filters.endDate}T23:59:59.999Z`;
      } else {
        if (filters.startDate) query.from = `${filters.startDate}T00:00:00Z`;
        if (filters.endDate) query.to = `${filters.endDate}T23:59:59.999Z`;
      }
      const data = await api.fetchUsers(query);
      setUsers(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleChange = (event) => {
    const { name, value } = event.target;
    setCreateForm((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    setMessage('');
    try {
      await api.createUser(createForm);
      setCreateForm({
        username: '',
        email: '',
        password: '',
        role: 'Viewer'
      });
      loadUsers();
      setMessage('המשתמש נוצר בהצלחה.');
    } catch (err) {
      setMessage(err.message);
    }
  };

  const handleSelectUser = async (userId) => {
    setDetailLoading(true);
    setDetailError('');
    setDetailMessage('');
    try {
      const data = await api.fetchUser(userId);
      setSelectedUser(data);
      setDetailForm({ role: data.role, email: data.email || '', twoFactorEnabled: data.twoFactorEnabled });
      const logs = await api.fetchLogs({ userId: data.id });
      setSelectedLogs(logs);
    } catch (err) {
      setDetailError(err.message);
    } finally {
      setDetailLoading(false);
    }
  };

  const handleDetailChange = (event) => {
    const { name, value, type, checked } = event.target;
    setDetailForm((prev) => ({ ...prev, [name]: type === 'checkbox' ? checked : value }));
  };

  const handleFilterChange = (event) => {
    const { name, value } = event.target;
    setFilters((prev) => ({ ...prev, [name]: value }));
  };

  const handleFilterSubmit = (event) => {
    event.preventDefault();
    loadUsers();
  };

  const handleFilterReset = () => {
    setFilters({
      username: '',
      email: '',
      role: '',
      startDate: todayIsoIsrael(),
      endDate: todayIsoIsrael()
    });
    loadUsers();
  };

  const handleUpdateUser = async (event) => {
    event.preventDefault();
    if (!selectedUser) return;
    setDetailMessage('');
    setDetailError('');
    try {
      const updated = await api.updateUser(selectedUser.id, detailForm);
      setSelectedUser(updated);
      setDetailMessage('המשתמש עודכן.');
      loadUsers();
    } catch (err) {
      setDetailError(err.message);
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
      <section className="filters-card">
        <form className="filters-grid" onSubmit={handleFilterSubmit}>
          <label>
            <span>שם משתמש</span>
            <input name="username" value={filters.username} onChange={handleFilterChange} />
          </label>
          <label>
            <span>דוא"ל</span>
            <input name="email" value={filters.email} onChange={handleFilterChange} />
          </label>
          <label>
            <span>תפקיד</span>
            <select name="role" value={filters.role} onChange={handleFilterChange}>
              <option value="">כל התפקידים</option>
              <option value="Viewer">{ROLE_LABELS.Viewer}</option>
              <option value="Editor">{ROLE_LABELS.Editor}</option>
              <option value="Admin">{ROLE_LABELS.Admin}</option>
            </select>
          </label>
          <label>
            <span>תאריך התחלה</span>
            <input type="date" name="startDate" value={filters.startDate} onChange={handleFilterChange} />
          </label>
          <label>
            <span>תאריך סיום</span>
            <input type="date" name="endDate" value={filters.endDate} onChange={handleFilterChange} />
          </label>
          <div className="filters-actions">
            <button type="submit" className="primary">
              חיפוש
            </button>
            <button type="button" className="ghost" onClick={handleFilterReset}>
              איפוס
            </button>
          </div>
        </form>
        <button className="ghost" onClick={() => setShowCreateForm((prev) => !prev)}>
          {showCreateForm ? 'סגור טופס הוספה' : 'הוסף משתמש'}
        </button>
        {loading && <p className="muted">טוען משתמשים…</p>}
        {error && <p className="error">שגיאה: {error}</p>}
        {message && <p className="success">{message}</p>}
      </section>

      {showCreateForm && (
        <section className="panel">
          <h3>הוספת משתמש</h3>
          <form className="form-grid" onSubmit={handleSubmit}>
            <label>
              שם משתמש
              <input name="username" value={createForm.username} onChange={handleChange} required />
            </label>
            <label>
              דוא"ל
              <input
                type="email"
                name="email"
                value={createForm.email}
                onChange={handleChange}
                required
              />
            </label>
            <label>
              סיסמה
              <input
                type="password"
                name="password"
                value={createForm.password}
                onChange={handleChange}
                required
              />
            </label>
            <label>
              תפקיד
              <select name="role" value={createForm.role} onChange={handleChange}>
                <option value="Viewer">{ROLE_LABELS.Viewer}</option>
                <option value="Editor">{ROLE_LABELS.Editor}</option>
                <option value="Admin">{ROLE_LABELS.Admin}</option>
              </select>
            </label>
            <div className="filters-actions">
              <button type="submit" className="primary">
                שמירה
              </button>
            </div>
          </form>
        </section>
      )}

      <section className="content-layout">
        <div className="list-panel">
          <div className="panel-header">
            <h2>משתמשים ({users.length})</h2>
          </div>
          <div className="table-wrapper">
            <table>
              <thead>
                <tr>
                  <th>שם משתמש</th>
                  <th>דוא"ל</th>
                  <th>תפקיד</th>
                  <th>OTP</th>
                  <th>תאריך יצירה</th>
                  <th>פעולות</th>
                </tr>
              </thead>
              <tbody>
                {loading && (
                  <tr>
                    <td colSpan="6" className="table-loading">
                      <span className="spinner" aria-label="טוען משתמשים" />
                    </td>
                  </tr>
                )}
                {users.map((u) => (
                  <tr key={u.id} className={selectedUser && selectedUser.id === u.id ? 'active' : ''}>
                    <td>
                      <button type="button" className="ghost" onClick={() => handleSelectUser(u.id)}>
                        {u.username}
                      </button>
                    </td>
                    <td>{u.email || '—'}</td>
                    <td>{ROLE_LABELS[u.role] || u.role}</td>
                    <td>{u.twoFactorEnabled ? 'פעיל' : 'מנוטרל'}</td>
                    <td>{u.createdAt ? new Date(u.createdAt).toLocaleDateString('he-IL') : '—'}</td>
                    <td>
                      <button type="button" className="ghost" onClick={() => handleSelectUser(u.id)}>
                        עריכה
                      </button>
                      <button
                        type="button"
                        className="ghost"
                        onClick={async () => {
                          try {
                            await api.resetUserTwoFactor(u.id);
                            setMessage('קוד ה-OTP אופס עבור המשתמש.');
                          } catch (err) {
                            setMessage(err.message);
                          }
                        }}
                      >
                        איפוס OTP
                      </button>
                    </td>
                  </tr>
                ))}
                {users.length === 0 && !loading && (
                  <tr>
                    <td colSpan="6" className="muted">
                      אין משתמשים להצגה.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>

        <div className="details-panel">
          <div className="panel-header">
            <h2>פרטי משתמש</h2>
          </div>
          {detailLoading && <p className="muted">טוען משתמש…</p>}
          {detailError && <p className="error">שגיאה: {detailError}</p>}
          {!detailLoading && !selectedUser && !detailError && <p className="muted">בחר משתמש לעריכה.</p>}
          {selectedUser && !detailLoading && (
            <div className="details-card">
              <div>
                <p className="eyebrow">חשבון</p>
                <h3>{selectedUser.username}</h3>
                <p className="subtitle">{selectedUser.email || 'ללא דוא"ל'}</p>
              </div>
              <form className="form-grid" onSubmit={handleUpdateUser}>
                <label>
                  תפקיד
                  <select name="role" value={detailForm.role} onChange={handleDetailChange}>
                    <option value="Viewer">{ROLE_LABELS.Viewer}</option>
                    <option value="Editor">{ROLE_LABELS.Editor}</option>
                    <option value="Admin">{ROLE_LABELS.Admin}</option>
                  </select>
                </label>
                <label>
                  דוא"ל
                  <input name="email" value={detailForm.email} onChange={handleDetailChange} type="email" />
                </label>
                <label className="checkbox">
                  <input
                    type="checkbox"
                    name="twoFactorEnabled"
                    checked={detailForm.twoFactorEnabled}
                    onChange={handleDetailChange}
                  />
                  דרוש OTP
                </label>
                <div className="filters-actions">
                  <button type="submit" className="primary">
                    שמירה
                  </button>
                </div>
              </form>
              {detailMessage && <p className="muted">{detailMessage}</p>}
            </div>
          )}
        </div>
      </section>
    </main>
  );
}
