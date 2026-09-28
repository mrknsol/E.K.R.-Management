import { useLang } from '../i18n/LanguageContext'

export function LanguageSwitcher() {
  const { lang, setLang, t } = useLang()

  return (
    <div className="lang-switch" role="group" aria-label={t('language')}>
      <button
        type="button"
        className={lang === 'en' ? 'active' : ''}
        onClick={() => setLang('en')}
      >
        EN
      </button>
      <button
        type="button"
        className={lang === 'ru' ? 'active' : ''}
        onClick={() => setLang('ru')}
      >
        RU
      </button>
    </div>
  )
}
