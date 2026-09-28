# EKR Management

Базовое веб-приложение для склада и заказов курток.

## Структура

- `Back` — ASP.NET Core 8 Web API (JWT, Identity, EF Core + SQL Server)
- `Front` — React + Vite

## База данных

Подключение в `Back/appsettings.json` → `ConnectionStrings:DefaultConnection`  
(сейчас: `sql.bsite.net\MSSQL2016`, database `ekrmanage_`).

При старте API сам применяет миграции и создаёт демо-пользователей.

## Фото товаров (Cloudinary, бесплатно)

Фото грузятся в Cloudinary, в БД сохраняется **полная ссылка** (`https://res.cloudinary.com/...`).

1. Зарегистрируйся: https://cloudinary.com/users/register_free  
2. Dashboard → **API Keys** скопируй:
   - Cloud name  
   - API Key  
   - API Secret  
3. В `Back/appsettings.json`:

```json
"Cloudinary": {
  "CloudName": "твой_cloud_name",
  "ApiKey": "твой_api_key",
  "ApiSecret": "твой_api_secret",
  "Folder": "ekr/products"
}
```

4. Перезапусти API. Фото появятся в **Media Library** → папка `ekr/products`.

## Роли

| Email | Пароль | Роль |
|---|---|---|
| `superadmin@ekr.local` | `SuperAdmin123!` | Супер-админ (мониторинг) |
| `admin@ekr.local` | `Admin123!` | Админ склада/заказов |
| `factory@ekr.local` | `Factory123!` | Фабрика |

## Статусы заказа

1. Создан  
2. Принят  
3. Отправлен на фабрику  
4. В производстве  
5. Готов  
6. Отгружен

Сток списывается при статусе **Принят (Accepted)** — чтобы другие заказы не брали уже зарезервированное количество.

## Запуск

### Backend

```bash
cd Back
dotnet run --launch-profile http
```

API: http://localhost:5263  
Swagger: http://localhost:5263/swagger

### Frontend

```bash
cd Front
npm install
npm run dev
```

UI: http://localhost:5173
