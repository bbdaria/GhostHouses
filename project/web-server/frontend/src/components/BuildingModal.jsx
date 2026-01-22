import React, { useRef } from 'react';

export default function BuildingModal({
  visible,
  mode,
  building,
  createFieldValues,
  createPhotoValue,
  createFieldGroups,
  createTemplateLoading,
  createSelectTablesLoading,
  editFieldValues,
  editPhotoValue,
  streets,
  selectTablesByName,
  selectTablesLoading,
  orderedFieldGroups,
  externalEntries,
  isRehabStatusRequired,
  isEditRehabStatusRequired,
  isRequiredCreateColumn,
  canEdit,
  actionMessage,
  duplicatePrompt,
  editDuplicatePrompt,
  onCreateFieldChange,
  onCreateSubmit,
  onDuplicateConfirm,
  onDuplicateCancel,
  onEditChange,
  onEditSubmit,
  onEditDuplicateConfirm,
  onEditDuplicateCancel,
  onOpenEdit,
  onOpenLogs,
  onDelete,
  onExportCard,
  onClose,
  onPhotoUpload,
  onPhotoDelete,
  photoLoading,
  photoError,
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
  openCreateCategories,
  toggleCreateCategory,
  handleCategoryToggleKeyDown
}) {
  if (!visible) return null;

  const loadingBuilding = (mode !== 'create' && !building);
  const fileInputRef = useRef(null);
  const resolvedPhotoValue =
    mode === 'create'
      ? createPhotoValue || ''
      : mode === 'edit'
        ? editPhotoValue || ''
        : building?.photos?.[0] || '';
  const photoValue = resolvedPhotoValue;
  const hasPhoto = Boolean(photoValue);
  const photoSrc = photoValue
    ? photoValue.startsWith('data:') || photoValue.startsWith('http')
      ? photoValue
      : `data:image/jpeg;base64,${photoValue}`
    : '';

  const stop = (e) => e.stopPropagation();
  const triggerPhotoSelect = () => fileInputRef.current?.click();
  const handlePhotoChange = (event) => {
    const file = event.target.files?.[0];
    if (!file || !onPhotoUpload) return;
    onPhotoUpload(file);
    event.target.value = '';
  };
  const renderLabel = (text, required = false) => (
    <span className="label-title">
      {text}
      {required && (
        <span className="required-marker" aria-hidden="true">
          *
        </span>
      )}
    </span>
  );

  const renderPhotoEditor = (allowEdit) => (
    <div className="full-span photo-editor">
      <div className="photo-editor__label">תמונה</div>
      {hasPhoto ? (
        <>
          <img className="photo-preview" src={photoSrc} alt="תמונת מבנה" />
          {allowEdit && (
            <button type="button" className="danger" onClick={onPhotoDelete} disabled={photoLoading}>
              מחיקת תמונה
            </button>
          )}
        </>
      ) : (
        <>
          <div className="photos-placeholder">אין תמונה</div>
          {allowEdit && (
            <>
              <input
                ref={fileInputRef}
                type="file"
                accept="image/*"
                className="photo-input"
                onChange={handlePhotoChange}
              />
              <button type="button" className="ghost" onClick={triggerPhotoSelect} disabled={photoLoading}>
                העלאת תמונה
              </button>
            </>
          )}
        </>
      )}
      {photoError && <p className="error">{photoError}</p>}
    </div>
  );

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
            <form className="details-card">
              {createTemplateLoading && <p className="muted">טוען שדות…</p>}
              {createSelectTablesLoading && <p className="muted">טוען טבלאות בחירה…</p>}
              {createFieldGroups.length === 0 && !createTemplateLoading && (
                <p className="muted">אין שדות זמינים.</p>
              )}
              {createFieldGroups.map(([category, fields]) => {
                const isOpen = openCreateCategories.has(category);
                return (
                  <div key={category} className="details-section">
                    <div
                      className={`details-section__header${isOpen ? ' is-open' : ''}`}
                      role="button"
                      tabIndex={0}
                      aria-expanded={isOpen}
                      onClick={() => toggleCreateCategory(category)}
                      onKeyDown={(event) =>
                        handleCategoryToggleKeyDown(event, () => toggleCreateCategory(category))
                      }
                    >
                      <h4>{category}</h4>
                      <span className="details-section__indicator" aria-hidden="true">
                        {isOpen ? '∨' : '∧'}
                      </span>
                    </div>
                    {isOpen && (
                      <div className="form-grid">
                        {category === 'מידע כללי' && renderPhotoEditor(true)}
                        {sortFieldsForDisplay(fields).map((field) => {
                          const columnName = field.columnName;
                          const fieldName = field.fieldName;
                          if (!columnName) return null;
                          const required = isRequiredCreateColumn(columnName);

                          if (columnName.toLowerCase() === 'streetid') {
                            return (
                              <label key={columnName}>
                                {renderLabel(getExcelAwareLabel(fieldName), false)}
                                <input type="text" value={createFieldValues.StreetId ?? ''} disabled />
                              </label>
                            );
                          }

                          if (columnName.toLowerCase() === 'streetname') {
                            return (
                              <label key={columnName}>
                                {renderLabel(getExcelAwareLabel(fieldName), true)}
                                <select
                                  value={createFieldValues.StreetId ?? ''}
                                  onChange={(e) => onCreateFieldChange('StreetId', e.target.value)}
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
                            );
                          }

                          const selectTableName = field.selectTableName;
                          const selectOptions =
                            selectTableName && selectTablesByName[selectTableName]
                              ? selectTablesByName[selectTableName]
                              : [];
                          const currentValue = createFieldValues[columnName] ?? '';
                          const isRehabStatusField = columnName.toLowerCase() === 'shikumstatus';

                          if (selectTableName && selectOptions.length > 0) {
                            return (
                              <label key={columnName}>
                                {renderLabel(
                                  getExcelAwareLabel(fieldName),
                                  required && (!isRehabStatusField || isRehabStatusRequired)
                                )}
                                <select
                                  value={currentValue}
                                  onChange={(e) => onCreateFieldChange(columnName, e.target.value)}
                                  required={required && (!isRehabStatusField || isRehabStatusRequired)}
                                  disabled={isRehabStatusField && !isRehabStatusRequired}
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
                          const isIdField = columnName.toLowerCase() === 'id';
                          const inputType = isDate ? 'date' : isIdField ? 'number' : 'text';

                          return (
                            <label key={columnName} className={useTextarea ? 'full-span' : ''}>
                              {renderLabel(getExcelAwareLabel(fieldName), required)}
                              {useTextarea ? (
                                <textarea
                                  value={currentValue}
                                  onChange={(e) => onCreateFieldChange(columnName, e.target.value)}
                                  required={required}
                                />
                              ) : (
                                <input
                                  type={inputType}
                                  value={currentValue}
                                  onChange={(e) => onCreateFieldChange(columnName, e.target.value)}
                                  required={required}
                                  min={isIdField ? 1 : undefined}
                                  step={isIdField ? 1 : undefined}
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
                        {category === 'מידע כללי' && renderPhotoEditor(canEdit)}
                        {sortFieldsForDisplay(fields).map((field) => {
                          const columnName = field.columnName;
                          const fieldName = field.fieldName;
                          if (!columnName) return null;
                          const required = isRequiredEditColumn(columnName);
                          if (columnName.toLowerCase() === 'streetid') {
                            return (
                              <label key={columnName}>
                                {renderLabel(getExcelAwareLabel(fieldName), false)}
                                <input type="text" value={editFieldValues[columnName] ?? ''} disabled />
                              </label>
                            );
                          }

                          if (columnName.toLowerCase() === 'streetname') {
                            return (
                              <label key={columnName}>
                                {renderLabel(getExcelAwareLabel(fieldName), true)}
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
                                {renderLabel(
                                  getExcelAwareLabel(fieldName),
                                  required && (!isRehabStatusField || isEditRehabStatusRequired)
                                )}
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
                          const isIdField = columnName.toLowerCase() === 'id';
                          const inputType = isDate ? 'date' : isIdField ? 'number' : 'text';

                          return (
                            <label key={columnName} className={useTextarea ? 'full-span' : ''}>
                              {renderLabel(getExcelAwareLabel(fieldName), required)}
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
                                  min={isIdField ? 1 : undefined}
                                  step={isIdField ? 1 : undefined}
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
                              {category === 'מידע כללי' && (
                                <div className="photo-field">
                                  <dt>תמונה</dt>
                                  <dd>
                                    {hasPhoto ? (
                                      <img className="photo-preview" src={photoSrc} alt="תמונת מבנה" />
                                    ) : (
                                      <span className="muted">אין תמונה</span>
                                    )}
                                  </dd>
                                </div>
                              )}
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
            {mode === 'view' && (
              <button type="button" className="ghost" onClick={onOpenLogs}>
                יומן
              </button>
            )}
            {canEdit && mode === 'view' && (
              <button type="button" className="ghost" onClick={onOpenEdit}>
                עריכה
              </button>
            )}
            {canEdit && mode === 'view' && (
              <button type="button" className="danger" onClick={() => onDelete(building?.id)}>
                מחק
              </button>
            )}
            {canEdit && mode === 'edit' && (
              <button type="button" className="danger" onClick={() => onDelete(building?.id)}>
                מחק
              </button>
            )}
            {canEdit && mode === 'edit' && !editDuplicatePrompt && (
              <button type="button" className="primary" onClick={onEditSubmit}>
                שמירת שינויים
              </button>
            )}
            {mode === 'create' && !duplicatePrompt && (
              <button type="button" className="primary" onClick={onCreateSubmit}>
                שמירה
              </button>
            )}
            {(mode === 'create' || mode === 'edit') && (
              <button type="button" className="ghost" onClick={onClose}>
                סגירה
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
          {mode === 'edit' && editDuplicatePrompt && (
            <div className="duplicate-warning">
              <span>{editDuplicatePrompt}</span>
              <div className="duplicate-actions">
                <button type="button" className="primary" onClick={onEditDuplicateConfirm}>
                  המשך והוסף בכל זאת
                </button>
                <button type="button" className="ghost" onClick={onEditDuplicateCancel}>
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
