import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './auth/AuthContext'
import { LanguageProvider } from './i18n/LanguageContext'
import { Layout } from './components/Layout'
import { ProtectedRoute } from './components/ProtectedRoute'
import { LoginPage } from './pages/LoginPage'
import { ProductsPage } from './pages/ProductsPage'
import { ProductFormPage } from './pages/ProductFormPage'
import { OrdersPage } from './pages/OrdersPage'
import { CreateOrderPage } from './pages/CreateOrderPage'
import './styles/app.css'

export default function App() {
  return (
    <LanguageProvider>
      <AuthProvider>
        <BrowserRouter>
          <Routes>
            <Route path="/login" element={<LoginPage />} />

            <Route element={<ProtectedRoute />}>
              <Route element={<Layout />}>
                <Route index element={<ProductsPage />} />
                <Route path="orders" element={<OrdersPage />} />
                <Route
                  element={<ProtectedRoute roles={['Admin', 'SuperAdmin']} />}
                >
                  <Route path="products/new" element={<ProductFormPage />} />
                  <Route path="products/:id/edit" element={<ProductFormPage />} />
                  <Route path="orders/new" element={<CreateOrderPage />} />
                </Route>
                <Route element={<ProtectedRoute roles={['SuperAdmin']} />}>
                  <Route path="monitor" element={<OrdersPage monitor />} />
                </Route>
              </Route>
            </Route>

            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </LanguageProvider>
  )
}
