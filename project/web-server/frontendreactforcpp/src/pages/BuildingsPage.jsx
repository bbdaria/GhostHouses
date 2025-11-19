import { useEffect, useMemo, useState } from 'react';
import api from '../api/client.js';
import { useAuth } from '../context/AuthContext.jsx';
import { ROLE_LABELS, STATUS_LABEL_MAP, STATUS_OPTIONS } from '../i18n.js';
import useDocumentTitle from '../hooks/useDocumentTitle.js';

const initialFilters = {
  street: '',
  houseNumber: '',
  nickname: '',
  status: '',
  area: ''
};


export default function BuildingsPage() {
  const { user } = useAuth();
  useDocumentTitle('מאגר מבנים - מוקד המבנים העירוני');
  const [filters, setFilters] = useState(initialFilters);
  const [buildings, setBuildings] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [selectedBuilding, setSelectedBuilding] = useState(null);
  const [logs, setLogs] = useState([]);
  const [logsLoading, setLogsLoading] = useState(false);
  const [detailError, setDetailError] = useState('');
  const [detailTab, setDetailTab] = useState('summary');
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [createForm, setCreateForm] = useState({
    streetName: '',
    bldNum: '',
    bldName: '',
    area: '',
    statusSummary: '',
    shikumStatusId: ''
  });
  const [editForm, setEditForm] = useState({
    bldName: '',
    area: '',
    statusSummary: '',
    shikumStatusId: ''
  });
  const [logForm, setLogForm] = useState({ actionType: '', description: '' });
  const [actionMessage, setActionMessage] = useState('');

  const canEdit = useMemo(
    () => user && (user.role === 'Editor' || user.role === 'Admin'),
    [user]
  );
  const isAdmin = user?.role === 'Admin';
  const roleLabel = ROLE_LABELS[user?.role] || user?.role;
  const statusLabelMap = STATUS_LABEL_MAP;

  useEffect(() => {
    loadBuildings(initialFilters);
  }, []);

  useEffect(() => {
    if (selectedBuilding) {
      setEditForm({
        bldName: selectedBuilding.nickname || '',
        area: selectedBuilding.area || '',
        statusSummary: selectedBuilding.statusSummary || '',
        shikumStatusId: selectedBuilding.statusId ? String(selectedBuilding.statusId) : ''
      });
    }
  }, [selectedBuilding]);

  const loadBuildings = async (appliedFilters = filters) => {
    setLoading(true);
    setError('');
    try {
      const data = await api.fetchBuildings(appliedFilters);
      setBuildings(data);
      if (selectedBuilding) {
        const stillExists = data.find((b) => b.id === selectedBuilding.id);
        if (!stillExists) {
          setSelectedBuilding(null);
          setLogs([]);
        }
      }
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const loadBuildingDetails = async (id) => {
    setDetailError('');
    try {
      const building = await api.fetchBuilding(id);
      setSelectedBuilding(building);
      setDetailTab('summary');
      await loadBuildingLogs(id);
    } catch (err) {
      setDetailError(err.message);
    }
  };

  const loadBuildingLogs = async (id) => {
    setLogsLoading(true);
    try {
      const data = await api.fetchBuildingLogs(id);
      setLogs(data);
    } catch (err) {
      setDetailError(err.message);
    } finally {
      setLogsLoading(false);
    }
  };

  const handleFilterChange = (event) => {
    const { name, value } = event.target;
    setFilters((prev) => ({ ...prev, [name]: value }));
  };

  const handleSearch = (event) => {
    event.preventDefault();
    loadBuildings(filters);
  };

  const handleReset = () => {
    setFilters(initialFilters);
    loadBuildings(initialFilters);
  };

  const handleCreateChange = (event) => {
    const { name, value } = event.target;
    setCreateForm((form) => ({ ...form, [name]: value }));
  };

  const handleCreateBuilding = async (event) => {
    event.preventDefault();
    setActionMessage('');
    try {
      const payload = {
        streetName: createForm.streetName,
        bldNum: createForm.bldNum,
        bldName: createForm.bldName,
        area: createForm.area,
        statusSummary: createForm.statusSummary
      };
      if (createForm.shikumStatusId) {
        payload.shikumStatusId = Number(createForm.shikumStatusId);
      }
      await api.createBuilding(payload);
      setCreateForm({
        streetName: '',
        bldNum: '',
        bldName: '',
        area: '',
        statusSummary: '',
        shikumStatusId: ''
      });
      setShowCreateForm(false);
      loadBuildings(filters);
      setActionMessage('המבנה נוסף בהצלחה.');
    } catch (err) {
      setActionMessage(err.message);
    }
  };

  const handleEditChange = (event) => {
    const { name, value } = event.target;
    setEditForm((form) => ({ ...form, [name]: value }));
  };

  const handleUpdateBuilding = async (event) => {
    event.preventDefault();
    if (!selectedBuilding) return;
    setActionMessage('');
    try {
      const payload = {
        bldName: editForm.bldName,
        area: editForm.area,
        statusSummary: editForm.statusSummary
      };
      if (editForm.shikumStatusId) {
        payload.shikumStatusId = Number(editForm.shikumStatusId);
      }
      const updated = await api.updateBuilding(selectedBuilding.id, payload);
      setSelectedBuilding(updated);
      loadBuildings(filters);
      setActionMessage('פרטי המבנה עודכנו.');
    } catch (err) {
      setActionMessage(err.message);
    }
  };

  const handleDeleteBuilding = async () => {
    if (!selectedBuilding) return;
    const confirmed = window.confirm('למחוק את המבנה לצמיתות?');
    if (!confirmed) return;
    try {
      await api.deleteBuilding(selectedBuilding.id, true);
      setSelectedBuilding(null);
      setLogs([]);
      loadBuildings(filters);
      setActionMessage('המבנה הוסר.');
    } catch (err) {
      setActionMessage(err.message);
    }
  };

  const handleLogChange = (event) => {
    const { name, value } = event.target;
    setLogForm((form) => ({ ...form, [name]: value }));
  };

  const handleAddLog = async (event) => {
    event.preventDefault();
    if (!selectedBuilding) return;
    try {
      await api.createBuildingLog(selectedBuilding.id, logForm);
      setLogForm({ actionType: '', description: '' });
      loadBuildingLogs(selectedBuilding.id);
      setActionMessage('נרשם לוג חדש.');
    } catch (err) {
      setActionMessage(err.message);
    }
  };

  const handleDeleteLog = async (logId) => {
    if (!selectedBuilding) return;
    const confirmed = window.confirm('למחוק רשומה זו?');
    if (!confirmed) return;
    try {
      await api.deleteBuildingLog(selectedBuilding.id, logId);
      loadBuildingLogs(selectedBuilding.id);
      setActionMessage('הרישום נמחק.');
    } catch (err) {
      setActionMessage(err.message);
    }
  };

  const statuses = useMemo(() => STATUS_OPTIONS, []);

  return (
    <main className="app buildings-app">
      <header className="page-header">
        <div>
          <p className="eyebrow">מאגר מבנים</p>
          <h1>מערכת ניהול מבנים נטושים</h1>
          <p className="subtitle">בצעו חיפושים, עדכונים ומעקב אחר לוגים עירוניים.</p>
        </div>
        <div className="health-chip">
          מחובר כ {user?.username} ({roleLabel})
        </div>
      </header>

      <section className="filters-card">
        <form className="filters-grid" onSubmit={handleSearch}>
          <label>
            <span>רחוב</span>
            <input
              type="text"
              name="street"
              value={filters.street}
              onChange={handleFilterChange}
              placeholder="לדוגמה: הרצל"
            />
          </label>
          <label>
            <span>מספר בית</span>
            <input
              type="text"
              name="houseNumber"
              value={filters.houseNumber}
              onChange={handleFilterChange}
              placeholder="לדוגמה: 12א"
            />
          </label>
          <label>
            <span>כינוי</span>
            <input
              type="text"
              name="nickname"
              value={filters.nickname}
              onChange={handleFilterChange}
              placeholder="לדוגמה: הטחנה"
            />
          </label>
          <label>
            <span>סטטוס</span>
            <select name="status" value={filters.status} onChange={handleFilterChange}>
              <option value="">כל הסטטוסים</option>
              {statuses.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
          <label>
            <span>אזור</span>
            <input
              type="text"
              name="area"
              value={filters.area}
              onChange={handleFilterChange}
              placeholder="לדוגמה: אזור תעשייה"
            />
          </label>
          <div className="filters-actions">
            <button type="submit" className="primary">
              חיפוש
            </button>
            <button type="button" onClick={handleReset} className="ghost">
              איפוס
            </button>
          </div>
        </form>
        {canEdit && (
          <button className="ghost" onClick={() => setShowCreateForm((prev) => !prev)}>
            {showCreateForm ? 'סגור טופס הוספה' : 'הוסף מבנה'}
          </button>
        )}
        {loading && <p className="muted">טוען מבנים…</p>}
        {error && <p className="error">שגיאה בטעינת מבנים: {error}</p>}
        {actionMessage && <p className="success">{actionMessage}</p>}
      </section>

      {canEdit && showCreateForm && (
        <section className="panel">
          <h3>הוספת מבנה</h3>
          <form className="form-grid" onSubmit={handleCreateBuilding}>
            <label>
              שם רחוב
              <input
                name="streetName"
                value={createForm.streetName}
                onChange={handleCreateChange}
                required
              />
            </label>
            <label>
              מספר בית
              <input
                name="bldNum"
                value={createForm.bldNum}
                onChange={handleCreateChange}
                required
              />
            </label>
            <label>
              כינוי
              <input name="bldName" value={createForm.bldName} onChange={handleCreateChange} />
            </label>
            <label>
              Status
              <select
                name="shikumStatusId"
                value={createForm.shikumStatusId}
                onChange={handleCreateChange}
              >
                <option value="">Select status</option>
                {STATUS_OPTIONS.map((option) => (
                  <option key={option.id} value={option.id}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            <label>
              אזור
              <input name="area" value={createForm.area} onChange={handleCreateChange} />
            </label>
            <label className="full-span">
              תקציר מצב
              <textarea
                name="statusSummary"
                value={createForm.statusSummary}
                onChange={handleCreateChange}
              />
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
            <h2>תוצאות ({buildings.length})</h2>
          </div>
          <div className="table-wrapper">
            <table>
              <thead>
                <tr>
                  <th>רחוב</th>
                  <th>מספר בית</th>
                  <th>כינוי</th>
                  <th>סטטוס</th>
                  <th>אזור</th>
                </tr>
              </thead>
              <tbody>
                {buildings.map((building) => {
                  const isActive = selectedBuilding && building.id === selectedBuilding.id;
                  const statusValue = building.status || 'Unknown';
                  const statusLabel = statusLabelMap[statusValue] || statusValue;
                  const statusSlug = statusValue.toLowerCase().replace(/\s+/g, '-');
                  return (
                    <tr
                      key={building.id}
                      onClick={() => loadBuildingDetails(building.id)}
                      className={isActive ? 'active' : ''}
                    >
                      <td>{building.street}</td>
                      <td>{building.houseNumber}</td>
                      <td>{building.nickname || '—'}</td>
                      <td>
                        <span className={`status status-${statusSlug}`}>{statusLabel}</span>
                      </td>
                      <td>{building.area || '—'}</td>
                    </tr>
                  );
                })}
                {buildings.length === 0 && !loading && (
                  <tr>
                    <td colSpan="5" className="muted">
                      אין מבנים שעונים על הסינון.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>

        <div className="details-panel">
          <div className="panel-header">
            <h2>פרטי מבנה</h2>
          </div>
          {detailError && <p className="error">שגיאה: {detailError}</p>}
          {!selectedBuilding && <p className="muted">בחרו מבנה להצגת הנתונים.</p>}
          {selectedBuilding && (
            <>
              <div className="tab-bar">
                <button
                  className={detailTab === 'summary' ? 'tab active' : 'tab'}
                  onClick={() => setDetailTab('summary')}
                >
                  תקציר
                </button>
                <button
                  className={detailTab === 'logs' ? 'tab active' : 'tab'}
                  onClick={() => setDetailTab('logs')}
                >
                  יומן
                </button>
                {canEdit && (
                  <button
                    className={detailTab === 'edit' ? 'tab active' : 'tab'}
                    onClick={() => setDetailTab('edit')}
                  >
                    עריכה
                  </button>
                )}
              </div>

              {detailTab === 'summary' && (
                <div className="details-card">
                  <div>
                    <p className="eyebrow">כתובת</p>
                    <h3>
                      {selectedBuilding.street} {selectedBuilding.houseNumber}
                    </h3>
                    {selectedBuilding.nickname && (
                      <p className="nickname">“{selectedBuilding.nickname}”</p>
                    )}
                  </div>
                  <dl>
                    <div>
                      <dt>סטטוס</dt>
                      <dd>{statusLabelMap[selectedBuilding.status || 'Unknown']}</dd>
                    </div>
                    <div>
                      <dt>אזור</dt>
                      <dd>{selectedBuilding.area || 'לא צוין'}</dd>
                    </div>
                    <div>
                      <dt>עודכן לאחרונה</dt>
                      <dd>{selectedBuilding.updatedAt}</dd>
                    </div>
                    <div>
                      <dt>תקציר מצב</dt>
                      <dd>{selectedBuilding.statusSummary || '—'}</dd>
                    </div>
                  </dl>
                  <div className="photos-placeholder">
                    <p>תמונות (טרם זמין)</p>
                  </div>
                  {isAdmin && (
                    <button className="danger" onClick={handleDeleteBuilding}>
                      מחיקת מבנה
                    </button>
                  )}
                </div>
              )}

              {detailTab === 'logs' && (
                <div className="details-card">
                  {logsLoading && <p className="muted">טוען יומן…</p>}
                  {!logsLoading && logs.length === 0 && <p className="muted">אין רשומות במבנה זה.</p>}
                  <ul className="log-list">
                    {logs.map((log) => (
                      <li key={log.id}>
                        <div>
                          <strong>{log.actionType}</strong>
                          <p>{log.description || '—'}</p>
                          <small>
                            {log.createdAt} • {log.username || log.userId || 'לא ידוע'}
                          </small>
                        </div>
                        {isAdmin && (
                          <button className="ghost danger" onClick={() => handleDeleteLog(log.id)}>
                            מחיקה
                          </button>
                        )}
                      </li>
                    ))}
                  </ul>
                  {canEdit && (
                    <form className="form-grid" onSubmit={handleAddLog}>
                      <label>
                        סוג פעולה
                        <input
                          name="actionType"
                          value={logForm.actionType}
                          onChange={handleLogChange}
                          required
                        />
                      </label>
                      <label className="full-span">
                        תיאור
                        <textarea
                          name="description"
                          value={logForm.description}
                          onChange={handleLogChange}
                        />
                      </label>
                      <div className="filters-actions">
                        <button type="submit" className="primary">
                          הוסף רישום
                        </button>
                      </div>
                    </form>
                  )}
                </div>
              )}

              {detailTab === 'edit' && canEdit && (
                <form className="details-card form-grid" onSubmit={handleUpdateBuilding}>
                  <label>
                    כינוי
                    <input name="bldName" value={editForm.bldName} onChange={handleEditChange} />
                  </label>
                  <label>
                    אזור
                    <input name="area" value={editForm.area} onChange={handleEditChange} />
                  </label>
                  <label>
                    סטטוס
                    <select
                      name="shikumStatusId"
                      value={editForm.shikumStatusId}
                      onChange={handleEditChange}
                    >
                      <option value="">Leave unchanged</option>
                      {STATUS_OPTIONS.map((option) => (
                        <option key={option.id} value={option.id}>
                          {option.label}
                        </option>
                      ))}
                    </select>
                  </label>
                  <label className="full-span">
                    תקציר מצב
                    <textarea
                      name="statusSummary"
                      value={editForm.statusSummary}
                      onChange={handleEditChange}
                    />
                  </label>
                  <div className="filters-actions">
                    <button type="submit" className="primary">
                      שמירת שינויים
                    </button>
                  </div>
                </form>
              )}
            </>
          )}
        </div>
      </section>
    </main>
  );
}
