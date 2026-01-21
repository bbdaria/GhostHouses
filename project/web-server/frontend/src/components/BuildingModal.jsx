import React from 'react';

export default function BuildingModal({
  visible,
  mode,
  building,
  createForm,
  editFieldValues,
  streets,
  sivugOptions,
  statusOptions,
  selectTablesByName,
  selectTablesLoading,
  orderedFieldGroups,
  externalEntries,
  isEditRehabStatusRequired,
  canEdit,
  actionMessage,
  duplicatePrompt,
  onCreateChange,
  onCreateSubmit,
  onDuplicateConfirm,
  onDuplicateCancel,
  onEditChange,
  onEditSubmit,
  onDelete,
  onExportCard,
  onClose,
  detailError,
  loadStreets,
  sortFieldsForDisplay,
  getExcelAwareLabel,
  isDateField,
  shouldUseTextarea,
  isRequiredEditColumn,
  displayOrDash,
  formatStatusFieldValue,
  formatLogDate,
  openViewCategories,
  toggleViewCategory,
  openEditCategories,
  toggleEditCategory,
  handleCategoryToggleKeyDown
}) {
  if (!visible) return null;

  const loadingBuilding = (mode !== 'create' && !building);

  const stop = (e) => e.stopPropagation();

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-window" role="dialog" aria-modal="true" onClick={stop}>
        <div className="modal-header">
          <h3>
            {mode === 'create' && 'הוספת מבנה'}
            {mode === 'edit' && 'עריכת מבנה'}
            {mode === 'view' && 'פרטי מבנה'}
          </h3>
          <button type="button" className="modal-close" onClick={onClose} aria-label="Close">×</button>
        </div>

        <div className="modal-body">
          {loadingBuilding && <p className="muted">טוען פרטי מבנה…</p>}
          {mode === 'create' && (
            <form className="form-grid">
              <label>
                שם רחוב
                <select
                  name="streetId"
                  value={createForm.streetId}
                  onChange={onCreateChange}
                  onFocus={loadStreets}
                  required
                >
                  <option value="">בחר רחוב</option>
                  {streets.map((street) => (
                    <option key={street.streetId} value={street.streetId}>
                      {street.name}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                מספר בית
                <input
                  name="bldNum"
                  value={createForm.bldNum}
                  onChange={onCreateChange}
                  required
                />
              </label>
              <label>
                כינוי הבניין
                <input
                  name="bldName"
                  value={createForm.bldName}
                  onChange={onCreateChange}
                  required
                />
              </label>
              <label>
                סיווג
                <select name="category" value={createForm.category} onChange={onCreateChange} required>
                  <option value="">בחר סיווג</option>
                  {sivugOptions.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                סטטוס שיקום
                <select
                  name="shikumStatusId"
                  value={createForm.shikumStatusId}
                  onChange={onCreateChange}
                  required={false}
                >
                  <option value="">בחר סטטוס שיקום</option>
                  {statusOptions.map((option) => (
                    <option key={option.id} value={option.id}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </label>
              <label className="full-span">
                תמונת מצב (תמצית מצב)
                <textarea
                  name="statusSummary"
                  value={createForm.statusSummary}
                  onChange={onCreateChange}
                  placeholder=""
                />
              </label>
              {/* actions are in modal footer */}
            </form>
          )}

          {mode === 'edit' && building && (
            <form className="details-card">
              {selectTablesLoading && <p className="muted">טוען טבלאות בחירה…</p>}
              {orderedFieldGroups.map(([category, fields]) => {
                const isOpen = openEditCategories.has(category);
                return (
                  <div key={category} className="details-section">
                    <div
                      className={`details-section__header${isOpen ? ' is-open' : ''}`}
                      role="button"
                      tabIndex={0}
                      aria-expanded={isOpen}
                      onClick={() => toggleEditCategory(category)}
                      onKeyDown={(event) =>
                        handleCategoryToggleKeyDown(event, () => toggleEditCategory(category))
                      }
                    >
                      <h4>{category}</h4>
                      <span className="details-section__indicator" aria-hidden="true">
                        {isOpen ? '∨' : '∧'}
                      </span>
                    </div>
                    {isOpen && (
                      <div className="form-grid">
                        {sortFieldsForDisplay(fields).map((field) => {
                          const columnName = field.columnName;
                          const fieldName = field.fieldName;
                          if (!columnName) return null;
                          const required = isRequiredEditColumn(columnName);
                          if (columnName.toLowerCase() === 'streetid') {
                            return (
                              <label key={columnName}>
                                {getExcelAwareLabel(fieldName)}
                                <input type="text" value={editFieldValues[columnName] ?? ''} disabled />
                              </label>
                            );
                          }

                          if (columnName.toLowerCase() === 'fidid') {
                            return (
                              <label key={columnName}>
                                {getExcelAwareLabel(fieldName)}
                                <input type="text" value={editFieldValues[columnName] ?? ''} disabled />
                              </label>
                            );
                          }

                          if (columnName.toLowerCase() === 'streetname') {
                            return (
                              <label key={columnName}>
                                {getExcelAwareLabel(fieldName)}
                                <select
                                  value={editFieldValues.StreetId ?? ''}
                                  onChange={(e) => onEditChange('StreetId', e.target.value)}
                                  required
                                >
                                  <option value="">בחר רחוב</option>
                                  {streets.map((street) => (
                                    <option key={street.streetId} value={street.streetId}>
                                      {street.name}
                                    </option>
                                  ))}
                                </select>
                              </label>
                            );
                          }

                          const selectTableName = field.selectTableName;
                          const selectOptions =
                            selectTableName && selectTablesByName[selectTableName]
                              ? selectTablesByName[selectTableName]
                              : [];
                          const currentValue = editFieldValues[columnName] ?? '';
                          const isRehabStatusField = columnName.toLowerCase() === 'shikumstatus';

                          if (selectTableName && selectOptions.length > 0) {
                            return (
                              <label key={columnName}>
                                {getExcelAwareLabel(fieldName)}
                                <select
                                  value={currentValue}
                                  onChange={(e) => onEditChange(columnName, e.target.value)}
                                  required={required && (!isRehabStatusField || isEditRehabStatusRequired)}
                                  disabled={isRehabStatusField && !isEditRehabStatusRequired}
                                >
                                  <option value="">—</option>
                                  {selectOptions.map((opt) => (
                                    <option key={opt.value} value={opt.value}>
                                      {opt.label}
                                    </option>
                                  ))}
                                </select>
                              </label>
                            );
                          }

                          const isDate = isDateField(field);
                          const useTextarea = shouldUseTextarea(fieldName) && !isDate;
                          const inputType = isDate ? 'date' : 'text';

                          return (
                            <label key={columnName} className={useTextarea ? 'full-span' : ''}>
                              {getExcelAwareLabel(fieldName)}
                              {useTextarea ? (
                                <textarea
                                  value={currentValue}
                                  onChange={(e) => onEditChange(columnName, e.target.value)}
                                  required={required}
                                />
                              ) : (
                                <input
                                  type={inputType}
                                  value={currentValue}
                                  onChange={(e) => onEditChange(columnName, e.target.value)}
                                  required={required}
                                  lang={inputType === 'date' ? 'he-IL' : undefined}
                                />
                              )}
                            </label>
                          );
                        })}
                      </div>
                    )}
                  </div>
                );
              })}
              {/* actions are in modal footer */}
              <div style={{ height: 12 }} />
            </form>
          )}

          {mode === 'view' && (
            <>
              {!building && detailError && <p className="error">{detailError}</p>}
              {!building && !detailError && <p className="muted">טוען פרטי מבנה…</p>}
              {building ? (
                <div className="details-card">
                  <div>
                    <p className="eyebrow">פרטי מבנה</p>
                    <h3>
                      {building.street} {building.houseNumber}
                    </h3>
                  </div>

                  {orderedFieldGroups.length > 0 ? (
                    orderedFieldGroups.map(([category, fields]) => {
                      const isOpen = openViewCategories.has(category);
                      return (
                        <div key={category} className="details-section">
                          <div
                            className={`details-section__header${isOpen ? ' is-open' : ''}`}
                            role="button"
                            tabIndex={0}
                            aria-expanded={isOpen}
                            onClick={() => toggleViewCategory(category)}
                            onKeyDown={(event) => handleCategoryToggleKeyDown(event, () => toggleViewCategory(category))}
                          >
                            <h4>{category}</h4>
                            <span className="details-section__indicator" aria-hidden="true">
                              {isOpen ? '∨' : '∧'}
                            </span>
                          </div>
                          {isOpen && (
                            <dl>
                              {sortFieldsForDisplay(fields).map((field) => {
                                const titleParts = [];
                                if (field.selectTableName) titleParts.push(`טבלת בחירה: ${field.selectTableName}`);
                                let value;
                                if (isDateField(field)) {
                                  value = formatLogDate(field.value);
                                } else {
                                  value = formatStatusFieldValue(field);
                                }
                                return (
                                  <div key={`${field.columnName}-${field.fieldName}`}>
                                    <dt title={titleParts.join(' | ')}>{getExcelAwareLabel(field.fieldName)}</dt>
                                    <dd>{value}</dd>
                                  </div>
                                );
                              })}
                            </dl>
                          )}
                        </div>
                      );
                    })
                  ) : (
                    <p className="muted">אין שדות להצגה.</p>
                  )}

                  <div className="details-section">
                    <div className={`details-section__header`}>
                      <h4>נתונים ממערכות חיצוניות</h4>
                    </div>
                    {externalEntries.length === 0 && <p className="muted">אין נתונים.</p>}
                    {externalEntries.map((entry) => {
                      const payload = entry.snapshot?.payload;
                      const parsed = typeof payload === 'string' ? (() => { try { return JSON.parse(payload); } catch { return null; } })() : null;
                      const status = parsed?.status || null;
                      const notes = parsed?.notes || null;
                      const updatedAt = parsed?.updatedAt || null;
                      return (
                        <div key={entry.key} className="external-card">
                          <div className="external-card__header">
                            <strong>{entry.label}</strong>
                            <span className="muted small">{formatLogDate(entry.snapshot?.retrievedAt)}</span>
                          </div>
                          <dl>
                            <div>
                              <dt>סטטוס</dt>
                              <dd>{displayOrDash(status)}</dd>
                            </div>
                            <div>
                              <dt>עודכן במקור</dt>
                              <dd>{updatedAt ? formatLogDate(updatedAt) : '—'}</dd>
                            </div>
                            <div>
                              <dt>הערות</dt>
                              <dd>{displayOrDash(notes)}</dd>
                            </div>
                          </dl>
                        </div>
                      );
                    })}
                  </div>
                </div>
              ) : null}
            </>
          )}
        </div>

        <div className="modal-footer">
          <div className="footer-actions">
            {mode !== 'create' && (
              <button type="button" className="ghost" onClick={() => onExportCard(building)}>
                ייצוא כרטיס מבנה
              </button>
            )}
            {canEdit && mode === 'edit' && (
              <button type="button" className="danger" onClick={() => onDelete(building?.id)}>
                מחק
              </button>
            )}
            {canEdit && mode === 'edit' && (
              <button type="button" className="primary" onClick={onEditSubmit}>
                שמירת שינויים
              </button>
            )}
            {mode === 'create' && !duplicatePrompt && (
              <button type="button" className="primary" onClick={onCreateSubmit}>
                שמירה
              </button>
            )}
          </div>
          {mode === 'create' && duplicatePrompt && (
            <div className="duplicate-warning">
              <span>{duplicatePrompt}</span>
              <div className="duplicate-actions">
                <button type="button" className="primary" onClick={onDuplicateConfirm}>
                  המשך והוסף בכל זאת
                </button>
                <button type="button" className="ghost" onClick={onDuplicateCancel}>
                  ביטול
                </button>
              </div>
            </div>
          )}
          {actionMessage && <div className="modal-action-message">{actionMessage}</div>}
        </div>
      </div>
    </div>
  );
}
