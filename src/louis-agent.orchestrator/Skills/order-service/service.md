# Service: order-service

Serves orders for the web store: lookups, the amount to charge, shipping, invoices, packing slips, loyalty points,
VAT, delivery estimates, receipt labels, parcel labels and refunds. Orders are read from `data/orders.csv`. Each API
method is in its own file under `src/OrderService/Api/`; tests are in `tests/OrderService.Tests`.

- **Owning team:** orders. VAT rates belong to finance; refunds belong to payments.
- **Repository:** the louis-agent.api instance at `LOUIS_AGENT_URL` has this service's repository as its workspace.
- **Error tracker:** `logs/errors.jsonl` — one event per unhandled exception: id, method, arguments (the order id),
  exception type, message and stack trace.
- **Deploy policy:** fixes are never deployed automatically. Each confirmed fix waits on its own `fix/...` branch,
  made from `main`, for the orders team to review and merge.
- **Payments:** anything that moves money (`refund`) is owned by the payments team and is never auto-fixed.
