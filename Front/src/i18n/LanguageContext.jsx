import { createContext, useContext, useEffect, useMemo, useState } from 'react'
import { translations } from './translations'

const LangContext = createContext(null)

export function LanguageProvider({ children }) {
  const [lang, setLang] = useState(() => localStorage.getItem('ekr_lang') || 'en')

  useEffect(() => {
    localStorage.setItem('ekr_lang', lang)
    document.documentElement.lang = lang
  }, [lang])

  const value = useMemo(() => {
    const dict = translations[lang] || translations.en
    return {
      lang,
      setLang,
      t: (key) => dict[key] ?? translations.en[key] ?? key,
      statusLabel: (status) => dict[`status_${status}`] ?? String(status),
    }
  }, [lang])

  return <LangContext.Provider value={value}>{children}</LangContext.Provider>
}

export function useLang() {
  const ctx = useContext(LangContext)
  if (!ctx) throw new Error('useLang must be used inside LanguageProvider')
  return ctx
}
