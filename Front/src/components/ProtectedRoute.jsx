import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { useLang } from '../i18n/LanguageContext'

export function ProtectedRoute({ roles }) {
  const { isAuthenticated, loading, hasRole } = useAuth()
  const { t } = useLang()

  if (loading) {
    return (
      <div className="page-center">
        <div className="loader" aria-label={t('loading')} />
      </div>
    )
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  if (roles?.length && !hasRole(...roles)) {
    return <Navigate to="/" replace />
  }

  return <Outlet />
}
