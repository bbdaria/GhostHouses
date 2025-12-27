import { useEffect, useState } from 'react';
import { ROLE_LABELS } from '../i18n.js';
import { useAuth } from '../context/AuthContext.jsx';
import useDocumentTitle from '../hooks/useDocumentTitle.js';
import { applyTheme, getStoredTheme } from '../utils/theme.js';

export default function SettingsPage() {
  const { user } = useAuth();
  const [profileForm, setProfileForm] = useState({ email: user?.email || '' });
  const [profileMessage, setProfileMessage] = useState('');
  const [passwordForm, setPasswordForm] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  });
  const [passwordMessage, setPasswordMessage] = useState('');
  const [theme, setTheme] = useState(getStoredTheme());

  useDocumentTitle('הגדרות אישיות');

  useEffect(() => {
    setProfileForm((prev) => ({ ...prev, email: user?.email || '' }));
  }, [user]);

  const handleProfileSubmit = (event) => {
    event.preventDefault();
    setProfileMessage('שמירת כתובת הדוא"ל העצמית תתווסף בהמשך. לעת עתה זהו מסך הכנה.');
  };

  const handlePasswordSubmit = (event) => {
    event.preventDefault();
    if (passwordForm.newPassword !== passwordForm.confirmPassword) {
      setPasswordMessage('הסיסמאות אינן תואמות.');
      return;
    }
    setPasswordMessage('שינוי סיסמה עצמי יופעל כשחיבור ה-API יתעדכן.');
  };

  const handleThemeChange = (value) => {
    const next = applyTheme(value);
    setTheme(next);
  };

  return (
    <main className="app settings-app">
      <header className="page-header">
        <div>
          <p className="eyebrow">חשבון</p>
          <h1>הגדרות אישיות</h1>
          <p className="subtitle">ניהול פרטי משתמש, סיסמה והעדפות תצוגה.</p>
        </div>
      </header>

      <section className="panel">
        <h3>פרטי חשבון</h3>
        <form className="form-grid" onSubmit={handleProfileSubmit}>
          <label>
            שם משתמש
            <input value={user?.username || ''} disabled />
          </label>
          <label>
            דוא"ל
            <input
              name="email"
              value={profileForm.email}
              onChange={(e) => setProfileForm({ email: e.target.value })}
              type="email"
              placeholder="user@example.com"
            />
          </label>
          <div className="filters-actions full-span">
            <button type="submit" className="primary">
              שמירת פרטים
            </button>
          </div>
        </form>
        {profileMessage && <p className="muted">{profileMessage}</p>}
      </section>

      <section className="panel">
        <h3>שינוי סיסמה</h3>
        <form className="form-grid" onSubmit={handlePasswordSubmit}>
          <label>
            סיסמה נוכחית
            <input
              type="password"
              name="currentPassword"
              value={passwordForm.currentPassword}
              onChange={(e) =>
                setPasswordForm((prev) => ({ ...prev, currentPassword: e.target.value }))
              }
              placeholder="••••••••"
            />
          </label>
          <label>
            סיסמה חדשה
            <input
              type="password"
              name="newPassword"
              value={passwordForm.newPassword}
              onChange={(e) => setPasswordForm((prev) => ({ ...prev, newPassword: e.target.value }))}
              placeholder="••••••••"
            />
          </label>
          <label>
            אימות סיסמה
            <input
              type="password"
              name="confirmPassword"
              value={passwordForm.confirmPassword}
              onChange={(e) =>
                setPasswordForm((prev) => ({ ...prev, confirmPassword: e.target.value }))
              }
              placeholder="••••••••"
            />
          </label>
          <div className="filters-actions full-span">
            <button type="submit" className="primary">
              עדכון סיסמה
            </button>
          </div>
        </form>
        {passwordMessage && <p className="muted">{passwordMessage}</p>}
      </section>

      <section className="panel">
        <h3>תצוגה ונגישות</h3>
        <div className="theme-toggle">
          <label className={theme === 'dark' ? 'theme-option active' : 'theme-option'}>
            <input
              type="radio"
              name="theme"
              value="dark"
              checked={theme === 'dark'}
              onChange={() => handleThemeChange('dark')}
            />
            מצב כהה
          </label>
          <label className={theme === 'light' ? 'theme-option active' : 'theme-option'}>
            <input
              type="radio"
              name="theme"
              value="light"
              checked={theme === 'light'}
              onChange={() => handleThemeChange('light')}
            />
            מצב בהיר
          </label>
        </div>
        <p className="small muted">העדפת התצוגה נשמרת מקומית בדפדפן.</p>
      </section>
    </main>
  );
}
