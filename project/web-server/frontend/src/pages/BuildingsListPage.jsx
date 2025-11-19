import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { BuildingApi } from '../api.js';
import { useAuth } from '../authContext.jsx';

export default function BuildingsListPage() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [filters, setFilters] = useState({
    street: '',
    houseNumber: '',
    name: '',
    status: '',
    neighborhood: '',
  });
  const [data, setData] = useState({ items: [], total: 0, page: 1, pageSize: 20 });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    loadBuildings();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleChange = (event) => {
    const { name, value } = event.target;
    setFilters((prev) => ({ ...prev, [name]: value }));
  };

  const loadBuildings = async () => {
    setLoading(true);
    setError('');
    try {
      const result = await BuildingApi.list(filters);
      setData(result);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <section className="card">
      <div className="card-header">
        <h2>Buildings</h2>
        {(user?.role === 'Editor' || user?.role === 'Admin') && (
          <button type="button" onClick={() => navigate('/buildings/add')}>
            Add Building
          </button>
        )}
      </div>
      {error ? <p className="error">{error}</p> : null}
      <form className="filters" onSubmit={(event) => event.preventDefault()}>
        <input name="street" placeholder="Street" value={filters.street} onChange={handleChange} />
        <input name="houseNumber" placeholder="House No." value={filters.houseNumber} onChange={handleChange} />
        <input name="name" placeholder="Nickname" value={filters.name} onChange={handleChange} />
        <input name="status" placeholder="Status" value={filters.status} onChange={handleChange} />
        <input name="neighborhood" placeholder="Neighborhood" value={filters.neighborhood} onChange={handleChange} />
        <button type="button" onClick={loadBuildings} disabled={loading}>
          {loading ? 'Searching…' : 'Search'}
        </button>
      </form>
      <table>
        <thead>
          <tr>
            <th>Building</th>
            <th>Location</th>
            <th>Status</th>
            <th>Category</th>
            <th />
          </tr>
        </thead>
         <tbody>
          {data.items.map((item) => (
            <tr key={item.id}>
              <td>{item.buildingName}</td>
              <td>
                {item.streetName} {item.houseNumber}, {item.neighborhood}
              </td>
              <td>{item.shikumStatus}</td>
              <td>{item.bldSivug}</td>
              <td>
                <Link to={`/buildings/${item.id}`}>Open</Link>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {!data.items.length && !loading ? <p>No buildings found. Try different filters.</p> : null}
    </section>
  );
}
