# Samples: what the agents work on

> **Reference.** The made-up repositories the agents work on: the one the triage workflow (F106.3) triages and fixes,
> and the one the site's recorded coding session (F120) fixes. A sample is a customer's
> repository, not the framework's code: the coder edits it, the reviewer reads it, and the repo's own size and folder
> rules leave it alone. **Elsewhere:** the workflow in [the F106.3 plan](../../../src/docs/plans/F106.3-triage-and-fix-a-service.md);
> the orchestrator that first watched this service in [its doc](../docs/projects/louis-agent.orchestrator.md).

## order-service

A tiny stand-in for a microservice, restored whole from the orchestrator demo of October (the tag `pre-cl10`): eleven
API methods over a CSV of orders, each method in its own file under `src/OrderService/Api/`, and **ten seeded bugs,
one per method**, which `data/requests.txt` trips. An unhandled exception is written to `logs/errors.jsonl` as one JSON
line (`id`, `timestamp`, `service`, `method`, `arguments`, `exception_type`, `message`, `stack_trace`), the way a real
service reports to its error tracker; the triage pack's feed reads that file.

```bash
cd demos/louis-agent/samples/order-service
dotnet run --project src/OrderService -- order-total 1003        # one call: a 500 and a line in logs/errors.jsonl
dotnet run --project src/OrderService -- serve data/requests.txt  # the traffic file: 13 errors
dotnet run --project src/OrderService -- serve data/requests-short.txt   # 5 errors, for a short run
dotnet test                                                      # the behaviour that works today
```

`ERROR_LOG_PATH` moves the log, so a replay of one request (the workflow checking a fix) stays out of the real log.
The workflow works on a fresh clone of this folder as its department's repository, never on the sample in the repo,
so a fix lands on a branch of the clone and the sample keeps its bugs for the next run.

The service has no solution entry of its own in `louis-agent-solution.sln`: it is a workspace, built and tested by
the agents with the dotnet pack, and by hand as above.

## invoice-matcher

A small C# library that matches bank payments to invoices by the invoice number in the reference and the full
amount, with four tests and **two seeded bugs** its tests catch: a lower-case reference is missed, and `INV-104` takes
the payment meant for `INV-1042`. The site's `/coding-agent` page plays the coding agent fixing both, recorded once
on a fresh git copy of this folder ([how to record it again](../recordings/README.md)). Like the order service, it
has no entry in the solution, so its failing tests never run in CI.

```bash
cd demos/louis-agent/samples/invoice-matcher
dotnet test InvoiceMatcher.slnx    # 2 failed, 2 passed: the two bugs
```

---
[Docs index](../../../src/docs/README.md) · Related: [The louis-agent demo](../docs/README.md)
