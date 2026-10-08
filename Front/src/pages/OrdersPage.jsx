import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, nextStatus, canAdvanceFromStatus } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { useLang } from '../i18n/LanguageContext'

export function OrdersPage({ monitor = false }) {
  const { hasRole, user } = useAuth()
  const { t, statusLabel, lang } = useLang()
  const [orders, setOrders] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [statusFilter, setStatusFilter] = useState('')

  const roles = user?.roles || []

  async function load() {
    setLoading(true)
    setError('')
    try {
      const status = statusFilter === '' ? undefined : Number(statusFilter)
      setOrders(await api.getOrders(status))
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    load()
  }, [statusFilter])

  async function advance(order) {
    const next = nextStatus(order.status)
    if (next == null) return
    if (!canAdvanceFromStatus(order.status, roles)) return

    const comment = prompt(t('statusComment')) || ''
    try {
      await api.updateOrderStatus(order.id, next, comment)
      await load()
    } catch (err) {
      alert(err.message)
    }
  }
  return (
    <div className="page">
      <header className="page-header">
        <div>
          <div className="page-kicker">{monitor ? '03 / Monitor' : '02 / Pipeline'}</div>
          <h1>{monitor ? t('monitorTitle') : t('ordersTitle')}</h1>
          <p className="muted">
            {monitor ? t('monitorSubtitle') : t('ordersSubtitle')}
          </p>
        </div>
        {!monitor && hasRole('Admin', 'SuperAdmin') && (
          <Link className="btn primary" to="/orders/new">
            {t('newOrder')}
          </Link>
        )}
      </header>

      <div className="search-bar">
        <select
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value)}
        >
          <option value="">{t('allStatuses')}</option>
          {[0, 1, 2, 3, 4, 5].map((s) => (
            <option key={s} value={s}>
              {statusLabel(s)}
            </option>
          ))}
        </select>
        <button className="btn" type="button" onClick={load}>
          {t('refresh')}
        </button>
      </div>

      {error && <div className="error">{error}</div>}
      {loading ? (
        <div className="loader" aria-label={t('loading')} />
      ) : (
        <div className="orders-list">
          {orders.map((order, index) => (
            <article
              key={order.id}
              className="order-card"
              style={{ animationDelay: `${index * 0.05}s` }}
            >
              <div className="order-top">
                <div>
                  <h3>{order.orderNumber}</h3>
                  <p className="muted">
                    {order.customerName} · {t('createdBy')} {order.createdBy}
                  </p>
                  <div className="order-tags">
                    <span className={`source-pill ${order.source === 1 ? 'website' : 'manual'}`}>
                      {order.source === 1 ? t('sourceWebsite') : t('sourceManual')}
                    </span>
                  </div>
                </div>
                <span className={`stamp status-${order.status}`}>
                  {statusLabel(order.status)}
                </span>
              </div>

              <div className="order-items">
                {order.items.map((item) => (
                  <div key={item.id} className="variant-row">
                    <span>
                      {item.modelName} · {item.color} / {item.size}
                    </span>
                    <strong>
                      {item.quantity} {t('pcs')}
                    </strong>
                  </div>
                ))}
              </div>

              {order.notes && <p className="notes">{order.notes}</p>}

              <div className="history">
                {order.history?.map((h, idx) => (
                  <div key={idx} className="history-row">
                    {statusLabel(h.toStatus)} · {h.changedBy} ·{' '}
                    {new Date(h.changedAt).toLocaleString(
                      lang === 'ru' ? 'ru-RU' : 'en-US',
                    )}
                    {h.comment ? ` — ${h.comment}` : ''}
                  </div>
                ))}
              </div>

              {canAdvanceFromStatus(order.status, roles) && (
                <div style={{ marginTop: 14 }}>
                  <button
                    type="button"
                    className="btn primary"
                    onClick={() => advance(order)}
                  >
                    {t('nextStatus')} → {statusLabel(nextStatus(order.status))}
                  </button>
                </div>
              )}
            </article>
          ))}
          {!orders.length && <div className="empty-state">{t('noOrders')}</div>}
        </div>
      )}
    </div>
  )
}
