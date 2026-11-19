# Little Lantern Toy Shop

ASP.NET Core MVC coursework application for a children's toy shop. It includes complete CRUD flows for product inventory, cart items, orders/fulfillment, and reviews, plus a simulated payment checkout.

## Open in Visual Studio

1. Open `ChildrensToyShop.slnx` in Visual Studio 2022 or later.
2. Run the entire `Database/CreateMockDatabase.sql` script in SQL Server Management Studio. It creates or upgrades the database, seeds the demo accounts and stock, and adds any missing demo coupon balances. Rerunning it preserves existing data and balances.
3. Set `ChildrensToyShop.Web` as the startup project and press F5.

The configured connection targets `localhost\\SQLEXPRESS` and database `ChildrensToyShopDb`. Change `appsettings.json` if your SQL Server instance has another name.

## Mock login

| Role | Email | Password |
|---|---|---|
| Administrator | admin@toyshop.local | 123456 |
| Customer | customer@toyshop.local | 123456 |

The demo customer starts with 99,999 of each coupon. In My Cart, select A (5%), B (10%), or C (15%) to apply one discount to the checkout total, including tax and gift wrap. The balance updates when the order is placed. Cancelling an unshipped order returns the coupon to the customer.

Admins can open **Low Stock Alerts** from the menu to see listed toys with stock below the reorder threshold.

The final checkout action is deliberately simulated: it immediately creates a PAID order, decrements inventory, and never contacts a bank. Admin users can progress orders through PAID, PROCESSING, SHIPPED, and DELIVERED; cancelling an unshipped order restores stock.
