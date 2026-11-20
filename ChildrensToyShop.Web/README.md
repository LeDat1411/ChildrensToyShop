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

Admins can open **Low Stock Alerts** from the menu to see listed toys with stock at or below the reorder threshold.

The final checkout action is deliberately simulated: it immediately creates a PAID order, decrements inventory, and never contacts a bank. Admin users can progress orders through PAID, PROCESSING, SHIPPED, and DELIVERED; cancelling an unshipped order restores stock.

## Run the system tests in Visual Studio

Open the solution, build it, then open **Test → Test Explorer → Run All**. The `ChildrensToyShop.Tests` project uses xUnit and Playwright to start the app and exercise its forms in headless Microsoft Edge. Search Test Explorer by case number, such as `TC18` or `TC19`, to run an individual scenario. From the solution directory, the same suite runs with `dotnet test ChildrensToyShop.slnx`.

The tests require installed Microsoft Edge and a running SQL Server instance on which your Windows account can create and drop databases. They default to `localhost\SQLEXPRESS`. To use another instance, set `TOYSHOP_TEST_SERVER` before starting Visual Studio, for example to `(localdb)\MSSQLLocalDB`. Each executed test creates a uniquely named `ToyShopTests_…` database with its own demo data and removes it on completion. The suite starts its own web server and does not require the shop to be running.

| Test cases | What the suite checks |
|---|---|
| TC-01, TC-02 | Both roles reach home after sign-in. A wrong password shows an error and the cart still requires sign-in. |
| TC-04 | A valid toy is saved and listed. |
| TC-05 | A duplicate SKU is rejected with a field error. Submitting a unique SKU saves the toy. |
| TC-06 | A negative price shows a field error and returns focus to the price field. A corrected price saves successfully. |
| TC-07 | Searching for lowercase “magnetic” returns the magnetic building set. |
| TC-09 | Toys at or below their reorder threshold appear in Low Stock Alerts. Toys above it are excluded. |
| TC-10 | Skipped until a supplier purchase order with two lines, a total and an Ordered status can be entered. |
| TC-13 | Simulated checkout verifies two purchased items, stock deductions, the paid order and the receipt. The separate cash-tender check is skipped until cash entry exists. |
| TC-14, TC-15, TC-17 | Checkout blocks a cart that now exceeds stock. Coupon B produces the 10% discount. Clearing a cart before payment preserves stock. |
| TC-18 | An admin changes a paid order to PROCESSING. The customer sees PROCESSING in My orders and on that order’s details page. |
| TC-19 | An admin approves a pending review. The customer sees APPROVED in My reviews and the review appears in the toy’s Parent feedback section. |

TC-08 is excluded at the project owner's request. A failing case indicates an observed difference from the documented expectation. Tests marked skipped represent unavailable workflows.
