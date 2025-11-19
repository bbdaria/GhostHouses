import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { BuildingApi, LogsApi } from '../api.js';
import { useAuth } from '../authContext.jsx';

export default function BuildingDetailsPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  const [building, setBuilding] = useState(null);
  const [logs, setLogs] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [deleteReason, setDeleteReason] = useState('');
  const [logForm, setLogForm] = useState({ title: '', message: '', category: 'general', severity: 'info' });

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      try {
        const detail = await BuildingApi.get(id);
        setBuilding(detail);
        const buildingLogs = await LogsApi.forBuilding(id);
        setLogs(buildingLogs);
      } catch (err) {
        setError(err.message);
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [id]);

  const handleLogChange = (event) => {
    const { name, value } = event.target;
    setLogForm((prev) => ({ ...prev, [name]: value }));
  };

  const submitLog = async (event) => {
    event.preventDefault();
    try {
      await LogsApi.create(id, logForm);
      setLogForm({ title: '', message: '', category: 'general', severity: 'info' });
      const buildingLogs = await LogsApi.forBuilding(id);
      setLogs(buildingLogs);
    } catch (err) {
      setError(err.message);
    }
  };

  const deleteBuilding = async () => {
    if (!window.confirm('Are you sure you want to remove this building?')) return;
    try {
      await BuildingApi.remove(id, { reason: deleteReason || 'No reason provided', confirm: true });
      navigate('/buildings');
    } catch (err) {
      setError(err.message);
    }
  };

  if (loading) {
    return <div className="card">Loading building…</div>;
  }

  if (!building) {
    return <div className="card danger">Building not found</div>;
  }

  const { summary, photos, externalData } = building;

  return (
    <section className="grid-two">
      <article className="card">
        <h2>{summary.buildingName}</h2>
        <p>
          {summary.streetName} {summary.houseNumber}, {summary.neighborhood}
        </p>
        <p>
          <strong>Status:</strong> {summary.shikumStatus} | <strong>Category:</strong> {summary.bldSivug}
        </p>
        <p>{building.statusSummary}</p>
        {photos.length ? (
          <div className="photos">
            {photos.map((url) => (
              <img key={url} src={url} alt={summary.buildingName} />
            ))}
          </div>
        ) : (
          <p className="muted">No photos uploaded</p>
        )}
        {(user?.role === 'Editor' || user?.role === 'Admin') && (
          <>
            <textarea
              placeholder="Reason for removal"
              value={deleteReason}
              onChange={(event) => setDeleteReason(event.target.value)}
            />
            <button type="button" onClick={deleteBuilding} className="danger">
              Remove Building
            </button>
          </>
        )}
      </article>
      <article className="card">
        <h3>External snapshots</h3>
        <ul>
          {Object.values(externalData).map((snapshot) => (
            <li key={snapshot.systemName}>
              <strong>{snapshot.systemName}</strong>
              <small>Updated {new Date(snapshot.retrievedAt).toLocaleString()}</small>
              <pre>{snapshot.payload}</pre>
            </li>
          ))}
        </ul>
      </article>
      <article className="card span-two">
        <h3>Logs</h3>
        {logs.length ? (
          <ul className="logs">
            {logs.map((log) => (
              <li key={log.id}>
                <strong>{log.title}</strong> <span>{log.severity}</span>
                <p>{log.message}</p>
                <small>
                  {new Date(log.createdAt).toLocaleString()} by {log.createdBy ?? 'system'}
                </small>
              </li>
            ))}
          </ul>
        ) : (
          <p className="muted">No logs yet.</p>
        )}
        {(user?.role === 'Editor' || user?.role === 'Admin') && (
          <form className="log-form" onSubmit={submitLog}>
            <input name="title" placeholder="Log title" value={logForm.title} onChange={handleLogChange} required />
            <textarea
              name="message"
              placeholder="Details"
              value={logForm.message}
              onChange={handleLogChange}
              required
            />
            <div className="row">
              <input name="category" placeholder="Category" value={logForm.category} onChange={handleLogChange} />
              <select name="severity" value={logForm.severity} onChange={handleLogChange}>
                <option value="info">Info</option>
                <option value="warning">Warning</option>
                <option value="critical">Critical</option>
              </select>
            </div>
            <button type="submit">Add Log</button>
          </form>
        )}
      </article>
      {error ? (
        <article className="card danger">
          <p>{error}</p>
        </article>
      ) : null}
    </section>
  );
}
