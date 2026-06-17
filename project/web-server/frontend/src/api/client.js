const API_BASE = '/api';

let authToken = null;

const toCamel = (key) => (key ? key.charAt(0).toLowerCase() + key.slice(1) : key);

const toOptionalInt = (value) => {
  if (value === null || value === undefined || value === '') return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? Math.trunc(parsed) : null;
};

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
    const error = new Error(message || 'Request failed');
    error.status = response.status;
    error.payload = payload;
    throw error;
  }

  return payload;
}

async function requestBlob(path, options = {}) {
  const headers = { ...(options.headers || {}) };
  if (authToken) {
    headers.Authorization = `Bearer ${authToken}`;
  }

  const response = await fetch(`${API_BASE}${path}`, {
    method: options.method || 'GET',
    headers,
    body: options.body
  });

  if (!response.ok) {
    let message = response.statusText;
    try {
      const text = await response.text();
      if (text) {
        const parsed = JSON.parse(text);
        message = parsed && parsed.error ? parsed.error : message;
      }
    } catch {
      // Ignore parsing errors and keep status text.
    }
    throw new Error(message || 'Request failed');
  }

  return response.blob();
}

const mapBuildingSummary = (item) => ({
  id: item.id,
  streetId: item.streetId,
  street: item.streetName,
  houseNumber: item.houseNumber,
  nickname: item.buildingName,
  bldSivug: item.bldSivug,
  status: item.shikumStatus,
  area: item.neighborhood,
  sugBaalut: item.sugBaalut,
  quarter: item.quarter,
  subQuarter: item.subQuarter,
  statisticalArea: item.statisticalArea,
  statusSummary: item.statusSummary || '',
  updatedAt: item.statusSummaryUpdatedAt
});

const getLogUsername = (log) => {
  if (log.createdBy || log.createdByUser || log.createdByUserId) {
    return log.createdBy || log.createdByUser || log.createdByUserId;
  }
  if (log.category === 'Seed' || log.title === 'אתחול מערכת') {
    return 'אתחול מערכת';
  }
  return 'system';
};

const mapLog = (log) => ({
  id: log.id,
  buildingId: log.buildingId,
  actionType: log.category || log.severity || log.title,
  description: log.message,
  username: getLogUsername(log),
  createdAt: log.createdAt,
  snapshot: parseSnapshot(log.message),
  buildingStreet: log.buildingStreet,
  buildingHouseNumber: log.buildingHouseNumber,
  buildingNickname: log.buildingNickname,
  buildingNeighborhood: log.buildingNeighborhood,
  buildingBldSivug: log.buildingBldSivug,
  buildingStatus: log.buildingStatus,
  buildingStatusSummary: log.buildingStatusSummary,
  buildingSugBaalut: log.buildingSugBaalut,
  buildingQuarter: log.buildingQuarter,
  buildingSubQuarter: log.buildingSubQuarter,
  buildingStatisticalArea: log.buildingStatisticalArea
});

const mapBuildingField = (field) => ({
  category: field.category,
  fieldName: field.fieldName,
  columnName: field.columnName,
  selectTableName: field.selectTableName,
  includeInEventLog: field.includeInEventLog,
  value: field.value,
  rawValue: field.rawValue
});

const mapBuildingDetail = (data) => ({
  id: data.summary.id,
  streetId: data.summary.streetId,
  street: data.summary.streetName,
  houseNumber: data.summary.houseNumber,
  nickname: data.summary.buildingName,
  bldSivug: data.summary.bldSivug,
  status: data.summary.shikumStatus,
  area: data.summary.neighborhood,
  sugBaalut: data.summary.sugBaalut,
  quarter: data.summary.quarter,
  subQuarter: data.summary.subQuarter,
  statisticalArea: data.summary.statisticalArea,
  statusSummary: data.statusSummary,
  updatedAt: data.statusSummaryUpdatedAt,
  complaints: data.complaints,
  photos: data.photos || [],
  external: data.externalData || {},
  fields: Array.isArray(data.fields)
    ? data.fields.map((field) => mapBuildingField(field))
    : [],
  logs: (data.recentLogs || []).map((log) => mapLog(log))
});

