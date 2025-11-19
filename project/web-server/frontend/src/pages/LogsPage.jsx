import { useEffect, useState } from 'react';
import { LogsApi } from '../api.js';

export default function LogsPage() {
  const [filters, setFilters] = useState({ buildingId: '', userId: '' });
  const [data, setData] = useState({ items: [] });
  const [error, setError] = useState('');

  const loadLogs = async () => {
    setError('');
    try {
      const payload = {
        buildingId: filters.buildingId || undefined,
        userId: filters.userId || undefined,
      };
      const response = await LogsApi.list(payload);
      setData(response);
    } catch (err) {
      setError(err.message);
    }
  };

  useEffect(() => {
    loadLogs();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleChange = (event) => {
    const { name, value } = event.target;
    setFilters((prev) => ({ ...prev, [name]: value }));
  };

  return (
    <section className="card">
      <h2>Global Logs</h2>
      {error ? <p className="error">{error}</p> : null}
      <div className="filters">
        <input name="buildingId" placeholder="Building ID" value={filters.buildingId} onChange={handleChange} />
        <input name="userId" placeholder="User ID" value={filters.userId} onChange={handleChange} />
        <button type="button" onClick={loadLogs}>
          Search
        </button>
      </div>
      <ul className="logs">
        {data.items.map((log) => (
          <li key={log.id}>
            <strong>
              #{log.buildingId} {log.title}
            </strong>
            <span>{log.severity}</span>
            <p>{log.message}</p>
            <small>
              {new Date(log.createdAt).toLocaleString()} by {log.createdBy ?? 'system'}
            </small>
          </li>
        ))}
      </ul>
    </section>
  );
}
