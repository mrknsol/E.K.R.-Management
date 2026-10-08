import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import { resolveImageUrl } from '../api/media'
import { useAuth } from '../auth/AuthContext'
import { useLang } from '../i18n/LanguageContext'

export function ProductsPage() {
  const { hasRole } = useAuth()
  const { t } = useLang()
  const [query, setQuery] = useState('')
  const [products, setProducts] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  async function load(q = query) {
    setLoading(true)
    setError('')
    try {
      setProducts(await api.getProducts(q))
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    load('')
  }, [])

  async function onDelete(id) {
    if (!confirm(t('deleteConfirm'))) return
    try {
      await api.deleteProduct(id)
      await load()
    } catch (err) {
      alert(err.message)
    }
  }

  return (
    <div className="page">
      <header className="page-header">
        <div>
          <div className="page-kicker">01 / Stock</div>
          <h1>{t('inventoryTitle')}</h1>
          <p className="muted">{t('inventorySubtitle')}</p>
        </div>
        {hasRole('Admin', 'SuperAdmin') && (
          <Link className="btn primary" to="/products/new">
            {t('addProduct')}
          </Link>
        )}
      </header>

      <form
        className="search-bar"
        onSubmit={(e) => {
          e.preventDefault()
          load(query)
        }}
      >
        <input
          placeholder={t('searchPlaceholder')}
          value={query}
          onChange={(e) => setQuery(e.target.value)}
        />
        <button className="btn" type="submit">
          {t('search')}
        </button>
      </form>

      {error && <div className="error">{error}</div>}
      {loading ? (
        <div className="loader" aria-label={t('loading')} />
      ) : (
        <div className="product-rack">
          {products.map((p) => (
            <article key={p.id} className="product-row">
              <div className="product-image">
                {p.imageUrl ? (
                  <img
                    src={resolveImageUrl(p.imageUrl, api.baseUrl)}
                    alt={p.modelName}
                  />
                ) : (
                  <div className="image-placeholder">{t('noPhoto')}</div>
                )}
              </div>
              <div className="product-body">
                <h3>{p.modelName}<span className="muted" style={{ marginLeft: 8, fontWeight: 400 }}>{p.code}</span></h3>
                <p className="muted">{p.description || t('noDescription')}</p>
                <div className="stock-total">
                  {t('totalStock')} · {p.totalStock} {t('pcs')}
                  {p.isPublished ? (
                    <span className="source-pill website" style={{ marginLeft: 10 }}>{t('onWebsite')}</span>
                  ) : (
                    <span className="source-pill manual" style={{ marginLeft: 10 }}>{t('warehouseOnly')}</span>
                  )}
                </div>
                <div className="variant-list">
                  {p.variants.map((v) => (
                    <div key={v.id} className="variant-row">
                      <span>
                        {v.color} / {v.size}
                      </span>
                      <strong>
                        {v.stockQuantity} {t('pcs')}
                      </strong>
                    </div>
                  ))}
                </div>
              </div>
              {hasRole('Admin', 'SuperAdmin') && (
                <div className="product-actions">
                  <Link className="btn ghost" to={`/products/${p.id}/edit`}>
                    {t('edit')}
                  </Link>
                  <button
                    type="button"
                    className="btn danger"
                    onClick={() => onDelete(p.id)}
                  >
                    {t('delete')}
                  </button>
                </div>
              )}
            </article>
          ))}
          {!products.length && (
            <div className="empty-state">{t('noProducts')}</div>
          )}
        </div>
      )}
    </div>
  )
}
