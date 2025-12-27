export const LAST_BUILDING_KEY = 'ghosthouses:lastBuildingId';

export const BUILDING_FIELD_LABELS = {
  street: 'שם רחוב',
  houseNumber: 'מספר בית',
  nickname: 'כינוי הבניין',
  status: 'סטטוס שיקום',
  area: 'אזור',
  statusSummary: 'תמונת מצב (תמצית מצב)'
};

export const STATUS_SELECT_PLACEHOLDER = 'בחר סטטוס שיקום';

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
  { key: 'nickname', label: 'כינוי הבניין' },
  { key: 'status', label: 'סטטוס שיקום' },
  { key: 'bldSivug', label: 'סיווג' },
  { key: 'summary', label: 'תמונת מצב (תמצית מצב)' },
  { key: 'user', label: 'משתמש' },
  { key: 'date', label: 'תאריך עדכון' },
  { key: 'actions', label: 'פעולות' }
];
