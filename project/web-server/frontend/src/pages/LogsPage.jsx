import { Fragment, useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import api from '../api/client.js';
import useDocumentTitle from '../hooks/useDocumentTitle.js';
import { STATUS_LABEL_MAP, STATUS_OPTIONS, STATUS_VALUE_BY_ID } from '../i18n.js';
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

const normalizeStatusValue = (value, statusIdToValue = {}) => {
  if (value === null || value === undefined) return 'Unknown';
  if (typeof value === 'number') {
    return statusIdToValue[value] || STATUS_VALUE_BY_ID[value] || 'Unknown';
  }
  if (typeof value === 'string') {
    const numeric = Number(value);
    if (!Number.isNaN(numeric) && (statusIdToValue[numeric] || STATUS_VALUE_BY_ID[numeric])) {
      return statusIdToValue[numeric] || STATUS_VALUE_BY_ID[numeric];
    }
    return value;
  }
  return 'Unknown';
};

const baseFilters = {
  buildingId: '',
  user: '',
  streetId: '',
  houseNumber: '',
  nickname: '',
  status: '',
  bldSivug: '',
  statusSummary: '',
  startDate: '',
  endDate: ''
};

const EXCEL_LABEL_OVERRIDES = {
  'ID נכס לצורך מערכת זו בלבד': 'ID',
  'תמצית מצב': 'תמונת מצב',
  'תאריך עדכון תמצית מצב': 'תאריך שינוי',
  'ציון עמידה בסטנדרט': 'ציון',
  'פרטי מחזיקים': 'פרטי מחזיק',
  'האם הייתה צריכת מים ב־6 החודשים האחרונים': 'צריכת מים ב-6 החודשים האחרונים',
  'האם הייתה צריכת חשמל ב־6 החודשים האחרונים': 'צריכת חשמל ב-6 החודשים האחרונים',
  'אחוז המבנה שמוגדר ניזוק': 'אחוז המבנה שעומד ניזוק',
  'קוארדינטות אורך': 'קוארדינטות',
  'קוארדינטות רוחב': 'קוארדינטות'
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
  const [streets, setStreets] = useState([]);
  const [statusOptions, setStatusOptions] = useState(STATUS_OPTIONS);
  const [statusLabelMap, setStatusLabelMap] = useState(STATUS_LABEL_MAP);
  const [sivugOptions, setSivugOptions] = useState([]);
  const [expandedLogId, setExpandedLogId] = useState(null);
  const [sortConfig, setSortConfig] = useState({ field: 'date', direction: 'desc' });
  const statusIdToValue = useMemo(
    () =>
      statusOptions.reduce((acc, opt) => {
        acc[opt.id] = opt.value;
        return acc;
      }, {}),
    [statusOptions]
  );
  const statuses = statusOptions;
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
  const getSivugLabel = (value) => {
    if (value === null || value === undefined || value === '') return '—';
    const match = sivugOptions.find((option) => String(option.value) === String(value));
    return match ? match.label : String(value);
  };
  const getExcelAwareLabel = (fieldName) => {
    if (!fieldName) return '';
    const excelName = EXCEL_LABEL_OVERRIDES[fieldName];
    if (!excelName || excelName === fieldName) return fieldName;
    if (excelName === 'תאריך שינוי') return excelName;
    if (excelName === 'קוארדינטות') {
      if (fieldName.includes('אורך')) return 'קוארדינטות (אורך)';
      if (fieldName.includes('רוחב')) return 'קוארדינטות (רוחב)';
      return excelName;
    }
    return `${excelName} (${fieldName})`;
  };
  const formatDate = (value) => {
    if (!value) return '—';
    try {
      return dateFormatter.format(new Date(value));
    } catch {
      return value;
    }
  };

  const handleShowLog = (log) => {
    setExpandedLogId((prev) => (prev === log.id ? null : log.id));
  };

  const handleSortClick = (field) => {
    setSortConfig((prev) => {
      if (prev.field === field) {
        return { field, direction: prev.direction === 'asc' ? 'desc' : 'asc' };
      }
      return { field, direction: field === 'date' ? 'desc' : 'asc' };
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

  useEffect(() => {
    const loadStatusOptions = async () => {
      try {
        const options = await api.fetchSelectTable('Tbl_StatusShikum');
        const mapped = options.map((opt) => ({
          id: opt.value,
          label: opt.label,
          value: STATUS_VALUE_BY_ID[opt.value] || opt.label
        }));
        const labelMap = mapped.reduce(
          (acc, opt) => {
            acc[opt.value] = opt.label;
            return acc;
          },
          { Unknown: 'לא ידוע' }
        );
        setStatusOptions(mapped);
        setStatusLabelMap(labelMap);
      } catch {
        setStatusOptions(STATUS_OPTIONS);
        setStatusLabelMap(STATUS_LABEL_MAP);
      }
    };

    const loadStreets = async () => {
      try {
        const data = await api.fetchStreets();
        setStreets(data || []);
      } catch {
        setStreets([]);
      }
    };

    const loadSivugOptions = async () => {
      try {
        const options = await api.fetchSelectTable('Tbl_Sivug');
        setSivugOptions(options);
      } catch {
        setSivugOptions([]);
      }
    };

    loadStatusOptions();
    loadSivugOptions();
    loadStreets();
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

  const sortedLogs = useMemo(() => {
    if (!logs || logs.length === 0) return [];
    if (!sortConfig.field) return logs;

    const compareValues = (aValue, bValue, { numeric = false } = {}) => {
      const aMissing = aValue === null || aValue === undefined || aValue === '';
      const bMissing = bValue === null || bValue === undefined || bValue === '';
      if (aMissing && bMissing) return 0;
      if (aMissing) return 1;
      if (bMissing) return -1;
      if (numeric) return aValue - bValue;
      return String(aValue).localeCompare(String(bValue), 'he');
    };

    const getSortValue = (log, field) => {
      const snapshot = log.snapshot || {};
      const street = snapshot.streetName || log.buildingStreet || '';
      const houseNumber = snapshot.houseNumber || log.buildingHouseNumber || '';
      const nickname = snapshot.buildingName || log.buildingNickname || '';
      const sivugValue = snapshot.bldSivug ?? log.buildingBldSivug;
      const statusValue = normalizeStatusValue(
        snapshot.shikumStatus || log.buildingStatus || 'Unknown',
        statusIdToValue
      );
      const statusLabel = statusLabelMap[statusValue] || statusValue || '';
      const summary = snapshot.statusSummary || log.buildingStatusSummary || log.description || '';
      const user = log.username || '';
      const dateValue = log.createdAt ? new Date(log.createdAt).getTime() : null;

      switch (field) {
        case 'street':
          return street;
        case 'houseNumber':
          return houseNumber;
        case 'nickname':
          return nickname;
        case 'status':
          return statusLabel;
        case 'bldSivug':
          return getSivugLabel(sivugValue);
        case 'summary':
          return summary;
        case 'user':
          return user;
        case 'date':
          return dateValue;
        default:
          return '';
      }
    };

    const copy = [...logs];
    copy.sort((a, b) => {
      const aValue = getSortValue(a, sortConfig.field);
      const bValue = getSortValue(b, sortConfig.field);
      const cmp = compareValues(aValue, bValue, { numeric: sortConfig.field === 'date' });
      if (cmp !== 0) return sortConfig.direction === 'desc' ? -cmp : cmp;
      if (sortConfig.field !== 'date') {
        const dateCmp = compareValues(getSortValue(a, 'date'), getSortValue(b, 'date'), {
          numeric: true
        });
        if (dateCmp !== 0) return -dateCmp;
      }
      if (sortConfig.field !== 'street') {
        const streetCmp = compareValues(getSortValue(a, 'street'), getSortValue(b, 'street'));
        if (streetCmp !== 0) return streetCmp;
      }
      return 0;
    });
    return copy;
  }, [logs, sortConfig, statusIdToValue, statusLabelMap, sivugOptions]);

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
            <select name="streetId" value={filters.streetId || ''} onChange={handleChange}>
              <option value="">בחר רחוב</option>
              {streets.map((street) => (
                <option key={street.streetId} value={street.streetId}>
                  {street.name}
                </option>
              ))}
            </select>
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
            <span>סיווג</span>
            <select name="bldSivug" value={filters.bldSivug || ''} onChange={handleChange}>
              <option value="">בחר סיווג</option>
              {sivugOptions.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
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
                {LOG_TABLE_COLUMNS.map((col) => {
                  if (col.key === 'actions') {
                    return <th key={col.key}>{col.label}</th>;
                  }
                  return (
                    <th key={col.key} aria-sort={getAriaSort(col.key)}>
                      <button
                        type="button"
                        className="sort-button"
                        onClick={() => handleSortClick(col.key)}
                      >
                        {col.label}
                        <span className="sort-indicator" aria-hidden="true">
                          {getSortIndicator(col.key)}
                        </span>
                      </button>
                    </th>
                  );
                })}
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
              {sortedLogs.map((log) => {
                const snapshot = log.snapshot || {};
                const changeEntries = Array.isArray(snapshot.changes) ? snapshot.changes : [];
                const snapshotFields = Array.isArray(snapshot.fields) ? snapshot.fields : [];
                const street = snapshot.streetName || log.buildingStreet || '—';
                const houseNumber = snapshot.houseNumber || log.buildingHouseNumber || '—';
                const nickname = snapshot.buildingName || log.buildingNickname || '—';
                const sivugValue = snapshot.bldSivug ?? log.buildingBldSivug;
                const statusValue = normalizeStatusValue(
                  snapshot.shikumStatus || log.buildingStatus || 'Unknown',
                  statusIdToValue
                );
                const statusLabel = statusLabelMap[statusValue] || statusValue || '—';
                const summary = snapshot.statusSummary ?? log.buildingStatusSummary ?? '';
                const timestamp = log.createdAt;
                const isExpanded = expandedLogId === log.id;
                const fieldOrder = new Map(
                  snapshotFields.map((field, index) => [(field.columnName || '').toLowerCase(), index])
                );
                const sortedChanges = [...changeEntries].sort((a, b) => {
                  const aKey = String(a.columnName || '').toLowerCase();
                  const bKey = String(b.columnName || '').toLowerCase();
                  const aOrder = fieldOrder.has(aKey) ? fieldOrder.get(aKey) : Number.MAX_SAFE_INTEGER;
                  const bOrder = fieldOrder.has(bKey) ? fieldOrder.get(bKey) : Number.MAX_SAFE_INTEGER;
                  return aOrder - bOrder;
                });
                const row = {
                  street: displayOrDash(street),
                  houseNumber: displayOrDash(houseNumber),
                  nickname: displayOrDash(nickname),
                  status: statusLabel,
                  bldSivug: getSivugLabel(sivugValue),
                  summary: displayOrDash(summary),
                  user: displayOrDash(log.username),
                  date: formatDate(timestamp),
                      actions: (
                    <>
                      <button
                        type="button"
                        className="ghost"
                        onClick={(event) => {
                          event.stopPropagation();
                          handleShowLog(log);
                        }}
                      >
                        הצג
                      </button>
                      <button
                        type="button"
                        className="danger"
                        aria-label={`מחק לוג ${log.id}`}
                        onClick={(event) => {
                          event.stopPropagation();
                          handleDeleteLog(log.id);
                        }}
                      >
                        מחק
                      </button>
                    </>
                  )
                };
                return (
                  <Fragment key={log.id}>
                    <tr
                      key={log.id}
                      className={isExpanded ? 'active' : ''}
                      onClick={() => handleShowLog(log)}
                    >
                      {LOG_TABLE_COLUMNS.map((col) => (
                        <td key={col.key}>{row[col.key]}</td>
                      ))}
                    </tr>
                    {isExpanded && (
                      <tr>
                        <td colSpan={LOG_TABLE_COLUMNS.length}>
                          <div className="details-card">
                            <div className="details-section">
                              <div className="table-wrapper">
                                <table>
                                  <thead>
                                    <tr>
                                      <th>ערכים שהשתנו</th>
                                      <th>ישן</th>
                                      <th>חדש</th>
                                    </tr>
                                  </thead>
                                  <tbody>
                                    {sortedChanges.length > 0 ? (
                                      sortedChanges.map((change, idx) => (
                                        <tr key={`${change.columnName || 'change'}-${idx}`}>
                                          <td>{getExcelAwareLabel(change.fieldName || change.columnName)}</td>
                                          <td>{displayOrDash(change.oldValue)}</td>
                                          <td>{displayOrDash(change.newValue)}</td>
                                        </tr>
                                      ))
                                    ) : (
                                      <tr>
                                        <td colSpan="3" className="muted">
                                          אין נתוני שינוי.
                                        </td>
                                      </tr>
                                    )}
                                  </tbody>
                                </table>
                              </div>
                            </div>
                          </div>
                        </td>
                      </tr>
                    )}
                  </Fragment>
                );
              })}
              {sortedLogs.length === 0 && !loading && (
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
