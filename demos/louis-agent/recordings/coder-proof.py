"""The coder's own sentences about its tests (F106.3.4): where its test reproduced the bug and where they all pass.

Reads the coder's answer on stdin and prints those sentences, at most two, word for word. record-triage.sh uses it for
the coder's step, so the step's reason is what the coder proved in its own words, never a cut-off tail of its answer.
"""
import re
import sys

text = sys.stdin.read()
# The answer streams sentences back to back, sometimes with no space after the full stop or a closing colon ("Now the
# fix:The diff"): split after . ! ? or : followed by a capital letter.
sentences = [s.strip() for s in re.split(r"(?<=[.!?:])\s*(?=[A-Z])", text) if s.strip()]
proof = [s for s in sentences if re.search(r"reproduces|fails with|tests? pass", s)]
print(" ".join(proof[:1] + proof[-1:] if len(proof) > 1 else proof))
