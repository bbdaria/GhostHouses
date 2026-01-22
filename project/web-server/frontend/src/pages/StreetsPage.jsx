import { useEffect, useMemo, useState } from 'react';
import api from '../api/client.js';
import { useAuth } from '../context/AuthContext.jsx';
import useDocumentTitle from '../hooks/useDocumentTitle.js';

const initialFilters = { search: '' };
const initialForm = { streetId: '', name: '' };
const requiredImportLabels = { StreetId: 'מזהה רחוב *', Name: 'שם רחוב *' };
const DUPLICATE_ID_WARNING = 'מזהה רחוב מופיע יותר מפעם אחת בקובץ הייבוא.';

export default function StreetsPage() {
  const { user } = useAuth();
  useDocumentTitle('רחובות - מוקד המבנים העירוני');
  const canEdit = useMemo(
    () => user && (user.role === 'Editor' || user.role === 'Admin'),
    [user]
  );
  const isAdmin = user?.role === 'Admin';

  const [filters, setFilters] = useState(initialFilters);
  const [streets, setStreets] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [actionMessage, setActionMessage] = useState('');
  const [exportError, setExportError] = useState('');
  const [exportMode, setExportMode] = useState(false);
  const [exportSelection, setExportSelection] = useState(new Set());
  const [createForm, setCreateForm] = useState(initialForm);
  const [editForm, setEditForm] = useState(initialForm);
  const [selectedStreetId, setSelectedStreetId] = useState('');
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [showImportModal, setShowImportModal] = useState(false);
  const [importFile, setImportFile] = useState(null);
  const [importRows, setImportRows] = useState([]);
  const [importPreviewing, setImportPreviewing] = useState(false);
  const [importApplying, setImportApplying] = useState(false);
  const [importSummary, setImportSummary] = useState('');
  const [importError, setImportError] = useState('');
  const [importEditRowId, setImportEditRowId] = useState(null);
  const [importEditValues, setImportEditValues] = useState(null);
  const [importEditError, setImportEditError] = useState('');
  const [importEditSaving, setImportEditSaving] = useState(false);
  const [importCompareRowId, setImportCompareRowId] = useState(null);
  const [sortConfig, setSortConfig] = useState({ field: 'name', direction: 'asc' });
  const allSelected = exportMode && streets.length > 0 && exportSelection.size === streets.length;

  useEffect(() => {
    loadStreets(filters);
  }, []);

  const loadStreets = async (appliedFilters = filters) => {
    setLoading(true);
    setError('');
    try {
      const data = await api.fetchStreets(appliedFilters.search);
      setStreets(data || []);
      if (selectedStreetId) {
        const stillExists = (data || []).find((s) => String(s.streetId) === selectedStreetId);
        if (!stillExists) {
          setSelectedStreetId('');
          setEditForm(initialForm);
        }
      }
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleFilterChange = (event) => {
    const { name, value } = event.target;
    setFilters((prev) => ({ ...prev, [name]: value }));
  };

  const handleSearch = (event) => {
    event.preventDefault();
    loadStreets(filters);
  };

  const handleReset = () => {
    setFilters(initialFilters);
    loadStreets(initialFilters);
  };

  const importBusy = importPreviewing || importApplying;
  const getImportRequiredLabel = (columnName) => requiredImportLabels[columnName] || columnName;

  const parseStreetId = (rawValue) => {
    if (rawValue === null || rawValue === undefined) return null;
    const value = String(rawValue).trim();
    if (!value) return null;
    const parsed = Number(value);
    if (!Number.isInteger(parsed) || parsed <= 0 || parsed === -1) return null;
    return parsed;
  };


  const downloadExportFile = (blob, prefix = 'streets') => {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    const dateStamp = new Date().toISOString().slice(0, 10);
    link.href = url;
    link.download = `${prefix}-${dateStamp}.xlsx`;
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
  };

  const handleToggleExportAll = () => {
    if (!exportMode) return;
    if (allSelected) {
      setExportSelection(new Set());
      return;
    }
    setExportSelection(new Set(streets.map((street) => street.streetId)));
  };

  const handleToggleExportStreet = (streetId) => {
    if (!exportMode) return;
    setExportSelection((prev) => {
      const next = new Set(prev);
      if (next.has(streetId)) {
        next.delete(streetId);
      } else {
        next.add(streetId);
      }
      return next;
    });
  };

  const handleExportSelected = async () => {
    setExportError('');
    try {
      const blob = await api.exportStreetsSelection([...exportSelection]);
      downloadExportFile(blob);
      setExportMode(false);
      setExportSelection(new Set());
    } catch (err) {
      setExportError(err.message || 'שגיאה בייצוא קובץ הרחובות.');
    }
  };

  const handleExportAction = () => {
    if (!exportMode) {
      setExportMode(true);
      setExportSelection(new Set());
      return;
    }
    handleExportSelected();
  };

  const handleCancelExport = () => {
    setExportMode(false);
    setExportSelection(new Set());
    setExportError('');
  };

  const openCreateModal = () => {
    setActionMessage('');
    setCreateForm(initialForm);
    setShowCreateModal(true);
  };

  const closeCreateModal = () => {
    setShowCreateModal(false);
  };

  const openEditModal = (street) => {
    setActionMessage('');
    setSelectedStreetId(String(street.streetId));
    setEditForm({ streetId: String(street.streetId), name: street.name });
    setShowEditModal(true);
  };

  const closeEditModal = () => {
    setShowEditModal(false);
  };

  const openImportModal = () => {
    setShowImportModal(true);
    setImportFile(null);
    setImportRows([]);
    setImportSummary('');
    setImportError('');
    setImportEditRowId(null);
    setImportEditValues(null);
    setImportEditError('');
    setImportCompareRowId(null);
  };

  const closeImportModal = () => {
    setShowImportModal(false);
    setImportFile(null);
    setImportRows([]);
    setImportSummary('');
    setImportError('');
    setImportEditRowId(null);
    setImportEditValues(null);
    setImportEditError('');
    setImportCompareRowId(null);
  };

  const handleCreateChange = (event) => {
    const { name, value } = event.target;
    setCreateForm((prev) => ({ ...prev, [name]: value }));
  };

  const handleEditChange = (event) => {
    const { name, value } = event.target;
    setEditForm((prev) => ({ ...prev, [name]: value }));
  };

  const handleCreate = async (event) => {
    event.preventDefault();
    setActionMessage('');
    try {
      if (!createForm.streetId || !createForm.name) {
        throw new Error('יש למלא מזהה רחוב ושם רחוב');
      }
      await api.createStreet({
        streetId: Number(createForm.streetId),
        name: createForm.name
      });
      setCreateForm(initialForm);
      await loadStreets(filters);
      setActionMessage('רחוב נוסף בהצלחה');
      closeCreateModal();
    } catch (err) {
      setActionMessage(err.message);
    }
  };

  const handleUpdate = async (event) => {
    event.preventDefault();
    if (!selectedStreetId) return;
    setActionMessage('');
    try {
      await api.updateStreet(Number(selectedStreetId), {
        streetId: Number(selectedStreetId),
        name: editForm.name
      });
      await loadStreets(filters);
      setActionMessage('פרטי הרחוב עודכנו.');
      closeEditModal();
    } catch (err) {
      setActionMessage(err.message);
    }
  };

  const handleDelete = async (streetIdParam) => {
    const targetId = streetIdParam ? String(streetIdParam) : selectedStreetId;
    if (!targetId) return;
    const confirmed = window.confirm('למחוק את הרחוב? זה ישפיע על בחירת רחוב במבנים.');
    if (!confirmed) return;
    try {
      await api.deleteStreet(Number(targetId));
      if (selectedStreetId === targetId) {
        setSelectedStreetId('');
        setEditForm(initialForm);
      }
      await loadStreets(filters);
      setActionMessage('רחוב נמחק.');
    } catch (err) {
      setActionMessage(err.message);
    }
  };

  const buildImportRows = (rows = []) =>
    rows.map((row) => ({
      rowNumber: row.rowNumber,
      values: row.values || {},
      idMatch: row.idMatch || null,
      hasIdConflict: Boolean(row.hasIdConflict),
      exactMatch: Boolean(row.exactMatch),
      missingRequired: Array.isArray(row.missingRequired) ? row.missingRequired : [],
      invalidValues: Array.isArray(row.invalidValues) ? row.invalidValues : [],
      warnings: Array.isArray(row.warnings) ? row.warnings : [],
      decision: row.exactMatch ? 'skip' : null,
      batchDuplicate: false
    }));

  const applyBatchDuplicates = (rows) => {
    const counts = rows.reduce((acc, row) => {
      if (row.decision === 'skip') return acc;
      const idValue = parseStreetId(row.values?.StreetId);
      if (!idValue) return acc;
      acc[idValue] = (acc[idValue] || 0) + 1;
      return acc;
    }, {});

    return rows.map((row) => {
      const idValue = parseStreetId(row.values?.StreetId);
      const isDuplicate = Boolean(idValue && counts[idValue] > 1);
      let warnings = Array.isArray(row.warnings) ? [...row.warnings] : [];
      if (isDuplicate && !warnings.includes(DUPLICATE_ID_WARNING)) {
        warnings.push(DUPLICATE_ID_WARNING);
      }
      if (!isDuplicate) {
        warnings = warnings.filter((warning) => warning !== DUPLICATE_ID_WARNING);
      }
      return { ...row, batchDuplicate: isDuplicate, warnings };
    });
  };

  const computeRowDecision = (row) => {
    if (row.exactMatch) return 'skip';
    const missingRequired = row.missingRequired?.length ?? 0;
    const invalidValues = row.invalidValues?.length ?? 0;
    if (!row.hasIdConflict && !row.batchDuplicate && missingRequired === 0 && invalidValues === 0) {
      return 'create';
    }
    return null;
  };

  const applyAutoDecisions = (rows) =>
    rows.map((row) => (row.decision ? row : { ...row, decision: computeRowDecision(row) }));

  const normalizeImportRows = (rows) => applyAutoDecisions(applyBatchDuplicates(rows));

  const updateImportRow = (rowNumber, updates) => {
    setImportRows((prev) =>
      normalizeImportRows(prev.map((row) => (row.rowNumber === rowNumber ? { ...row, ...updates } : row)))
    );
  };

  const handleImportFileChange = (event) => {
    const file = event.target.files?.[0];
    setImportFile(file || null);
    setImportRows([]);
    setImportSummary('');
    setImportError('');
  };

  const triggerImportFileSelect = () => {
    const input = document.getElementById('streets-import-file');
    if (input && !input.disabled) {
      input.click();
    }
  };

  const handlePreviewImport = async () => {
    if (!importFile) {
      setImportError('נא לבחור קובץ אקסל.');
      return;
    }
    setImportError('');
    setImportSummary('');
    setImportPreviewing(true);
    try {
      const result = await api.previewImportStreets(importFile);
      const rows = normalizeImportRows(buildImportRows(result.rows || []));
      setImportRows(rows);
      setImportSummary(`נטענו ${rows.length} שורות לייבוא.`);
    } catch (err) {
      setImportError(err.message || 'שגיאה בקריאת קובץ הייבוא.');
    } finally {
      setImportPreviewing(false);
    }
  };

  const handleImportApply = async () => {
    if (importRows.length === 0) {
      setImportError('אין שורות לייבוא.');
      return;
    }
    if (!importReadyToApply) {
      setImportError('יש שורות שדורשות טיפול לפני הייבוא.');
      return;
    }

    const payloadRows = importRows
      .filter((row) => row.decision)
      .map((row) => ({
        rowNumber: row.rowNumber,
        action: row.decision,
        values: row.values
      }));

    if (payloadRows.length === 0) {
      setImportError('אין שורות לביצוע.');
      return;
    }

    setImportError('');
    setImportApplying(true);
    try {
      const result = await api.applyImportStreets(payloadRows);
      const summary = `הייבוא הושלם. נוספו ${result.created} רחובות, הוחלפו ${result.replaced}, דולגו ${result.skipped}.`;
      setActionMessage(summary);
      closeImportModal();
      loadStreets(filters);
    } catch (err) {
      setImportError(err.message || 'שגיאה בייבוא קובץ הייבוא.');
    } finally {
      setImportApplying(false);
    }
  };

  const handleImportSkipRow = (rowNumber) => {
    updateImportRow(rowNumber, { decision: 'skip' });
  };

  const handleImportReplaceRow = (rowNumber) => {
    updateImportRow(rowNumber, { decision: 'replace' });
  };

  const handleImportSkipAll = () => {
    setImportRows((prev) =>
      normalizeImportRows(
        prev.map((row) => {
          if (row.decision) return row;
          if (importStage2Rows.find((item) => item.rowNumber === row.rowNumber)) {
            return { ...row, decision: 'skip' };
          }
          return row;
        })
      )
    );
  };

  const handleImportSkipAllStage1 = () => {
    setImportRows((prev) =>
      normalizeImportRows(
        prev.map((row) => {
          if (row.decision) return row;
          if (importStage1Rows.find((item) => item.rowNumber === row.rowNumber)) {
            return { ...row, decision: 'skip' };
          }
          return row;
        })
      )
    );
  };

  const handleImportReplaceAll = () => {
    setImportRows((prev) =>
      normalizeImportRows(
        prev.map((row) => {
          if (row.decision) return row;
          if (importStage2Rows.find((item) => item.rowNumber === row.rowNumber) && row.idMatch) {
            return { ...row, decision: 'replace' };
          }
          return row;
        })
      )
    );
  };

  const openImportEdit = (rowNumber) => {
    const row = importRows.find((item) => item.rowNumber === rowNumber);
    if (!row) return;
    setImportEditRowId(rowNumber);
    setImportEditValues({ ...(row.values || {}) });
    setImportEditError('');
  };

  const closeImportEdit = () => {
    setImportEditRowId(null);
    setImportEditValues(null);
    setImportEditError('');
  };

  const handleImportEditValueChange = (field, value) => {
    setImportEditValues((prev) => ({ ...(prev || {}), [field]: value }));
  };

  const handleImportEditSave = async () => {
    if (!importEditRowId || !importEditValues) return;
    setImportEditError('');
    setImportEditSaving(true);
    try {
      const result = await api.validateImportStreet(importEditValues);
      updateImportRow(importEditRowId, {
        values: result.values || importEditValues,
        idMatch: result.idMatch || null,
        hasIdConflict: Boolean(result.hasIdConflict),
        exactMatch: Boolean(result.exactMatch),
        missingRequired: Array.isArray(result.missingRequired) ? result.missingRequired : [],
        invalidValues: Array.isArray(result.invalidValues) ? result.invalidValues : [],
        warnings: Array.isArray(result.warnings) ? result.warnings : [],
        decision: null
      });
      closeImportEdit();
    } catch (err) {
      setImportEditError(err.message || 'שגיאה בעדכון השורה.');
    } finally {
      setImportEditSaving(false);
    }
  };

  const openImportCompare = (rowNumber) => {
    setImportCompareRowId(rowNumber);
  };

  const closeImportCompare = () => {
    setImportCompareRowId(null);
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

  const sortedStreets = useMemo(() => {
    if (!streets || streets.length === 0) return [];
    if (!sortConfig.field) return streets;

    const compareValues = (aValue, bValue, { numeric = false } = {}) => {
      const aMissing = aValue === null || aValue === undefined || aValue === '';
      const bMissing = bValue === null || bValue === undefined || bValue === '';
      if (aMissing && bMissing) return 0;
      if (aMissing) return 1;
      if (bMissing) return -1;
      if (numeric) return aValue - bValue;
      return String(aValue).localeCompare(String(bValue), 'he');
    };

    const getSortValue = (street) => {
      if (sortConfig.field === 'streetId') return street.streetId;
      return street.name;
    };

    const copy = [...streets];
    copy.sort((a, b) => {
      const aValue = getSortValue(a);
      const bValue = getSortValue(b);
      const cmp = compareValues(aValue, bValue, { numeric: sortConfig.field === 'streetId' });
      if (cmp !== 0) return sortConfig.direction === 'desc' ? -cmp : cmp;
      if (sortConfig.field !== 'name') {
        const nameCmp = compareValues(a.name, b.name);
        if (nameCmp !== 0) return nameCmp;
      }
      if (sortConfig.field !== 'streetId') {
        const idCmp = compareValues(a.streetId, b.streetId, { numeric: true });
        if (idCmp !== 0) return idCmp;
      }
      return 0;
    });
    return copy;
  }, [streets, sortConfig]);

  const importCompareRow = useMemo(
    () => importRows.find((row) => row.rowNumber === importCompareRowId) || null,
    [importRows, importCompareRowId]
  );

  const importStage1Rows = useMemo(
    () =>
      importRows.filter(
        (row) =>
          row.decision === null &&
          ((row.missingRequired?.length ?? 0) > 0 ||
            (row.invalidValues?.length ?? 0) > 0 ||
            row.batchDuplicate)
      ),
    [importRows]
  );

  const importStage2Rows = useMemo(
    () =>
      importRows.filter(
        (row) =>
          row.decision === null &&
          !row.batchDuplicate &&
          (row.missingRequired?.length ?? 0) === 0 &&
          (row.invalidValues?.length ?? 0) === 0 &&
          row.hasIdConflict &&
          !row.exactMatch
      ),
    [importRows]
  );

  const importReadyToApply =
    importRows.length > 0 && importStage1Rows.length === 0 && importStage2Rows.length === 0;

  const importStats = useMemo(() => {
    return importRows.reduce(
      (acc, row) => {
        if (row.decision === 'create') acc.create += 1;
        else if (row.decision === 'replace') acc.replace += 1;
        else if (row.decision === 'skip') acc.skip += 1;
        else acc.pending += 1;
        return acc;
      },
      { create: 0, replace: 0, skip: 0, pending: 0 }
    );
  }, [importRows]);

  const renderImportIssues = (row) => {
    const missingLabels = (row.missingRequired || []).map((field) => getImportRequiredLabel(field));
    const invalidLabels = (row.invalidValues || []).map((issue) => {
      const label = getImportRequiredLabel(issue.field);
      return `${label}: ${issue.message}`;
    });
    const warnings = row.warnings || [];
    const parts = [];
    if (missingLabels.length > 0) parts.push(`חסרים: ${missingLabels.join(', ')}`);
    if (invalidLabels.length > 0) parts.push(`שגויים: ${invalidLabels.join(', ')}`);
    if (warnings.length > 0) parts.push(`אזהרות: ${warnings.join(', ')}`);
    return (
      <div className="import-missing-fields">
        <p>{parts.length > 0 ? parts.join(' | ') : '—'}</p>
      </div>
    );
  };

  return (
    <main className="app streets-app">
      <header className="page-header">
        <div>
          <p className="eyebrow">מאגר רחובות</p>
          <h1>ניהול רחובות</h1>
          <p className="subtitle">הוספה, עדכון ומחיקה של רחובות זמינים למבנים.</p>
        </div>
      </header>

      <section className="filters-card">
        <form className="filters-grid" onSubmit={handleSearch}>
          <label>
            <span>חיפוש לפי שם או מזהה</span>
            <input
              type="text"
              name="search"
              value={filters.search}
              onChange={handleFilterChange}
              placeholder="חפש רחוב..."
            />
          </label>
          <div className="filters-actions full-span align-right">
            <button type="submit" className="primary">
              חיפוש
            </button>
            <button type="button" onClick={handleReset} className="ghost">
              איפוס
            </button>
            {canEdit && (
              <button type="button" className="ghost" onClick={openCreateModal}>
                הוסף רחוב
              </button>
            )}
            {isAdmin && (
              <button type="button" className="ghost" onClick={openImportModal} disabled={importBusy}>
                {importBusy ? 'מייבא...' : 'יבוא רחובות'}
              </button>
            )}
            {!exportMode ? (
              <button type="button" className="ghost" onClick={handleExportAction}>
                יצוא לאקסל
              </button>
            ) : (
              <>
                <button type="button" className="ghost" onClick={handleExportAction}>
                  יצוא נבחרים
                </button>
                <button type="button" className="ghost" onClick={handleCancelExport}>
                  ביטול
                </button>
              </>
            )}
          </div>
        </form>
        {loading && <p className="muted">טוען רחובות…</p>}
        {error && <p className="error">שגיאה: {error}</p>}
        {actionMessage && <p className="success">{actionMessage}</p>}
        {exportError && <p className="error">שגיאה בייצוא: {exportError}</p>}
      </section>

      <section className="content-layout">
        <div className="list-panel full-span">
          <div className="panel-header">
            <div>
              <h2>רחובות ({streets.length})</h2>
              {exportMode && <p className="muted">נבחרו {exportSelection.size} רחובות לייצוא</p>}
            </div>
          </div>
          <div className="table-wrapper">
            <table className={exportMode ? 'export-table' : ''}>
              <thead>
                <tr>
                  {exportMode && (
                    <th>
                      <input
                        type="checkbox"
                        aria-label="בחר הכל"
                        checked={allSelected}
                        onChange={handleToggleExportAll}
                      />
                    </th>
                  )}
                  <th aria-sort={getAriaSort('streetId')}>
                    <button type="button" className="sort-button" onClick={() => handleSortClick('streetId')}>
                      מזהה רחוב
                      <span className="sort-indicator" aria-hidden="true">
                        {getSortIndicator('streetId')}
                      </span>
                    </button>
                  </th>
                  <th aria-sort={getAriaSort('name')}>
                    <button type="button" className="sort-button" onClick={() => handleSortClick('name')}>
                      שם רחוב
                      <span className="sort-indicator" aria-hidden="true">
                        {getSortIndicator('name')}
                      </span>
                    </button>
                  </th>
                  <th>פעולות</th>
                </tr>
              </thead>
              <tbody>
                {sortedStreets.map((street) => {
                  const isSelected = exportSelection.has(street.streetId);
                  return (
                    <tr
                      key={street.streetId}
                      className={exportMode && isSelected ? 'active' : ''}
                      onClick={() => {
                        if (exportMode) {
                          handleToggleExportStreet(street.streetId);
                        }
                      }}
                    >
                    {exportMode && (
                      <td>
                        <input
                          type="checkbox"
                          aria-label={`בחר רחוב ${street.name}`}
                          checked={exportSelection.has(street.streetId)}
                          onChange={(event) => {
                            event.stopPropagation();
                            handleToggleExportStreet(street.streetId);
                          }}
                        />
                      </td>
                    )}
                    <td>{street.streetId}</td>
                    <td>{street.name}</td>
                    <td className="table-actions">
                      {canEdit && (
                        <button
                          type="button"
                          className="ghost"
                          onClick={(event) => {
                            event.stopPropagation();
                            openEditModal(street);
                          }}
                        >
                          עריכה
                        </button>
                      )}
                      {canEdit && (
                        <button
                          type="button"
                          className="danger"
                          onClick={(event) => {
                            event.stopPropagation();
                            handleDelete(street.streetId);
                          }}
                        >
                          מחק
                        </button>
                      )}
                    </td>
                    </tr>
                  );
                })}
                {streets.length === 0 && !loading && (
                  <tr>
                    <td colSpan={exportMode ? 4 : 3} className="muted">
                      אין רחובות להצגה.
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
            <form onSubmit={handleCreate}>
              <div className="modal-header">
                <h3>הוספת רחוב</h3>
                <button type="button" className="modal-close" onClick={closeCreateModal}>
                  ✕
                </button>
              </div>
              <div className="modal-body">
                <div className="form-grid">
                  <label>
                    <span className="label-title">
                      מזהה רחוב <span className="required-mark">*</span>
                    </span>
                    <input
                      name="streetId"
                      value={createForm.streetId}
                      onChange={handleCreateChange}
                      required
                    />
                  </label>
                  <label>
                    <span className="label-title">
                      שם רחוב <span className="required-mark">*</span>
                    </span>
                    <input name="name" value={createForm.name} onChange={handleCreateChange} required />
                  </label>
                </div>
              </div>
              <div className="modal-footer">
                <div className="footer-actions">
                  <button type="button" className="ghost" onClick={closeCreateModal}>
                    סגירה
                  </button>
                  <button type="submit" className="primary">
                    שמירה
                  </button>
                </div>
              </div>
            </form>
          </div>
        </div>
      )}

      {showEditModal && (
        <div className="modal-overlay" onClick={closeEditModal}>
          <div className="modal-window" onClick={(event) => event.stopPropagation()}>
            <form onSubmit={handleUpdate}>
              <div className="modal-header">
                <h3>עריכת רחוב</h3>
                <button type="button" className="modal-close" onClick={closeEditModal}>
                  ✕
                </button>
              </div>
              <div className="modal-body">
                <div className="form-grid">
                  <label>
                    <span className="label-title">
                      מזהה רחוב <span className="required-mark">*</span>
                    </span>
                    <input name="streetId" value={editForm.streetId} readOnly />
                  </label>
                  <label>
                    <span className="label-title">
                      שם רחוב <span className="required-mark">*</span>
                    </span>
                    <input name="name" value={editForm.name} onChange={handleEditChange} required />
                  </label>
                </div>
              </div>
              <div className="modal-footer">
                <div className="footer-actions">
                  <button type="button" className="ghost" onClick={closeEditModal}>
                    סגירה
                  </button>
                  <button type="submit" className="primary">
                    שמירת שינויים
                  </button>
                </div>
              </div>
            </form>
          </div>
        </div>
      )}

      {showImportModal && (
        <div className="modal-overlay" onClick={closeImportModal}>
          <div className="modal-window modal-large" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <h3>יבוא רחובות</h3>
              <button type="button" className="modal-close" onClick={closeImportModal}>
                ✕
              </button>
            </div>
            <div className="modal-body">
              <div className="import-controls">
                <div className="file-input">
                  <input
                    id="streets-import-file"
                    type="file"
                    accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                    onChange={handleImportFileChange}
                    disabled={importBusy}
                  />
                  <button
                    type="button"
                    className="ghost file-input__button"
                    onClick={triggerImportFileSelect}
                    disabled={importBusy}
                  >
                    בחר קובץ
                  </button>
                  <span className={`file-input__name ${importFile ? '' : 'muted'}`}>
                    {importFile ? importFile.name : 'לא נבחר קובץ'}
                  </span>
                </div>
                <button
                  type="button"
                  className="ghost"
                  onClick={handlePreviewImport}
                  disabled={importPreviewing || !importFile}
                >
                  {importPreviewing ? 'טוען…' : 'טעינת קובץ'}
                </button>
              </div>
              {importSummary && <p className="muted">{importSummary}</p>}
              {importError && <p className="error">{importError}</p>}

              {importRows.length > 0 && (
                <div className="import-summary">
                  <span className="muted">
                    להוספה אוטומטית: {importStats.create} | להחלפה: {importStats.replace} | דילוג: {importStats.skip} | לטיפול: {importStats.pending}
                  </span>
                </div>
              )}

              {importStage1Rows.length > 0 && (
                <div className="import-stage">
                  <h4>שלב 1: שדות חסרים או שגויים</h4>
                  <div className="import-actions">
                    <button type="button" className="ghost" onClick={handleImportSkipAllStage1}>
                      דלג על הכל
                    </button>
                  </div>
                  <div className="table-wrapper">
                    <table className="import-table">
                      <thead>
                        <tr>
                          <th>שורה</th>
                          <th>
                            מזהה רחוב <span className="required-mark">*</span>
                          </th>
                          <th>
                            שם רחוב <span className="required-mark">*</span>
                          </th>
                          <th>הערות</th>
                          <th>פעולות</th>
                        </tr>
                      </thead>
                      <tbody>
                        {importStage1Rows.map((row) => (
                          <tr key={row.rowNumber} className="import-row import-row--needs">
                            <td>{row.rowNumber}</td>
                            <td>{row.values?.StreetId || '—'}</td>
                            <td>{row.values?.Name || '—'}</td>
                            <td>{renderImportIssues(row)}</td>
                            <td>
                              <div className="import-actions">
                                <button type="button" className="ghost" onClick={() => openImportEdit(row.rowNumber)}>
                                  עריכה
                                </button>
                                <button
                                  type="button"
                                  className="ghost"
                                  onClick={() => handleImportSkipRow(row.rowNumber)}
                                >
                                  דלג
                                </button>
                              </div>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              )}

              {importStage2Rows.length > 0 && (
                <div className="import-stage">
                  <h4>שלב 2: כפילויות מזהה רחוב</h4>
                  <div className="import-actions">
                    <button type="button" className="ghost" onClick={handleImportReplaceAll}>
                      החלף הכל
                    </button>
                    <button type="button" className="ghost" onClick={handleImportSkipAll}>
                      דלג על הכל
                    </button>
                  </div>
                  <div className="table-wrapper">
                    <table className="import-table">
                      <thead>
                        <tr>
                          <th>שורה</th>
                          <th>
                            מזהה רחוב <span className="required-mark">*</span>
                          </th>
                          <th>
                            שם בקובץ <span className="required-mark">*</span>
                          </th>
                          <th>שם במערכת</th>
                          <th>פעולות</th>
                        </tr>
                      </thead>
                      <tbody>
                        {importStage2Rows.map((row) => (
                          <tr key={row.rowNumber} className="import-row import-row--conflict">
                            <td>{row.rowNumber}</td>
                            <td>{row.values?.StreetId || '—'}</td>
                            <td>{row.values?.Name || '—'}</td>
                            <td>{row.idMatch?.name || '—'}</td>
                            <td>
                              <div className="import-actions">
                                <button type="button" className="ghost" onClick={() => openImportCompare(row.rowNumber)}>
                                  השוואה
                                </button>
                                <button
                                  type="button"
                                  className="primary"
                                  disabled={!row.idMatch}
                                  onClick={() => handleImportReplaceRow(row.rowNumber)}
                                >
                                  החלף
                                </button>
                                <button
                                  type="button"
                                  className="ghost"
                                  onClick={() => handleImportSkipRow(row.rowNumber)}
                                >
                                  דלג
                                </button>
                                <button type="button" className="ghost" onClick={() => openImportEdit(row.rowNumber)}>
                                  עריכה
                                </button>
                              </div>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              )}

              {importRows.length > 0 && importReadyToApply && (
                <p className="muted">אין שורות בעייתיות. ניתן לבצע יבוא.</p>
              )}
            </div>
            <div className="modal-footer">
              <div className="footer-actions">
                <button type="button" className="ghost" onClick={closeImportModal}>
                  סגירה
                </button>
                <button
                  type="button"
                  className="primary"
                  onClick={handleImportApply}
                  disabled={!importReadyToApply || importApplying}
                >
                  {importApplying ? 'מייבא...' : 'ביצוע יבוא'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {importEditRowId && (
        <div className="modal-overlay" onClick={closeImportEdit}>
          <div className="modal-window" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <h3>עריכת שורה {importEditRowId}</h3>
              <button type="button" className="modal-close" onClick={closeImportEdit}>
                ✕
              </button>
            </div>
            <div className="modal-body">
              {importEditError && <p className="error">{importEditError}</p>}
              <div className="import-edit-grid">
                <label>
                  <span className="label-title">
                    מזהה רחוב <span className="required-mark">*</span>
                  </span>
                  <input
                    value={importEditValues?.StreetId ?? ''}
                    onChange={(event) => handleImportEditValueChange('StreetId', event.target.value)}
                  />
                </label>
                <label>
                  <span className="label-title">
                    שם רחוב <span className="required-mark">*</span>
                  </span>
                  <input
                    value={importEditValues?.Name ?? ''}
                    onChange={(event) => handleImportEditValueChange('Name', event.target.value)}
                  />
                </label>
              </div>
            </div>
            <div className="modal-footer">
              <div className="footer-actions">
                <button type="button" className="ghost" onClick={closeImportEdit} disabled={importEditSaving}>
                  סגירה
                </button>
                <button type="button" className="primary" onClick={handleImportEditSave} disabled={importEditSaving}>
                  {importEditSaving ? 'שומר...' : 'שמירה'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {importCompareRow && (
        <div className="modal-overlay" onClick={closeImportCompare}>
          <div className="modal-window" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <h3>השוואת שורה {importCompareRow.rowNumber}</h3>
              <button type="button" className="modal-close" onClick={closeImportCompare}>
                ✕
              </button>
            </div>
            <div className="modal-body">
              <div className="table-wrapper">
                <table className="import-compare-table">
                  <thead>
                    <tr>
                      <th>שדה</th>
                      <th>מיובא</th>
                      <th>קיים במערכת</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr>
                      <td>
                        מזהה רחוב <span className="required-mark">*</span>
                      </td>
                      <td>{importCompareRow.values?.StreetId || '—'}</td>
                      <td>{importCompareRow.idMatch?.streetId ?? '—'}</td>
                    </tr>
                    <tr>
                      <td>
                        שם רחוב <span className="required-mark">*</span>
                      </td>
                      <td>{importCompareRow.values?.Name || '—'}</td>
                      <td>{importCompareRow.idMatch?.name || '—'}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
            <div className="modal-footer">
              <div className="footer-actions">
                <button type="button" className="ghost" onClick={closeImportCompare}>
                  סגירה
                </button>
                <button
                  type="button"
                  className="ghost"
                  onClick={() => {
                    handleImportSkipRow(importCompareRow.rowNumber);
                    closeImportCompare();
                  }}
                >
                  דלג
                </button>
                <button
                  type="button"
                  className="primary"
                  disabled={!importCompareRow.idMatch}
                  onClick={() => {
                    handleImportReplaceRow(importCompareRow.rowNumber);
                    closeImportCompare();
                  }}
                >
                  החלף
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </main>
  );
}
