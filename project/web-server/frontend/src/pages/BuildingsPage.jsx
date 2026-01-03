import { Fragment, useEffect, useMemo, useState } from 'react';
import api from '../api/client.js';
import { useAuth } from '../context/AuthContext.jsx';
import { ROLE_LABELS, STATUS_LABEL_MAP, STATUS_OPTIONS, STATUS_VALUE_BY_ID } from '../i18n.js';
import useDocumentTitle from '../hooks/useDocumentTitle.js';
import { BUILDING_FIELD_PLACEHOLDERS, LAST_BUILDING_KEY } from '../constants.js';

const initialFilters = {
  streetId: '',
  houseNumber: '',
  nickname: '',
  status: '',
  bldSivug: '',
  sugBaalut: '',
  quarter: '',
  subQuarter: '',
  statisticalArea: '',
  updatedFrom: '',
  updatedTo: '',
  statusSummary: ''
};
const REQUIRED_EDIT_FIELDS = [
  { key: 'StreetId', label: 'שם רחוב' },
  { key: 'BldNum', label: 'מספר בית' },
  { key: 'BldName', label: 'כינוי הבניין' },
  { key: 'ShikumStatus', label: 'סטטוס שיקום' },
  { key: 'BldSivug', label: 'סיווג' },
  { key: 'StatusSummary', label: 'תמונת מצב (תמצית מצב)' }
];
const REQUIRED_EDIT_COLUMNS = new Set(
  REQUIRED_EDIT_FIELDS.filter((field) => field.key !== 'StreetId').map((field) => field.key)
);

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

