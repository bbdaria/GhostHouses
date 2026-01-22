import { Fragment, useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../api/client.js';
import BuildingModal from '../components/BuildingModal.jsx';
import { useAuth } from '../context/AuthContext.jsx';
import { ROLE_LABELS, STATUS_LABEL_MAP, STATUS_OPTIONS, STATUS_VALUE_BY_ID } from '../i18n.js';
import useDocumentTitle from '../hooks/useDocumentTitle.js';
import { BUILDING_FIELD_PLACEHOLDERS, LAST_BUILDING_KEY } from '../constants.js';
import { formatDate, formatTime, formatDateTime } from '../utils/formatDate.js';

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

const NO_STREET_OPTION = { streetId: -1, name: 'ללא שם רחוב' };
const REQUIRED_EDIT_FIELDS = [
  { key: 'Id', label: 'ID' },
  { key: 'StreetId', label: 'שם רחוב' },
  { key: 'BldNum', label: 'מספר בית' },
  { key: 'BldName', label: 'כינוי הבניין' },
  { key: 'BldSivug', label: 'סיווג' },
  { key: 'ShikumStatus', label: 'סטטוס שיקום' }
];
const REQUIRED_EDIT_COLUMNS = new Set(
  REQUIRED_EDIT_FIELDS.filter((field) => field.key !== 'StreetId').map((field) => field.key)
);
const REQUIRED_CREATE_COLUMNS = new Set(['BldNum', 'BldName', 'BldSivug', 'ShikumStatus']);

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
  const navigate = useNavigate();
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
  const [showModal, setShowModal] = useState(false);
  const [modalMode, setModalMode] = useState('view');
  const [unsavedChanges, setUnsavedChanges] = useState(false);
  const [exporting, setExporting] = useState(false);
  const [exportError, setExportError] = useState('');
  const [exportSelection, setExportSelection] = useState(() => new Set());
  const [exportMode, setExportMode] = useState(false);
  const [showImportModal, setShowImportModal] = useState(false);
  const [importPreviewing, setImportPreviewing] = useState(false);
  const [importApplying, setImportApplying] = useState(false);
  const [importError, setImportError] = useState('');
  const [importFile, setImportFile] = useState(null);
  const [importRows, setImportRows] = useState([]);
  const [importCompareRowId, setImportCompareRowId] = useState(null);
  const [importCompareTargetId, setImportCompareTargetId] = useState(null);
  const [importEditRowId, setImportEditRowId] = useState(null);
  const [importEditValues, setImportEditValues] = useState(null);
  const [importEditSaving, setImportEditSaving] = useState(false);
  const [importEditError, setImportEditError] = useState('');
  const [importSummary, setImportSummary] = useState('');
  const [cardExporting, setCardExporting] = useState(false);
  const [cardExportError, setCardExportError] = useState('');
  const [photoLoading, setPhotoLoading] = useState(false);
  const [photoError, setPhotoError] = useState('');
  const [createFieldTemplate, setCreateFieldTemplate] = useState([]);
  const [createFieldValues, setCreateFieldValues] = useState({});
  const [createTemplateLoading, setCreateTemplateLoading] = useState(false);
  const [createSelectTablesLoading, setCreateSelectTablesLoading] = useState(false);
  const [editFieldValues, setEditFieldValues] = useState({});
  const [selectTablesByName, setSelectTablesByName] = useState({});
  const [selectTablesLoading, setSelectTablesLoading] = useState(false);
  const [actionMessage, setActionMessage] = useState('');
  const [duplicatePrompt, setDuplicatePrompt] = useState('');
  const [editDuplicatePrompt, setEditDuplicatePrompt] = useState('');
  const [selectedView, setSelectedView] = useState('view');
  const [sortConfig, setSortConfig] = useState({ field: 'street', direction: 'asc' });
  const [openViewCategories, setOpenViewCategories] = useState(() => new Set());
  const [openEditCategories, setOpenEditCategories] = useState(() => new Set());
  const [openCreateCategories, setOpenCreateCategories] = useState(() => new Set());
  const [isExternalOpen, setIsExternalOpen] = useState(false);
  const [isLogsOpen, setIsLogsOpen] = useState(false);

  const importBusy = importPreviewing || importApplying;
  const importCompareRow = useMemo(
    () => importRows.find((row) => row.rowNumber === importCompareRowId) || null,
    [importRows, importCompareRowId]
  );
  const importEditRow = useMemo(
    () => importRows.find((row) => row.rowNumber === importEditRowId) || null,
    [importRows, importEditRowId]
  );
  const importCompareTarget = useMemo(() => {
    if (!importCompareRow) return null;
    const matches = importCompareRow.addressMatches || [];
    if (importCompareTargetId) {
      const found = matches.find((match) => match.id === importCompareTargetId);
      if (found) return found;
    }
    return matches[0] || null;
  }, [importCompareRow, importCompareTargetId]);

  const rehabSivugValue = useMemo(() => {
    const match =
      sivugOptions.find((option) => option.label === 'ריק ובהליך שיקום') ||
      sivugOptions.find((option) => option.label && option.label.includes('שיקום'));
    return match ? String(match.value) : null;
  }, [sivugOptions]);

  const createSivugValue = createFieldValues.BldSivug ?? '';
  const isRehabStatusRequired = useMemo(() => {
    if (!rehabSivugValue || !createSivugValue) return false;
    return String(createSivugValue) === rehabSivugValue;
  }, [createSivugValue, rehabSivugValue]);

  const editSivugValue = editFieldValues.BldSivug ?? selectedBuilding?.bldSivug ?? '';
  const isEditRehabStatusRequired = useMemo(() => {
    if (!rehabSivugValue) return false;
    if (editSivugValue === '' || editSivugValue === null || editSivugValue === undefined) return false;
    return String(editSivugValue) === rehabSivugValue;
  }, [editSivugValue, rehabSivugValue]);

  const isFilterRehabStatusRequired = useMemo(() => {
    if (!rehabSivugValue) return false;
    if (!filters.bldSivug && filters.bldSivug !== 0) return false;
    return String(filters.bldSivug) === rehabSivugValue;
  }, [filters.bldSivug, rehabSivugValue]);

  const canEdit = useMemo(
    () => user && (user.role === 'Editor' || user.role === 'Admin'),
    [user]
  );
  const isAdmin = user?.role === 'Admin';
  const roleLabel = ROLE_LABELS[user?.role] || user?.role;
  const formatLogDate = (value) => {
    if (!value) return '—';
    try {
      return formatDateTime(value);
    } catch {
      return value;
    }
  };

  const loadStreets = useCallback(async () => {
    try {
      const data = await api.fetchStreets();
      setStreets(data || []);
    } catch {
      setStreets([]);
    }
  }, []);

  const loadCreateTemplate = useCallback(async () => {
    setCreateTemplateLoading(true);
    try {
      const fields = await api.fetchBuildingFieldTemplate();
      setCreateFieldTemplate(fields || []);
      const nextValues = {};
      (fields || []).forEach((field) => {
        if (!field?.columnName) return;
        if (field.columnName.toLowerCase() === 'streetname') return;
        if (field.selectTableName) {
          const rawValue = field.rawValue;
          nextValues[field.columnName] =
            rawValue === null || rawValue === undefined || rawValue === 0 ? '' : String(rawValue);
        } else {
          nextValues[field.columnName] = field.value ?? '';
        }
        if (field.columnName.toLowerCase() === 'id' && nextValues[field.columnName] === '0') {
          nextValues[field.columnName] = '';
        }
      });
      if (!Object.prototype.hasOwnProperty.call(nextValues, 'StreetId')) {
        nextValues.StreetId = '';
      }
      setCreateFieldValues(nextValues);
    } catch {
      setCreateFieldTemplate([]);
      setCreateFieldValues({});
    } finally {
      setCreateTemplateLoading(false);
    }
  }, []);

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
  const isDataImageValue = (value) =>
    typeof value === 'string' && value.trim().startsWith('data:image');
  const renderImportValue = (value) => {
    const displayValue = displayOrDash(value);
    if (isDataImageValue(displayValue)) {
      return <img className="log-change-image" src={displayValue} alt="תמונה" />;
    }
    return displayValue;
  };
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
  const formatStatusFieldValue = (field) => {
    if (!field || field.fieldName !== 'סטטוס שיקום') {
      return displayOrDash(field?.value);
    }
    const value = field.value;
    if (!value || value === '0' || value === 'Unknown' || value === 'לא ידוע') {
      return '—';
    }
    return value;
  };
  const isRequiredEditColumn = (columnName) => REQUIRED_EDIT_COLUMNS.has(columnName);
  const isRequiredCreateColumn = (columnName) => REQUIRED_CREATE_COLUMNS.has(columnName);
  const requiredImportLabels = {
    Id: 'ID',
    StreetId: 'שם רחוב',
    BldNum: 'מספר בית',
    BldName: 'כינוי הבניין',
    BldSivug: 'סיווג',
    ShikumStatus: 'סטטוס שיקום'
  };
  const resolveSelectValue = (raw, options) => {
    if (raw === null || raw === undefined) return null;
    const text = String(raw).trim();
    if (!text) return null;
    const numeric = Number(text);
    if (Number.isFinite(numeric)) return numeric;
    const match = options.find((opt) => String(opt.label || '').trim() === text);
    if (!match) return null;
    const value = match.value ?? match.id;
    return value === null || value === undefined ? null : Number(value);
  };
  const resolveSelectLabel = (raw, options) => {
    if (raw === null || raw === undefined) return '';
    const text = String(raw).trim();
    if (!text) return '';
    const numeric = Number(text);
    if (Number.isFinite(numeric)) {
      const match = options.find(
        (opt) => String(opt.value ?? opt.id) === String(numeric)
      );
      return match?.label ?? text;
    }
    return text;
  };
  const getMissingImportRequired = useCallback(
    (values) => {
      const missing = [];
      const getValue = (key) => (values?.[key] ?? '').toString().trim();

      const idValue = getValue('Id');
      if (idValue && (!Number.isFinite(Number(idValue)) || Number(idValue) <= 0)) {
        missing.push('Id');
      }

      const streetIdValue = getValue('StreetId');
      if (!streetIdValue || !Number.isFinite(Number(streetIdValue))) {
        missing.push('StreetId');
      }

      if (!getValue('BldNum')) {
        missing.push('BldNum');
      }

      if (!getValue('BldName')) {
        missing.push('BldName');
      }

      const sivugRaw = getValue('BldSivug');
      const sivugValue = resolveSelectValue(sivugRaw, sivugOptions);
      if (!sivugRaw || sivugValue === null || Number.isNaN(sivugValue)) {
        missing.push('BldSivug');
      }

      if (rehabSivugValue && sivugValue !== null && String(sivugValue) === String(rehabSivugValue)) {
        const shikumRaw = getValue('ShikumStatus');
        const shikumValue = resolveSelectValue(shikumRaw, statusOptions);
        if (!shikumRaw || shikumValue === null || Number.isNaN(shikumValue)) {
          missing.push('ShikumStatus');
        }
      }

      return missing;
    },
    [rehabSivugValue, sivugOptions, statusOptions]
  );
  const getImportRequiredLabel = (columnName) =>
    requiredImportLabels[columnName] || getExcelAwareLabel(columnName) || columnName;

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
  }, [loadStreets]);

  useEffect(() => {
    setExportSelection((prev) => {
      const currentIds = new Set(buildings.map((building) => building.id));
      const next = new Set();
      prev.forEach((id) => {
        if (currentIds.has(id)) {
          next.add(id);
        }
      });
      return next;
    });
  }, [buildings]);

  useEffect(() => {
    if (showModal && modalMode === 'create') {
      loadStreets();
      loadCreateTemplate();
    }
  }, [loadStreets, loadCreateTemplate, showModal, modalMode]);

  useEffect(() => {
    if (!selectedBuilding) {
      setEditFieldValues({});
      setPhotoError('');
      setPhotoLoading(false);
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
    } else if (selectedBuilding.street === NO_STREET_OPTION.name) {
      nextValues.StreetId = String(NO_STREET_OPTION.streetId);
    }

    setEditFieldValues(nextValues);
    setPhotoError('');
    setPhotoLoading(false);
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

  useEffect(() => {
    const loadCreateSelectTables = async () => {
      if (!showModal || modalMode !== 'create') return;
      if (!createFieldTemplate || createFieldTemplate.length === 0) return;
      const tableNames = new Set(
        createFieldTemplate
          .map((field) => field.selectTableName)
          .filter((name) => name && name.trim())
      );
      const missing = [...tableNames].filter((name) => !selectTablesByName[name]);
      if (missing.length === 0) {
        setCreateSelectTablesLoading(false);
        return;
      }

      setCreateSelectTablesLoading(true);
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
        setCreateSelectTablesLoading(false);
      }
    };

    loadCreateSelectTables();
  }, [showModal, modalMode, createFieldTemplate, selectTablesByName]);

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

  const loadBuildingDetails = async (id, view = 'view') => {
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
    setFilters((prev) => {
      const next = { ...prev, [name]: value };
      if (name === 'bldSivug' && String(value) !== rehabSivugValue) {
        next.status = '';
      }
      return next;
    });
  };

  const handleSearch = (event) => {
    event.preventDefault();
    loadBuildings(filters);
  };

  const handleReset = () => {
    setFilters(initialFilters);
    loadBuildings(initialFilters);
  };

  const downloadExportFile = (blob, prefix = 'buildings') => {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    const dateStamp = new Date().toISOString().slice(0, 10);
    const extension = blob?.type === 'application/zip' ? 'zip' : 'xlsx';
    link.href = url;
    link.download = `${prefix}-${dateStamp}.${extension}`;
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
    setExportSelection(new Set(buildings.map((b) => b.id)));
  };

  const handleToggleExportBuilding = (id) => {
    if (!exportMode) return;
    setExportSelection((prev) => {
      const next = new Set(prev);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  };

  const handleExportSelected = async (includeImages) => {
    if (exporting) return;
    setExportError('');
    setExporting(true);
    try {
      const blob = await api.exportBuildingsByIds([...exportSelection], includeImages);
      downloadExportFile(blob);
      setExportMode(false);
      setExportSelection(new Set());
    } catch (err) {
      setExportError(err.message || 'שגיאה בייצוא קובץ הייצוא.');
    } finally {
      setExporting(false);
    }
  };

  const handleExportAction = () => {
    if (!exportMode) {
      setExportMode(true);
    }
  };

  const handleCancelExportMode = () => {
    setExportMode(false);
    setExportSelection(new Set());
  };

  const openImportModal = () => {
    setImportError('');
    setImportSummary('');
    setImportFile(null);
    setImportRows([]);
    setImportCompareRowId(null);
    setImportCompareTargetId(null);
    setImportEditRowId(null);
    setImportEditValues(null);
    setImportEditError('');
    setImportPreviewing(false);
    setImportApplying(false);
    setShowImportModal(true);
  };

  const closeImportModal = () => {
    setImportError('');
    setImportSummary('');
    setImportFile(null);
    setImportRows([]);
    setImportCompareRowId(null);
    setImportCompareTargetId(null);
    setImportEditRowId(null);
    setImportEditValues(null);
    setImportEditError('');
    setImportPreviewing(false);
    setImportApplying(false);
    setShowImportModal(false);
  };

  const handleImportFileChange = (event) => {
    const file = event.target.files?.[0] ?? null;
    setImportFile(file);
    setImportError('');
    setImportSummary('');
    setImportRows([]);
    setImportCompareRowId(null);
    setImportCompareTargetId(null);
    setImportEditRowId(null);
    setImportEditValues(null);
    setImportEditError('');
  };

  const buildImportRows = (rows) =>
    rows.map((row) => {
      const missingRequired = Array.isArray(row.missingRequired)
        ? row.missingRequired
        : getMissingImportRequired(row.values);
      const hasIdConflict = Boolean(row.hasIdConflict);
      return {
        ...row,
        missingRequired,
        hasIdConflict,
        decision: row.decision ?? null,
        replaceIds: Array.isArray(row.replaceIds) ? row.replaceIds : [],
        allowDuplicate: false,
        compareTargetId: row.addressMatches?.[0]?.id ?? null
      };
    });

  const applyBatchIdConflicts = (rows) => {
    const idCounts = rows.reduce((acc, row) => {
      const rawId = row.values?.Id ?? '';
      const idValue = String(rawId).trim();
      if (!idValue) return acc;
      const parsed = Number(idValue);
      if (!Number.isInteger(parsed) || parsed <= 0) return acc;
      acc[parsed] = (acc[parsed] || 0) + 1;
      return acc;
    }, {});

    return rows.map((row) => {
      const rawId = row.values?.Id ?? '';
      const idValue = String(rawId).trim();
      const parsed = Number(idValue);
      const batchConflict = Number.isInteger(parsed) && parsed > 0 && idCounts[parsed] > 1;
      let warnings = Array.isArray(row.warnings) ? [...row.warnings] : [];
      if (batchConflict && !warnings.includes('ID מופיע יותר מפעם אחת בקובץ הייבוא.')) {
        warnings.push('ID מופיע יותר מפעם אחת בקובץ הייבוא.');
      }
      if (!batchConflict) {
        warnings = warnings.filter((warning) => warning !== 'ID מופיע יותר מפעם אחת בקובץ הייבוא.');
      }
      const dbConflict = Boolean(row.hasIdConflict);
      return {
        ...row,
        batchIdConflict: batchConflict,
        warnings,
        hasIdConflict: dbConflict || batchConflict
      };
    });
  };

  const applyAutoDecisions = (rows) =>
    rows.map((row) => (row.decision ? row : { ...row, decision: computeRowDecision(row) }));

  const normalizeImportRows = (rows) => applyAutoDecisions(applyBatchIdConflicts(rows));

  const updateImportRow = (rowNumber, updates) => {
    setImportRows((prev) =>
      normalizeImportRows(prev.map((row) => (row.rowNumber === rowNumber ? { ...row, ...updates } : row)))
    );
  };

  const handlePreviewImport = async () => {
    if (!importFile) {
      setImportError('נא לבחור קובץ אקסל או ZIP.');
      return;
    }
    setImportError('');
    setImportSummary('');
    setImportPreviewing(true);
    try {
      const result = await api.previewImportBuildings(importFile);
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
    const invalidRows = importRows.filter(
      (row) => row.decision === null && ((row.missingRequired?.length ?? 0) > 0 || row.hasIdConflict)
    );
    if (invalidRows.length > 0) {
      setImportError('יש שורות שחובה לתקן לפני הייבוא.');
      return;
    }

    const unresolvedConflicts = importRows.filter(
      (row) =>
        row.decision === null &&
        (row.addressMatches?.length ?? 0) > 0 &&
        !row.exactMatch
    );
    if (unresolvedConflicts.length > 0) {
      setImportError('יש כפילויות שטרם טופלו.');
      return;
    }

    const payloadRows = importRows
      .filter((row) => row.decision)
      .map((row) => ({
        rowNumber: row.rowNumber,
        action: row.decision,
        values: row.values,
        allowDuplicate: row.decision === 'add_anyway' || row.decision === 'replace',
        replaceIds: row.decision === 'replace' ? row.replaceIds : null
      }));

    if (payloadRows.length === 0) {
      setImportError('אין שורות לביצוע.');
      return;
    }

    setImportError('');
    setImportApplying(true);
    try {
      const result = await api.applyImportBuildings(payloadRows);
      const summary = `הייבוא הושלם. נוספו ${result.created} מבנים, עודכנו ${result.updated}, דולגו ${result.skipped}.`;
      setActionMessage(summary);
      closeImportModal();
      loadBuildings(filters);
    } catch (err) {
      setImportError(err.message || 'שגיאה בייבוא קובץ הייבוא.');
    } finally {
      setImportApplying(false);
    }
  };

  const computeRowDecision = (row) => {
    const missingRequired = row.missingRequired?.length ?? 0;
    const hasAddressConflict = (row.addressMatches?.length ?? 0) > 0;
    if (row.exactMatch) return 'skip';
    if (!row.hasIdConflict && missingRequired === 0 && !hasAddressConflict) return 'create';
    return null;
  };

  const handleResolveConflict = (rowNumber, decision, replaceIds = []) => {
    updateImportRow(rowNumber, {
      decision,
      replaceIds,
      allowDuplicate: decision === 'add_anyway' || decision === 'replace'
    });
  };

  const handleImportSkipAll = () => {
    setImportRows((prev) =>
      normalizeImportRows(
        prev.map((row) => {
          if (row.decision) return row;
          if ((row.missingRequired?.length ?? 0) > 0 || row.hasIdConflict) return row;
          if ((row.addressMatches?.length ?? 0) === 0 || row.exactMatch) return row;
          return { ...row, decision: 'skip' };
        })
      )
    );
  };

  const handleImportReplaceAll = () => {
    setImportRows((prev) =>
      normalizeImportRows(
        prev.map((row) => {
          if (row.decision) return row;
          if ((row.missingRequired?.length ?? 0) > 0 || row.hasIdConflict) return row;
          if ((row.addressMatches?.length ?? 0) === 0 || row.exactMatch) return row;
          const allIds = row.addressMatches?.map((match) => match.id) ?? [];
          return { ...row, decision: 'replace', replaceIds: allIds, allowDuplicate: true };
        })
      )
    );
  };

  const handleImportAddAnywayAll = () => {
    setImportRows((prev) =>
      normalizeImportRows(
        prev.map((row) => {
          if (row.decision) return row;
          if ((row.missingRequired?.length ?? 0) > 0 || row.hasIdConflict) return row;
          if ((row.addressMatches?.length ?? 0) === 0 || row.exactMatch) return row;
          return { ...row, decision: 'add_anyway', allowDuplicate: true };
        })
      )
    );
  };

  const openImportCompare = (rowNumber) => {
    const row = importRows.find((item) => item.rowNumber === rowNumber);
    if (!row) return;
    if ((row.replaceIds?.length ?? 0) === 0 && (row.addressMatches?.length ?? 0) > 0) {
      updateImportRow(rowNumber, { replaceIds: row.addressMatches.map((match) => match.id) });
    }
    setImportCompareRowId(rowNumber);
    const targetId = row.compareTargetId ?? row.addressMatches?.[0]?.id ?? null;
    setImportCompareTargetId(targetId);
  };

  const closeImportCompare = () => {
    setImportCompareRowId(null);
    setImportCompareTargetId(null);
  };

  const handleSelectCompareTarget = (rowNumber, targetId) => {
    setImportCompareTargetId(targetId);
    updateImportRow(rowNumber, { compareTargetId: targetId });
  };

  const toggleReplaceSelection = (rowNumber, targetId) => {
    setImportRows((prev) =>
      normalizeImportRows(
        prev.map((row) => {
          if (row.rowNumber !== rowNumber) return row;
          const current = Array.isArray(row.replaceIds) ? row.replaceIds : [];
          const next = current.includes(targetId)
            ? current.filter((id) => id !== targetId)
            : [...current, targetId];
          return { ...row, replaceIds: next };
        })
      )
    );
  };

  const openImportEdit = (rowNumber) => {
    const row = importRows.find((item) => item.rowNumber === rowNumber);
    if (!row) return;
    setImportEditRowId(rowNumber);
    const nextValues = { ...(row.values || {}) };
    nextValues.BldSivug = resolveSelectLabel(nextValues.BldSivug, sivugOptions);
    nextValues.ShikumStatus = resolveSelectLabel(nextValues.ShikumStatus, statusOptions);
    setImportEditValues(nextValues);
    setImportEditError('');
  };

  const closeImportEdit = () => {
    setImportEditRowId(null);
    setImportEditValues(null);
    setImportEditError('');
  };

  const handleImportEditValueChange = (field, value) => {
    setImportEditValues((prev) => {
      const next = { ...(prev || {}), [field]: value };
      if (field === 'Id' && String(value ?? '').trim() === '0') {
        next.Id = '';
      }
      if (field === 'BldSivug') {
        const sivugValue = resolveSelectValue(value, sivugOptions);
        if (rehabSivugValue && String(sivugValue) !== String(rehabSivugValue)) {
          next.ShikumStatus = '';
        }
      }
      return next;
    });
  };

  const handleImportEditSave = async () => {
    if (!importEditRowId || !importEditValues) return;
    setImportEditSaving(true);
    setImportEditError('');
    try {
      const result = await api.validateImportRow(importEditValues);
      updateImportRow(importEditRowId, {
        values: result.values || importEditValues,
        addressMatches: result.addressMatches || [],
        idMatch: result.idMatch || null,
        hasIdConflict: Boolean(result.hasIdConflict),
        exactMatch: Boolean(result.exactMatch),
        missingRequired: Array.isArray(result.missingRequired) ? result.missingRequired : [],
        warnings: Array.isArray(result.warnings) ? result.warnings : [],
        importFields: result.importFields || [],
        compareTargetId: result.addressMatches?.[0]?.id ?? null
      });
      closeImportEdit();
    } catch (err) {
      setImportEditError(err.message || 'שגיאה בעדכון השורה.');
    } finally {
      setImportEditSaving(false);
    }
  };

  const handleExportCard = async (building) => {
    if (!building || cardExporting) return;
    setCardExportError('');
    setCardExporting(true);
    try {
      const blob = await api.exportBuildingCard(building.id);
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `building-card-${building.id}.pptx`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
    } catch (err) {
      setCardExportError(err.message || 'שגיאה בייצוא כרטיס מבנה.');
    } finally {
      setCardExporting(false);
    }
  };

  const handleCreateFieldChange = (columnName, value) => {
    setCreateFieldValues((prev) => {
      const next = { ...prev, [columnName]: value };
      if (columnName === 'BldSivug' && String(value) !== rehabSivugValue) {
        next.ShikumStatus = '';
      }
      return next;
    });
    setDuplicatePrompt('');
    setUnsavedChanges(true);
  };

  const openCreateModal = () => {
    setModalMode('create');
    setShowModal(true);
    setUnsavedChanges(false);
    setDuplicatePrompt('');
  };

  const openBuildingModal = async (id, view = 'view') => {
    try {
      await loadBuildingDetails(id, view);
      setModalMode(view);
      setShowModal(true);
      setUnsavedChanges(false);
    } catch (err) {
      // loadBuildingDetails sets detailError
    }
  };

  const handleCloseModal = () => {
    if (unsavedChanges) {
      const confirmed = window.confirm('ישנם שינויים שלא נשמרו. האם ברצונך לסגור ללא שמירה?');
      if (!confirmed) return;
    }
    setShowModal(false);
    setModalMode('view');
    setUnsavedChanges(false);
    setSelectedBuilding(null);
    setDuplicatePrompt('');
    setEditDuplicatePrompt('');
  };

  const handleOpenEditModal = () => {
    if (!selectedBuilding) return;
    openBuildingModal(selectedBuilding.id, 'edit');
  };

  const handleOpenLogsModal = () => {
    if (!selectedBuilding) return;
    navigate(`/logs?buildingId=${selectedBuilding.id}`);
  };

  const handleCreateBuilding = async (event, allowDuplicate = false) => {
    if (event && event.preventDefault) event.preventDefault();
    setActionMessage('');
    if (!allowDuplicate) {
      setDuplicatePrompt('');
    }
    try {
      const getCreateValue = (key) => createFieldValues[key] ?? '';
      const streetId = String(getCreateValue('StreetId'));
      const houseNumber = String(getCreateValue('BldNum')).trim();
      const buildingName = String(getCreateValue('BldName')).trim();
      const bldSivug = String(getCreateValue('BldSivug'));
      const shikumStatusId = String(getCreateValue('ShikumStatus'));
      const statusSummary = getCreateValue('StatusSummary');
      const idRaw = String(getCreateValue('Id')).trim();
      const complaints = getCreateValue('complaints') || getCreateValue('Complaints');

      const idNumber = idRaw ? Number(idRaw) : null;
      if (idRaw && (!Number.isInteger(idNumber) || idNumber <= 0)) {
        throw new Error('ID חייב להיות מספר חיובי');
      }
      if (!streetId) {
        throw new Error('יש לבחור רחוב מהרשימה');
      }
      if (!houseNumber) {
        throw new Error('יש להזין מספר בית');
      }
      if (!buildingName) {
        throw new Error('יש להזין כינוי הבניין');
      }
      if (!bldSivug) {
        throw new Error('יש לבחור סיווג');
      }
      if (isRehabStatusRequired && !shikumStatusId) {
        throw new Error('יש לבחור סטטוס שיקום');
      }
      let statusOption = null;
      if (isRehabStatusRequired) {
        statusOption = statusOptions.find(
          (option) => String(option.id) === shikumStatusId
        );
        if (!statusOption) {
          throw new Error('סטטוס השיקום שבחרת אינו חוקי');
        }
      }
      const sivugOption = sivugOptions.find(
        (option) => String(option.value) === bldSivug
      );
      if (!sivugOption) {
        throw new Error('הסיווג שבחרת אינו חוקי');
      }
      const streetOption = streets.find((street) => String(street.streetId) === streetId);
      if (!streetOption) {
        throw new Error('יש לבחור רחוב מהרשימה');
      }
      const created = await api.createBuilding({
        id: idNumber ?? undefined,
        streetId: streetOption.streetId,
        houseNumber,
        nickname: buildingName,
        bldSivug,
        status: statusOption ? statusOption.value : undefined,
        statusSummary,
        complaints,
        allowDuplicate
      });
      const coreColumns = new Set([
        'Id',
        'StreetId',
        'StreetName',
        'BldNum',
        'BldName',
        'BldSivug',
        'ShikumStatus',
        'StatusSummary',
        'complaints',
        'Complaints'
      ]);
      const extraFields = Object.entries(createFieldValues).reduce((acc, [column, value]) => {
        if (!column) return acc;
        if (coreColumns.has(column)) return acc;
        if (column.toLowerCase() === 'streetname') return acc;
        if (value === null || value === undefined) return acc;
        if (typeof value === 'string' && value.trim() === '') return acc;
        acc[column] = value;
        return acc;
      }, {});
      if (Object.keys(extraFields).length > 0) {
        await api.updateBuildingFields(created.id, extraFields, allowDuplicate);
      }
      setDuplicatePrompt('');
      loadCreateTemplate();
      setShowModal(false);
      setUnsavedChanges(false);
      loadBuildings(filters);
      setActionMessage('המבנה נוסף בהצלחה.');
    } catch (err) {
      if (err?.payload?.isIdDuplicate) {
        setActionMessage(err?.payload?.error || 'קיים מבנה עם ID זה');
        return;
      }
      if (err?.payload?.isDuplicate || err?.status === 409) {
        setDuplicatePrompt(err?.payload?.error || 'נמצאה כפילות');
        return;
      }
      setActionMessage(err.message);
    }
  };

  const handleDuplicateConfirm = () => {
    handleCreateBuilding(null, true);
  };

  const handleDuplicateCancel = () => {
    setDuplicatePrompt('');
  };

  const handleEditFieldChange = (columnName, value) => {
    setEditFieldValues((prev) => {
      const next = { ...prev, [columnName]: value };
      if (columnName === 'BldSivug' && String(value) !== rehabSivugValue) {
        next.ShikumStatus = '';
      }
      return next;
    });
    setEditDuplicatePrompt('');
    setUnsavedChanges(true);
  };

  const handleUpdateBuildingFields = async (event, allowDuplicate = false) => {
    if (event && event.preventDefault) event.preventDefault();
    if (!selectedBuilding) return;
    setActionMessage('');
    if (!allowDuplicate) {
      setEditDuplicatePrompt('');
    }
    const isMissing = (value) =>
      value === null || value === undefined || (typeof value === 'string' && value.trim() === '');
    const missingField = REQUIRED_EDIT_FIELDS.find((field) => {
      if (field.key === 'ShikumStatus' && !isEditRehabStatusRequired) return false;
      return isMissing(editFieldValues[field.key]);
    });
    if (missingField) {
      setActionMessage(`חובה למלא ${missingField.label}.`);
      return;
    }
    try {
      const cleaned = Object.entries(editFieldValues).reduce((acc, [key, value]) => {
        acc[key] = value === '' ? null : value;
        return acc;
      }, {});

      const updated = await api.updateBuildingFields(selectedBuilding.id, cleaned, allowDuplicate);
      setSelectedBuilding(updated);
      loadBuildings(filters);
      setActionMessage('פרטי המבנה עודכנו.');
      setEditDuplicatePrompt('');
      setUnsavedChanges(false);
    } catch (err) {
      if (err?.payload?.isIdDuplicate) {
        setActionMessage(err?.payload?.error || 'קיים מבנה עם ID זה');
        return;
      }
      if (err?.payload?.isDuplicate || err?.status === 409) {
        setEditDuplicatePrompt(err?.payload?.error || 'נמצאה כפילות');
        return;
      }
      setActionMessage(err.message);
    }
  };

  const handleEditDuplicateConfirm = () => {
    handleUpdateBuildingFields(null, true);
  };

  const handleEditDuplicateCancel = () => {
    setEditDuplicatePrompt('');
  };

  const readFileAsDataUrl = (file) =>
    new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => resolve(reader.result);
      reader.onerror = () => reject(new Error('שגיאה בקריאת הקובץ.'));
      reader.readAsDataURL(file);
    });

  const handlePhotoUpload = async (file) => {
    if (!selectedBuilding || !file) return;
    setPhotoError('');
    setActionMessage('');
    if (!file.type.startsWith('image/')) {
      setPhotoError('נא לבחור קובץ תמונה.');
      return;
    }
    const maxSizeMb = 5;
    if (file.size > maxSizeMb * 1024 * 1024) {
      setPhotoError(`גודל התמונה חייב להיות עד ${maxSizeMb}MB.`);
      return;
    }
    if (selectedBuilding.photos?.length) {
      setPhotoError('ניתן לשמור תמונה אחת בלבד.');
      return;
    }
    try {
      setPhotoLoading(true);
      const dataUrl = await readFileAsDataUrl(file);
      const updated = await api.updateBuildingFields(selectedBuilding.id, { PhotoUrls: dataUrl }, true);
      setSelectedBuilding(updated);
      setActionMessage('התמונה נשמרה.');
    } catch (err) {
      setPhotoError(err.message || 'שגיאה בהעלאת התמונה.');
    } finally {
      setPhotoLoading(false);
    }
  };

  const handlePhotoDelete = async () => {
    if (!selectedBuilding) return;
    setPhotoError('');
    setActionMessage('');
    try {
      setPhotoLoading(true);
      const updated = await api.updateBuildingFields(selectedBuilding.id, { PhotoUrls: '' }, true);
      setSelectedBuilding(updated);
      setActionMessage('התמונה נמחקה.');
    } catch (err) {
      setPhotoError(err.message || 'שגיאה במחיקת התמונה.');
    } finally {
      setPhotoLoading(false);
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
  const allSelected = useMemo(
    () =>
      exportMode &&
      buildings.length > 0 &&
      buildings.every((building) => exportSelection.has(building.id)),
    [buildings, exportSelection, exportMode]
  );
  const importStats = useMemo(() => {
    return importRows.reduce(
      (acc, row) => {
        const action = row.decision;
        if (!action) {
          acc.pending += 1;
          return acc;
        }
        if (action === 'create') acc.create += 1;
        else if (action === 'replace') acc.replace += 1;
        else if (action === 'add_anyway') acc.addAnyway += 1;
        else acc.skip += 1;
        return acc;
      },
      { create: 0, replace: 0, addAnyway: 0, skip: 0, pending: 0 }
    );
  }, [importRows]);
  const importStage1Rows = useMemo(
    () =>
      importRows.filter(
        (row) => row.decision === null && ((row.missingRequired?.length ?? 0) > 0 || row.hasIdConflict)
      ),
    [importRows]
  );
  const importStage2Rows = useMemo(
    () =>
      importRows.filter(
        (row) =>
          row.decision === null &&
          (row.missingRequired?.length ?? 0) === 0 &&
          !row.hasIdConflict &&
          (row.addressMatches?.length ?? 0) > 0 &&
          !row.exactMatch
      ),
    [importRows]
  );
  const importReadyToApply = importRows.length > 0 && importStage1Rows.length === 0 && importStage2Rows.length === 0;

  const handleTabChange = (tab) => {
    setSelectedView(tab);
  };

  const handleCategoryToggleKeyDown = (event, toggle) => {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      toggle();
    }
  };

  const toggleViewCategory = (category) => {
    setOpenViewCategories((prev) => {
      const next = new Set(prev);
      if (next.has(category)) {
        next.delete(category);
      } else {
        next.add(category);
      }
      return next;
    });
  };

  const toggleEditCategory = (category) => {
    setOpenEditCategories((prev) => {
      const next = new Set(prev);
      if (next.has(category)) {
        next.delete(category);
      } else {
        next.add(category);
      }
      return next;
    });
  };

  const toggleCreateCategory = (category) => {
    setOpenCreateCategories((prev) => {
      const next = new Set(prev);
      if (next.has(category)) {
        next.delete(category);
      } else {
        next.add(category);
      }
      return next;
    });
  };

  const toggleExternalSection = () => {
    setIsExternalOpen((prev) => !prev);
  };

  const toggleLogsSection = () => {
    setIsLogsOpen((prev) => !prev);
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

  const createFieldsByCategory = useMemo(() => {
    const fields = createFieldTemplate || [];
    return fields.reduce((acc, field) => {
      const category = field.category || 'כללי';
      if (!acc[category]) acc[category] = [];
      acc[category].push(field);
      return acc;
    }, {});
  }, [createFieldTemplate]);

  const orderedFieldGroups = useMemo(() => {
    const entries = Object.entries(fieldsByCategory);
    if (entries.length === 0) return [];
    const priority = (category) => {
      if (category === 'מידע כללי') return 0;
      if (category === 'פרטים מזהים') return 1;
      return 2;
    };
    return entries
      .map((entry, index) => ({ entry, index }))
      .sort((a, b) => {
        const aPriority = priority(a.entry[0]);
        const bPriority = priority(b.entry[0]);
        if (aPriority !== bPriority) return aPriority - bPriority;
        return a.index - b.index;
      })
      .map((item) => item.entry);
  }, [fieldsByCategory]);

  const createOrderedFieldGroups = useMemo(() => {
    const entries = Object.entries(createFieldsByCategory);
    if (entries.length === 0) return [];
    const priority = (category) => {
      if (category === 'מידע כללי') return 0;
      if (category === 'פרטים מזהים') return 1;
      return 2;
    };
    return entries
      .map((entry, index) => ({ entry, index }))
      .sort((a, b) => {
        const aPriority = priority(a.entry[0]);
        const bPriority = priority(b.entry[0]);
        if (aPriority !== bPriority) return aPriority - bPriority;
        return a.index - b.index;
      })
      .map((item) => item.entry);
  }, [createFieldsByCategory]);

  const defaultOpenCategories = useMemo(() => {
    if (orderedFieldGroups.length === 0) return [];
    const categories = orderedFieldGroups.map(([category]) => category);
    const defaultCategory = categories.includes('מידע כללי') ? 'מידע כללי' : categories[0];
    return defaultCategory ? [defaultCategory] : [];
  }, [orderedFieldGroups]);

  const defaultCreateOpenCategories = useMemo(() => {
    if (createOrderedFieldGroups.length === 0) return [];
    const categories = createOrderedFieldGroups.map(([category]) => category);
    const defaultCategory = categories.includes('מידע כללי') ? 'מידע כללי' : categories[0];
    return defaultCategory ? [defaultCategory] : [];
  }, [createOrderedFieldGroups]);

  useEffect(() => {
    if (!selectedBuilding) {
      setOpenViewCategories(new Set());
      setOpenEditCategories(new Set());
      setIsExternalOpen(false);
      setIsLogsOpen(false);
      return;
    }
    setOpenViewCategories(new Set(defaultOpenCategories));
    setOpenEditCategories(new Set(defaultOpenCategories));
    setIsExternalOpen(false);
    setIsLogsOpen(false);
  }, [selectedBuilding, defaultOpenCategories]);

  useEffect(() => {
    if (showModal && modalMode === 'create') {
      setOpenCreateCategories(new Set(defaultCreateOpenCategories));
    }
  }, [showModal, modalMode, defaultCreateOpenCategories]);

  const sortFieldsForDisplay = (fields) => {
    if (!Array.isArray(fields)) return [];
    const fieldPriority = (name) => {
      if (name === 'שם רחוב') return 0;
      if (name === 'מספר בית') return 1;
      if (name === 'כינוי הבניין') return 2;
      if (name === 'סיווג') return 3;
      if (name === 'סטטוס שיקום') return 4;
      return 5;
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
    if (excelName === 'ID') return excelName;
    if (excelName === 'תאריך שינוי') return excelName;
    if (excelName === 'קוארדינטות') {
      if (fieldName.includes('אורך')) return 'קוארדינטות (אורך)';
      if (fieldName.includes('רוחב')) return 'קוארדינטות (רוחב)';
      return excelName;
    }
    return `${excelName} (${fieldName})`;
  };

  const importCompareFields = useMemo(() => {
    if (!importCompareRow || !importCompareTarget) return [];
    const ordered = sortFieldsForDisplay(importCompareRow.importFields || []);
    const existingByColumn = new Map(
      (importCompareTarget.fields || [])
        .filter((field) => field?.columnName)
        .map((field) => [field.columnName.toLowerCase(), field])
    );
    return ordered.map((field) => {
      const columnKey = field.columnName || field.fieldName;
      const lookupKey = columnKey ? columnKey.toLowerCase() : '';
      return {
        columnName: columnKey,
        label: getExcelAwareLabel(field.fieldName || field.columnName),
        importValue: field.value,
        existingValue: existingByColumn.get(lookupKey)?.value ?? null
      };
    });
  }, [importCompareRow, importCompareTarget, sortFieldsForDisplay]);

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

  const importEditSivugValue = resolveSelectValue(importEditValues?.BldSivug ?? '', sivugOptions);
  const isImportEditRehabRequired =
    rehabSivugValue && String(importEditSivugValue) === String(rehabSivugValue);

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
            <select
              name="status"
              value={filters.status}
              onChange={handleFilterChange}
              disabled={!isFilterRehabStatusRequired}
            >
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
              lang="he-IL"
              onChange={handleFilterChange}
            />
          </label>
          <label>
            <span>תאריך שינוי - עד</span>
            <input
              type="date"
              name="updatedTo"
              value={filters.updatedTo}
              lang="he-IL"
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
              <button type="button" className="ghost" onClick={openCreateModal}>
                הוסף מבנה
              </button>
            )}
            {isAdmin && (
              <>
                {!exportMode && (
                  <button type="button" className="ghost" onClick={handleExportAction} disabled={exporting}>
                    {exporting ? 'מייצא...' : 'יצוא לאקסל'}
                  </button>
                )}
                {exportMode && (
                  <>
                    <button
                      type="button"
                      className="ghost"
                      onClick={() => handleExportSelected(false)}
                      disabled={exporting}
                    >
                      {exporting ? 'מייצא...' : 'יצוא ללא תמונות (Excel)'}
                    </button>
                    <button
                      type="button"
                      className="ghost"
                      onClick={() => handleExportSelected(true)}
                      disabled={exporting}
                    >
                      {exporting ? 'מייצא...' : 'יצוא עם תמונות (ZIP)'}
                    </button>
                    <button type="button" className="ghost" onClick={handleCancelExportMode} disabled={exporting}>
                      ביטול יצוא
                    </button>
                  </>
                )}
                <button type="button" className="ghost" onClick={openImportModal} disabled={importBusy}>
                  {importBusy ? 'מייבא...' : 'יבוא קובץ'}
                </button>
              </>
            )}
          </div>
        </form>
        {loading && <p className="muted">טוען מבנים…</p>}
        {error && <p className="error">שגיאה בטעינת מבנים: {error}</p>}
        {actionMessage && !showModal && <p className="success">{actionMessage}</p>}
        {exportError && <p className="error">שגיאה בייצוא: {exportError}</p>}
        {cardExportError && <p className="error">שגיאה בייצוא כרטיס מבנה: {cardExportError}</p>}
      </section>

      <section className="content-layout">
        <div className="list-panel full-span">
          <div className="panel-header">
            <div>
              <h2>תוצאות ({buildings.length})</h2>
              {exportMode && <p className="muted">נבחרו {exportSelection.size} מבנים לייצוא</p>}
            </div>
          </div>
          <div className="table-wrapper">
            <table>
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
                  const statusMissing = statusValue === 'Unknown';
                  const statusLabel = statusLabelMap[statusValue] || statusValue;
                  const statusSlug = statusValue.toLowerCase().replace(/\s+/g, '-');
                  const sivugLabel = getSivugLabel(building.bldSivug);
                  const ownershipLabel = getOwnershipLabel(building.sugBaalut);
                  const isSelected = exportSelection.has(building.id);
                  return (
                    <Fragment key={building.id}>
                      <tr
                        className={`${isActive ? 'active' : ''}${isSelected ? ' selected-row' : ''}`}
                        onClick={() => {
                          if (exportMode) {
                            handleToggleExportBuilding(building.id);
                          } else {
                            openBuildingModal(building.id, 'view');
                          }
                        }}
                        onDoubleClick={() => openBuildingModal(building.id, 'view')}
                      >
                        {exportMode && (
                          <td>
                            <input
                              type="checkbox"
                              checked={isSelected}
                              onChange={() => handleToggleExportBuilding(building.id)}
                              onClick={(event) => event.stopPropagation()}
                            />
                          </td>
                        )}
                        <td>{building.street}</td>
                        <td>{building.houseNumber}</td>
                        <td>{building.nickname || '—'}</td>
                        <td>{sivugLabel}</td>
                        <td>
                          {statusMissing ? (
                            '—'
                          ) : (
                            <span className={`status status-${statusSlug}`}>{statusLabel}</span>
                          )}
                        </td>
                        <td>{ownershipLabel}</td>
                        <td>{building.quarter || '—'}</td>
                        <td>{building.subQuarter || '—'}</td>
                        <td>{building.statisticalArea || '—'}</td>
                        <td>{formatLogDate(building.updatedAt)}</td>
                        <td>{building.statusSummary || '—'}</td>
                        <td>
                          {canEdit && (
                            <button
                              type="button"
                              className="ghost"
                              onClick={(event) => {
                                event.stopPropagation();
                                openBuildingModal(building.id, 'edit');
                              }}
                            >
                              עריכה
                            </button>
                          )}
                          <button
                            type="button"
                            className="ghost"
                            onClick={(event) => {
                              event.stopPropagation();
                              handleExportCard(building);
                            }}
                            disabled={cardExporting}
                          >
                            {cardExporting ? 'מייצא...' : 'ייצוא כרטיס מבנה'}
                          </button>
                          <button
                            type="button"
                            className="ghost"
                            onClick={(event) => {
                              event.stopPropagation();
                              navigate(`/logs?buildingId=${building.id}`);
                            }}
                          >
                            יומן
                          </button>
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
                    </Fragment>
                  );
                })}
                {buildings.length === 0 && !loading && (
                  <tr>
                    <td colSpan={exportMode ? 13 : 12} className="muted">
                      אין מבנים שעונים על הסינון.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>

      </section>
      {showImportModal && (
        <div className="modal-overlay" onClick={closeImportModal}>
          <div className="modal-window modal-large" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <h3>יבוא מבנים</h3>
              <button type="button" className="modal-close" onClick={closeImportModal}>
                ✕
              </button>
            </div>
            <div className="modal-body">
              <div className="import-controls">
                <input
                  type="file"
                  accept=".xlsx,.zip,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet,application/zip"
                  onChange={handleImportFileChange}
                  disabled={importBusy}
                />
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
                <>
                  <div className="import-summary">
                    <span className="muted">
                      להוספה אוטומטית: {importStats.create} | להחלפה: {importStats.replace} | הוספה בכל זאת:{' '}
                      {importStats.addAnyway} | דילוג: {importStats.skip} | לטיפול: {importStats.pending}
                    </span>
                  </div>
                  {importStage1Rows.length > 0 && (
                    <div className="import-stage">
                      <h4>שלב 1: השלמת שדות חובה / מזהה</h4>
                      <p className="muted">יש להשלים את השדות החסרים ולפתור כפילויות ID לפני מעבר לכפילויות.</p>
                      <div className="table-wrapper">
                        <table className="import-table">
                          <thead>
                            <tr>
                              <th>שורה</th>
                              <th>ID</th>
                              <th>שם רחוב</th>
                              <th>מספר בית</th>
                              <th>כינוי הבניין</th>
                              <th>בעיה</th>
                              <th>פעולה</th>
                            </tr>
                          </thead>
                          <tbody>
                            {importStage1Rows.map((row) => {
                              const missingLabels = (row.missingRequired || [])
                                .map((column) => getImportRequiredLabel(column))
                                .join(', ');
                              const flags = [];
                              if (row.hasIdConflict) flags.push('כפילות ID');
                              if (missingLabels) flags.push(`חסרים שדות: ${missingLabels}`);
                              if (row.warnings?.length) flags.push(`אזהרות: ${row.warnings.join(', ')}`);
                              const statusText = flags.length > 0 ? flags.join(' | ') : '—';
                              const rowStreetName =
                                row.values?.StreetName ||
                                streets.find((street) => String(street.streetId) === String(row.values?.StreetId))
                                  ?.name ||
                                '';
                              return (
                                <tr
                                  key={row.rowNumber}
                                  className="import-row import-row--needs"
                                  onClick={() => openImportEdit(row.rowNumber)}
                                >
                                  <td>{row.rowNumber}</td>
                                  <td>{displayOrDash(row.values?.Id)}</td>
                                  <td>{displayOrDash(rowStreetName)}</td>
                                  <td>{displayOrDash(row.values?.BldNum)}</td>
                                  <td>{displayOrDash(row.values?.BldName)}</td>
                                  <td>{statusText}</td>
                                  <td>
                                    <button
                                      type="button"
                                      className="ghost"
                                      onClick={(event) => {
                                        event.stopPropagation();
                                        openImportEdit(row.rowNumber);
                                      }}
                                    >
                                      עריכה
                                    </button>
                                  </td>
                                </tr>
                              );
                            })}
                          </tbody>
                        </table>
                      </div>
                    </div>
                  )}

                  {importStage1Rows.length === 0 && importStage2Rows.length > 0 && (
                    <div className="import-stage">
                      <h4>שלב 2: כפילויות כתובת</h4>
                      <p className="muted">
                        בחרו לכל שורה אם לדלג, להוסיף בכל זאת, או להחליף מבנים קיימים.
                      </p>
                      <div className="table-wrapper">
                        <table className="import-table">
                          <thead>
                            <tr>
                              <th>שורה</th>
                              <th>ID</th>
                              <th>שם רחוב</th>
                              <th>מספר בית</th>
                              <th>כינוי הבניין</th>
                              <th>כפילויות קיימות</th>
                              <th>פעולות</th>
                            </tr>
                          </thead>
                          <tbody>
                            {importStage2Rows.map((row) => {
                              const rowStreetName =
                                row.values?.StreetName ||
                                streets.find((street) => String(street.streetId) === String(row.values?.StreetId))
                                  ?.name ||
                                '';
                              return (
                                <tr
                                  key={row.rowNumber}
                                  className="import-row import-row--conflict"
                                  onClick={() => openImportCompare(row.rowNumber)}
                                >
                                  <td>{row.rowNumber}</td>
                                  <td>{displayOrDash(row.values?.Id)}</td>
                                  <td>{displayOrDash(rowStreetName)}</td>
                                  <td>{displayOrDash(row.values?.BldNum)}</td>
                                  <td>{displayOrDash(row.values?.BldName)}</td>
                                  <td>{row.addressMatches?.length ?? 0}</td>
                                  <td className="import-actions">
                                    <button
                                      type="button"
                                      className="ghost"
                                      onClick={(event) => {
                                        event.stopPropagation();
                                        openImportCompare(row.rowNumber);
                                      }}
                                    >
                                      השוואה / החלפה
                                    </button>
                                    <button
                                      type="button"
                                      className="ghost"
                                      onClick={(event) => {
                                        event.stopPropagation();
                                        handleResolveConflict(row.rowNumber, 'skip');
                                      }}
                                    >
                                      דלג
                                    </button>
                                    <button
                                      type="button"
                                      className="ghost"
                                      onClick={(event) => {
                                        event.stopPropagation();
                                        handleResolveConflict(row.rowNumber, 'add_anyway');
                                      }}
                                    >
                                      הוסף בכל זאת
                                    </button>
                                  </td>
                                </tr>
                              );
                            })}
                          </tbody>
                        </table>
                      </div>
                    </div>
                  )}

                  {importStage1Rows.length === 0 && importStage2Rows.length === 0 && (
                    <p className="muted">אין שורות שדורשות טיפול נוסף. אפשר לבצע את הייבוא.</p>
                  )}
                </>
              )}
            </div>
            <div className="modal-footer">
              <div className="footer-actions">
                <button type="button" className="ghost" onClick={closeImportModal} disabled={importBusy}>
                  סגירה
                </button>
                {importStage1Rows.length === 0 && importStage2Rows.length > 0 && (
                  <>
                    <button type="button" className="ghost" onClick={handleImportSkipAll} disabled={importBusy}>
                      דלג הכל
                    </button>
                    <button type="button" className="ghost" onClick={handleImportReplaceAll} disabled={importBusy}>
                      החלף הכל
                    </button>
                    <button type="button" className="ghost" onClick={handleImportAddAnywayAll} disabled={importBusy}>
                      הוסף בכל זאת הכל
                    </button>
                  </>
                )}
                {importRows.length > 0 && (
                  <>
                    <button
                      type="button"
                      className="primary"
                      onClick={handleImportApply}
                      disabled={importApplying || importBusy || !importReadyToApply}
                    >
                      {importApplying ? 'מייבא...' : 'בצע ייבוא'}
                    </button>
                  </>
                )}
              </div>
              {importStage1Rows.length > 0 && (
                <p className="error">יש שורות שחובה להשלים לפני הייבוא.</p>
              )}
              {importStage1Rows.length === 0 && importStage2Rows.length > 0 && (
                <p className="error">יש כפילויות שדורשות טיפול לפני הייבוא.</p>
              )}
            </div>
          </div>
        </div>
      )}
      {importEditRow && (
        <div className="modal-overlay" onClick={closeImportEdit}>
          <div className="modal-window modal-large" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <h3>תיקון שורה {importEditRow.rowNumber}</h3>
              <button type="button" className="modal-close" onClick={closeImportEdit}>
                ✕
              </button>
            </div>
            <div className="modal-body">
              <p className="muted">השלימו את שדות החובה. השאירו ID ריק ליצירה אוטומטית.</p>
              {importEditRow.hasIdConflict && (
                <p className="error">קיים מבנה עם ID זה או שה־ID מופיע יותר מפעם אחת בקובץ.</p>
              )}
              {(importEditRow.missingRequired?.length ?? 0) > 0 && (
                <p className="error">
                  חסרים שדות חובה:{' '}
                  {importEditRow.missingRequired.map((column) => getImportRequiredLabel(column)).join(', ')}
                </p>
              )}
              {importEditRow.warnings?.length > 0 && (
                <p className="muted">אזהרות: {importEditRow.warnings.join(', ')}</p>
              )}
              {importEditError && <p className="error">{importEditError}</p>}
              <div className="import-edit-grid">
                <label>
                  ID (אופציונלי)
                  <input
                    type="number"
                    min="1"
                    step="1"
                    value={importEditValues?.Id ?? ''}
                    onChange={(event) => handleImportEditValueChange('Id', event.target.value)}
                  />
                </label>
                <label className="required">
                  שם רחוב <span className="required-mark">*</span>
                  <select
                    value={importEditValues?.StreetId ?? ''}
                    onChange={(event) => handleImportEditValueChange('StreetId', event.target.value)}
                  >
                    <option value="">בחר רחוב</option>
                    {streets.map((street) => (
                      <option key={street.streetId} value={street.streetId}>
                        {street.name}
                      </option>
                    ))}
                  </select>
                </label>
                <label className="required">
                  מספר בית <span className="required-mark">*</span>
                  <input
                    type="text"
                    value={importEditValues?.BldNum ?? ''}
                    onChange={(event) => handleImportEditValueChange('BldNum', event.target.value)}
                  />
                </label>
                <label className="required">
                  כינוי הבניין <span className="required-mark">*</span>
                  <input
                    type="text"
                    value={importEditValues?.BldName ?? ''}
                    onChange={(event) => handleImportEditValueChange('BldName', event.target.value)}
                  />
                </label>
                <label className="required">
                  סיווג <span className="required-mark">*</span>
                  <select
                    value={importEditValues?.BldSivug ?? ''}
                    onChange={(event) => handleImportEditValueChange('BldSivug', event.target.value)}
                  >
                    <option value="">—</option>
                    {sivugOptions.map((opt) => (
                      <option key={opt.value} value={opt.label}>
                        {opt.label}
                      </option>
                    ))}
                  </select>
                </label>
                <label className={isImportEditRehabRequired ? 'required' : ''}>
                  סטטוס שיקום {isImportEditRehabRequired && <span className="required-mark">*</span>}
                  <select
                    value={importEditValues?.ShikumStatus ?? ''}
                    onChange={(event) => handleImportEditValueChange('ShikumStatus', event.target.value)}
                    disabled={!isImportEditRehabRequired}
                  >
                    <option value="">—</option>
                    {statusOptions.map((opt) => (
                      <option key={opt.value ?? opt.id} value={opt.label}>
                        {opt.label}
                      </option>
                    ))}
                  </select>
                </label>
              </div>
            </div>
            <div className="modal-footer">
              <div className="footer-actions">
                <button type="button" className="ghost" onClick={closeImportEdit} disabled={importEditSaving}>
                  סגירה
                </button>
                <button
                  type="button"
                  className="primary"
                  onClick={handleImportEditSave}
                  disabled={importEditSaving}
                >
                  {importEditSaving ? 'שומר...' : 'שמירה'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
      {importCompareRow && (
        <div className="modal-overlay" onClick={closeImportCompare}>
          <div className="modal-window modal-large" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <h3>השוואת שורה {importCompareRow.rowNumber}</h3>
              <button type="button" className="modal-close" onClick={closeImportCompare}>
                ✕
              </button>
            </div>
            <div className="modal-body">
              {importCompareRow.warnings?.length > 0 && (
                <p className="muted">אזהרות: {importCompareRow.warnings.join(', ')}</p>
              )}
              <p className="muted">
                נמצאו {importCompareRow.addressMatches?.length ?? 0} מבנים באותה כתובת. בחרו אילו למחוק והחליפו.
              </p>
              <div className="import-match-list">
                {(importCompareRow.addressMatches || []).map((match) => {
                  const isSelected = (importCompareRow.replaceIds || []).includes(match.id);
                  const isActive = importCompareTarget?.id === match.id;
                  return (
                    <div key={match.id} className={`import-match ${isActive ? 'active' : ''}`}>
                      <button
                        type="button"
                        className="ghost"
                        onClick={() => handleSelectCompareTarget(importCompareRow.rowNumber, match.id)}
                      >
                        {match.streetName} {match.houseNumber} · {match.buildingName} (ID {match.id})
                      </button>
                      <label className="import-match-checkbox">
                        <input
                          type="checkbox"
                          checked={isSelected}
                          onChange={(event) => {
                            event.stopPropagation();
                            toggleReplaceSelection(importCompareRow.rowNumber, match.id);
                          }}
                        />
                        להחלפה
                      </label>
                    </div>
                  );
                })}
              </div>
              {!importCompareTarget && <p className="muted">בחרו מבנה להשוואה כדי לראות את ההבדלים.</p>}

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
                    {importCompareFields.map((row) => (
                      <tr key={row.columnName}>
                        <td>{row.label}</td>
                        <td>{renderImportValue(row.importValue)}</td>
                        <td>{renderImportValue(row.existingValue)}</td>
                      </tr>
                    ))}
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
                    handleResolveConflict(importCompareRow.rowNumber, 'skip');
                    closeImportCompare();
                  }}
                >
                  דלג
                </button>
                <button
                  type="button"
                  className="ghost"
                  onClick={() => {
                    handleResolveConflict(importCompareRow.rowNumber, 'add_anyway');
                    closeImportCompare();
                  }}
                >
                  הוסף בכל זאת
                </button>
                <button
                  type="button"
                  className="primary"
                  disabled={(importCompareRow.replaceIds?.length ?? 0) === 0}
                  onClick={() => {
                    handleResolveConflict(
                      importCompareRow.rowNumber,
                      'replace',
                      importCompareRow.replaceIds || []
                    );
                    closeImportCompare();
                  }}
                >
                  החלף נבחרים
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
      <BuildingModal
        visible={showModal}
        mode={modalMode}
        building={selectedBuilding}
        createFieldValues={createFieldValues}
        createFieldGroups={createOrderedFieldGroups}
        createTemplateLoading={createTemplateLoading}
        createSelectTablesLoading={createSelectTablesLoading}
        editFieldValues={editFieldValues}
        streets={streets}
        selectTablesByName={selectTablesByName}
        selectTablesLoading={selectTablesLoading}
        orderedFieldGroups={orderedFieldGroups}
        externalEntries={externalEntries}
        isRehabStatusRequired={isRehabStatusRequired}
        isEditRehabStatusRequired={isEditRehabStatusRequired}
        isRequiredCreateColumn={isRequiredCreateColumn}
        canEdit={canEdit}
        actionMessage={actionMessage}
        duplicatePrompt={duplicatePrompt}
        editDuplicatePrompt={editDuplicatePrompt}
        onCreateFieldChange={handleCreateFieldChange}
        onCreateSubmit={handleCreateBuilding}
        onDuplicateConfirm={handleDuplicateConfirm}
        onDuplicateCancel={handleDuplicateCancel}
        onEditChange={handleEditFieldChange}
        onEditSubmit={handleUpdateBuildingFields}
        onEditDuplicateConfirm={handleEditDuplicateConfirm}
        onEditDuplicateCancel={handleEditDuplicateCancel}
        onOpenEdit={handleOpenEditModal}
        onOpenLogs={handleOpenLogsModal}
        onDelete={handleDeleteBuilding}
        onExportCard={handleExportCard}
        onPhotoUpload={handlePhotoUpload}
        onPhotoDelete={handlePhotoDelete}
        photoLoading={photoLoading}
        photoError={photoError}
        onClose={handleCloseModal}
        detailError={detailError}
        loadStreets={loadStreets}
        sortFieldsForDisplay={sortFieldsForDisplay}
        getExcelAwareLabel={getExcelAwareLabel}
        isDateField={isDateField}
        shouldUseTextarea={shouldUseTextarea}
        isRequiredEditColumn={isRequiredEditColumn}
        displayOrDash={displayOrDash}
        formatStatusFieldValue={formatStatusFieldValue}
        formatLogDate={formatLogDate}
        openViewCategories={openViewCategories}
        toggleViewCategory={toggleViewCategory}
        openEditCategories={openEditCategories}
        toggleEditCategory={toggleEditCategory}
        openCreateCategories={openCreateCategories}
        toggleCreateCategory={toggleCreateCategory}
        handleCategoryToggleKeyDown={handleCategoryToggleKeyDown}
      />
    </main>
  );
}
