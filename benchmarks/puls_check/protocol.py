"""The portal's line protocol, written (F31): plain lines for people, and '@' then one JSON object per event for the
API's BenchmarkProtocol, which records them into the benchmark store. Every event but the first carries its mode's
repeat; a question and a turn carry their session, so the ledger's rows join to them. Never a cost: the API takes
it from the ledger."""
import json
import sys
import time

VERSION = 1


def emit(event, **fields):
    """One event line: '@' and one JSON object, with the time (Unix seconds)."""
    sys.stdout.write("@" + json.dumps({"event": event, "t": round(time.time(), 3), **fields}, ensure_ascii=False) + "\n")
    sys.stdout.flush()


def start():
    """The first line: the protocol's version, so the API knows what it reads."""
    emit("protocol", version=VERSION)


def say(text):
    """A line for people; one that happens to start with '@' gets a space, so it's never read as an event."""
    sys.stdout.write((" " + text if text.startswith("@") else text) + "\n")
    sys.stdout.flush()


def run_tag(run, mode, repeat):
    """The tag a check's sessions carry, so the ledger's rows find their run: {BENCHMARK_RUN}:{mode}:{repeat}."""
    return f"{run}:{mode}:{repeat}"
