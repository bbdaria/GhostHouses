import { Link } from 'react-router-dom';
import api from '../api/client.js';
import { useAuth } from '../context/AuthContext.jsx';
import { ROLE_LABELS } from '../i18n.js';
import useDocumentTitle from '../hooks/useDocumentTitle.js';
import { useState } from 'react';

const SYNC_LABELS = {
  GIS_IMPORT: 'GIS'
};

export default function DashboardPage() {
  const { user } = useAuth();
  const [syncMessage, setSyncMessage] = useState('');
  const roleLabel = ROLE_LABELS[user?.role] || user?.role;
  useDocumentTitle('לוח בקרה - מוקד המבנים העירוני');

  const runSync = async (jobType) => {
    try {
      const result = await api.runSync(jobType, { requestedBy: user?.username });
      const syncName = SYNC_LABELS[jobType] || jobType;
      setSyncMessage(`סנכרון ${syncName} הסתיים: ${result.result?.note || 'הושלם'}`);
    } catch (err) {
      setSyncMessage(`שגיאה בסנכרון: ${err.message}`);
    }
  };

  return (
    <main className="app dashboard-app">
      <header className="page-header">
        <div>
          <p className="eyebrow">מרכז שליטה עירוני</p>
          <h1>ברוך הבא, {user?.username}</h1>
          <p className="subtitle">בחרו פעולה להמשך.</p>
        </div>
        <div className="health-chip">תפקיד: {roleLabel}</div>
      </header>
      <section className="grid-cards">
        <Link to="/buildings" className="card-link">
          <h3>רשימת מבנים</h3>
          <p>חיפוש וניהול מבנים נטושים.</p>
        </Link>
        <Link to="/logs" className="card-link">
          <h3>יומן פעילויות</h3>
          <p>סקירת היסטוריית תיעוד ותקלות.</p>
        </Link>
        {(user?.role === 'Editor' || user?.role === 'Admin') && (
          <button className="card-link" onClick={() => runSync('GIS_IMPORT')}>
            <h3>סנכרון GIS</h3>
            <p>הרצת סנכרון דמה מול המערכות החיצוניות.</p>
          </button>
        )}
        {user?.role === 'Admin' && (
          <Link to="/users" className="card-link">
            <h3>ניהול משתמשים</h3>
            <p>ניהול הרשאות ויצירת משתמשים חדשים.</p>
          </Link>
        )}
      </section>
      {syncMessage && <p className="muted">{syncMessage}</p>}
    </main>
  );
}
