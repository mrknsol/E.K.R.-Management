import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api } from '../api/client'
import { resolveImageUrl } from '../api/media'
import { useLang } from '../i18n/LanguageContext'

const emptyVariant = () => ({ id: null, color: '', size: '', stockQuantity: 0 })

export function ProductFormPage() {
  const { id } = useParams()
  const isEdit = Boolean(id)
  const navigate = useNavigate()
  const { t } = useLang()

  const [code, setCode] = useState('')
  const [modelName, setModelName] = useState('')
  const [description, setDescription] = useState('')
  const [price, setPrice] = useState(0)
  const [season, setSeason] = useState('AW')
  const [modelType, setModelType] = useState('Jacket')
  const [piecesPerSeries, setPiecesPerSeries] = useState(4)
  const [minQuantity, setMinQuantity] = useState(4)
  const [isPublished, setIsPublished] = useState(true)
  const [colorMeta, setColorMeta] = useState('')
  const [variants, setVariants] = useState([emptyVariant()])
  const [image, setImage] = useState(null)
  const [existingImageUrl, setExistingImageUrl] = useState(null)
  const [previewUrl, setPreviewUrl] = useState(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    if (!isEdit) return
    api.getProduct(id).then((p) => {
      setCode(p.code || '')
      setModelName(p.modelName)
      setDescription(p.description || '')
      setPrice(p.price ?? 0)
      setSeason(p.season || 'AW')
      setModelType(p.modelType || 'Jacket')
      setPiecesPerSeries(p.piecesPerSeries ?? 4)
      setMinQuantity(p.minQuantity ?? 4)
      setIsPublished(p.isPublished !== false)
      setColorMeta((p.colorMeta || []).join('\n'))
      setExistingImageUrl(p.imageUrl || null)
      setVariants(p.variants.map((v) => ({ id: v.id, color: v.color, size: v.size, stockQuantity: v.stockQuantity })))
    }).catch((err) => setError(err.message))
  }, [id, isEdit])

  useEffect(() => {
    if (!image) { setPreviewUrl(null); return }
    const url = URL.createObjectURL(image)
    setPreviewUrl(url)
    return () => URL.revokeObjectURL(url)
  }, [image])

  const shownImage = useMemo(
    () => previewUrl || resolveImageUrl(existingImageUrl, api.baseUrl),
    [previewUrl, existingImageUrl],
  )

  function updateVariant(index, key, value) {
    setVariants((prev) => prev.map((v, i) => (i === index ? { ...v, [key]: value } : v)))
  }

  async function onSubmit(e) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      const metaLines = colorMeta.split('\n').map((s) => s.trim()).filter(Boolean)
      const payload = {
        code, modelName, description,
        price: Number(price), season, modelType,
        piecesPerSeries: Number(piecesPerSeries),
        minQuantity: Number(minQuantity),
        colorMeta: metaLines, isPublished,
        variants: variants.map((v) => ({ id: v.id || null, color: v.color, size: v.size, stockQuantity: Number(v.stockQuantity) })),
      }
      const formData = new FormData()
      formData.append('data', JSON.stringify(payload))
      if (image) formData.append('image', image)
      if (isEdit) await api.updateProduct(id, formData)
      else await api.createProduct(formData)
      navigate('/')
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
          <div className="page-kicker">01 / Product</div>
          <h1>{isEdit ? t('editProduct') : t('newProduct')}</h1>
          <p className="muted">{t('productFormSubtitle')}</p>
        </div>
        <Link className="btn ghost" to="/">{t('back')}</Link>
      </header>

      <form className="form-panel" onSubmit={onSubmit}>
        <div className="form-grid-2">
          <label>{t('productCode')}<input value={code} onChange={(e) => setCode(e.target.value)} placeholder="EKR-CLASSIC" required /></label>
          <label>{t('modelName')}<input value={modelName} onChange={(e) => setModelName(e.target.value)} required /></label>
        </div>
        <div className="form-grid-2">
          <label>{t('modelType')}<input value={modelType} onChange={(e) => setModelType(e.target.value)} required /></label>
          <label>{t('season')}<input value={season} onChange={(e) => setSeason(e.target.value)} required /></label>
        </div>
        <div className="form-grid-3">
          <label>{t('wholesalePrice')}<input type="number" min="0" value={price} onChange={(e) => setPrice(e.target.value)} required /></label>
          <label>{t('piecesPerSeries')}<input type="number" min="1" value={piecesPerSeries} onChange={(e) => setPiecesPerSeries(e.target.value)} required /></label>
          <label>{t('minQuantity')}<input type="number" min="1" value={minQuantity} onChange={(e) => setMinQuantity(e.target.value)} required /></label>
        </div>
        <label>{t('description')}<textarea value={description} onChange={(e) => setDescription(e.target.value)} rows={3} /></label>
        <label>
          {t('colorMeta')}
          <textarea value={colorMeta} onChange={(e) => setColorMeta(e.target.value)} rows={3} placeholder={t('colorMetaHint')} />
          <span className="muted">{t('colorMetaHint')}</span>
        </label>
        <label className="checkbox-row">
          <input type="checkbox" checked={isPublished} onChange={(e) => setIsPublished(e.target.checked)} />
          <span>{t('publishToWebsite')}</span>
        </label>
        <label>
          {t('photo')}
          {shownImage && (
            <div className="photo-preview">
              <img src={shownImage} alt={modelName || 'product'} />
              <span className="muted">{image ? t('newPhotoSelected') : t('currentPhoto')}</span>
            </div>
          )}
          <input type="file" accept="image/png,image/jpeg,image/webp" onChange={(e) => setImage(e.target.files?.[0] || null)} />
          {isEdit && !image && <span className="muted">{t('photoKeepHint')}</span>}
        </label>
        <div className="section-title">
          <h3>{t('variants')}</h3>
          <button type="button" className="btn ghost" onClick={() => setVariants((v) => [...v, emptyVariant()])}>{t('addVariant')}</button>
        </div>
        {variants.map((v, index) => (
          <div className="variant-form-row" key={index}>
            <input placeholder={t('color')} value={v.color} onChange={(e) => updateVariant(index, 'color', e.target.value)} required />
            <input type="text" placeholder={t('sizePlaceholder')} value={v.size} onChange={(e) => updateVariant(index, 'size', e.target.value)} maxLength={80} required />
            <input type="number" min="0" placeholder={t('stock')} value={v.stockQuantity} onChange={(e) => updateVariant(index, 'stockQuantity', e.target.value)} required />
            {variants.length > 1 && (
              <button type="button" className="btn danger" onClick={() => setVariants((prev) => prev.filter((_, i) => i !== index))}>×</button>
            )}
          </div>
        ))}
        {error && <div className="error">{error}</div>}
        <button className="btn primary" type="submit" disabled={loading}>{loading ? t('saving') : t('save')}</button>
      </form>
    </div>
  )
}
