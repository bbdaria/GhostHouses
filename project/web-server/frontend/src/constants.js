export const LAST_BUILDING_KEY = 'ghosthouses:lastBuildingId';

export const BUILDING_FIELD_LABELS = {
  street: 'שם רחוב',
  houseNumber: 'מספר בית',
  nickname: 'כינוי',
  status: 'סטטוס',
  area: 'אזור',
  statusSummary: 'תקציר מצב'
};

export const STATUS_SELECT_PLACEHOLDER = 'בחר סטטוס';

export const BUILDING_FIELD_PLACEHOLDERS = {
  street: 'לדוגמה: הרצל',
  houseNumber: 'לדוגמה: 12א',
  nickname: 'לדוגמה: הטחנה',
  area: 'לדוגמה: אזור תעשייה',
  statusSummary: 'לדוגמה: ממתין לסקר'
};

export const LOG_TABLE_COLUMNS = [
  { key: 'street', label: 'שם רחוב' },
  { key: 'houseNumber', label: 'מספר בית' },
  { key: 'nickname', label: 'כינוי' },
  { key: 'status', label: 'סטטוס' },
  { key: 'area', label: 'אזור' },
  { key: 'summary', label: 'תקציר מצב' },
  { key: 'user', label: 'משתמש' },
  { key: 'date', label: 'תאריך עדכון' },
  { key: 'actions', label: 'ניהול' }
];
