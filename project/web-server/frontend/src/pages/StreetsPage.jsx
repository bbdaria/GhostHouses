import { useEffect, useMemo, useState } from 'react';
import api from '../api/client.js';
import { useAuth } from '../context/AuthContext.jsx';
import useDocumentTitle from '../hooks/useDocumentTitle.js';
import { ROLE_LABELS } from '../i18n.js';

const initialFilters = { search: '' };
const initialForm = { streetId: '', name: '' };

export default function StreetsPage() {
  const { user } = useAuth();
  useDocumentTitle('רחובות - מוקד המבנים העירוני');
  const canEdit = useMemo(
    () => user && (user.role === 'Editor' || user.role === 'Admin'),
    [user]
  );
  const roleLabel = ROLE_LABELS[user?.role] || user?.role;

  const [filters, setFilters] = useState(initialFilters);
  const [streets, setStreets] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [actionMessage, setActionMessage] = useState('');
  const [createForm, setCreateForm] = useState(initialForm);
  const [editForm, setEditForm] = useState(initialForm);
  const [selectedStreetId, setSelectedStreetId] = useState('');
  const [sortConfig, setSortConfig] = useState({ field: 'name', direction: 'asc' });

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
    } catch (err) {
      setActionMessage(err.message);
    }
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

  const handleSelectStreet = (street) => {
    setSelectedStreetId(String(street.streetId));
    setEditForm({ streetId: String(street.streetId), name: street.name });
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
          <div className="filters-actions">
            <button type="submit" className="primary">
              חיפוש
            </button>
            <button type="button" onClick={handleReset} className="ghost">
              איפוס
            </button>
          </div>
        </form>
        {loading && <p className="muted">טוען רחובות…</p>}
        {error && <p className="error">שגיאה: {error}</p>}
        {actionMessage && <p className="success">{actionMessage}</p>}
      </section>

      <section className="content-layout">
        {canEdit && (
          <section className="panel full-span">
            <div className="panel-header">
              <h3>הוספת רחוב</h3>
            </div>
            <form className="form-grid" onSubmit={handleCreate}>
              <label>
                מזהה רחוב
                <input
                  name="streetId"
                  value={createForm.streetId}
                  onChange={handleCreateChange}
                  required
                />
              </label>
              <label>
                שם רחוב
                <input name="name" value={createForm.name} onChange={handleCreateChange} required />
              </label>
              <div className="filters-actions">
                <button type="submit" className="primary">
                  שמירה
                </button>
                <button type="button" className="ghost" onClick={() => setCreateForm(initialForm)}>
                  ניקוי
                </button>
              </div>
            </form>
          </section>
        )}

        <div className="list-panel full-span">
          <div className="panel-header">
            <h2>רחובות ({streets.length})</h2>
          </div>
          <div className="table-wrapper">
            <table>
              <thead>
                <tr>
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
                  const isActive = selectedStreetId === String(street.streetId);
                  return (
                    <>
                      <tr key={street.streetId} className={isActive ? 'active' : ''}>
                        <td>{street.streetId}</td>
                        <td>{street.name}</td>
                        <td>
                          <button
                            type="button"
                            className="ghost"
                            onClick={() => handleSelectStreet(street)}
                          >
                            עריכה
                          </button>
                          <button
                            type="button"
                            className="danger"
                            onClick={() => handleDelete(street.streetId)}
                          >
                            מחק
                          </button>
                        </td>
                      </tr>
                      {canEdit && isActive && (
                        <tr>
                          <td colSpan="3">
                            <form className="form-grid" onSubmit={handleUpdate}>
                              <label>
                                מזהה רחוב
                                <input
                                  name="streetId"
                                  value={editForm.streetId}
                                  onChange={handleEditChange}
                                  readOnly
                                />
                              </label>
                              <label>
                                שם רחוב
                                <input
                                  name="name"
                                  value={editForm.name}
                                  onChange={handleEditChange}
                                  required
                                />
                              </label>
                              <div className="filters-actions">
                                <button type="submit" className="primary">
                                  שמירת שינויים
                                </button>
                                <button
                                  type="button"
                                  className="ghost"
                                  onClick={() => {
                                    setSelectedStreetId('');
                                    setEditForm(initialForm);
                                  }}
                                >
                                  סגירה
                                </button>
                              </div>
                            </form>
                          </td>
                        </tr>
                      )}
                    </>
                  );
                })}
                {streets.length === 0 && !loading && (
                  <tr>
                    <td colSpan="3" className="muted">
                      אין רחובות להצגה.
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
