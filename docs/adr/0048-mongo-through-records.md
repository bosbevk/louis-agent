# 0048. MongoDB goes through records: a typed collection for a shape we know, `BsonDocument` only for one we don't

**Status:** Proposed · **Date:** 2026-10-28 (Louis, reading `RunDocuments.ToBson` in 7c step 3a: "must use
mongocollection"; "make it an adr, i do not know when to not use a mongocollection") · **Recorded:** 2026-10-28

## Context

The eval store writes five collections (runs, conversations, turns, requests, notes), the run's document holding its
prompts and slices, and two more stores keep human grades and calibration reports. Every one of them is an `IMongoCollection<BsonDocument>` with a
mapper written by hand (`RunDocuments.ToBson`, `ConversationOf`, …): about 600 lines that build each document field by
field and read it back the same way, with `BsonValues` for nulls, dates and enums. Step 7b chose that on purpose, so
what `mongosh` shows is exactly what the portal reads, with no convention in between. The cost showed in 7c: every
field the protocol adds is written three times (the contract record, the mapper out, the mapper in), a typo in a
field name is found only by a test that reads it back, and the mappers are the biggest files in the API.

MongoDB's own guidance for the .NET driver is the other way round: "We recommend using POCOs in your C# code to
adhere to idiomatic driver usage and achieve the best performance"
([POCOs](https://www.mongodb.com/docs/drivers/csharp/current/serialization/poco/)). A POCO is a plain class or record
of our own; the driver maps it to and from BSON through a class map, which it builds by itself (automapping) and we
adjust in one of three documented ways: attributes on the record (`[BsonElement("started_at")]`, `[BsonId]`,
`[BsonIgnore]`, `[BsonRepresentation]`), a class map registered in code before the first connection
(`BsonClassMap.RegisterClassMap<T>`, [class mapping](https://www.mongodb.com/docs/drivers/csharp/current/serialization/class-mapping/)),
or a `ConventionPack` that applies one rule to every field (camel case, say). `BsonDocument` stays for a shape the
code doesn't know in advance. This is the same line [ADR 0041](0041-json-through-records-by-hand-only-for-bytes.md)
draws for JSON: a known shape is a record the serializer writes and reads; a bag built by hand only when the shape
is decided at run time or the bytes are the contract.

Considered: **(A) one document record per collection** with attributes, converted from and to the contract's record
by `Of` and `ToRun()` (Fowler's *Metadata Mapping* on a *Data Transfer Object*); **(B) class maps on the contract
records themselves**, so the protocol's `Run` is the document (least types; but a contract change silently changes
what is stored, and a stored run outlives the API's record); **(C) conventions only**, snake case and wire names
applied to every type by rule (least code; but the field names are implicit, and a renamed property renames the
field unseen). Keeping the hand mappers was rejected by Louis.

## Decision

- **A shape we know is a record in a typed collection.** Each collection is `IMongoCollection<TDocument>` over a
  sealed record of its own (`RunDocument`, `ConversationDocument`, …) beside the store that uses it
  (`Evals/Store/Runs/`, `Evals/Store/Conversations/`, …; Louis, 2026-10-28), with the
  stored field name on each property as `[BsonElement("started_at")]`, the key as `[BsonId]`, and
  `[BsonIgnoreExtraElements]` so a field added later never breaks a read. The record has a static `Of(run)`
  and a `ToRun()` (ADR 0041's *Replace Constructor with Factory Function*), and nothing else: no logic, no resource
  ([ADR 0040](0040-records-for-values-classes-for-things.md)).
- **Enums are stored as their wire names.** One `WireNameSerializer<TEnum>`, registered once at start-up beside the
  class maps, writes `.Wire()` and reads with `SettingChoice.TryParse` ([ADR 0024](0024-choices-are-enums.md),
  [ADR 0043](0043-repeated-names-are-enums.md)); no `[BsonRepresentation(BsonType.String)]`, which would store the
  C# name.
- **Dates are stored as BSON dates in UTC** and read back as `DateTimeOffset` at offset zero
  ([ADR 0046](0046-dates-are-utc.md)).
- **Filters, sorts and indexes are typed too:** `Builders<RunDocument>.Filter.Eq(d => d.Check, check)`, never a
  field name in a string, so a renamed field fails the build.
- **`BsonDocument` only for a shape decided at run time:** a JSON bag we keep as the caller sent it (a grader's
  `details`, if 7c leaves one), held as a `BsonDocument` property of the record; and a one-off admin or migration
  query. Never for a record the code already has.
- **The in-memory stores stay as they are:** they hold the contract's records; only the Mongo side maps.

## Consequences

- The hand mappers and most of `BsonValues` go; each collection's shape is one record a reader sees whole, and
  `mongosh` still shows the same field names, now declared on the record instead of typed into a builder.
- Two records per collection (the contract's and the document's), kept in step by `Of` and `ToRun()`; the store
  contract tests (one per store, run on both implementations) hold the round trip.
- Class maps and serializers must be registered before the first connection; `MongoEvalDatabase` does it once.
- Stored eval data is disposable until hosting (Phase 9): no migration, no alias; `[BsonIgnoreExtraElements]` stays
  only so a field added later never breaks a read (Louis, 2026-10-28;
  [ADR 0049](0049-nothing-is-compatible-before-a-sale.md)).

## Links

[0022](0022-benchmarks-recorded-from-the-protocol.md) · [0024](0024-choices-are-enums.md) ·
[0040](0040-records-for-values-classes-for-things.md) · [0041](0041-json-through-records-by-hand-only-for-bytes.md) ·
[0046](0046-dates-are-utc.md) · [F18 plan](../plans/F18-evals.md), 7c step 3a · MongoDB .NET driver:
[POCOs](https://www.mongodb.com/docs/drivers/csharp/current/serialization/poco/),
[class mapping](https://www.mongodb.com/docs/drivers/csharp/current/serialization/class-mapping/)

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
