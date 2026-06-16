import { useState } from 'react';
import api from '../api/client.js';
import useDocumentTitle from '../hooks/useDocumentTitle.js';

export default function TemplateConverterPage() {
  useDocumentTitle('ממיר תבניות - מוקד המבנים העירוני');
  const [buildingsFile, setBuildingsFile] = useState(null);
  const [streetsFile, setStreetsFile] = useState(null);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const triggerFileInput = (id) => {
    const input = document.getElementById(id);
    if (input && !input.disabled) {
      input.click();
    }
  };

  const handleConvert = async (type) => {
    setError('');
    setMessage('');
    setBusy(true);
    try {
      const file = type === 'buildings' ? buildingsFile : streetsFile;
      if (!file) {
        throw new Error('נא לבחור קובץ להמרה.');
      }

      const blob =
        type === 'buildings'
          ? await api.convertBuildingsTemplate(file)
          : await api.convertStreetsTemplate(file);

      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      const prefix = type === 'buildings' ? 'buildings-template' : 'streets-template';
      link.download = `${prefix}-${new Date().toISOString().slice(0, 10)}.xlsx`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
      setMessage('ההמרה הושלמה והקובץ הורד.');
    } catch (err) {
      setError(err.message || 'שגיאה בהמרת הקובץ.');
    } finally {
      setBusy(false);
    }
  };

  return (
    <main className="app settings-app">
      <header className="page-header">
        <div>
          <h1>ממיר תבניות</h1>
          <p className="subtitle">
            המירו את תבניות הלקוח לתבניות הייבוא של המערכת. הכלי זמני וניתן להסרה לאחר ההטמעה.
          </p>
        </div>
      </header>

      <section className="panel full-span">
        <div className="panel-header">
          <h3>המרת תבנית מבנים</h3>
        </div>
        <div className="form-grid">
          <label>
            קובץ תבנית מבנים (לקוח)
            <div className="file-input">
              <input
                id="converter-buildings-file"
                type="file"
                accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                onChange={(event) => setBuildingsFile(event.target.files?.[0] ?? null)}
                disabled={busy}
              />
              <button
                type="button"
                className="ghost file-input__button"
                onClick={() => triggerFileInput('converter-buildings-file')}
                disabled={busy}
              >
                בחר קובץ
              </button>
              <span className={`file-input__name ${buildingsFile ? '' : 'muted'}`}>
                {buildingsFile ? buildingsFile.name : 'לא נבחר קובץ'}
              </span>
            </div>
          </label>
          <div className="filters-actions full-span align-right">
            <button
              type="button"
              className="primary"
              onClick={() => handleConvert('buildings')}
              disabled={busy || !buildingsFile}
            >
              המרה והורדה
            </button>
          </div>
        </div>
      </section>

      <section className="panel full-span">
        <div className="panel-header">
          <h3>המרת תבנית רחובות</h3>
        </div>
        <div className="form-grid">
          <label>
            קובץ תבנית רחובות (לקוח)
            <div className="file-input">
              <input
                id="converter-streets-file"
                type="file"
                accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                onChange={(event) => setStreetsFile(event.target.files?.[0] ?? null)}
                disabled={busy}
              />
              <button
                type="button"
                className="ghost file-input__button"
                onClick={() => triggerFileInput('converter-streets-file')}
                disabled={busy}
              >
                בחר קובץ
              </button>
              <span className={`file-input__name ${streetsFile ? '' : 'muted'}`}>
                {streetsFile ? streetsFile.name : 'לא נבחר קובץ'}
              </span>
            </div>
          </label>
          <div className="filters-actions full-span align-right">
            <button
              type="button"
              className="primary"
              onClick={() => handleConvert('streets')}
              disabled={busy || !streetsFile}
            >
              המרה והורדה
            </button>
          </div>
        </div>
      </section>

      {message && <p className="success">{message}</p>}
      {error && <p className="error">{error}</p>}
    </main>
  );
}
