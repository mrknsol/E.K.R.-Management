import { useState } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { useLang } from '../i18n/LanguageContext'
import { LanguageSwitcher } from '../components/LanguageSwitcher'

export function LoginPage() {
  const { login, isAuthenticated } = useAuth()
  const { t } = useLang()
  const [email, setEmail] = useState('admin@ekr.local')
  const [password, setPassword] = useState('Admin123!')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  if (isAuthenticated) {
    return <Navigate to="/" replace />
  }

  async function onSubmit(e) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      await login(email, password)
    } catch (err) {
      setError(err.message || t('loginError'))
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="login-page">
      <section className="login-hero">
        <div className="hero-badge">Control deck // 2026</div>
        <div>
          <div className="brand-mark">{t('brand')}</div>
          <h2>{t('loginTitle')}</h2>
          <p>{t('loginSubtitle')}</p>
        </div>
        <div className="brand-sub">Inventory · Factory · Ship</div>
      </section>

      <section className="login-side">
        <form className="login-form" onSubmit={onSubmit}>
          <div className="login-form-top">
            <div>
              <div className="page-kicker">{t('brandSub')}</div>
              <h1>{t('signIn')}</h1>
            </div>
            <LanguageSwitcher />
          </div>

          <label>
            {t('email')}
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </label>

          <label>
            {t('password')}
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
          </label>

          {error && <div className="error">{error}</div>}

          <button className="btn primary" type="submit" disabled={loading}>
            {loading ? t('signingIn') : t('signIn')}
          </button>

          <div className="demo-users">
            <p>{t('demoAccounts')}</p>
            <code>superadmin@ekr.local / SuperAdmin123!</code>
            <code>admin@ekr.local / Admin123!</code>
            <code>factory@ekr.local / Factory123!</code>
          </div>
        </form>
      </section>
    </div>
  )
}
