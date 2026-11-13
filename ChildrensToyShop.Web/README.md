# Little Lantern Toy Shop

ASP.NET Core MVC coursework application for a children's toy shop. It includes complete CRUD flows for product inventory, cart items, orders/fulfillment, and reviews, plus a simulated payment checkout.

## Open in Visual Studio

1. Open `ChildrensToyShop.slnx` in Visual Studio 2022 or later.
2. Run `Database/CreateMockDatabase.sql` in SQL Server Management Studio against your local SQL Server Express instance.
3. Set `ChildrensToyShop.Web` as the startup project and press F5.

The configured connection targets `localhost\\SQLEXPRESS` and database `ChildrensToyShopDb`. Change `appsettings.json` if your SQL Server instance has another name.

## Mock login

| Role | Email | Password |
|---|---|---|
| Administrator | admin@toyshop.local | 123456 |
| Customer | customer@toyshop.local | 123456 |

The final checkout action is deliberately simulated: it immediately creates a PAID order, decrements inventory, and never contacts a bank. Admin users can progress orders through PAID, PROCESSING, SHIPPED, and DELIVERED; cancelling an unshipped order restores stock.
