import { useEffect, useMemo, useState } from 'react';
import api from '../api/client.js';
import { useAuth } from '../context/AuthContext.jsx';
import { ROLE_LABELS, STATUS_LABEL_MAP, STATUS_OPTIONS, STATUS_VALUE_BY_ID } from '../i18n.js';
import useDocumentTitle from '../hooks/useDocumentTitle.js';
import {
  BUILDING_FIELD_LABELS,
  BUILDING_FIELD_PLACEHOLDERS,
  LAST_BUILDING_KEY,
  STATUS_SELECT_PLACEHOLDER
} from '../constants.js';

const initialFilters = {
  streetId: '',
  houseNumber: '',
  nickname: '',
  status: '',
  area: '',
  statusSummary: ''
};
const SORT_FIELDS = [
  { value: 'street', label: 'שם רחוב' },
  { value: 'houseNumber', label: 'מספר בית' },
  { value: 'nickname', label: 'כינוי' },
  { value: 'status', label: 'סטטוס' },
  { value: 'area', label: 'אזור' }
];

export default function BuildingsPage() {
  const { user } = useAuth();
  useDocumentTitle('מאגר מבנים - מוקד המבנים העירוני');
  const [filters, setFilters] = useState(initialFilters);
  const [buildings, setBuildings] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [statusOptions, setStatusOptions] = useState(STATUS_OPTIONS);
  const [statusLabelMap, setStatusLabelMap] = useState(STATUS_LABEL_MAP);
  const [streets, setStreets] = useState([]);
  const [selectedBuilding, setSelectedBuilding] = useState(null);
  const [detailError, setDetailError] = useState('');
  const [detailTab, setDetailTab] = useState('summary');
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [createForm, setCreateForm] = useState({
    fldId: '',
    streetId: '',
    bldNum: '',
    bldName: '',
    area: '',
    statusSummary: '',
    shikumStatusId: '',
    complaints: '',
    category: ''
  });
  const [editForm, setEditForm] = useState({
    bldName: '',
    area: '',
    statusSummary: '',
    shikumStatusId: '',
    category: '',
    streetId: ''
  });
  const [actionMessage, setActionMessage] = useState('');
  const [selectedView, setSelectedView] = useState('summary');
  const [sortCriteria, setSortCriteria] = useState([
    { field: 'street', direction: 'asc' },
    { field: 'houseNumber', direction: 'asc' },
    { field: '', direction: 'asc' },
    { field: '', direction: 'asc' },
    { field: '', direction: 'asc' },
    { field: '', direction: 'asc' }
  ]);

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

  const displayOrDash = (value) => (value === null || value === undefined || value === '' ? '—' : value);

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

    loadStatusOptions();
    loadStreets();
    loadBuildings(initialFilters);
  }, []);

  useEffect(() => {
    if (selectedBuilding) {
      const statusOption = statusOptions.find(
        (option) => option.value === selectedBuilding.status
      );
      setEditForm({
        bldName: selectedBuilding.nickname || '',
        area: selectedBuilding.area || '',
        statusSummary: selectedBuilding.statusSummary || '',
        shikumStatusId: statusOption ? String(statusOption.id) : '',
        category:
          selectedBuilding.bldSivug === null || selectedBuilding.bldSivug === undefined
            ? ''
            : String(selectedBuilding.bldSivug),
        streetId:
          selectedBuilding.streetId === null || selectedBuilding.streetId === undefined
            ? ''
            : String(selectedBuilding.streetId)
      });
    }
  }, [selectedBuilding, statusOptions]);

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
        }
      }
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const loadBuildingDetails = async (id, view = 'summary') => {
    setDetailError('');
    try {
      const building = await api.fetchBuilding(id);
      setSelectedBuilding(building);
      setDetailTab(view);
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

  const handleCreateChange = (event) => {
    const { name, value } = event.target;
    setCreateForm((form) => ({ ...form, [name]: value }));
  };

  const handleCreateBuilding = async (event) => {
    event.preventDefault();
    setActionMessage('');
    try {
      const statusOption = statusOptions.find(
        (option) => String(option.id) === createForm.shikumStatusId
      );
      const streetOption = streets.find((street) => String(street.streetId) === createForm.streetId);
      if (!streetOption) {
        throw new Error('יש לבחור רחוב מהרשימה');
      }
      const payload = {
        fldId: createForm.fldId,
        streetId: streetOption.streetId,
        houseNumber: createForm.bldNum,
        buildingName: createForm.bldName || streetOption.name,
        neighborhood: createForm.area,
        bldSivug: createForm.category,
        shikumStatus: statusOption ? statusOption.value : 'Unknown',
        statusSummary: createForm.statusSummary,
        complaints: createForm.complaints || ''
      };
      await api.createBuilding(payload);
      setCreateForm({
        fldId: '',
        streetId: '',
        bldNum: '',
        bldName: '',
        area: '',
        statusSummary: '',
        shikumStatusId: '',
        complaints: '',
        category: ''
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
      const statusOption = statusOptions.find(
        (option) => String(option.id) === editForm.shikumStatusId
      );
      const chosenStreetId = editForm.streetId || (selectedBuilding.streetId ? String(selectedBuilding.streetId) : '');
      const streetOption = streets.find((street) => String(street.streetId) === chosenStreetId);
      if (!streetOption) {
        throw new Error('יש לבחור רחוב מהרשימה');
      }
      const payload = {
        fldId: selectedBuilding.fldId ?? selectedBuilding.id,
        streetId: streetOption.streetId,
        houseNumber: selectedBuilding.houseNumber,
        buildingName: editForm.bldName || selectedBuilding.nickname || streetOption.name || selectedBuilding.street,
        neighborhood: editForm.area || selectedBuilding.area || '',
        bldSivug: editForm.category ?? selectedBuilding.bldSivug,
        shikumStatus: statusOption ? statusOption.value : selectedBuilding.status || 'Unknown',
        statusSummary: editForm.statusSummary,
        complaints: selectedBuilding.complaints || ''
      };
      const updated = await api.updateBuilding(selectedBuilding.id, payload);
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
    setDetailTab(tab);
    setSelectedView(tab);
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

  const sortedBuildings = useMemo(() => {
    if (!buildings || buildings.length === 0) return [];
    const criteria = sortCriteria.filter((c) => c.field);
    if (criteria.length === 0) return buildings;

    const compare = (a, b, field, direction) => {
      let result = 0;
      if (field === 'houseNumber') {
        result = (a.houseNumber || '').localeCompare(b.houseNumber || '', 'he');
      } else if (field === 'status') {
        result = (a.status || '').localeCompare(b.status || '', 'he');
      } else if (field === 'street') {
        result = (a.street || '').localeCompare(b.street || '', 'he');
      } else if (field === 'nickname') {
        result = (a.nickname || '').localeCompare(b.nickname || '', 'he');
      } else if (field === 'area') {
        result = (a.area || '').localeCompare(b.area || '', 'he');
      }
      return direction === 'desc' ? -result : result;
    };

    const copy = [...buildings];
    copy.sort((a, b) => {
      for (const c of criteria) {
        const cmp = compare(a, b, c.field, c.direction);
        if (cmp !== 0) return cmp;
      }
      return 0;
    });
    return copy;
  }, [buildings, sortCriteria]);

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
            <span>{BUILDING_FIELD_LABELS.street}</span>
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
            <span>{BUILDING_FIELD_LABELS.houseNumber}</span>
            <input
              type="text"
              name="houseNumber"
              value={filters.houseNumber}
              onChange={handleFilterChange}
              placeholder={BUILDING_FIELD_PLACEHOLDERS.houseNumber}
            />
          </label>
          <label>
            <span>{BUILDING_FIELD_LABELS.nickname}</span>
            <input
              type="text"
              name="nickname"
              value={filters.nickname}
              onChange={handleFilterChange}
              placeholder={BUILDING_FIELD_PLACEHOLDERS.nickname}
            />
          </label>
          <label>
            <span>{BUILDING_FIELD_LABELS.status}</span>
            <select name="status" value={filters.status} onChange={handleFilterChange}>
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
              type="text"
              name="area"
              value={filters.area}
              onChange={handleFilterChange}
              placeholder={BUILDING_FIELD_PLACEHOLDERS.area}
            />
          </label>
          <label className="full-span">
            <span>{BUILDING_FIELD_LABELS.statusSummary}</span>
            <input
              type="text"
              name="statusSummary"
              value={filters.statusSummary}
              onChange={handleFilterChange}
              placeholder={BUILDING_FIELD_PLACEHOLDERS.statusSummary}
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
              כינוי
              <input
                name="bldName"
                value={createForm.bldName}
                onChange={handleCreateChange}
                placeholder={BUILDING_FIELD_PLACEHOLDERS.nickname}
              />
            </label>
            <label>
              סטטוס
              <select
                name="shikumStatusId"
                value={createForm.shikumStatusId}
                onChange={handleCreateChange}
              >
                <option value="">{STATUS_SELECT_PLACEHOLDER}</option>
                {statusOptions.map((option) => (
                  <option key={option.id} value={option.id}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            <label>
              אזור
              <input
                name="area"
                value={createForm.area}
                onChange={handleCreateChange}
                placeholder={BUILDING_FIELD_PLACEHOLDERS.area}
              />
            </label>
            <label className="full-span">
              תקציר מצב
              <textarea
                name="statusSummary"
                value={createForm.statusSummary}
                onChange={handleCreateChange}
                placeholder={BUILDING_FIELD_PLACEHOLDERS.statusSummary}
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
                  <th>שם רחוב</th>
                  <th>מספר בית</th>
                  <th>כינוי</th>
                  <th>סטטוס</th>
                  <th>אזור</th>
                  <th>תקציר מצב</th>
                  <th>פעולות</th>
                </tr>
              </thead>
              <tbody>
                {sortedBuildings.map((building) => {
                  const isActive = selectedBuilding && building.id === selectedBuilding.id;
                  const statusValue = building.status || 'Unknown';
                  const statusLabel = statusLabelMap[statusValue] || statusValue;
                  const statusSlug = statusValue.toLowerCase().replace(/\s+/g, '-');
                  return (
                    <>
                      <tr key={building.id} className={isActive ? 'active' : ''}>
                        <td>{building.street}</td>
                        <td>{building.houseNumber}</td>
                        <td>{building.nickname || '—'}</td>
                        <td>
                          <span className={`status status-${statusSlug}`}>{statusLabel}</span>
                        </td>
                        <td>{building.area || '—'}</td>
                        <td>{building.statusSummary || '—'}</td>
                        <td>
                          <button
                            type="button"
                            className="ghost"
                            onClick={() => loadBuildingDetails(building.id, 'summary')}
                          >
                            הצג
                          </button>
                          <button
                            type="button"
                            className="ghost"
                            onClick={() => loadBuildingDetails(building.id, 'all')}
                          >
                            הצג הכל
                          </button>
                          <button
                            type="button"
                            className="ghost"
                            onClick={() => loadBuildingDetails(building.id, 'edit')}
                          >
                            עריכה
                          </button>
                          <button
                            type="button"
                            className="danger"
                            onClick={() => {
                              if (window.confirm('למחוק את המבנה לצמיתות?')) {
                                handleDeleteBuilding(building.id);
                              }
                            }}
                          >
                            מחק
                          </button>
                        </td>
                      </tr>
                      {isActive && selectedBuilding && (
                        <tr>
                          <td colSpan="7">
                            {selectedView === 'edit' && canEdit && (
                              <form className="details-card form-grid" onSubmit={handleUpdateBuilding}>
                                <label>
                                  כינוי
                                  <input
                                    name="bldName"
                                    value={editForm.bldName}
                                    onChange={handleEditChange}
                                  />
                                </label>
                                <label>
                                  רחוב
                                  <select name="streetId" value={editForm.streetId} onChange={handleEditChange} required>
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
                                    name="houseNumber"
                                    value={selectedBuilding.houseNumber}
                                    onChange={() => {}}
                                    disabled
                                  />
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
                                    {statusOptions.map((option) => (
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
                                  <button
                                    type="button"
                                    className="ghost"
                                    onClick={() => setSelectedBuilding(null)}
                                  >
                                    סגירה
                                  </button>
                                </div>
                              </form>
                            )}
                            {selectedView === 'summary' && (
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
                                    <dd>{formatLogDate(selectedBuilding.updatedAt)}</dd>
                                  </div>
                                  <div>
                                    <dt>תקציר מצב</dt>
                                    <dd>{selectedBuilding.statusSummary || '—'}</dd>
                                  </div>
                                </dl>
                              </div>
                            )}
                            {selectedView === 'all' && (
                              <div className="details-card">
                                <pre style={{ whiteSpace: 'pre-wrap', direction: 'ltr' }}>
{JSON.stringify(selectedBuilding, null, 2)}
                                </pre>
                              </div>
                            )}
                          </td>
                        </tr>
                      )}
                    </>
                  );
                })}
                {buildings.length === 0 && !loading && (
                  <tr>
                    <td colSpan="7" className="muted">
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
