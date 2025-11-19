const API_BASE = '/api';

let authToken = null;

export function setAuthToken(token) {
  authToken = token;
}

export function clearAuthToken() {
  authToken = null;
}

async function request(path, options = {}) {
  const headers = options.headers ? { ...options.headers } : {};
  if (authToken) {
    headers.Authorization = `Bearer ${authToken}`;
  }
  let body = options.body;
  if (body && typeof body !== 'string' && !(body instanceof FormData)) {
    headers['Content-Type'] = 'application/json';
    body = JSON.stringify(body);
  }
  const response = await fetch(`${API_BASE}${path}`, {
    method: options.method || 'GET',
    headers,
    body
  });
  const text = await response.text();
  let payload = null;
  if (text) {
    try {
      payload = JSON.parse(text);
    } catch {
      payload = text;
    }
  }
  if (!response.ok) {
    const message = payload && payload.error ? payload.error : response.statusText;
    throw new Error(message);
  }
  return payload;
}

const api = {
  async login(username, password) {
    return request('/auth/login', {
      method: 'POST',
      body: { username, password }
    });
  },
  async verifyOtp(username, otp, otpChallengeId) {
    return request('/auth/verify-otp', {
      method: 'POST',
      body: { username, otp, otpChallengeId }
    });
  },
  async me() {
    return request('/auth/me');
  },
  async logout() {
    return request('/auth/logout', { method: 'POST' });
  },
  async fetchBuildings(params = {}) {
    const query = new URLSearchParams(params);
    return request(`/buildings${query.toString() ? `?${query}` : ''}`);
  },
  async fetchBuilding(id) {
    return request(`/buildings/${id}`);
  },
  async createBuilding(payload) {
    return request('/buildings', { method: 'POST', body: payload });
  },
  async updateBuilding(id, payload) {
    return request(`/buildings/${id}`, { method: 'PUT', body: payload });
  },
  async deleteBuilding(id, confirm = true) {
    return request(`/buildings/${id}`, { method: 'DELETE', body: { confirm } });
  },
  async fetchBuildingLogs(id) {
    return request(`/buildings/${id}/logs`);
  },
  async createBuildingLog(id, payload) {
    return request(`/buildings/${id}/logs`, { method: 'POST', body: payload });
  },
  async updateBuildingLog(id, logId, payload) {
    return request(`/buildings/${id}/logs/${logId}`, { method: 'PUT', body: payload });
  },
  async deleteBuildingLog(id, logId) {
    return request(`/buildings/${id}/logs/${logId}`, { method: 'DELETE' });
  },
  async fetchLogs(params = {}) {
    const query = new URLSearchParams(params);
    return request(`/logs${query.toString() ? `?${query}` : ''}`);
  },
  async fetchUsers() {
    return request('/users');
  },
  async fetchUser(id) {
    return request(`/users/${id}`);
  },
  async createUser(payload) {
    return request('/users', { method: 'POST', body: payload });
  },
  async updateUser(id, payload) {
    return request(`/users/${id}`, { method: 'PUT', body: payload });
  },
  async deleteUser(id) {
    return request(`/users/${id}`, { method: 'DELETE', body: { confirm: true } });
  },
  async listSyncJobs() {
    return request('/sync/jobs');
  },
  async enqueueSync(jobType, payload = {}) {
    return request('/sync/jobs', { method: 'POST', body: { jobType, ...payload } });
  },
  async runSync(jobType, payload = {}) {
    return request('/sync/run', { method: 'POST', body: { jobType, ...payload } });
  }
};

export default api;
