import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import { useLang } from '../i18n/LanguageContext'

export function CreateOrderPage() {
  const navigate = useNavigate()
  const { t } = useLang()
  const [products, setProducts] = useState([])
  const [customerName, setCustomerName] = useState('')
  const [notes, setNotes] = useState('')
  const [items, setItems] = useState([{ productVariantId: '', quantity: 1 }])
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    api
      .getProducts()
      .then(setProducts)
      .catch((err) => setError(err.message))
  }, [])

  const variants = useMemo(
    () =>
      products.flatMap((p) =>
        p.variants.map((v) => ({
          id: v.id,
          label: `${p.modelName} · ${v.color} / ${v.size} (${t('stockLabel')} ${v.stockQuantity})`,
          stock: v.stockQuantity,
        })),
      ),
    [products, t],
  )

  function updateItem(index, key, value) {
    setItems((prev) =>
      prev.map((item, i) => (i === index ? { ...item, [key]: value } : item)),
    )
  }

  async function onSubmit(e) {
    e.preventDefault()
    setError('')
    setLoading(true)

    try {
      await api.createOrder({
        customerName,
        notes,
        items: items.map((i) => ({
          productVariantId: i.productVariantId,
          quantity: Number(i.quantity),
        })),
      })
      navigate('/orders')
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="page narrow">
      <header className="page-header">
        <div>
          <div className="page-kicker">02 / Order</div>
          <h1>{t('newOrderTitle')}</h1>
          <p className="muted">{t('newOrderSubtitle')}</p>
        </div>
        <Link className="btn ghost" to="/orders">
          {t('back')}
        </Link>
      </header>

      <form className="form-panel" onSubmit={onSubmit}>
        <label>
          {t('customerSeries')}
          <input
            value={customerName}
            onChange={(e) => setCustomerName(e.target.value)}
            placeholder={t('customerPlaceholder')}
            required
          />
        </label>

        <label>
          {t('notes')}
          <textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            rows={3}
          />
        </label>

        <div className="section-title">
          <h3>{t('lineItems')}</h3>
          <button
            type="button"
            className="btn ghost"
            onClick={() =>
              setItems((prev) => [
                ...prev,
                { productVariantId: '', quantity: 1 },
              ])
            }
          >
            {t('addItem')}
          </button>
        </div>

        {items.map((item, index) => (
          <div className="variant-form-row" key={index}>
            <select
              value={item.productVariantId}
              onChange={(e) =>
                updateItem(index, 'productVariantId', e.target.value)
              }
              required
            >
              <option value="">{t('selectVariant')}</option>
              {variants.map((v) => (
                <option key={v.id} value={v.id}>
                  {v.label}
                </option>
              ))}
            </select>
            <input
              type="number"
              min="1"
              value={item.quantity}
              onChange={(e) => updateItem(index, 'quantity', e.target.value)}
              required
            />
            {items.length > 1 && (
              <button
                type="button"
                className="btn danger"
                onClick={() =>
                  setItems((prev) => prev.filter((_, i) => i !== index))
                }
              >
                ×
              </button>
            )}
          </div>
        ))}

        {error && <div className="error">{error}</div>}

        <button className="btn primary" type="submit" disabled={loading}>
          {loading ? t('creating') : t('create')}
        </button>
      </form>
    </div>
  )
}
