import { useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import api from '../api/client.js';
import useDocumentTitle from '../hooks/useDocumentTitle.js';
import { STATUS_LABEL_MAP } from '../i18n.js';
import { LAST_BUILDING_KEY } from '../constants.js';

const baseFilters = {
  buildingId: '',
  userId: '',
  actionType: '',
  street: '',
  houseNumber: '',
  nickname: '',
  status: '',
  area: '',
  startDate: '',
  endDate: ''
};

const todayIso = () => new Date().toISOString().slice(0, 10);

const buildDefaultFilters = (overrides = {}) => ({
  ...baseFilters,
  startDate: todayIso(),
  endDate: todayIso(),
  ...overrides
});

export default function LogsPage() {
  const [searchParams] = useSearchParams();
  const initialBuildingId =
    searchParams.get('buildingId') || sessionStorage.getItem(LAST_BUILDING_KEY) || '';
  const [filters, setFilters] = useState(() => buildDefaultFilters({ buildingId: initialBuildingId }));
  const [logs, setLogs] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const statusLabelMap = STATUS_LABEL_MAP;
  const dateFormatter = useMemo(
    () =>
      new Intl.DateTimeFormat('he-IL', {
        dateStyle: 'short',
        timeStyle: 'short',
        timeZone: 'Asia/Jerusalem'
      }),
    []
  );
  useDocumentTitle('יומן פעילויות - מוקד המבנים העירוני');
  const displayOrDash = (value) => (value === null || value === undefined || value === '' ? '—' : value);
  const formatDate = (value) => {
    if (!value) return '—';
    try {
      return dateFormatter.format(new Date(value));
    } catch {
      return value;
    }
  };

  useEffect(() => {
    loadLogs({ ...baseFilters, buildingId: initialBuildingId });
  }, []);

  useEffect(() => {
    const paramId = searchParams.get('buildingId');
    if (!paramId) {
      return;
    }
    setFilters((prev) => {
      if (prev.buildingId === paramId) {
        return prev;
      }
      const next = { ...prev, buildingId: paramId };
      loadLogs({ ...baseFilters, buildingId: paramId });
      return next;
    });
  }, [searchParams]);

  const loadLogs = async (appliedFilters = filters) => {
    setLoading(true);
    setError('');
    try {
      const clean = Object.fromEntries(
        Object.entries(appliedFilters).filter(([, value]) => value && value.trim() !== '')
      );
      const result = await api.fetchLogs(clean);
      setLogs(result);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleChange = (event) => {
    const { name, value } = event.target;
    setFilters((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = (event) => {
    event.preventDefault();
    loadLogs(filters);
  };

  const handleReset = () => {
    const reset = buildDefaultFilters();
    setFilters(reset);
    loadLogs(baseFilters);
  };

  return (
    <main className="app logs-app">
      <header className="page-header">
        <div>
          <p className="eyebrow">מרכז פעילות</p>
          <h1>יומן פעילויות</h1>
          <p className="subtitle">נטרו את כל השינויים שנרשמים במערכת.</p>
        </div>
      </header>
      <section className="filters-card">
        <form className="filters-grid" onSubmit={handleSubmit}>
          <label>
            <span>שם רחוב</span>
            <input name="street" value={filters.street || ''} onChange={handleChange} />
          </label>
          <label>
            <span>מספר בית</span>
            <input name="houseNumber" value={filters.houseNumber || ''} onChange={handleChange} />
          </label>
          <label>
            <span>כינוי</span>
            <input name="nickname" value={filters.nickname || ''} onChange={handleChange} />
          </label>
          <label>
            <span>סטטוס</span>
            <input name="status" value={filters.status || ''} onChange={handleChange} />
          </label>
          <label>
            <span>אזור</span>
            <input name="area" value={filters.area || ''} onChange={handleChange} />
          </label>
          <label>
            <span>משתמש</span>
            <input name="userId" value={filters.userId} onChange={handleChange} />
          </label>
          <label>
            <span>תאריך התחלה</span>
            <input type="date" name="startDate" value={filters.startDate} onChange={handleChange} />
          </label>
          <label>
            <span>תאריך סיום</span>
            <input type="date" name="endDate" value={filters.endDate} onChange={handleChange} />
          </label>
          <div className="filters-actions">
            <button type="submit" className="primary">
              חיפוש
            </button>
            <button type="button" className="ghost" onClick={handleReset}>
              איפוס
            </button>
          </div>
        </form>
        {loading && <p className="muted">טוען רישומים…</p>}
        {error && <p className="error">שגיאה: {error}</p>}
      </section>

      <section className="panel">
        <div className="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>שם רחוב</th>
                <th>מספר בית</th>
                <th>כינוי</th>
                <th>סטטוס</th>
                <th>אזור</th>
                <th>תקציר מצב</th>
                <th>משתמש</th>
                <th>תאריך</th>
              </tr>
            </thead>
            <tbody>
              {logs.map((log) => {
                const snapshot = log.snapshot || {};
                const street = snapshot.streetName || log.buildingStreet || '—';
                const houseNumber = snapshot.houseNumber || log.buildingHouseNumber || '—';
                const nickname = snapshot.buildingName || log.buildingNickname || '—';
                const neighborhood = snapshot.neighborhood || log.buildingNeighborhood || '—';
                const statusValue = snapshot.shikumStatus || log.buildingStatus || 'Unknown';
                const statusLabel = statusLabelMap[statusValue] || statusValue || '—';
                const summary = snapshot.statusSummary || log.buildingStatusSummary || log.description || '—';
                const timestamp = snapshot.statusSummaryUpdatedAt || log.createdAt;
                return (
                  <tr key={log.id}>
                    <td>{displayOrDash(street)}</td>
                    <td>{displayOrDash(houseNumber)}</td>
                    <td>{displayOrDash(nickname)}</td>
                    <td>{statusLabel}</td>
                    <td>{displayOrDash(neighborhood)}</td>
                    <td>{displayOrDash(summary)}</td>
                    <td>{displayOrDash(log.username)}</td>
                    <td>{formatDate(timestamp)}</td>
                  </tr>
                );
              })}
              {logs.length === 0 && !loading && (
                <tr>
                  <td colSpan="8" className="muted">
                    אין רשומות.
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
