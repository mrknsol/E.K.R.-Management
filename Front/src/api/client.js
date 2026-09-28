export function nextStatus(status) {
  if (status >= 5) return null
  return status + 1
}

/** Who may advance FROM the current status */
export function canAdvanceFromStatus(status, roles = []) {
  const next = nextStatus(status)
  if (next == null) return false

  if (roles.includes('SuperAdmin')) return true

  if (roles.includes('Admin')) {
    // Created → Accepted → SentToFactory, Ready → Shipped
    return status === 0 || status === 1 || status === 4
  }

  if (roles.includes('Factory')) {
    // SentToFactory → InProduction → Ready
    return status === 2 || status === 3
  }

  return false
}

const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:5263'

function getToken() {
  return localStorage.getItem('ekr_token')
}

async function request(path, options = {}) {
  const headers = { ...(options.headers || {}) }
  const token = getToken()

  if (token) {
    headers.Authorization = `Bearer ${token}`
  }

  if (options.body && !(options.body instanceof FormData)) {
    headers['Content-Type'] = 'application/json'
    options.body = JSON.stringify(options.body)
  }

  const res = await fetch(`${API_BASE}${path}`, { ...options, headers })

  if (res.status === 204) {
    return null
  }

  const data = await res.json().catch(() => ({}))

  if (!res.ok) {
    throw new Error(data.message || data.title || 'Request failed')
  }

  return data
}

export const api = {
  baseUrl: API_BASE,
  login: (email, password) =>
    request('/api/auth/login', { method: 'POST', body: { email, password } }),
  me: () => request('/api/auth/me'),
  getProducts: (q = '') =>
    request(`/api/products${q ? `?q=${encodeURIComponent(q)}` : ''}`),
  getProduct: (id) => request(`/api/products/${id}`),
  createProduct: (formData) =>
    request('/api/products', { method: 'POST', body: formData }),
  updateProduct: (id, formData) =>
    request(`/api/products/${id}`, { method: 'PUT', body: formData }),
  deleteProduct: (id) => request(`/api/products/${id}`, { method: 'DELETE' }),
  getOrders: (status) =>
    request(`/api/orders${status != null ? `?status=${status}` : ''}`),
  getOrder: (id) => request(`/api/orders/${id}`),
  createOrder: (payload) =>
    request('/api/orders', { method: 'POST', body: payload }),
  updateOrderStatus: (id, status, comment) =>
    request(`/api/orders/${id}/status`, {
      method: 'PATCH',
      body: { status, comment },
    }),
}
