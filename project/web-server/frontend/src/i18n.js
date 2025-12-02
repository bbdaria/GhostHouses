export const ROLE_LABELS = {
  Viewer: 'צופה',
  Editor: 'עורך',
  Admin: 'מנהל'
};

export const STATUS_OPTIONS = [
  { id: 1, label: 'נטוש', value: 'Abandoned' },
  { id: 2, label: 'בבדיקה', value: 'UnderInspection' },
  { id: 3, label: 'נהרס', value: 'Demolished' },
  { id: 4, label: 'מאוכלס', value: 'Occupied' }
];

export const STATUS_LABEL_MAP = STATUS_OPTIONS.reduce(
  (acc, option) => {
    acc[option.value] = option.label;
    return acc;
  },
  { Unknown: 'לא ידוע', 'Under Inspection': 'בבדיקה', 'Pending Survey': 'בבדיקה' }
);

export const statusToLabel = (status) => STATUS_LABEL_MAP[status] || status || 'לא ידוע';
