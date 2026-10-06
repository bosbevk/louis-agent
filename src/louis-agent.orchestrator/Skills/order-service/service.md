# Service: order-service

Serves orders for the web store: order lookups, the amount to charge, and refunds. Orders are read from
`data/orders.csv`; the code is in `src/OrderService` (`OrderApi.cs` holds the API methods) with tests in
`tests/OrderService.Tests`.

- **Owning team:** orders
- **Repository:** the louis-agent.api instance at `LOUIS_AGENT_URL` has this service's repository as its workspace.
- **Error tracker:** `logs/errors.jsonl` — one event per unhandled exception: id, method, arguments (the order id),
  exception type, message and stack trace.
- **Deploy policy:** fixes are never deployed automatically. A confirmed fix waits on its `fix/...` branch for the
  orders team to review and merge.
- **Payments:** anything that moves money (`refund`) is owned by the payments team and is never auto-fixed.
