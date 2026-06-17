import { useState } from 'react';
import { Navigate, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext.jsx';
import useDocumentTitle from '../hooks/useDocumentTitle.js';

export default function OtpPage() {
  const { otpChallenge, verifyOtp, token, user } = useAuth();
  const navigate = useNavigate();
  const [otp, setOtp] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  useDocumentTitle('אימות דו-שלבי - מוקד המבנים העירוני');

  if (token && user) {
    return <Navigate to="/" replace />;
  }

  if (!otpChallenge) {
    return <Navigate to="/login" replace />;
  }

  const handleSubmit = async (event) => {
    event.preventDefault();
    setError('');
    setLoading(true);
    try {
      await verifyOtp(otp);
      navigate('/');
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <main className="auth-page">
      <form className="auth-card" onSubmit={handleSubmit}>
        <h1>הזנת קוד OTP</h1>
        <p className="subtitle">הקלידו את הקוד החד-פעמי שקיבלתם (לקוח כעת מהדמו).</p>
        <label>
          קוד OTP
          <input
            value={otp}
            onChange={(event) => setOtp(event.target.value)}
            maxLength={6}
            required
          />
        </label>
        <button type="submit" className="primary" disabled={loading}>
          {loading ? 'מאמת…' : 'אימות'}
        </button>
        {error && <p className="error">{error}</p>}
        <p className="muted">
          קוד דמה: <strong>{otpChallenge.demoCode}</strong>
        </p>
      </form>
    </main>
  );
}