const mapUser = (user) => ({
  id: user.id,
  username: user.username,
  email: user.email,
  role: user.role,
  twoFactorEnabled: user.twoFactorEnabled,
  createdAt: user.createdAt
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
    if (filters.streetId) params.append('streetId', filters.streetId);
    if (filters.houseNumber) params.append('houseNumber', filters.houseNumber);
    if (filters.nickname) params.append('name', filters.nickname);
    if (filters.status) params.append('status', filters.status);
    if (filters.bldSivug) params.append('bldSivug', filters.bldSivug);
    if (filters.sugBaalut) params.append('sugBaalut', filters.sugBaalut);
    if (filters.quarter) params.append('quarter', filters.quarter);
    if (filters.subQuarter) params.append('subQuarter', filters.subQuarter);
    if (filters.statisticalArea) params.append('statisticalArea', filters.statisticalArea);
    if (filters.updatedFrom) params.append('updatedFrom', filters.updatedFrom);
    if (filters.updatedTo) params.append('updatedTo', filters.updatedTo);
    if (filters.statusSummary) params.append('statusSummary', filters.statusSummary);

    const data = await request(`/buildings${params.toString() ? `?${params}` : ''}`);
    return (data.items || []).map(mapBuildingSummary);
  },
  async fetchMapBuildings(bounds = {}, filters = {}) {
    const params = new URLSearchParams();
    if (bounds.north !== undefined) params.append('north', bounds.north);
    if (bounds.south !== undefined) params.append('south', bounds.south);
    if (bounds.east !== undefined) params.append('east', bounds.east);
    if (bounds.west !== undefined) params.append('west', bounds.west);
    if (filters.status) params.append('status', filters.status);
    if (filters.bldSivug) params.append('bldSivug', filters.bldSivug);

    const data = await request(`/buildings/map${params.toString() ? `?${params}` : ''}`);
    return (data || []).map((item) => ({
      id: item.id,
      streetId: item.streetId,
      street: item.streetName,
      houseNumber: item.houseNumber,
      nickname: item.buildingName,
      area: item.neighborhood,
      status: item.shikumStatus,
      bldSivug: item.bldSivug,
      statusSummary: item.statusSummary || '',
      updatedAt: item.statusSummaryUpdatedAt,
      latitude: item.latitude,
      longitude: item.longitude
    }));
  },
  async exportBuildings(filters = {}, includeImages = false) {
    const params = new URLSearchParams();
    if (filters.street) params.append('street', filters.street);
    if (filters.streetId) params.append('streetId', filters.streetId);
    if (filters.houseNumber) params.append('houseNumber', filters.houseNumber);
    if (filters.nickname) params.append('name', filters.nickname);
    if (filters.status) params.append('status', filters.status);
    if (filters.bldSivug) params.append('bldSivug', filters.bldSivug);
    if (filters.sugBaalut) params.append('sugBaalut', filters.sugBaalut);
    if (filters.quarter) params.append('quarter', filters.quarter);
    if (filters.subQuarter) params.append('subQuarter', filters.subQuarter);
    if (filters.statisticalArea) params.append('statisticalArea', filters.statisticalArea);
    if (filters.updatedFrom) params.append('updatedFrom', filters.updatedFrom);
    if (filters.updatedTo) params.append('updatedTo', filters.updatedTo);
    if (filters.statusSummary) params.append('statusSummary', filters.statusSummary);

    if (includeImages) params.append('includeImages', 'true');
    const query = params.toString();
    const path = query ? `/buildings/export?${query}` : '/buildings/export';
    return requestBlob(path);
  },
  async exportBuildingsByIds(ids = [], includeImages = false) {
    const query = includeImages ? '?includeImages=true' : '';
    return requestBlob(`/buildings/export${query}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ ids })
    });
  },
  async convertBuildingsTemplate(file) {
    if (!file) {
      throw new Error('excel file is required');
    }
    const formData = new FormData();
    formData.append('file', file);
    return requestBlob('/buildings/convert-template', { method: 'POST', body: formData });
  },
  async previewImportBuildings(file) {
    if (!file) {
      throw new Error('excel file is required');
    }
    const formData = new FormData();
    formData.append('file', file);
    return request('/buildings/import/preview', { method: 'POST', body: formData });
  },
  async applyImportBuildings(rows = []) {
    return request('/buildings/import/apply', {
      method: 'POST',
      body: { rows }
    });
  },
  async validateImportRow(values = {}) {
    return request('/buildings/import/validate', {
      method: 'POST',
      body: { values }
    });
  },
  async exportBuildingCard(id) {
    if (!id && id !== 0) {
      throw new Error('building id is required');
    }
    return requestBlob(`/buildings/${id}/card`);
  },
  async exportBuildingCardsByIds(ids = []) {
    return requestBlob('/buildings/export-cards', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ ids })
    });
  },
  async exportStreets() {
    return requestBlob('/streets/export');
  },
  async exportStreetsSelection(ids = []) {
    return requestBlob('/streets/export', {
      method: 'POST',
      body: { streetIds: ids }
    });
  },
  async previewImportStreets(file) {
    if (!file) {
      throw new Error('excel file is required');
    }
    const formData = new FormData();
    formData.append('file', file);
    return request('/streets/import/preview', { method: 'POST', body: formData });
  },
  async validateImportStreet(values = {}) {
    return request('/streets/import/validate', {
      method: 'POST',
      body: { values }
    });
  },
  async applyImportStreets(rows = []) {
    return request('/streets/import/apply', {
      method: 'POST',
      body: { rows }
    });
  },
  async convertStreetsTemplate(file) {
    if (!file) {
      throw new Error('excel file is required');
    }
    const formData = new FormData();
    formData.append('file', file);
    return requestBlob('/streets/convert-template', { method: 'POST', body: formData });
  },
  async fetchBuilding(id) {
    const data = await request(`/buildings/${id}`);
    return mapBuildingDetail(data);
  },
  async fetchBuildingFieldTemplate() {
    const data = await request('/buildings/template');
    return Array.isArray(data) ? data.map((field) => mapBuildingField(field)) : [];
  },
  async createBuilding(form) {
    const id = toOptionalInt(form.id ?? form.Id);
    const bldSivug = toOptionalInt(form.category ?? form.bldSivug);
    const payload = {
      streetId: toOptionalInt(form.streetId),
      houseNumber: form.bldNum || form.houseNumber || '',
      buildingName: form.bldName || form.nickname || form.streetName || 'מבנה',
      neighborhood: form.area || form.neighborhood || '',
      bldSivug,
      shikumStatus: form.status || form.shikumStatus || 'Unknown',
      statusSummary: form.statusSummary || '',
      complaints: form.complaints || '',
      allowDuplicate: Boolean(form.allowDuplicate)
    };
    if (id !== null && id !== undefined) {
      payload.id = id;
    }
    const created = await request('/buildings', { method: 'POST', body: payload });
    return mapBuildingSummary(created);
  },
  async updateBuilding(id, form) {
    const nextId = toOptionalInt(form.id ?? form.Id);
    const bldSivug = toOptionalInt(form.category ?? form.bldSivug);
    const payload = {
      id: nextId ?? id,
      streetId: toOptionalInt(form.streetId) ?? toOptionalInt(form.street),
      houseNumber: form.bldNum || form.houseNumber || '',
      buildingName: form.bldName || form.nickname || '',
      neighborhood: form.area || form.neighborhood || '',
      bldSivug,
      shikumStatus: form.status || form.shikumStatus || 'Unknown',
      statusSummary: form.statusSummary || '',
      complaints: form.complaints || ''
    };
    await request(`/buildings/${id}`, { method: 'PUT', body: payload });
    return this.fetchBuilding(id);
  },
  async updateBuildingFields(id, fields, allowDuplicate = false) {
    const payload = { fields, allowDuplicate };
    const data = await request(`/buildings/${id}/fields`, { method: 'PUT', body: payload });
    return mapBuildingDetail(data);
  },
  async deleteBuilding(id, reason = 'Administrative request') {
    return request(`/buildings/${id}`, {
      method: 'DELETE',
      body: { reason, confirm: true }
    });
  },
  async fetchStreets(search = '') {
    const params = new URLSearchParams();
    if (search) params.append('search', search);
    return request(`/streets${params.toString() ? `?${params}` : ''}`);
  },
  async createStreet(payload) {
    return request('/streets', { method: 'POST', body: payload });
  },
  async updateStreet(id, payload) {
    return request(`/streets/${id}`, { method: 'PUT', body: payload });
  },
  async deleteStreet(id) {
    return request(`/streets/${id}`, { method: 'DELETE' });
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
  async restoreBuildingFromLog(logId) {
    return request(`/buildings/restore/${logId}`, { method: 'POST' });
  },
  async fetchLogs(filters = {}) {
    const params = new URLSearchParams();
    if (filters.buildingId) params.append('buildingId', filters.buildingId);
    if (filters.userId) params.append('userId', filters.userId);
    if (filters.user) params.append('user', filters.user);
    if (filters.logType) params.append('logType', filters.logType);
    if (filters.street) params.append('street', filters.street);
    if (filters.streetId) params.append('streetId', filters.streetId);
    if (filters.houseNumber) params.append('houseNumber', filters.houseNumber);
    if (filters.nickname) params.append('name', filters.nickname);
    if (filters.status) params.append('status', filters.status);
    if (filters.area) params.append('neighborhood', filters.area);
    if (filters.bldSivug) params.append('bldSivug', filters.bldSivug);
    if (filters.sugBaalut) params.append('sugBaalut', filters.sugBaalut);
    if (filters.quarter) params.append('quarter', filters.quarter);
    if (filters.subQuarter) params.append('subQuarter', filters.subQuarter);
    if (filters.statisticalArea) params.append('statisticalArea', filters.statisticalArea);
    if (filters.statusSummary) params.append('statusSummary', filters.statusSummary);
    if (filters.updatedFrom) params.append('from', filters.updatedFrom);
    if (filters.updatedTo) params.append('to', filters.updatedTo);
    if (filters.startDate) params.append('from', filters.startDate);
    if (filters.endDate) params.append('to', filters.endDate);
    const data = await request(`/logs${params.toString() ? `?${params}` : ''}`);
    const items = Array.isArray(data.items) ? data.items : data;
    return (items || []).map(mapLog);
  },
  async fetchSelectTable(name) {
    if (!name) throw new Error('select table name is required');
    return request(`/select-tables/${encodeURIComponent(name)}`);
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
  async setUserPassword(id, newPassword) {
    return request(`/users/${id}/password`, {
      method: 'POST',
      body: { newPassword }
    });
  },
  async healthCheck() {
    return request('/health/db');
  }
};

export default api;
