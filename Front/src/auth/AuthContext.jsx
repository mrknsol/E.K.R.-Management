import { createContext, useContext, useEffect, useMemo, useState } from 'react'
import { api } from '../api/client'

const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const token = localStorage.getItem('ekr_token')
    if (!token) {
      setLoading(false)
      return
    }

    const raw = localStorage.getItem('ekr_user')
    if (raw) {
      try {
        setUser(JSON.parse(raw))
      } catch {
        localStorage.removeItem('ekr_user')
      }
    }
    setLoading(false)
  }, [])

  async function login(email, password) {
    const data = await api.login(email, password)
    const nextUser = {
      email: data.email,
      fullName: data.fullName,
      roles: data.roles || [],
    }
    localStorage.setItem('ekr_token', data.token)
    localStorage.setItem('ekr_user', JSON.stringify(nextUser))
    setUser(nextUser)
    return nextUser
  }

  function logout() {
    localStorage.removeItem('ekr_token')
    localStorage.removeItem('ekr_user')
    setUser(null)
  }

  function hasRole(...roles) {
    if (!user?.roles) return false
    return roles.some((r) => user.roles.includes(r))
  }

  const value = useMemo(
    () => ({ user, loading, login, logout, hasRole, isAuthenticated: !!user }),
    [user, loading],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider')
  return ctx
}
