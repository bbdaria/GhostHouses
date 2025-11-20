const API_BASE = '/api';

let authToken = null;

const toCamel = (key) => (key ? key.charAt(0).toLowerCase() + key.slice(1) : key);

const normalizeSnapshot = (value) => {
  if (value === null || typeof value !== 'object') {
    return value;
  }

  if (Array.isArray(value)) {
    return value.map(normalizeSnapshot);
  }

  return Object.entries(value).reduce((acc, [k, v]) => {
    acc[toCamel(k)] = normalizeSnapshot(v);
    return acc;
  }, {});
};

const parseSnapshot = (message) => {
  if (!message) return null;
  try {
    const parsed = JSON.parse(message);
    if (parsed && typeof parsed === 'object') {
      return normalizeSnapshot(parsed);
    }
  } catch {
    return null;
  }
  return null;
};

export function setAuthToken(token) {
  authToken = token;
}

export function clearAuthToken() {
  authToken = null;
}

async function request(path, options = {}) {
  const headers = { ...(options.headers || {}) };
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
    throw new Error(message || 'Request failed');
  }

  return payload;
}

const mapBuildingSummary = (item) => ({
  id: item.id,
  fldId: item.fldId,
  street: item.streetName,
  houseNumber: item.houseNumber,
  nickname: item.buildingName,
  status: item.shikumStatus,
  area: item.neighborhood,
  statusSummary: item.statusSummary || '',
  updatedAt: item.statusSummaryUpdatedAt
});

const mapLog = (log) => ({
  id: log.id,
  buildingId: log.buildingId,
  actionType: log.category || log.severity || log.title,
  description: log.message,
  username: log.createdBy || log.createdByUser || log.createdByUserId || 'system',
  createdAt: log.createdAt,
  snapshot: parseSnapshot(log.message),
  buildingStreet: log.buildingStreet,
  buildingHouseNumber: log.buildingHouseNumber,
  buildingNickname: log.buildingNickname,
  buildingNeighborhood: log.buildingNeighborhood,
  buildingStatus: log.buildingStatus,
  buildingStatusSummary: log.buildingStatusSummary
});

const mapBuildingDetail = (data) => ({
  id: data.summary.id,
  fldId: data.summary.fldId,
  street: data.summary.streetName,
  houseNumber: data.summary.houseNumber,
  nickname: data.summary.buildingName,
  bldSivug: data.summary.bldSivug,
  status: data.summary.shikumStatus,
  area: data.summary.neighborhood,
  statusSummary: data.statusSummary,
  updatedAt: data.statusSummaryUpdatedAt,
  complaints: data.complaints,
  photos: data.photos || [],
  external: data.externalData || {},
  logs: (data.recentLogs || []).map((log) => ({
    ...mapLog(log),
    username: log.createdBy || 'system'
  }))
});

const mapUser = (user) => ({
  id: user.id,
  username: user.username,
  email: user.email,
  role: user.role,
  twoFactorEnabled: user.twoFactorEnabled
});

