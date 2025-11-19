import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { UsersApi } from '../api.js';

export default function UserListPage() {
  const [users, setUsers] = useState([]);
  const [error, setError] = useState('');
  const [form, setForm] = useState({ username: '', email: '', password: '', role: 'Viewer' });

  const load = async () => {
    try {
      const list = await UsersApi.list();
      setUsers(list);
    } catch (err) {
      setError(err.message);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const handleChange = (event) => {
    const { name, value } = event.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  };

  const createUser = async (event) => {
    event.preventDefault();
    try {
      await UsersApi.create(form);
      setForm({ username: '', email: '', password: '', role: 'Viewer' });
      load();
    } catch (err) {
      setError(err.message);
    }
  };

  return (
    <section className="grid-two">
      <article className="card">
        <h2>Users</h2>
        {error ? <p className="error">{error}</p> : null}
        <table>
          <thead>
            <tr>
              <th>Name</th>
              <th>Email</th>
              <th>Role</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {users.map((user) => (
              <tr key={user.id}>
                <td>{user.username}</td>
                <td>{user.email}</td>
                <td>{user.role}</td>
                <td>
                  <Link to={`/users/${user.id}`}>Details</Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </article>
      <article className="card">
        <h3>Create User</h3>
        <form className="form-grid" onSubmit={createUser}>
          <label>
            Username
            <input name="username" value={form.username} onChange={handleChange} required />
          </label>
          <label>
            Email
            <input name="email" value={form.email} onChange={handleChange} required />
          </label>
          <label>
            Password
            <input type="password" name="password" value={form.password} onChange={handleChange} required />
          </label>
          <label>
            Role
            <select name="role" value={form.role} onChange={handleChange}>
              <option value="Viewer">Viewer</option>
              <option value="Editor">Editor</option>
              <option value="Admin">Admin</option>
            </select>
          </label>
          <button type="submit">Add</button>
        </form>
      </article>
    </section>
  );
}
