# E.K.R. Management

Внутренний склад и заказы на фабрику. **Общая PostgreSQL-база `EKR_DB` с публичным сайтом** (схема `mgmt`).

## Архитектура

| Что | Где |
|-----|-----|
| Каталог курток + сток | `mgmt.Products` / `mgmt.ProductVariants` |
| Заказы на фабрику (ручные + с сайта) | `mgmt.Orders` |
| Пользователи Management | `mgmt.AspNetUsers` |
| Сайт (клиенты, оплата) | схема `public` (Company Project) |

Добавил куртку в Management с галочкой «Показывать на сайте» → видна в Company Project.  
Клиент оформил заказ на сайте → в Management заказ с меткой **Website**.

## Запуск

1. PostgreSQL: база `EKR_DB`
2. API: `dotnet run --project Back --launch-profile http` → `:5263`
3. Front: `cd Front && npm run dev` → `:5173`

## Демо

| Email | Пароль | Роль |
|-------|--------|------|
| superadmin@ekr.local | SuperAdmin123! | SuperAdmin |
| admin@ekr.local | Admin123! | Admin |
| factory@ekr.local | Factory123! | Factory |
