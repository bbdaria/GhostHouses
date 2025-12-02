import { useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import api from '../api/client.js';
import useDocumentTitle from '../hooks/useDocumentTitle.js';
import { STATUS_LABEL_MAP, STATUS_OPTIONS } from '../i18n.js';
import {
  BUILDING_FIELD_LABELS,
  BUILDING_FIELD_PLACEHOLDERS,
  LOG_TABLE_COLUMNS,
  STATUS_SELECT_PLACEHOLDER
} from '../constants.js';

const todayIso = () =>
  new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Asia/Jerusalem',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  })
    .format(new Date())
    .replace(/\//g, '-');

const STATUS_ID_TO_VALUE = STATUS_OPTIONS.reduce((acc, option) => {
  acc[option.id] = option.value;
  return acc;
}, {});

const normalizeStatusValue = (value) => {
  if (value === null || value === undefined) return 'Unknown';
  if (typeof value === 'number') {
    return STATUS_ID_TO_VALUE[value] || 'Unknown';
  }
  if (typeof value === 'string') {
    const numeric = Number(value);
    if (!Number.isNaN(numeric) && STATUS_ID_TO_VALUE[numeric]) {
      return STATUS_ID_TO_VALUE[numeric];
    }
    return value;
  }
  return 'Unknown';
};

const baseFilters = {
  buildingId: '',
  userId: '',
  user: '',
  actionType: '',
  street: '',
  houseNumber: '',
  nickname: '',
  status: '',
  area: '',
  statusSummary: '',
  startDate: '',
  endDate: ''
};

const buildDefaultFilters = (overrides = {}) => ({
  ...baseFilters,
  startDate: todayIso(),
  endDate: todayIso(),
  ...overrides
});

const expandDateRange = (filters) => {
  const next = { ...filters };
  if (filters.startDate && filters.endDate && filters.startDate === filters.endDate) {
    const start = new Date(`${filters.startDate}T00:00:00`).toISOString();
    const end = new Date(`${filters.endDate}T23:59:59.999`).toISOString();
    next.startDate = start;
    next.endDate = end;
  } else {
    if (filters.startDate) {
      next.startDate = new Date(`${filters.startDate}T00:00:00`).toISOString();
    }
    if (filters.endDate) {
      next.endDate = new Date(`${filters.endDate}T23:59:59.999`).toISOString();
    }
  }
  return next;
};

export default function LogsPage() {
  const [searchParams] = useSearchParams();
  const initialBuildingId = searchParams.get('buildingId') || '';
  const [filters, setFilters] = useState(() => buildDefaultFilters({ buildingId: initialBuildingId }));
  const [logs, setLogs] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const statusLabelMap = STATUS_LABEL_MAP;
  const statuses = STATUS_OPTIONS;
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
    loadLogs(buildDefaultFilters({ buildingId: initialBuildingId }));
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
      const expanded = expandDateRange(appliedFilters);
      const clean = Object.fromEntries(
        Object.entries(expanded).filter(([, value]) => value && value.trim() !== '')
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

  const handleDeleteLog = async (logId) => {
    try {
      await api.deleteBuildingLog(logId);
      await loadLogs(filters);
    } catch (err) {
      setError(err.message);
    }
  };

  const handleReset = () => {
    const reset = buildDefaultFilters({ buildingId: '' });
    setFilters(reset);
    loadLogs(reset);
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
            <span>{BUILDING_FIELD_LABELS.street}</span>
            <input
              name="street"
              value={filters.street || ''}
              onChange={handleChange}
              placeholder={BUILDING_FIELD_PLACEHOLDERS.street}
            />
          </label>
          <label>
            <span>{BUILDING_FIELD_LABELS.houseNumber}</span>
            <input
              name="houseNumber"
              value={filters.houseNumber || ''}
              onChange={handleChange}
              placeholder={BUILDING_FIELD_PLACEHOLDERS.houseNumber}
            />
          </label>
          <label>
            <span>{BUILDING_FIELD_LABELS.nickname}</span>
            <input
              name="nickname"
              value={filters.nickname || ''}
              onChange={handleChange}
              placeholder={BUILDING_FIELD_PLACEHOLDERS.nickname}
            />
          </label>
          <label>
            <span>{BUILDING_FIELD_LABELS.status}</span>
            <select name="status" value={filters.status || ''} onChange={handleChange}>
              <option value="">{STATUS_SELECT_PLACEHOLDER}</option>
              {statuses.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
          <label>
            <span>{BUILDING_FIELD_LABELS.area}</span>
            <input
              name="area"
              value={filters.area || ''}
              onChange={handleChange}
              placeholder={BUILDING_FIELD_PLACEHOLDERS.area}
            />
          </label>
          <label className="full-span">
            <span>{BUILDING_FIELD_LABELS.statusSummary}</span>
            <input
              name="statusSummary"
              value={filters.statusSummary || ''}
              onChange={handleChange}
              placeholder={BUILDING_FIELD_PLACEHOLDERS.statusSummary}
            />
          </label>
          <label>
            <span>משתמש</span>
            <input name="user" value={filters.user} onChange={handleChange} placeholder="שם משתמש" />
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
                {LOG_TABLE_COLUMNS.map((col) => (
                  <th key={col.key}>{col.label}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {loading && (
                <tr>
                  <td colSpan={LOG_TABLE_COLUMNS.length} className="table-loading">
                    <span className="spinner" aria-label="טוען נתונים" />
                  </td>
                </tr>
              )}
              {logs.map((log) => {
                const snapshot = log.snapshot || {};
                const street = snapshot.streetName || log.buildingStreet || '—';
                const houseNumber = snapshot.houseNumber || log.buildingHouseNumber || '—';
                const nickname = snapshot.buildingName || log.buildingNickname || '—';
                const neighborhood = snapshot.neighborhood || log.buildingNeighborhood || '—';
                const statusValue = normalizeStatusValue(snapshot.shikumStatus || log.buildingStatus || 'Unknown');
                const statusLabel = statusLabelMap[statusValue] || statusValue || '—';
                const summary = snapshot.statusSummary || log.buildingStatusSummary || log.description || '—';
                const timestamp = snapshot.statusSummaryUpdatedAt || log.createdAt;
                const row = {
                  street: displayOrDash(street),
                  houseNumber: displayOrDash(houseNumber),
                  nickname: displayOrDash(nickname),
                  status: statusLabel,
                  area: displayOrDash(neighborhood),
                  summary: displayOrDash(summary),
                  user: displayOrDash(log.username),
                  date: formatDate(timestamp),
                  actions: (
                    <button
                      type="button"
                      className="danger"
                      aria-label={`מחק לוג ${log.id}`}
                      onClick={() => handleDeleteLog(log.id)}
                    >
                      מחק
                    </button>
                  )
                };
                return (
                  <tr key={log.id}>
                    {LOG_TABLE_COLUMNS.map((col) => (
                      <td key={col.key}>{row[col.key]}</td>
                    ))}
                  </tr>
                );
              })}
              {logs.length === 0 && !loading && (
                <tr>
                  {LOG_TABLE_COLUMNS.map((col) => (
                    <td key={col.key} />
                  ))}
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </section>
    </main>
  );
}
