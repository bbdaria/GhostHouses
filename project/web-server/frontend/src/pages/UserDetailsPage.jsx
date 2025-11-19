import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { UsersApi } from '../api.js';

export default function UserDetailsPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [user, setUser] = useState(null);
  const [error, setError] = useState('');

  const load = async () => {
    try {
      const detail = await UsersApi.get(id);
      setUser(detail);
    } catch (err) {
      setError(err.message);
    }
  };

  useEffect(() => {
    load();
  }, [id]);

  if (!user) {
    return <div className="card">Loading user…</div>;
  }

  const update = async (changes) => {
    try {
      await UsersApi.update(id, changes);
      load();
    } catch (err) {
      setError(err.message);
    }
  };

  const resetTwoFactor = async () => {
    try {
      await UsersApi.reset2fa(id);
    } catch (err) {
      setError(err.message);
    }
  };

  return (
    <section className="card">
      <button type="button" onClick={() => navigate(-1)}>
        ← Back
      </button>
      <h2>{user.username}</h2>
      {error ? <p className="error">{error}</p> : null}
      <p>
        <strong>Email:</strong> {user.email}
      </p>
      <label>
        Role
        <select value={user.role} onChange={(event) => update({ role: event.target.value })}>
          <option value="Viewer">Viewer</option>
          <option value="Editor">Editor</option>
          <option value="Admin">Admin</option>
        </select>
      </label>
      <label>
        Two Factor Enabled
        <select
          value={user.twoFactorEnabled ? 'true' : 'false'}
          onChange={(event) => update({ twoFactorEnabled: event.target.value === 'true' })}
        >
          <option value="true">Enabled</option>
          <option value="false">Disabled</option>
        </select>
      </label>
      <button type="button" onClick={resetTwoFactor}>
        Reset 2FA secret
      </button>
    </section>
  );
}
