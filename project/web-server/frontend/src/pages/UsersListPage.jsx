import { Fragment, useEffect, useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import api from '../api/client.js';
import { ROLE_LABELS } from '../i18n.js';
import useDocumentTitle from '../hooks/useDocumentTitle.js';
import { formatDate, formatDateTime } from '../utils/formatDate.js';

export default function UsersListPage() {
  const { id: selectedParamId } = useParams();
  const [filters, setFilters] = useState({
    username: '',
    email: '',
    role: '',
    startDate: '',
    endDate: ''
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
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showDetailModal, setShowDetailModal] = useState(false);
  const [userModalMode, setUserModalMode] = useState('view');
  const [sortConfig, setSortConfig] = useState({ field: 'username', direction: 'asc' });
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
      setShowCreateModal(false);
    } catch (err) {
      setMessage(err.message);
    }
  };

  const openUser = async (userId, mode = 'view') => {
    const asString = String(userId);
    setSelectedUserId(asString);
    setSelectedUser(null);
    setSelectedLogs([]);
    setDetailLoading(true);
    setDetailError('');
    setDetailMessage('');
    setUserModalMode(mode);
    setShowDetailModal(true);
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
    setUserModalMode('view');
    setShowDetailModal(false);
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
      startDate: '',
      endDate: ''
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

  const openCreateModal = () => {
    setMessage('');
    setShowCreateModal(true);
  };

  const closeCreateModal = () => {
    setMessage('');
    setCreateForm({
      username: '',
      email: '',
      password: '',
      role: 'Viewer'
    });
    setShowCreateModal(false);
  };

  const handleSortClick = (field) => {
    setSortConfig((prev) => {
      if (prev.field === field) {
        return { field, direction: prev.direction === 'asc' ? 'desc' : 'asc' };
      }
      return { field, direction: 'asc' };
    });
  };

  const getSortIndicator = (field) => {
    if (sortConfig.field !== field) return '';
    return sortConfig.direction === 'asc' ? '∧' : '∨';
  };

  const getAriaSort = (field) => {
    if (sortConfig.field !== field) return 'none';
    return sortConfig.direction === 'asc' ? 'ascending' : 'descending';
  };

  const sortedUsers = useMemo(() => {
    if (!users || users.length === 0) return [];
    if (!sortConfig.field) return users;

    const compareValues = (aValue, bValue, { numeric = false } = {}) => {
      const aMissing = aValue === null || aValue === undefined || aValue === '';
      const bMissing = bValue === null || bValue === undefined || bValue === '';
      if (aMissing && bMissing) return 0;
      if (aMissing) return 1;
      if (bMissing) return -1;
      if (numeric) return aValue - bValue;
      return String(aValue).localeCompare(String(bValue), 'he');
    };

    const getSortValue = (user, field) => {
      switch (field) {
        case 'createdAt':
          return user.createdAt ? new Date(user.createdAt).getTime() : null;
        case 'twoFactorEnabled':
          return user.twoFactorEnabled ? 1 : 0;
        case 'role':
          return ROLE_LABELS[user.role] || user.role || '';
        default:
          return user[field] || '';
      }
    };

    const copy = [...users];
    copy.sort((a, b) => {
      const aValue = getSortValue(a, sortConfig.field);
      const bValue = getSortValue(b, sortConfig.field);
      const cmp = compareValues(aValue, bValue, {
        numeric: sortConfig.field === 'createdAt' || sortConfig.field === 'twoFactorEnabled'
      });
      if (cmp !== 0) return sortConfig.direction === 'desc' ? -cmp : cmp;
      if (sortConfig.field !== 'username') {
        const nameCmp = compareValues(getSortValue(a, 'username'), getSortValue(b, 'username'));
        if (nameCmp !== 0) return nameCmp;
      }
      if (sortConfig.field !== 'role') {
        const roleCmp = compareValues(getSortValue(a, 'role'), getSortValue(b, 'role'));
        if (roleCmp !== 0) return roleCmp;
      }
      return 0;
    });
    return copy;
  }, [users, sortConfig]);

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
            <input type="date" name="startDate" value={filters.startDate} lang="he-IL" onChange={handleFilterChange} />
          </label>
          <label>
            <span>תאריך סיום</span>
            <input type="date" name="endDate" value={filters.endDate} lang="he-IL" onChange={handleFilterChange} />
          </label>
          <div className="filters-actions full-span align-right">
            <button type="submit" className="primary">
              חיפוש
            </button>
            <button type="button" className="ghost" onClick={handleFilterReset}>
              איפוס
            </button>
            <button type="button" className="ghost" onClick={openCreateModal}>
              הוסף משתמש
            </button>
          </div>
        </form>
        {loading && <p className="muted">טוען משתמשים…</p>}
        {error && <p className="error">שגיאה: {error}</p>}
        {message && !showCreateModal && <p className="success">{message}</p>}
      </section>

      <section className="content-layout">
        <div className="list-panel full-span">
          <div className="panel-header">
            <h2>משתמשים ({users.length})</h2>
          </div>
          <div className="table-wrapper">
            <table>
              <thead>
                <tr>
                  <th aria-sort={getAriaSort('username')}>
                    <button type="button" className="sort-button" onClick={() => handleSortClick('username')}>
                      שם משתמש
                      <span className="sort-indicator" aria-hidden="true">
                        {getSortIndicator('username')}
                      </span>
                    </button>
                  </th>
                  <th aria-sort={getAriaSort('email')}>
                    <button type="button" className="sort-button" onClick={() => handleSortClick('email')}>
                      דוא"ל
                      <span className="sort-indicator" aria-hidden="true">
                        {getSortIndicator('email')}
                      </span>
                    </button>
                  </th>
                  <th aria-sort={getAriaSort('role')}>
                    <button type="button" className="sort-button" onClick={() => handleSortClick('role')}>
                      תפקיד
                      <span className="sort-indicator" aria-hidden="true">
                        {getSortIndicator('role')}
                      </span>
                    </button>
                  </th>
                  <th aria-sort={getAriaSort('twoFactorEnabled')}>
                    <button
                      type="button"
                      className="sort-button"
                      onClick={() => handleSortClick('twoFactorEnabled')}
                    >
                      OTP
                      <span className="sort-indicator" aria-hidden="true">
                        {getSortIndicator('twoFactorEnabled')}
                      </span>
                    </button>
                  </th>
                  <th aria-sort={getAriaSort('createdAt')}>
                    <button type="button" className="sort-button" onClick={() => handleSortClick('createdAt')}>
                      תאריך יצירה
                      <span className="sort-indicator" aria-hidden="true">
                        {getSortIndicator('createdAt')}
                      </span>
                    </button>
                  </th>
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
                      <tr className={isActive ? 'active' : ''} onClick={() => openUser(u.id, 'view')}>
                        <td>{u.username}</td>
                        <td>{u.email || '—'}</td>
                        <td>{ROLE_LABELS[u.role] || u.role}</td>
                        <td>{u.twoFactorEnabled ? 'פעיל' : 'מנוטרל'}</td>
                        <td>{u.createdAt ? formatDate(u.createdAt) : '—'}</td>
                        <td className="table-actions">
                          <button
                            type="button"
                            className="ghost"
                            onClick={(event) => {
                              event.stopPropagation();
                              openUser(u.id, 'edit');
                            }}
                          >
                            עריכה
                          </button>
                          <button
                            type="button"
                            className="ghost"
                            onClick={async (event) => {
                              event.stopPropagation();
                              setMessage('');
                              try {
                                await api.resetUserTwoFactor(u.id);
                                setMessage('קוד ה-OTP אופס עבור המשתמש.');
                                loadUsers();
                              } catch (err) {
                                setMessage(err.message);
                              }
                            }}
                          >
                            איפוס OTP
                          </button>
                        </td>
                      </tr>
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
      {showCreateModal && (
        <div className="modal-overlay" onClick={closeCreateModal}>
          <div className="modal-window" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <h3>הוספת משתמש</h3>
              <button type="button" className="modal-close" onClick={closeCreateModal}>
                ✕
              </button>
            </div>
            <div className="modal-body">
              <form className="form-grid" id="user-create-form" onSubmit={handleSubmit}>
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
              </form>
              {message && <p className="success">{message}</p>}
            </div>
            <div className="modal-footer">
              <div className="footer-actions">
                <button type="submit" className="primary" form="user-create-form">
                  שמירה
                </button>
                <button type="button" className="ghost" onClick={closeCreateModal}>
                  סגירה
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {showDetailModal && (
        <div className="modal-overlay" onClick={closeUser}>
          <div className="modal-window modal-large" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <h3>{userModalMode === 'view' ? 'פרטי משתמש' : 'עריכת משתמש'}</h3>
              <button type="button" className="modal-close" onClick={closeUser}>
                ✕
              </button>
            </div>
            <div className="modal-body">
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

                  {userModalMode === 'view' ? (
                    <div className="details-section">
                      <p>
                        <strong>תפקיד:</strong> {ROLE_LABELS[selectedUser.role] || selectedUser.role}
                      </p>
                      <p>
                        <strong>OTP:</strong> {selectedUser.twoFactorEnabled ? 'פעיל' : 'מנוטרל'}
                      </p>
                      <p>
                        <strong>תאריך יצירה:</strong>{' '}
                        {selectedUser.createdAt ? formatDate(selectedUser.createdAt) : '—'}
                      </p>
                    </div>
                  ) : (
                    <>
                      <form className="form-grid" id="user-edit-form" onSubmit={handleUpdateUser}>
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
                    </>
                  )}

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
                            <span className="muted">{formatDateTime(log.createdAt)}</span>
                          </li>
                        ))}
                      </ul>
                    )}
                  </div>
                </div>
              )}
            </div>
            <div className="modal-footer">
              <div className="footer-actions">
                {userModalMode === 'view' ? (
                  <>
                    <button type="button" className="primary" onClick={() => setUserModalMode('edit')}>
                      עריכה
                    </button>
                    <button type="button" className="ghost" onClick={closeUser}>
                      סגירה
                    </button>
                  </>
                ) : (
                  <>
                    <button type="submit" className="primary" form="user-edit-form">
                      שמירה
                    </button>
                    <button type="button" className="ghost" onClick={closeUser}>
                      סגירה
                    </button>
                    <button
                      type="button"
                      className="ghost"
                      onClick={async () => {
                        try {
                          await api.resetUserTwoFactor(selectedUser.id);
                          setDetailMessage('קוד ה-OTP אופס עבור המשתמש.');
                        } catch (err) {
                          setDetailError(err.message);
                        }
                      }}
                    >
                      איפוס OTP
                    </button>
                  </>
                )}
              </div>
            </div>
          </div>
        </div>
      )}
    </main>
  );
}
