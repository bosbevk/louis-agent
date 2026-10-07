# samples/order-service — design

> **Kind:** console app + NUnit tests, its own solution (`OrderService.slnx`), **not** in the louis-agent solution ·
> **Path:** `samples/order-service/` · **Runs in:** the demo containers, as its own git repository in
> `.demo/order-service`

## Purpose

A small, realistic stand-in for a microservice, with **ten planted bugs**, for the orchestrator demo. It gives the
orchestrator something to watch and louis-agent something to fix — and its layout is designed so ten independent fix
branches merge without conflicts.

## How it works

- **The "API"** is a console program: each request is a method name and an order id (`order-total 1003`), or
  `serve data/requests.txt` replays a traffic file. Answers are printed as `200 …` or `500 …`.
- **Data** comes from `data/orders.csv` (orders with items, discount code, payment reference, created date, country,
  gift message), loaded by `OrderRepository`.
- **Errors** are recorded like an error tracker would: every unhandled exception is appended to `logs/errors.jsonl`
  (id, method, arguments, exception type, message, stack trace). `ERROR_LOG_PATH` redirects it — the orchestrator's
  replays use their own file, so they never look like production errors.
- **One file per method** under `src/OrderService/Api/` (a partial `OrderApi` class), so each fix touches one file; the
  route table in `Program.cs` never needs changing for a fix.
- **Tests** (`tests/OrderService.Tests`): 14 tests of today's working behaviour; louis-agent adds one regression test
  file per fix under `Regression/`, again so branches don't conflict.

## Structure

| Path | What's in it |
|---|---|
| `src/OrderService/Program.cs` | Routes (method → handler), `serve` mode, error handling and logging |
| `src/OrderService/Orders.cs` | `Order`, `OrderItem`, `OrderNotFoundException`, `OrderRepository` (CSV) |
| `src/OrderService/OrderApi.cs` | The shared part of `OrderApi` (repository, subtotal) |
| `src/OrderService/Api/*.cs` | One method each: get-order, order-total, shipping-cost, invoice-number, packing-slip, loyalty-points, vat, delivery-estimate, discount-label, customer-initials, refund |
| `src/OrderService/ErrorLog.cs` | The JSON-lines error log |
| `data/orders.csv`, `data/requests.txt` | The orders, and the traffic: healthy calls, the ten bug triggers, and three errors that must not be fixed |
| `tests/OrderService.Tests/` | The baseline tests |

## The planted bugs

Ten bugs, one per method, from nine causes: an order without items (which breaks two methods), a retired discount
code, a date with a time, a missing gift message, an overflow on a huge order, a lower-case country code, a Sunday order,
a code without a percentage, and a single-word name. The full table with the expected fixes is in the
[demo README](../../samples/README.md#what-the-demo-traffic-triggers).
Plus three that must not be fixed: a correctly rejected duplicate refund, a payments crash to escalate, and a 404.

## Changing it

- Keep **one method per file** and **one regression test file per fix**, or the demo's branches start conflicting.
- A new method also needs a runbook in `src/louis-agent.orchestrator/Skills/order-service/`
  ([Runbooks](../RUNBOOKS.md)) and, for the demo, a line in `data/requests.txt`.
- The demo copies this folder into a fresh repository on every clean run, so edits here take effect on the next run.

## Related docs

[Orchestrator demo](../../samples/README.md) · [Sample output](../../samples/sample-output/README.md) ·
[louis-agent.orchestrator](louis-agent.orchestrator.md)

---
[Projects](README.md) · Previous: [Tests](tests.md)
