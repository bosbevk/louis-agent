# 0041. JSON goes through records; build it by hand only when the shape or the bytes need it

**Status:** Accepted · **Date:** 2026-10-25 (F18 step 5.1, Louis on the HTTP session client's body: "or is that the
correct way?", "so must we build up the json object?", then "I want an ADR explaining when to build the JsonObject and
when to use a serialiser") · **Recorded:** 2026-10-25

## Context

System.Text.Json gives five ways to write or read JSON, and the code uses all of them. On the F18 branch on 2026-10-25,
core, the hosts, the packs, the evals and the tools had:
- **The serializer over a type** (`JsonSerializer`, `PostAsJsonAsync`, `ReadFromJsonAsync`): about 70 calls in about
  40 files. `CreateSessionRequest` is read this way, and the datasets with `UnmappedMemberHandling.Disallow`.
- **Anonymous objects** handed to the serializer (`Results.Json(new { … })`): 20 replies in 7 of the API's endpoint
  files.
- **The writable DOM** (`JsonObject`, `JsonNode`): one file, `ScriptTool`, which builds a tool's JSON schema and its
  input from a skill file's parameters.
- **The read-only DOM** (`JsonDocument`, `JsonElement`): 42 parses in 27 files. A provider's answer, an SSE event's data
  and the model's tool arguments are read this way.
- **The writer** (`Utf8JsonWriter`): two files. `ProtocolWriter` writes the eval protocol's lines, and `PromptHash`
  hashes a prompt's canonical form.

Nothing says which to use when. Step 5.1's first draft of `HttpSessionClient` built its request body by hand, field by
field, as `ProtocolWriter` does. That's the pattern the code next to it showed. And 4f.3 found a bug of the same kind:
`LogContent` hashed the model's string argument as its raw JSON, quotes included, because a `JsonElement` was turned
into text the way an object is.

These were considered:
- **Always the serializer.** Rejected. A shape that is only known at run time has no type to serialize, and a format
  whose bytes are its contract (a hash's input, a line another program compares) can't leave its field order and
  escaping to the serializer's options.
- **Always the DOM.** Rejected. Hand-built JSON has no type the compiler checks. A misspelt key or a dropped field
  compiles, and the shape lives only in the code that writes it.
- **Source-generated contexts** (`JsonSerializerContext`). Not taken yet. They matter for trimming and native AOT, which
  no host uses.

## Decision

- **A known shape is a record, and the serializer writes and reads it.** That covers a request body, a reply, a
  settings file, a dataset file and a stored document: anything whose fields are known when the code is written. The
  record is the contract ([0040](0040-records-for-values-classes-for-things.md)), so the compiler checks it and a test
  pins its JSON. Optional fields are nullable, and the options say whether a null is written.
- **One options object per wire, never changed for one caller.** A contract's naming, null handling and enum names live
  in one `JsonSerializerOptions`, as `EvalJson.SnakeCase` does for the scores and datasets. A caller that needs a
  difference gets its own options built from the shared one (`new(SnakeCase) { DefaultIgnoreCondition = WhenWritingNull
  }`), so the others keep theirs. The scores must write `null`, and the API's request bodies must leave nulls out.
  It's `WhenWritingNull`, not `WhenWritingDefault`, so a `false` or a `0` is still sent.
- **Enums go over the wire by name** ([0024](0024-choices-are-enums.md)), through `JsonStringEnumConverter` in the
  wire's options. Never by number, and never by a `ToString()` at the call site.
- **A reply another program reads is a named record.** That covers a web app, a script or a test. An anonymous object
  still goes through the serializer, but its shape lives only inside one lambda, where no client can share it and no
  test can name it.
- **The writable DOM (`JsonObject`) is for a shape decided at run time.** A schema built from a skill file's parameters
  (`ScriptTool`) is one such case. So is a document whose unknown fields must survive an edit, or a body passed on to
  another service. It isn't for a body whose fields the code already knows.
- **The read-only DOM (`JsonDocument`, `JsonElement`) is for input the code doesn't fully own.** A provider's answer, an
  SSE event's data, the model's tool arguments and an outside API's reply are all read this way: the code reads the
  fields it needs and ignores the rest. A string is read with `GetString()`. `GetRawText()` is that string's JSON,
  quotes and escapes included (the 4f.3 bug). A `JsonDocument` is disposed, and an element kept beyond it is
  `Clone()`d.
- **The writer (`Utf8JsonWriter`) is for when the bytes are the contract.** A line format another program compares line
  for line is one case: `ProtocolWriter`'s lines must match the Python checks', field order and ASCII escaping included.
  A canonical form that is hashed is another (`PromptHash`), and so is output too large to hold in memory. The code that
  writes such a format also documents it, and a test compares its exact bytes.

## Consequences

- **Review holds the rule,** with 0040's. A hand-built body for a known shape, an anonymous reply, a `GetRawText()` read
  as a value, or a shared options object changed for one caller are findings. No test holds the rule.
- **Existing code isn't swept.** The API's 20 anonymous replies become records when a step changes them, starting with
  the admin endpoints that F18 step 5.2 and 7b touch. `ScriptTool`, `ProtocolWriter` and `PromptHash` already follow the
  rule.
- **Step 5.1's `HttpSessionClient`** sends records through a wire options object (snake case, nulls left out, enums by
  name). Its test pins the body, `model: false` included.
- **One more type per contract.** A request and its reply are now types to name and keep, and in return the compiler and
  a test hold their shape.
- **Not decided here:** source-generated serializer contexts, until a host is trimmed or compiled ahead of time.

## Links

- [0040](0040-records-for-values-classes-for-things.md): a request and a reply are values, so they are records.
- [0024](0024-choices-are-enums.md): a choice is an enum, parsed once; on the wire it's the enum's name.
- [0009](0009-message-text-as-enum-templates.md): message text comes from enums, and a JSON key is not message text.
- [0039](0039-api-routes-under-v1.md): the API's routes; their bodies and replies are the records this record names.
- [F18 Evaluation](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F18-evaluation.md): step 4f.3 (the `GetRawText` bug) and step 5.1 (the session client).
