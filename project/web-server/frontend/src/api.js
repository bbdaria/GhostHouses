const BASE_URL = '/api';

const getToken = () => localStorage.getItem('gh_token');

const buildHeaders = (extra = {}) => {
  const headers = {
    'Content-Type': 'application/json',
    ...extra,
  };
  const token = getToken();
  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }
  return headers;
};

const toQueryString = (params = {}) => {
  const filtered = Object.entries(params).filter(([, value]) => value !== undefined && value !== null && value !== '');
  if (!filtered.length) {
    return '';
  }

  const qs = new URLSearchParams(filtered).toString();
  return qs ? `?${qs}` : '';
};

export async function apiFetch(path, options = {}) {
  const response = await fetch(`${BASE_URL}${path}`, {
    ...options,
    headers: buildHeaders(options.headers),
  });

  if (response.status === 401) {
    throw new Error('Unauthorized');
  }

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || 'Request failed');
  }

  if (response.status === 204) {
    return null;
  }

  return response.json();
}

export const AuthApi = {
  login: (payload) => apiFetch('/auth/login', { method: 'POST', body: JSON.stringify(payload) }),
  verify2fa: (payload) => apiFetch('/auth/verify-2fa', { method: 'POST', body: JSON.stringify(payload) }),
  me: () => apiFetch('/auth/me'),
};

export const BuildingApi = {
  list: (params = {}) => apiFetch(`/buildings${toQueryString(params)}`),
  get: (id) => apiFetch(`/buildings/${id}`),
  create: (payload) => apiFetch('/buildings', { method: 'POST', body: JSON.stringify(payload) }),
  update: (id, payload) => apiFetch(`/buildings/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  remove: (id, payload) => apiFetch(`/buildings/${id}`, { method: 'DELETE', body: JSON.stringify(payload) }),
};

export const LogsApi = {
  list: (params = {}) => apiFetch(`/logs${toQueryString(params)}`),
  forBuilding: (buildingId) => apiFetch(`/logs/building/${buildingId}`),
  create: (buildingId, payload) =>
    apiFetch(`/logs/building/${buildingId}`, { method: 'POST', body: JSON.stringify(payload) }),
  update: (id, payload) => apiFetch(`/logs/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  remove: (id) => apiFetch(`/logs/${id}`, { method: 'DELETE' }),
};

export const UsersApi = {
  list: () => apiFetch('/users'),
  get: (id) => apiFetch(`/users/${id}`),
  create: (payload) => apiFetch('/users', { method: 'POST', body: JSON.stringify(payload) }),
  update: (id, payload) => apiFetch(`/users/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  reset2fa: (id) => apiFetch(`/users/${id}/reset-2fa`, { method: 'POST' }),
};
