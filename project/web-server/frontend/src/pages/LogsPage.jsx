import { useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import api from '../api/client.js';
import useDocumentTitle from '../hooks/useDocumentTitle.js';
import { formatDateTime } from '../utils/formatDate.js';
import { STATUS_OPTIONS, STATUS_VALUE_BY_ID } from '../i18n.js';
import {
  BUILDING_FIELD_LABELS,
  BUILDING_FIELD_PLACEHOLDERS,
  LOG_TABLE_COLUMNS,
  STATUS_SELECT_PLACEHOLDER
} from '../constants.js';

const baseFilters = {
  buildingId: '',
  streetId: '',
  houseNumber: '',
  nickname: '',
  bldSivug: '',
  status: '',
  sugBaalut: '',
  quarter: '',
  subQuarter: '',
  statisticalArea: '',
  updatedFrom: '',
  updatedTo: '',
  statusSummary: '',
  logType: '',
  user: ''
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
  ...overrides
});

const expandDateRange = (filters) => {
  const next = { ...filters };
  if (filters.updatedFrom && filters.updatedTo && filters.updatedFrom === filters.updatedTo) {
    const start = new Date(`${filters.updatedFrom}T00:00:00`).toISOString();
    const end = new Date(`${filters.updatedTo}T23:59:59.999`).toISOString();
    next.updatedFrom = start;
    next.updatedTo = end;
  } else {
    if (filters.updatedFrom) {
      next.updatedFrom = new Date(`${filters.updatedFrom}T00:00:00`).toISOString();
    }
    if (filters.updatedTo) {
      next.updatedTo = new Date(`${filters.updatedTo}T23:59:59.999`).toISOString();
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
  const [sivugOptions, setSivugOptions] = useState([]);
  const [ownershipOptions, setOwnershipOptions] = useState([]);
  const [sortConfig, setSortConfig] = useState({ field: 'updatedAt', direction: 'desc' });
  const [restoringLogId, setRestoringLogId] = useState(null);
  const rehabSivugValue = useMemo(() => {
    const match = sivugOptions.find((option) => option.label === 'ריק ובהליך שיקום');
    return match ? String(match.value) : '3';
  }, [sivugOptions]);
  const isFilterRehabStatusRequired = useMemo(() => {
    if (!filters.bldSivug && filters.bldSivug !== 0) return false;
    return String(filters.bldSivug) === rehabSivugValue;
  }, [filters.bldSivug, rehabSivugValue]);
  const statuses = statusOptions;
  // use shared formatters (dd/mm/yyyy and HH:mm)
  useDocumentTitle('יומן פעילויות - מוקד המבנים העירוני');
  const displayOrDash = (value) => {
    if (value === null || value === undefined || value === '') return '—';
    if (value === 'Unknown' || value === 'לא ידוע') return '—';
    return value;
  };
  const isImageValue = (value) =>
    typeof value === 'string' && value.trim().startsWith('data:image');
  const renderChangeValue = (value) => {
    const displayValue = displayOrDash(value);
    if (isImageValue(displayValue)) {
      return <img className="log-change-image" src={displayValue} alt="תמונה" />;
    }
    return displayValue;
  };
  const getSnapshotFieldValue = (fields, columnName) => {
    if (!Array.isArray(fields)) return null;
    const match = fields.find(
      (field) =>
        field.columnName &&
        field.columnName.toLowerCase() === columnName.toLowerCase()
    );
    return match?.value ?? null;
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
      return formatDateTime(value);
    } catch {
      return value;
    }
  };

  const getSortedChanges = (snapshot, snapshotFields) => {
    const changes = Array.isArray(snapshot?.changes) ? snapshot.changes : [];
    if (changes.length === 0) return [];
    const fieldOrder = new Map(
      (snapshotFields || []).map((field, index) => [(field.columnName || '').toLowerCase(), index])
    );
    return [...changes].sort((a, b) => {
      const aKey = String(a.columnName || '').toLowerCase();
      const bKey = String(b.columnName || '').toLowerCase();
      const aOrder = fieldOrder.has(aKey) ? fieldOrder.get(aKey) : Number.MAX_SAFE_INTEGER;
      const bOrder = fieldOrder.has(bKey) ? fieldOrder.get(bKey) : Number.MAX_SAFE_INTEGER;
      return aOrder - bOrder;
    });
  };

  const isBlankValue = (value) =>
    value === null || value === undefined || (typeof value === 'string' && value.trim() === '');

  const getDisplayChanges = (log, snapshotFields) => {
    const snapshot = log.snapshot || {};
    const sortedChanges = getSortedChanges(snapshot, snapshotFields);
    if (sortedChanges.length === 0) return [];
    if (log.actionType === 'מחיקה') {
      return sortedChanges.filter((change) => !isBlankValue(change.oldValue));
    }
    return sortedChanges;
  };

  const handleSortClick = (field) => {
    setSortConfig((prev) => {
      if (prev.field === field) {
        return { field, direction: prev.direction === 'asc' ? 'desc' : 'asc' };
      }
      return { field, direction: field === 'updatedAt' ? 'desc' : 'asc' };
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
        setStatusOptions(mapped);
      } catch {
        setStatusOptions(STATUS_OPTIONS);
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

    const loadOwnershipOptions = async () => {
      try {
        const options = await api.fetchSelectTable('Tbl_SugBaalut');
        setOwnershipOptions(options);
      } catch {
        setOwnershipOptions([]);
      }
    };

    loadStatusOptions();
    loadSivugOptions();
    loadOwnershipOptions();
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
    setFilters((prev) => {
      const next = { ...prev, [name]: value };
      if (name === 'bldSivug' && String(value) !== rehabSivugValue) {
        next.status = '';
      }
      return next;
    });
  };

  const handleSubmit = (event) => {
    event.preventDefault();
    loadLogs(filters);
  };

  const handleRestoreBuilding = async (log) => {
    setError('');
    setRestoringLogId(log.id);
    try {
      await api.restoreBuildingFromLog(log.id);
      await loadLogs(filters);
    } catch (err) {
      setError(err.message);
    } finally {
      setRestoringLogId(null);
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
      const snapshotFields = Array.isArray(snapshot.fields) ? snapshot.fields : [];
      const street =
        snapshot.streetName || getSnapshotFieldValue(snapshotFields, 'StreetName') || '';
      const houseNumber =
        snapshot.houseNumber || getSnapshotFieldValue(snapshotFields, 'BldNum') || '';
      const nickname =
        snapshot.buildingName || getSnapshotFieldValue(snapshotFields, 'BldName') || '';
      const displayChanges = getDisplayChanges(log, snapshotFields);
      const changedFields = displayChanges
        .map((change) => getExcelAwareLabel(change.fieldName || change.columnName))
        .join(', ');
      const oldValues = displayChanges.map((change) => displayOrDash(change.oldValue)).join(', ');
      const newValues = displayChanges.map((change) => displayOrDash(change.newValue)).join(', ');
      const updatedAtValue = log.createdAt ? new Date(log.createdAt).getTime() : null;

      switch (field) {
        case 'street':
          return street;
        case 'houseNumber':
          return houseNumber;
        case 'nickname':
          return nickname;
        case 'changedFields':
          return changedFields;
        case 'updatedAt':
          return updatedAtValue;
        case 'oldValues':
          return oldValues;
        case 'newValues':
          return newValues;
        case 'user':
          return log.username || '';
        default:
          return '';
      }
    };

    const copy = [...logs];
    copy.sort((a, b) => {
      const aValue = getSortValue(a, sortConfig.field);
      const bValue = getSortValue(b, sortConfig.field);
      const cmp = compareValues(aValue, bValue, { numeric: sortConfig.field === 'updatedAt' });
      if (cmp !== 0) return sortConfig.direction === 'desc' ? -cmp : cmp;
      if (sortConfig.field !== 'updatedAt') {
        const dateCmp = compareValues(getSortValue(a, 'updatedAt'), getSortValue(b, 'updatedAt'), {
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
  }, [logs, sortConfig]);

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
          <label>
            <span>{BUILDING_FIELD_LABELS.status}</span>
            <select
              name="status"
              value={filters.status || ''}
              onChange={handleChange}
              disabled={!isFilterRehabStatusRequired}
            >
              <option value="">{STATUS_SELECT_PLACEHOLDER}</option>
              {statuses.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
          <label>
            <span>סוג הבעלות</span>
            <select name="sugBaalut" value={filters.sugBaalut} onChange={handleChange}>
              <option value="">בחר סוג הבעלות</option>
              {ownershipOptions.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
          <label>
            <span>רובע</span>
            <input
              type="text"
              name="quarter"
              value={filters.quarter}
              onChange={handleChange}
              placeholder={BUILDING_FIELD_PLACEHOLDERS.quarter}
            />
          </label>
          <label>
            <span>תת רובע</span>
            <input
              type="text"
              name="subQuarter"
              value={filters.subQuarter}
              onChange={handleChange}
              placeholder={BUILDING_FIELD_PLACEHOLDERS.subQuarter}
            />
          </label>
          <label>
            <span>אזור סטטיסטי</span>
            <input
              type="text"
              name="statisticalArea"
              value={filters.statisticalArea}
              onChange={handleChange}
              placeholder={BUILDING_FIELD_PLACEHOLDERS.statisticalArea}
            />
          </label>
          <label>
            <span>תאריך שינוי - החל מ</span>
            <input
              type="date"
              name="updatedFrom"
              value={filters.updatedFrom}
              lang="he-IL"
              onChange={handleChange}
            />
          </label>
          <label>
            <span>תאריך שינוי - עד</span>
            <input
              type="date"
              name="updatedTo"
              value={filters.updatedTo}
              lang="he-IL"
              onChange={handleChange}
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
            <span>סוג לוג</span>
            <select name="logType" value={filters.logType} onChange={handleChange}>
              <option value="">הכל</option>
              <option value="deleted">נמחקו</option>
              <option value="created">נוצרו</option>
            </select>
          </label>
          <div className="filters-actions full-span align-right">
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
                const snapshotFields = Array.isArray(snapshot.fields) ? snapshot.fields : [];
                const displayChanges = getDisplayChanges(log, snapshotFields);
                const hasChanges = displayChanges.length > 0;
                const street =
                  snapshot.streetName ||
                  getSnapshotFieldValue(snapshotFields, 'StreetName') ||
                  '—';
                const houseNumber =
                  snapshot.houseNumber ||
                  getSnapshotFieldValue(snapshotFields, 'BldNum') ||
                  '—';
                const nickname =
                  snapshot.buildingName ||
                  getSnapshotFieldValue(snapshotFields, 'BldName') ||
                  '—';
                const changeRows = hasChanges
                  ? displayChanges.map((change) => ({
                      label: getExcelAwareLabel(change.fieldName || change.columnName),
                      oldValue: change.oldValue,
                      newValue: change.newValue
                    }))
                  : [{ label: '—', oldValue: null, newValue: null }];
                const showRestore = log.actionType === 'מחיקה';

                return (
                  <tr key={log.id}>
                    <td>{displayOrDash(street)}</td>
                    <td>{displayOrDash(houseNumber)}</td>
                    <td>{displayOrDash(nickname)}</td>
                    <td>{formatDate(log.createdAt)}</td>
                    <td colSpan={3} className="log-change-span">
                      <div className="log-change-table">
                        {changeRows.map((row, index) => (
                          <div
                            key={`change-${log.id}-${index}`}
                            className={`log-change-row ${index % 2 === 0 ? 'even' : 'odd'}`}
                          >
                            <div className="log-change-cell">{displayOrDash(row.label)}</div>
                            <div className="log-change-cell">{renderChangeValue(row.oldValue)}</div>
                            <div className="log-change-cell">{renderChangeValue(row.newValue)}</div>
                          </div>
                        ))}
                      </div>
                    </td>
                    <td>{displayOrDash(log.username)}</td>
                    <td>
                      {showRestore ? (
                        <button
                          type="button"
                          className="ghost"
                          disabled={restoringLogId === log.id}
                          onClick={() => handleRestoreBuilding(log)}
                        >
                          הבניין נמחק – שחזור
                        </button>
                      ) : (
                        <span className="muted">—</span>
                      )}
                    </td>
                  </tr>
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
