import { Fragment, useEffect, useMemo, useState } from 'react';
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

const SORT_FIELDS = [
  { value: 'username', label: 'שם משתמש' },
  { value: 'email', label: 'דוא"ל' },
  { value: 'role', label: 'תפקיד' },
  { value: 'twoFactorEnabled', label: 'OTP' },
  { value: 'createdAt', label: 'תאריך יצירה' }
];

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
  const [selectedUserId, setSelectedUserId] = useState('');
  const [selectedUser, setSelectedUser] = useState(null);
  const [selectedLogs, setSelectedLogs] = useState([]);
  const [detailLoading, setDetailLoading] = useState(false);
  const [detailError, setDetailError] = useState('');
  const [detailMessage, setDetailMessage] = useState('');
  const [detailForm, setDetailForm] = useState({ role: 'Viewer', email: '', twoFactorEnabled: true });
  const [passwordForm, setPasswordForm] = useState({ newPassword: '', confirmPassword: '' });
  const [passwordMessage, setPasswordMessage] = useState('');
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [sortCriteria, setSortCriteria] = useState([
    { field: 'username', direction: 'asc' },
    { field: 'role', direction: 'asc' }
  ]);
  useDocumentTitle('ניהול משתמשים - מוקד המבנים העירוני');

  useEffect(() => {
    loadUsers();
  }, []);

  useEffect(() => {
    if (selectedParamId) {
      openUser(selectedParamId);
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
      if (selectedUserId) {
        const stillExists = (data || []).some((u) => String(u.id) === String(selectedUserId));
        if (!stillExists) {
          setSelectedUserId('');
          setSelectedUser(null);
          setSelectedLogs([]);
        }
      }
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

  const openUser = async (userId) => {
    const asString = String(userId);
    setSelectedUserId(asString);
    setSelectedUser(null);
    setSelectedLogs([]);
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

  const closeUser = () => {
    setSelectedUserId('');
    setSelectedUser(null);
    setSelectedLogs([]);
    setDetailLoading(false);
    setDetailError('');
    setDetailMessage('');
    setPasswordMessage('');
    setPasswordForm({ newPassword: '', confirmPassword: '' });
  };

  const toggleUser = async (userId) => {
    const asString = String(userId);
    if (selectedUserId && String(selectedUserId) === asString) {
      closeUser();
      return;
    }
    await openUser(userId);
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

  const handlePasswordSubmit = async (event) => {
    event.preventDefault();
    if (!selectedUser) return;
    if (!passwordForm.newPassword || passwordForm.newPassword.length < 6) {
      setPasswordMessage('הסיסמה חייבת להיות באורך 6 תווים לפחות.');
      return;
    }
    if (passwordForm.newPassword !== passwordForm.confirmPassword) {
      setPasswordMessage('הסיסמאות אינן תואמות.');
      return;
    }
    setPasswordMessage('');
    try {
      await api.setUserPassword(selectedUser.id, passwordForm.newPassword);
      setPasswordMessage('הסיסמה עודכנה בהצלחה.');
      setPasswordForm({ newPassword: '', confirmPassword: '' });
    } catch (err) {
      setPasswordMessage(err.message);
    }
  };

  const handleSortFieldChange = (index, value) => {
    setSortCriteria((prev) => {
      const next = [...prev];
      next.forEach((c, i) => {
        if (i !== index && c.field === value) {
          next[i] = { ...next[i], field: '' };
        }
      });
      next[index] = { ...next[index], field: value };
      return next;
    });
  };

  const handleSortDirectionChange = (index, value) => {
    setSortCriteria((prev) => {
      const next = [...prev];
      next[index] = { ...next[index], direction: value };
      return next;
    });
  };

  const sortedUsers = useMemo(() => {
    if (!users || users.length === 0) return [];
    const criteria = sortCriteria.filter((c) => c.field);
    if (criteria.length === 0) return users;

    const compare = (a, b, field, direction) => {
      let result = 0;

      if (field === 'createdAt') {
        const ad = a.createdAt ? new Date(a.createdAt).getTime() : 0;
        const bd = b.createdAt ? new Date(b.createdAt).getTime() : 0;
        result = ad - bd;
      } else if (field === 'twoFactorEnabled') {
        const av = a.twoFactorEnabled ? 1 : 0;
        const bv = b.twoFactorEnabled ? 1 : 0;
        result = av - bv;
      } else if (field === 'role') {
        const av = ROLE_LABELS[a.role] || a.role || '';
        const bv = ROLE_LABELS[b.role] || b.role || '';
        result = av.localeCompare(bv, 'he');
      } else {
        const av = (a[field] || '').toString();
        const bv = (b[field] || '').toString();
        result = av.localeCompare(bv, 'he');
      }

      return direction === 'desc' ? -result : result;
    };

    const copy = [...users];
    copy.sort((a, b) => {
      for (const c of criteria) {
        const cmp = compare(a, b, c.field, c.direction);
        if (cmp !== 0) return cmp;
      }
      return 0;
    });
    return copy;
  }, [users, sortCriteria]);

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
        <div className="list-panel full-span">
          <div className="panel-header">
            <h2>משתמשים ({users.length})</h2>
          </div>
          <section className="panel">
            <h3>מיון</h3>
            <div className="form-grid">
              {sortCriteria.map((crit, idx) => (
                <div key={idx}>
                  <label>
                    {`עדיפות ${idx + 1}`}
                    <select
                      value={crit.field}
                      onChange={(e) => handleSortFieldChange(idx, e.target.value)}
                    >
                      <option value="">ללא</option>
                      {SORT_FIELDS.map((f) => (
                        <option key={f.value} value={f.value}>
                          {f.label}
                        </option>
                      ))}
                    </select>
                  </label>
                  <label>
                    סדר
                    <select
                      value={crit.direction}
                      onChange={(e) => handleSortDirectionChange(idx, e.target.value)}
                    >
                      <option value="asc">עולה</option>
                      <option value="desc">יורד</option>
                    </select>
                  </label>
                </div>
              ))}
            </div>
          </section>
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
                {sortedUsers.map((u) => {
                  const isActive = selectedUserId && String(selectedUserId) === String(u.id);
                  return (
                    <Fragment key={u.id}>
                      <tr className={isActive ? 'active' : ''} onClick={() => toggleUser(u.id)}>
                        <td>{u.username}</td>
                        <td>{u.email || '—'}</td>
                        <td>{ROLE_LABELS[u.role] || u.role}</td>
                        <td>{u.twoFactorEnabled ? 'פעיל' : 'מנוטרל'}</td>
                        <td>{u.createdAt ? new Date(u.createdAt).toLocaleDateString('he-IL') : '—'}</td>
                        <td>
                          <button
                            type="button"
                            className="ghost"
                            onClick={(event) => {
                              event.stopPropagation();
                              openUser(u.id);
                            }}
                          >
                            עריכה
                          </button>
                          <button
                            type="button"
                            className="ghost"
                            onClick={async (event) => {
                              event.stopPropagation();
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
                      {isActive && (
                        <tr>
                          <td colSpan="6">
                            {detailLoading && <p className="muted">טוען משתמש…</p>}
                            {detailError && <p className="error">שגיאה: {detailError}</p>}
                            {!detailLoading && !selectedUser && !detailError && (
                              <p className="muted">לא נמצאו פרטים למשתמש.</p>
                            )}
                            {selectedUser && !detailLoading && (
                              <div className="details-card">
                                <div>
                                  <p className="eyebrow">חשבון</p>
                                  <h3>{selectedUser.username}</h3>
                                  <p className="subtitle">{selectedUser.email || 'ללא דוא\"ל'}</p>
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
                                    <input
                                      name="email"
                                      value={detailForm.email}
                                      onChange={handleDetailChange}
                                      type="email"
                                    />
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
                                    <button type="button" className="ghost" onClick={closeUser}>
                                      סגירה
                                    </button>
                                  </div>
                                </form>
                                {detailMessage && <p className="muted">{detailMessage}</p>}

                                <hr />
                                <form className="form-grid" onSubmit={handlePasswordSubmit}>
                                  <label className="full-span">
                                    <strong>איפוס סיסמה</strong>
                                  </label>
                                  <label>
                                    סיסמה חדשה
                                    <input
                                      type="password"
                                      name="newPassword"
                                      value={passwordForm.newPassword}
                                      onChange={(e) =>
                                        setPasswordForm((prev) => ({ ...prev, newPassword: e.target.value }))
                                      }
                                      required
                                    />
                                  </label>
                                  <label>
                                    אימות סיסמה
                                    <input
                                      type="password"
                                      name="confirmPassword"
                                      value={passwordForm.confirmPassword}
                                      onChange={(e) =>
                                        setPasswordForm((prev) => ({ ...prev, confirmPassword: e.target.value }))
                                      }
                                      required
                                    />
                                  </label>
                                  <div className="filters-actions full-span">
                                    <button type="submit" className="ghost">
                                      שמירת סיסמה חדשה
                                    </button>
                                  </div>
                                </form>
                                {passwordMessage && <p className="muted">{passwordMessage}</p>}

                                <div className="details-section">
                                  <h4>יומן פעילות</h4>
                                  {selectedLogs.length === 0 && <p className="muted">אין רשומות להצגה.</p>}
                                  {selectedLogs.length > 0 && (
                                    <ul className="log-list">
                                      {selectedLogs.map((log) => (
                                        <li key={log.id}>
                                          <span>
                                            {log.actionType} — {log.username}
                                          </span>
                                          <span className="muted">{log.createdAt}</span>
                                        </li>
                                      ))}
                                    </ul>
                                  )}
                                </div>
                              </div>
                            )}
                          </td>
                        </tr>
                      )}
                    </Fragment>
                  );
                })}
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
      </section>
    </main>
  );
}