const api = {
  async login(username, password) {
    const result = await request('/auth/login', {
      method: 'POST',
      body: { username, password }
    });
    return {
      userId: result.userId,
      challengeToken: result.challengeToken,
      demoCode: result.devTwoFactorCode
    };
  },
  async verifyOtp({ userId, challengeToken, code }) {
    const result = await request('/auth/verify-2fa', {
      method: 'POST',
      body: { userId, challengeToken, code }
    });
    return {
      token: result.token,
      profile: {
        id: result.id,
        username: result.username,
        email: result.email,
        role: result.role
      }
    };
  },
  async me() {
    return request('/auth/me');
  },
  async fetchBuildings(filters = {}) {
    const params = new URLSearchParams();
    if (filters.street) params.append('street', filters.street);
    if (filters.houseNumber) params.append('houseNumber', filters.houseNumber);
    if (filters.nickname) params.append('name', filters.nickname);
    if (filters.status) params.append('status', filters.status);
    if (filters.area) params.append('neighborhood', filters.area);

    const data = await request(`/buildings${params.toString() ? `?${params}` : ''}`);
    return (data.items || []).map(mapBuildingSummary);
  },
  async fetchBuilding(id) {
    const data = await request(`/buildings/${id}`);
    return mapBuildingDetail(data);
  },
  async createBuilding(form) {
    const payload = {
      fldId: form.fldId || `GH-${Date.now()}`,
      streetName: form.streetName || '',
      houseNumber: form.bldNum || form.houseNumber || '',
      buildingName: form.bldName || form.nickname || form.streetName || 'מבנה',
      neighborhood: form.area || form.neighborhood || '',
      bldSivug: form.category || 'Unclassified',
      shikumStatus: form.status || form.shikumStatus || 'Unknown',
      statusSummary: form.statusSummary || '',
      complaints: form.complaints || ''
    };
    const created = await request('/buildings', { method: 'POST', body: payload });
    return mapBuildingSummary(created);
  },
  async updateBuilding(id, form) {
    const payload = {
      fldId: String(form.fldId || `GH-${id}`),
      streetName: form.streetName || form.street || '',
      houseNumber: form.bldNum || form.houseNumber || '',
      buildingName: form.bldName || form.nickname || '',
      neighborhood: form.area || form.neighborhood || '',
      bldSivug: form.category || 'Unclassified',
      shikumStatus: form.status || form.shikumStatus || 'Unknown',
      statusSummary: form.statusSummary || '',
      complaints: form.complaints || ''
    };
    await request(`/buildings/${id}`, { method: 'PUT', body: payload });
    return this.fetchBuilding(id);
  },
  async deleteBuilding(id, reason = 'Administrative request') {
    return request(`/buildings/${id}`, {
      method: 'DELETE',
      body: { reason, confirm: true }
    });
  },
  async fetchBuildingLogs(id) {
    const data = await request(`/logs/building/${id}`);
    return data.map(mapLog);
  },
  async createBuildingLog(id, payload) {
    const body = {
      title: payload.actionType || 'עדכון',
      message: payload.description || '',
      category: payload.actionType || 'General',
      severity: payload.severity || 'info'
    };
    return request(`/logs/building/${id}`, { method: 'POST', body });
  },
  async deleteBuildingLog(logId) {
    return request(`/logs/${logId}`, { method: 'DELETE' });
  },
  async fetchLogs(filters = {}) {
    const params = new URLSearchParams();
    if (filters.buildingId) params.append('buildingId', filters.buildingId);
    if (filters.userId) params.append('userId', filters.userId);
    if (filters.startDate) params.append('from', filters.startDate);
    if (filters.endDate) params.append('to', filters.endDate);
    const data = await request(`/logs${params.toString() ? `?${params}` : ''}`);
    const items = Array.isArray(data.items) ? data.items : data;
    return (items || []).map(mapLog);
  },
  async fetchUsers() {
    const data = await request('/users');
    return data.map(mapUser);
  },
  async fetchUser(id) {
    const detail = await request(`/users/${id}`);
    return mapUser(detail);
  },
  async createUser(form) {
    const payload = {
      username: form.username,
      email: form.email || `${form.username}@example.com`,
      password: form.password,
      role: form.role || 'Viewer'
    };
    return request('/users', { method: 'POST', body: payload });
  },
  async updateUser(id, form) {
    const payload = {};
    if (form.email) payload.email = form.email;
    if (form.role) payload.role = form.role;
    if (typeof form.twoFactorEnabled === 'boolean') {
      payload.twoFactorEnabled = form.twoFactorEnabled;
    }
    await request(`/users/${id}`, { method: 'PUT', body: payload });
    return this.fetchUser(id);
  },
  async resetUserTwoFactor(id) {
    return request(`/users/${id}/reset-2fa`, { method: 'POST' });
  },
  async healthCheck() {
    return request('/health/db');
  }
};

export default api;
