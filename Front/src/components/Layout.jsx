import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { useLang } from '../i18n/LanguageContext'
import { LanguageSwitcher } from './LanguageSwitcher'

export function Layout() {
  const { user, logout, hasRole } = useAuth()
  const { t, lang } = useLang()

  const tickerItems =
    lang === 'ru'
      ? [
          'СКЛАД · ЗАКАЗЫ · ФАБРИКА',
          'АВТОСПИСАНИЕ ПРИ ПРИНЯТИИ',
          'E.K.R. CONTROL DECK',
          'LIVE STATUS PIPELINE',
        ]
      : [
          'INVENTORY · ORDERS · FACTORY',
          'STOCK RESERVED ON ACCEPT',
          'E.K.R. CONTROL DECK',
          'LIVE STATUS PIPELINE',
        ]

  return (
    <div className="app-shell">
      <header className="topbar">
        <div className="brand-lockup">
          <div className="brand-mark">{t('brand')}</div>
          <div className="brand-sub">{t('brandSub')}</div>
        </div>

        <nav className="nav">
          <NavLink to="/" end>
            {t('inventory')}
          </NavLink>
          <NavLink to="/orders">{t('orders')}</NavLink>
          {hasRole('Admin', 'SuperAdmin') && (
            <NavLink to="/products/new">{t('addProduct')}</NavLink>
          )}
          {hasRole('Admin', 'SuperAdmin') && (
            <NavLink to="/orders/new">{t('newOrder')}</NavLink>
          )}
          {hasRole('SuperAdmin') && (
            <NavLink to="/monitor">{t('monitor')}</NavLink>
          )}
        </nav>

        <div className="topbar-right">
          <LanguageSwitcher />
          <div className="user-chip">
            <strong>{user?.fullName}</strong>
            <span>{user?.roles?.join(' · ')}</span>
          </div>
          <button type="button" className="btn ghost" onClick={logout}>
            {t('logout')}
          </button>
        </div>
      </header>

      <div className="ticker" aria-hidden="true">
        <div className="ticker-track">
          {[...tickerItems, ...tickerItems].map((item, i) => (
            <span key={i}>{item}</span>
          ))}
        </div>
      </div>

      <main className="content">
        <Outlet />
      </main>
    </div>
  )
}
