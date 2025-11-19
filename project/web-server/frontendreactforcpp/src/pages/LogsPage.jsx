import { useEffect, useState } from 'react';
import api from '../api/client.js';
import useDocumentTitle from '../hooks/useDocumentTitle.js';

const logFilters = {
  buildingId: '',
  userId: '',
  actionType: '',
  startDate: '',
  endDate: ''
};

export default function LogsPage() {
  const [filters, setFilters] = useState(logFilters);
  const [logs, setLogs] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  useDocumentTitle('יומן פעילויות - מוקד המבנים העירוני');

  useEffect(() => {
    loadLogs();
  }, []);

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
    setFilters(logFilters);
    loadLogs(logFilters);
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
            <span>מזהה מבנה</span>
            <input name="buildingId" value={filters.buildingId} onChange={handleChange} />
          </label>
          <label>
            <span>מזהה משתמש</span>
            <input name="userId" value={filters.userId} onChange={handleChange} />
          </label>
          <label>
            <span>סוג פעולה</span>
            <input name="actionType" value={filters.actionType} onChange={handleChange} />
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
                <th>תאריך</th>
                <th>מבנה</th>
                <th>פעולה</th>
                <th>משתמש</th>
                <th>תיאור</th>
              </tr>
            </thead>
            <tbody>
              {logs.map((log) => (
                <tr key={log.id}>
                  <td>{log.createdAt}</td>
                  <td>{log.buildingId}</td>
                  <td>{log.actionType}</td>
                  <td>{log.username || log.userId || '—'}</td>
                  <td>{log.description || '—'}</td>
                </tr>
              ))}
              {logs.length === 0 && !loading && (
                <tr>
                  <td colSpan="5" className="muted">
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