export default function BuildingsPage() {
  const { user } = useAuth();
  useDocumentTitle('מאגר מבנים - מוקד המבנים העירוני');
  const [filters, setFilters] = useState(initialFilters);
  const [buildings, setBuildings] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [statusOptions, setStatusOptions] = useState(STATUS_OPTIONS);
  const [statusLabelMap, setStatusLabelMap] = useState(STATUS_LABEL_MAP);
  const [sivugOptions, setSivugOptions] = useState([]);
  const [ownershipOptions, setOwnershipOptions] = useState([]);
  const [streets, setStreets] = useState([]);
  const [selectedBuilding, setSelectedBuilding] = useState(null);
  const [detailError, setDetailError] = useState('');
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [exporting, setExporting] = useState(false);
  const [exportError, setExportError] = useState('');
  const [createForm, setCreateForm] = useState({
    fldId: '',
    streetId: '',
    bldNum: '',
    bldName: '',
    statusSummary: '',
    shikumStatusId: '',
    category: ''
  });
  const [editFieldValues, setEditFieldValues] = useState({});
  const [selectTablesByName, setSelectTablesByName] = useState({});
  const [selectTablesLoading, setSelectTablesLoading] = useState(false);
  const [actionMessage, setActionMessage] = useState('');
  const [selectedView, setSelectedView] = useState('all');
  const [sortConfig, setSortConfig] = useState({ field: 'street', direction: 'asc' });

  const canEdit = useMemo(
    () => user && (user.role === 'Editor' || user.role === 'Admin'),
    [user]
  );
  const isAdmin = user?.role === 'Admin';
  const roleLabel = ROLE_LABELS[user?.role] || user?.role;
  const israelDateFormatter = useMemo(
    () =>
      new Intl.DateTimeFormat('he-IL', {
        dateStyle: 'short',
        timeStyle: 'short',
        timeZone: 'Asia/Jerusalem'
      }),
    []
  );
  const formatLogDate = (value) => {
    if (!value) return '—';
    try {
      return israelDateFormatter.format(new Date(value));
    } catch {
      return value;
    }
  };

  const expandUpdatedRange = (filterValues) => {
    const next = { ...filterValues };
    if (filterValues.updatedFrom) {
      next.updatedFrom = new Date(`${filterValues.updatedFrom}T00:00:00`).toISOString();
    }
    if (filterValues.updatedTo) {
      next.updatedTo = new Date(`${filterValues.updatedTo}T23:59:59.999`).toISOString();
    }
    return next;
  };

  const displayOrDash = (value) => (value === null || value === undefined || value === '' ? '—' : value);
  const getSivugLabel = (value) => {
    if (value === null || value === undefined || value === '') return '—';
    const match = sivugOptions.find((option) => String(option.value) === String(value));
    return match ? match.label : String(value);
  };
  const getOwnershipLabel = (value) => {
    if (value === null || value === undefined || value === '') return '—';
    const match = ownershipOptions.find((option) => String(option.value) === String(value));
    return match ? match.label : String(value);
  };
  const isRequiredEditColumn = (columnName) => REQUIRED_EDIT_COLUMNS.has(columnName);

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
        // Fall back to static defaults if lookup endpoint is unavailable.
        setStatusOptions(STATUS_OPTIONS);
        setStatusLabelMap(STATUS_LABEL_MAP);
      }
    };

    const loadStreets = async () => {
      try {
        const data = await api.fetchStreets();
        setStreets(data);
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
    loadBuildings(initialFilters);
  }, []);

  useEffect(() => {
    if (!selectedBuilding) {
      setEditFieldValues({});
      return;
    }

    const nextValues = {};
    (selectedBuilding.fields || []).forEach((field) => {
      if (!field?.columnName) return;
      if (field.columnName.toLowerCase() === 'streetname') return;

      if (field.selectTableName) {
        nextValues[field.columnName] =
          field.rawValue === null || field.rawValue === undefined ? '' : String(field.rawValue);
      } else {
        nextValues[field.columnName] = field.value ?? '';
      }
    });

    if (selectedBuilding.streetId !== null && selectedBuilding.streetId !== undefined) {
      nextValues.StreetId = String(selectedBuilding.streetId);
    }

    setEditFieldValues(nextValues);
  }, [selectedBuilding]);

  useEffect(() => {
    const loadSelectTables = async () => {
      if (!selectedBuilding || selectedView !== 'edit') return;
      const tableNames = new Set(
        (selectedBuilding.fields || [])
          .map((field) => field.selectTableName)
          .filter((name) => name && name.trim())
      );
      const missing = [...tableNames].filter((name) => !selectTablesByName[name]);
      if (missing.length === 0) return;

      setSelectTablesLoading(true);
      try {
        const results = await Promise.all(
          missing.map(async (name) => {
            try {
              const options = await api.fetchSelectTable(name);
              return [name, options];
            } catch {
              return [name, []];
            }
          })
        );
        setSelectTablesByName((prev) => {
          const next = { ...prev };
          results.forEach(([name, options]) => {
            next[name] = options;
          });
          return next;
        });
      } finally {
        setSelectTablesLoading(false);
      }
    };

    loadSelectTables();
  }, [selectedBuilding, selectedView, selectTablesByName]);

  const loadBuildings = async (appliedFilters = filters) => {
    setLoading(true);
    setError('');
    try {
      const data = await api.fetchBuildings(expandUpdatedRange(appliedFilters));
      setBuildings(data);
      if (selectedBuilding) {
        const stillExists = data.find((b) => b.id === selectedBuilding.id);
        if (!stillExists) {
          setSelectedBuilding(null);
        }
      }
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const loadBuildingDetails = async (id, view = 'all') => {
    setDetailError('');
    try {
      const building = await api.fetchBuilding(id);
      setSelectedBuilding(building);
      setSelectedView(view);
      sessionStorage.setItem(LAST_BUILDING_KEY, String(id));
    } catch (err) {
      setDetailError(err.message);
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

  const handleExport = async () => {
    if (exporting) return;
    setExportError('');
    setExporting(true);
    try {
      const blob = await api.exportBuildings(expandUpdatedRange(filters));
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      const dateStamp = new Date().toISOString().slice(0, 10);
      link.href = url;
      link.download = `buildings-${dateStamp}.xlsx`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
    } catch (err) {
      setExportError(err.message || 'שגיאה בייצוא קובץ האקסל.');
    } finally {
      setExporting(false);
    }
  };

  const handleCreateChange = (event) => {
    const { name, value } = event.target;
    setCreateForm((form) => ({ ...form, [name]: value }));
  };

  const handleCreateBuilding = async (event) => {
    event.preventDefault();
    setActionMessage('');
    try {
      if (!createForm.streetId) {
        throw new Error('יש לבחור רחוב מהרשימה');
      }
      if (!createForm.bldNum.trim()) {
        throw new Error('יש להזין מספר בית');
      }
      if (!createForm.bldName.trim()) {
        throw new Error('יש להזין כינוי הבניין');
      }
      if (!createForm.shikumStatusId) {
        throw new Error('יש לבחור סטטוס שיקום');
      }
      if (!createForm.category) {
        throw new Error('יש לבחור סיווג');
      }
      if (!createForm.statusSummary.trim()) {
        throw new Error('יש להזין תמונת מצב');
      }
      const statusOption = statusOptions.find(
        (option) => String(option.id) === createForm.shikumStatusId
      );
      if (!statusOption) {
        throw new Error('סטטוס השיקום שבחרת אינו חוקי');
      }
      const sivugOption = sivugOptions.find(
        (option) => String(option.value) === createForm.category
      );
      if (!sivugOption) {
        throw new Error('הסיווג שבחרת אינו חוקי');
      }
      const streetOption = streets.find((street) => String(street.streetId) === createForm.streetId);
      if (!streetOption) {
        throw new Error('יש לבחור רחוב מהרשימה');
      }
      await api.createBuilding({
        fldId: createForm.fldId,
        streetId: streetOption.streetId,
        houseNumber: createForm.bldNum,
        nickname: createForm.bldName,
        bldSivug: createForm.category,
        status: statusOption.value,
        statusSummary: createForm.statusSummary,
        complaints: ''
      });
      setCreateForm({
        fldId: '',
        streetId: '',
        bldNum: '',
        bldName: '',
        statusSummary: '',
        shikumStatusId: '',
        category: ''
      });
      setShowCreateForm(false);
      loadBuildings(filters);
      setActionMessage('המבנה נוסף בהצלחה.');
    } catch (err) {
      setActionMessage(err.message);
    }
  };

  const handleEditFieldChange = (columnName, value) => {
    setEditFieldValues((prev) => ({ ...prev, [columnName]: value }));
  };

  const handleUpdateBuildingFields = async (event) => {
    event.preventDefault();
    if (!selectedBuilding) return;
    setActionMessage('');
    const isMissing = (value) =>
      value === null || value === undefined || (typeof value === 'string' && value.trim() === '');
    const missingField = REQUIRED_EDIT_FIELDS.find((field) => isMissing(editFieldValues[field.key]));
    if (missingField) {
      setActionMessage(`חובה למלא ${missingField.label}.`);
      return;
    }
    try {
      const cleaned = Object.entries(editFieldValues).reduce((acc, [key, value]) => {
        acc[key] = value === '' ? null : value;
        return acc;
      }, {});

      const updated = await api.updateBuildingFields(selectedBuilding.id, cleaned);
      setSelectedBuilding(updated);
      loadBuildings(filters);
      setActionMessage('פרטי המבנה עודכנו.');
    } catch (err) {
      setActionMessage(err.message);
    }
  };

  const handleDeleteBuilding = async (buildingId) => {
    const id = buildingId ?? selectedBuilding?.id;
    if (!id) return;
    const confirmed = window.confirm('למחוק את המבנה לצמיתות?');
    if (!confirmed) return;
    try {
      await api.deleteBuilding(id);
      setSelectedBuilding(null);
      loadBuildings(filters);
      setActionMessage('המבנה הוסר.');
    } catch (err) {
      setActionMessage(err.message);
    }
  };

  const statuses = useMemo(() => statusOptions, [statusOptions]);

  const handleTabChange = (tab) => {
    setSelectedView(tab);
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

  const sortedBuildings = useMemo(() => {
    if (!buildings || buildings.length === 0) return [];
    if (!sortConfig.field) return buildings;

    const compareValues = (aValue, bValue, { numeric = false } = {}) => {
      const aMissing = aValue === null || aValue === undefined || aValue === '';
      const bMissing = bValue === null || bValue === undefined || bValue === '';
      if (aMissing && bMissing) return 0;
      if (aMissing) return 1;
      if (bMissing) return -1;
      if (numeric) return aValue - bValue;
      return String(aValue).localeCompare(String(bValue), 'he');
    };

    const getSortValue = (building) => {
      switch (sortConfig.field) {
        case 'street':
          return building.street;
        case 'houseNumber':
          return building.houseNumber;
        case 'nickname':
          return building.nickname;
        case 'status':
          return statusLabelMap[building.status] || building.status || '';
        case 'bldSivug':
          return getSivugLabel(building.bldSivug);
        case 'sugBaalut':
          return getOwnershipLabel(building.sugBaalut);
        case 'quarter':
          return building.quarter;
        case 'subQuarter':
          return building.subQuarter;
        case 'statisticalArea':
          return building.statisticalArea;
        case 'updatedAt':
          return building.updatedAt ? new Date(building.updatedAt).getTime() : null;
        case 'statusSummary':
          return building.statusSummary;
        default:
          return '';
      }
    };

    const copy = [...buildings];
    copy.sort((a, b) => {
      const aValue = getSortValue(a);
      const bValue = getSortValue(b);
      const cmp = compareValues(aValue, bValue, { numeric: sortConfig.field === 'updatedAt' });
      if (cmp !== 0) return sortConfig.direction === 'desc' ? -cmp : cmp;
      if (sortConfig.field !== 'street') {
        const streetCmp = compareValues(a.street, b.street);
        if (streetCmp !== 0) return streetCmp;
      }
      if (sortConfig.field !== 'houseNumber') {
        const houseCmp = compareValues(a.houseNumber, b.houseNumber);
        if (houseCmp !== 0) return houseCmp;
      }
      return 0;
    });
    return copy;
  }, [buildings, sortConfig, sivugOptions, statusLabelMap, ownershipOptions]);

  const tryParseJson = (value) => {
    if (!value) return null;
    try {
      return JSON.parse(value);
    } catch {
      return null;
    }
  };

  const externalEntries = useMemo(() => {
    if (!selectedBuilding?.external) return [];
    const entries = [
      { key: 'gis', label: 'GIS' },
      { key: 'water', label: 'מים' },
      { key: 'electricity', label: 'חשמל' },
      { key: 'tax', label: 'ארנונה' },
      { key: 'complaints106', label: 'מוקד 106' }
    ];
    return entries
      .map((entry) => ({
        ...entry,
        snapshot: selectedBuilding.external?.[entry.key]
      }))
      .filter((entry) => entry.snapshot);
  }, [selectedBuilding]);

  const fieldsByCategory = useMemo(() => {
    const fields = selectedBuilding?.fields || [];
    return fields.reduce((acc, field) => {
      const category = field.category || 'כללי';
      if (!acc[category]) acc[category] = [];
      acc[category].push(field);
      return acc;
    }, {});
  }, [selectedBuilding]);

  const sortFieldsForDisplay = (fields) => {
    if (!Array.isArray(fields)) return [];
    const fieldPriority = (name) => {
      if (name === 'סיווג') return 0;
      if (name === 'סטטוס שיקום') return 1;
      return 2;
    };
    return fields
      .map((field, index) => ({ field, index }))
      .sort((a, b) => {
        const aPriority = fieldPriority(a.field.fieldName);
        const bPriority = fieldPriority(b.field.fieldName);
        if (aPriority !== bPriority) return aPriority - bPriority;
        return a.index - b.index;
      })
      .map((entry) => entry.field);
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

  const shouldUseTextarea = (fieldName) => {
    if (!fieldName) return false;
    return (
      fieldName.includes('פרטי') ||
      fieldName.includes('תלונות') ||
      fieldName.includes('תמצית') ||
      fieldName.includes('תקציר') ||
      fieldName.includes('הסיבה') ||
      fieldName.includes('הערות')
    );
  };

  const isDateField = (field) => {
    if (!field) return false;
    const name = field.fieldName || '';
    const column = (field.columnName || '').toLowerCase();
    if (name.includes('תאריך')) return true;
    if (column.endsWith('dt') || column.includes('date')) return true;
    if (typeof field.value === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(field.value)) return true;
    return false;
  };

  return (
    <main className="app buildings-app">
      <header className="page-header">
        <div>
          <p className="eyebrow">מאגר מבנים</p>
          <h1>מערכת ניהול מבנים נטושים</h1>
          <p className="subtitle">בצעו חיפושים, עדכונים ומעקב אחר לוגים עירוניים.</p>
        </div>
      </header>

      <section className="filters-card">
        <form className="filters-grid" onSubmit={handleSearch}>
          <label>
            <span>שם רחוב</span>
            <select name="streetId" value={filters.streetId} onChange={handleFilterChange}>
              <option value="">בחר רחוב</option>
              {streets.map((street) => (
                <option key={street.streetId} value={street.streetId}>
                  {street.name}
                </option>
              ))}
            </select>
          </label>
          <label>
            <span>מספר בית</span>
            <input
              type="text"
              name="houseNumber"
              value={filters.houseNumber}
              onChange={handleFilterChange}
              placeholder={BUILDING_FIELD_PLACEHOLDERS.houseNumber}
            />
          </label>
          <label>
            <span>כינוי הבניין</span>
            <input
              type="text"
              name="nickname"
              value={filters.nickname}
              onChange={handleFilterChange}
              placeholder={BUILDING_FIELD_PLACEHOLDERS.nickname}
            />
          </label>
          <label>
            <span>סיווג</span>
            <select name="bldSivug" value={filters.bldSivug} onChange={handleFilterChange}>
              <option value="">בחר סיווג</option>
              {sivugOptions.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
          <label>
            <span>סטטוס שיקום</span>
            <select name="status" value={filters.status} onChange={handleFilterChange}>
              <option value="">בחר סטטוס שיקום</option>
              {statuses.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
          <label>
            <span>סוג הבעלות</span>
            <select name="sugBaalut" value={filters.sugBaalut} onChange={handleFilterChange}>
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
              onChange={handleFilterChange}
              placeholder={BUILDING_FIELD_PLACEHOLDERS.quarter}
            />
          </label>
          <label>
            <span>תת רובע</span>
            <input
              type="text"
              name="subQuarter"
              value={filters.subQuarter}
              onChange={handleFilterChange}
              placeholder={BUILDING_FIELD_PLACEHOLDERS.subQuarter}
            />
          </label>
          <label>
            <span>אזור סטטיסטי</span>
            <input
              type="text"
              name="statisticalArea"
              value={filters.statisticalArea}
              onChange={handleFilterChange}
              placeholder={BUILDING_FIELD_PLACEHOLDERS.statisticalArea}
            />
          </label>
          <label>
            <span>תאריך שינוי - החל מ</span>
            <input
              type="date"
              name="updatedFrom"
              value={filters.updatedFrom}
              onChange={handleFilterChange}
            />
          </label>
          <label>
            <span>תאריך שינוי - עד</span>
            <input
              type="date"
              name="updatedTo"
              value={filters.updatedTo}
              onChange={handleFilterChange}
            />
          </label>
          <label className="full-span">
            <span>תמונת מצב (תמצית מצב)</span>
            <input
              type="text"
              name="statusSummary"
              value={filters.statusSummary}
              onChange={handleFilterChange}
              placeholder={BUILDING_FIELD_PLACEHOLDERS.statusSummary}
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
              <button type="button" className="ghost" onClick={() => setShowCreateForm((prev) => !prev)}>
                {showCreateForm ? 'סגור טופס הוספה' : 'הוסף מבנה'}
              </button>
            )}
            {isAdmin && (
              <button type="button" className="ghost" onClick={handleExport} disabled={exporting}>
                {exporting ? 'מייצא...' : 'יצוא לאקסל'}
              </button>
            )}
          </div>
        </form>
        {loading && <p className="muted">טוען מבנים…</p>}
        {error && <p className="error">שגיאה בטעינת מבנים: {error}</p>}
        {actionMessage && <p className="success">{actionMessage}</p>}
        {exportError && <p className="error">שגיאה בייצוא: {exportError}</p>}
      </section>

      {canEdit && showCreateForm && (
        <section className="panel">
          <h3>הוספת מבנה</h3>
          <form className="form-grid" onSubmit={handleCreateBuilding}>
            <label>
              שם רחוב
              <select name="streetId" value={createForm.streetId} onChange={handleCreateChange} required>
                <option value="">בחר רחוב</option>
                {streets.map((street) => (
                  <option key={street.streetId} value={street.streetId}>
                    {street.name}
                  </option>
                ))}
              </select>
            </label>
            <label>
              מספר בית
              <input
                name="bldNum"
                value={createForm.bldNum}
                onChange={handleCreateChange}
                placeholder={BUILDING_FIELD_PLACEHOLDERS.houseNumber}
                required
              />
            </label>
            <label>
              כינוי הבניין
              <input
                name="bldName"
                value={createForm.bldName}
                onChange={handleCreateChange}
                placeholder={BUILDING_FIELD_PLACEHOLDERS.nickname}
                required
              />
            </label>
            <label>
              סטטוס שיקום
              <select
                name="shikumStatusId"
                value={createForm.shikumStatusId}
                onChange={handleCreateChange}
                required
              >
                <option value="">בחר סטטוס שיקום</option>
                {statusOptions.map((option) => (
                  <option key={option.id} value={option.id}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            <label>
              סיווג
              <select
                name="category"
                value={createForm.category}
                onChange={handleCreateChange}
                required
              >
                <option value="">בחר סיווג</option>
                {sivugOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            <label className="full-span">
              תמונת מצב (תמצית מצב)
              <textarea
                name="statusSummary"
                value={createForm.statusSummary}
                onChange={handleCreateChange}
                placeholder={BUILDING_FIELD_PLACEHOLDERS.statusSummary}
                required
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
        <div className="list-panel full-span">
          <div className="panel-header">
            <h2>תוצאות ({buildings.length})</h2>
          </div>
          <div className="table-wrapper">
            <table>
              <thead>
                <tr>
                  <th aria-sort={getAriaSort('street')}>
                    <button type="button" className="sort-button" onClick={() => handleSortClick('street')}>
                      שם רחוב
                      <span className="sort-indicator" aria-hidden="true">
                        {getSortIndicator('street')}
                      </span>
                    </button>
                  </th>
                  <th aria-sort={getAriaSort('houseNumber')}>
                    <button
                      type="button"
                      className="sort-button"
                      onClick={() => handleSortClick('houseNumber')}
                    >
                      מספר בית
                      <span className="sort-indicator" aria-hidden="true">
                        {getSortIndicator('houseNumber')}
                      </span>
                    </button>
                  </th>
                  <th aria-sort={getAriaSort('nickname')}>
                    <button type="button" className="sort-button" onClick={() => handleSortClick('nickname')}>
                      כינוי הבניין
                      <span className="sort-indicator" aria-hidden="true">
                        {getSortIndicator('nickname')}
                      </span>
                    </button>
                  </th>
                  <th aria-sort={getAriaSort('bldSivug')}>
                    <button type="button" className="sort-button" onClick={() => handleSortClick('bldSivug')}>
                      סיווג
                      <span className="sort-indicator" aria-hidden="true">
                        {getSortIndicator('bldSivug')}
                      </span>
                    </button>
                  </th>
                  <th aria-sort={getAriaSort('status')}>
                    <button type="button" className="sort-button" onClick={() => handleSortClick('status')}>
                      סטטוס שיקום
                      <span className="sort-indicator" aria-hidden="true">
                        {getSortIndicator('status')}
                      </span>
                    </button>
                  </th>
                  <th aria-sort={getAriaSort('sugBaalut')}>
                    <button type="button" className="sort-button" onClick={() => handleSortClick('sugBaalut')}>
                      סוג הבעלות
                      <span className="sort-indicator" aria-hidden="true">
                        {getSortIndicator('sugBaalut')}
                      </span>
                    </button>
                  </th>
                  <th aria-sort={getAriaSort('quarter')}>
                    <button type="button" className="sort-button" onClick={() => handleSortClick('quarter')}>
                      רובע
                      <span className="sort-indicator" aria-hidden="true">
                        {getSortIndicator('quarter')}
                      </span>
                    </button>
                  </th>
                  <th aria-sort={getAriaSort('subQuarter')}>
                    <button type="button" className="sort-button" onClick={() => handleSortClick('subQuarter')}>
                      תת רובע
                      <span className="sort-indicator" aria-hidden="true">
                        {getSortIndicator('subQuarter')}
                      </span>
                    </button>
                  </th>
                  <th aria-sort={getAriaSort('statisticalArea')}>
                    <button
                      type="button"
                      className="sort-button"
                      onClick={() => handleSortClick('statisticalArea')}
                    >
                      אזור סטטיסטי
                      <span className="sort-indicator" aria-hidden="true">
                        {getSortIndicator('statisticalArea')}
                      </span>
                    </button>
                  </th>
                  <th aria-sort={getAriaSort('updatedAt')}>
                    <button type="button" className="sort-button" onClick={() => handleSortClick('updatedAt')}>
                      תאריך שינוי
                      <span className="sort-indicator" aria-hidden="true">
                        {getSortIndicator('updatedAt')}
                      </span>
                    </button>
                  </th>
                  <th aria-sort={getAriaSort('statusSummary')}>
                    <button type="button" className="sort-button" onClick={() => handleSortClick('statusSummary')}>
                      תמונת מצב (תמצית מצב)
                      <span className="sort-indicator" aria-hidden="true">
                        {getSortIndicator('statusSummary')}
                      </span>
                    </button>
                  </th>
                  <th>פעולות</th>
                </tr>
              </thead>
              <tbody>
                {sortedBuildings.map((building) => {
                  const isActive = selectedBuilding && building.id === selectedBuilding.id;
                  const statusValue = building.status || 'Unknown';
                  const statusLabel = statusLabelMap[statusValue] || statusValue;
                  const statusSlug = statusValue.toLowerCase().replace(/\s+/g, '-');
                  const sivugLabel = getSivugLabel(building.bldSivug);
                  const ownershipLabel = getOwnershipLabel(building.sugBaalut);
                  return (
                    <Fragment key={building.id}>
                      <tr
                        className={isActive ? 'active' : ''}
                        onClick={() => {
                          if (isActive) {
                            setSelectedBuilding(null);
                            return;
                          }
                          loadBuildingDetails(building.id, 'all');
                        }}
                      >
                        <td>{building.street}</td>
                        <td>{building.houseNumber}</td>
                        <td>{building.nickname || '—'}</td>
                        <td>{sivugLabel}</td>
                        <td>
                          <span className={`status status-${statusSlug}`}>{statusLabel}</span>
                        </td>
                        <td>{ownershipLabel}</td>
                        <td>{building.quarter || '—'}</td>
                        <td>{building.subQuarter || '—'}</td>
                        <td>{building.statisticalArea || '—'}</td>
                        <td>{formatLogDate(building.updatedAt)}</td>
                        <td>{building.statusSummary || '—'}</td>
                        <td>
                          <button
                            type="button"
                            className="ghost"
                            onClick={(event) => {
                              event.stopPropagation();
                              if (
                                selectedBuilding &&
                                selectedBuilding.id === building.id &&
                                selectedView === 'all'
                              ) {
                                setSelectedBuilding(null);
                                return;
                              }
                              loadBuildingDetails(building.id, 'all');
                            }}
                          >
                            הצג
                          </button>
                          {canEdit && (
                            <button
                              type="button"
                              className="ghost"
                              onClick={(event) => {
                                event.stopPropagation();
                                if (
                                  selectedBuilding &&
                                  selectedBuilding.id === building.id &&
                                  selectedView === 'edit'
                                ) {
                                  setSelectedBuilding(null);
                                  return;
                                }
                                loadBuildingDetails(building.id, 'edit');
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
                                handleDeleteBuilding(building.id);
                              }}
                            >
                              מחק
                            </button>
                          )}
                        </td>
                      </tr>
                      {isActive && selectedBuilding && (
                        <tr>
                          <td colSpan="12">
                            {selectedView === 'edit' && canEdit && (
                              <form onSubmit={handleUpdateBuildingFields} className="details-card">
                                {selectTablesLoading && <p className="muted">טוען טבלאות בחירה…</p>}
                                {Object.entries(fieldsByCategory).map(([category, fields]) => (
                                  <div key={category} className="details-section">
                                    <h4>{category}</h4>
                                    <div className="form-grid">
                                        {sortFieldsForDisplay(fields).map((field) => {
                                          const columnName = field.columnName;
                                          const fieldName = field.fieldName;
                                          if (!columnName) return null;
                                          const required = isRequiredEditColumn(columnName);
                                          if (columnName.toLowerCase() === 'streetid') {
                                            return (
                                              <label key={columnName}>
                                                {getExcelAwareLabel(fieldName)}
                                                <input type="text" value={editFieldValues[columnName] ?? ''} disabled />
                                              </label>
                                            );
                                          }

                                          if (columnName.toLowerCase() === 'streetname') {
                                            return (
                                              <label key={columnName}>
                                                {getExcelAwareLabel(fieldName)}
                                              <select
                                                value={editFieldValues.StreetId ?? ''}
                                                onChange={(e) => handleEditFieldChange('StreetId', e.target.value)}
                                                required
                                              >
                                                <option value="">בחר רחוב</option>
                                                {streets.map((street) => (
                                                  <option key={street.streetId} value={street.streetId}>
                                                    {street.name}
                                                  </option>
                                                ))}
                                              </select>
                                            </label>
                                          );
                                        }

                                        const selectTableName = field.selectTableName;
                                        const selectOptions =
                                          selectTableName && selectTablesByName[selectTableName]
                                            ? selectTablesByName[selectTableName]
                                            : [];
                                        const currentValue = editFieldValues[columnName] ?? '';

                                        if (selectTableName && selectOptions.length > 0) {
                                          return (
                                            <label key={columnName}>
                                              {getExcelAwareLabel(fieldName)}
                                              <select
                                                value={currentValue}
                                                onChange={(e) => handleEditFieldChange(columnName, e.target.value)}
                                                required={required}
                                              >
                                                <option value="">—</option>
                                                {selectOptions.map((opt) => (
                                                  <option key={opt.value} value={opt.value}>
                                                    {opt.label}
                                                  </option>
                                                ))}
                                              </select>
                                            </label>
                                          );
                                        }

                                        const isDate = isDateField(field);
                                        const useTextarea = shouldUseTextarea(fieldName) && !isDate;
                                        const inputType = isDate ? 'date' : 'text';

                                        return (
                                          <label key={columnName} className={useTextarea ? 'full-span' : ''}>
                                            {getExcelAwareLabel(fieldName)}
                                            {useTextarea ? (
                                              <textarea
                                                value={currentValue}
                                                onChange={(e) => handleEditFieldChange(columnName, e.target.value)}
                                                required={required}
                                              />
                                            ) : (
                                              <input
                                                type={inputType}
                                                value={currentValue}
                                                onChange={(e) => handleEditFieldChange(columnName, e.target.value)}
                                                required={required}
                                              />
                                            )}
                                          </label>
                                        );
                                      })}
                                    </div>
                                  </div>
                                ))}
                                <div className="filters-actions">
                                  <button type="submit" className="primary">
                                    שמירת שינויים
                                  </button>
                                  <button type="button" className="ghost" onClick={() => setSelectedBuilding(null)}>
                                    סגירה
                                  </button>
                                </div>
                              </form>
                            )}
                            {selectedView === 'all' && (
                              <div className="details-card">
                                <div>
                                  <p className="eyebrow">פרטי מבנה</p>
                                  <h3>
                                    {selectedBuilding.street} {selectedBuilding.houseNumber}
                                  </h3>
                                </div>

                                {Object.keys(fieldsByCategory).length > 0 ? (
                                  Object.entries(fieldsByCategory).map(([category, fields]) => (
                                    <div key={category} className="details-section">
                                      <h4>{category}</h4>
                                      <dl>
                                        {sortFieldsForDisplay(fields).map((field) => {
                                          const value = displayOrDash(field.value);
                                          const titleParts = [];
                                          if (field.selectTableName)
                                            titleParts.push(`טבלת בחירה: ${field.selectTableName}`);
                                          return (
                                            <div key={`${field.columnName}-${field.fieldName}`}>
                                              <dt title={titleParts.join(' | ')}>{getExcelAwareLabel(field.fieldName)}</dt>
                                              <dd>{value}</dd>
                                            </div>
                                          );
                                        })}
                                      </dl>
                                    </div>
                                  ))
                                ) : (
                                  <p className="muted">אין שדות להצגה.</p>
                                )}

                                <div className="details-section">
                                  <h4>נתונים ממערכות חיצוניות</h4>
                                  {externalEntries.length === 0 && <p className="muted">אין נתונים.</p>}
                                  {externalEntries.map((entry) => {
                                    const payload = entry.snapshot?.payload;
                                    const parsed = typeof payload === 'string' ? tryParseJson(payload) : null;
                                    const status = parsed?.status || null;
                                    const notes = parsed?.notes || null;
                                    const updatedAt = parsed?.updatedAt || null;
                                    return (
                                      <div key={entry.key} className="external-card">
                                        <div className="external-card__header">
                                          <strong>{entry.label}</strong>
                                          <span className="muted small">
                                            {formatLogDate(entry.snapshot?.retrievedAt)}
                                          </span>
                                        </div>
                                        <dl>
                                          <div>
                                            <dt>סטטוס</dt>
                                            <dd>{displayOrDash(status)}</dd>
                                          </div>
                                          <div>
                                            <dt>עודכן במקור</dt>
                                            <dd>{displayOrDash(updatedAt)}</dd>
                                          </div>
                                          <div>
                                            <dt>הערות</dt>
                                            <dd>{displayOrDash(notes)}</dd>
                                          </div>
                                        </dl>
                                      </div>
                                    );
                                  })}
                                </div>

                                <div className="details-section">
                                  <h4>יומן פעולות (אחרונות)</h4>
                                  {selectedBuilding.logs?.length ? (
                                    <ul className="log-list">
                                      {selectedBuilding.logs.map((log) => (
                                        <li key={log.id}>
                                          <span>
                                            {displayOrDash(log.actionType)} — {displayOrDash(log.username)}
                                          </span>
                                          <span className="muted">{formatLogDate(log.createdAt)}</span>
                                        </li>
                                      ))}
                                    </ul>
                                  ) : (
                                    <p className="muted">אין רישומים.</p>
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
                {buildings.length === 0 && !loading && (
                  <tr>
                    <td colSpan="12" className="muted">
                      אין מבנים שעונים על הסינון.
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
