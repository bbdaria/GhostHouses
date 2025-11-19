import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { BuildingApi } from '../api.js';

const initialState = {
  fldId: '',
  buildingName: '',
  streetName: '',
  houseNumber: '',
  neighborhood: '',
  bldSivug: '',
  shikumStatus: '',
  statusSummary: '',
  complaints: '',
  photos: '',
};

export default function AddBuildingPage() {
  const navigate = useNavigate();
  const [form, setForm] = useState(initialState);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleChange = (event) => {
    const { name, value } = event.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    setLoading(true);
    setError('');
    try {
      await BuildingApi.create({
        ...form,
        photos: form.photos ? form.photos.split(',').map((x) => x.trim()) : [],
      });
      navigate('/buildings');
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <section className="card">
      <h2>Add Building</h2>
      {error ? <p className="error">{error}</p> : null}
      <form className="form-grid" onSubmit={handleSubmit}>
        {Object.entries({
          fldId: 'Municipal ID',
          buildingName: 'Building Name',
          streetName: 'Street',
          houseNumber: 'Number',
          neighborhood: 'Neighborhood',
          bldSivug: 'Category',
          shikumStatus: 'Rehab Status',
          statusSummary: 'Status Summary',
          complaints: 'Complaints',
          photos: 'Photo URLs (comma separated)',
        }).map(([name, label]) => (
          <label key={name}>
            {label}
            <input
              name={name}
              value={form[name]}
              onChange={handleChange}
              required={!['complaints', 'photos'].includes(name)}
            />
          </label>
        ))}
        <button type="submit" disabled={loading}>
          {loading ? 'Saving…' : 'Save'}
        </button>
      </form>
    </section>
  );
}
