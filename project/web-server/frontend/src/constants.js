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
  statusSummary: 'לדוגמה: ממתין לסקר',
  quarter: 'לדוגמה: 4',
  subQuarter: 'לדוגמה: 4א',
  statisticalArea: 'לדוגמה: 1234'
};

export const LOG_TABLE_COLUMNS = [
  { key: 'street', label: 'שם רחוב' },
  { key: 'houseNumber', label: 'מספר בית' },
  { key: 'nickname', label: 'כינוי הבניין' },
  { key: 'updatedAt', label: 'תאריך שינוי' },
  { key: 'changedFields', label: 'ערכים שהשתנו' },
  { key: 'oldValues', label: 'ערך קודם' },
  { key: 'newValues', label: 'ערך מעודכן' },
  { key: 'user', label: 'משתמש' },
  { key: 'actions', label: 'פעולות' }
];
