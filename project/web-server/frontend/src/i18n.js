export const ROLE_LABELS = {
  Viewer: 'צופה',
  Editor: 'עורך',
  Admin: 'מנהל'
};

export const STATUS_OPTIONS = [
  { id: 1, label: 'מיפוי החסמים וגיבוש פתרון', value: 'MappingBarriersAndSolution' },
  { id: 2, label: 'העברת בעלות', value: 'OwnershipTransfer' },
  { id: 3, label: 'חסמים המונעים פיתוח', value: 'DevelopmentBarriers' },
  { id: 4, label: 'הבעלים בוחן אפיק פעולה לשיקום', value: 'OwnerConsideringAction' },
  { id: 5, label: 'הכנת תכנית שיקום', value: 'PreparingRehabPlan' },
  { id: 6, label: 'תכנית מאושרת, הכנה לביצוע', value: 'PlanApprovedPreparingExecution' },
  { id: 7, label: 'בביצוע', value: 'InExecution' },
  { id: 8, label: 'הליך אכלוס', value: 'OccupancyProcess' }
];

export const STATUS_LABEL_MAP = STATUS_OPTIONS.reduce(
  (acc, option) => {
    acc[option.value] = option.label;
    return acc;
  },
  { Unknown: 'לא ידוע' }
);

export const statusToLabel = (status) => STATUS_LABEL_MAP[status] || status || 'לא ידוע';

// Map numeric SelectTable values to the enum names expected by the backend.
export const STATUS_VALUE_BY_ID = {
  1: 'MappingBarriersAndSolution',
  2: 'OwnershipTransfer',
  3: 'DevelopmentBarriers',
  4: 'OwnerConsideringAction',
  5: 'PreparingRehabPlan',
  6: 'PlanApprovedPreparingExecution',
  7: 'InExecution',
  8: 'OccupancyProcess'
};
