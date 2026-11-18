---
name: free-website-entry
description: How you finish an entry for the two free websites once its form comes back, from reading the answers to the card and sending it to Louis. Read it as soon as the free-website entry form comes back filled in.
metadata:
  level: agent
  load: on-demand
---
# Finishing a free-website entry

The form has come back. Louis reads every entry and chooses the two businesses himself.

## Never

1. **Never put an entry that makes no sense on the card**, and never offer to send it.
2. **Never promise them a place.** Louis chooses the two, never you.
3. **Never send before Send it comes back**, and never send twice.

## Steps

1. Read what comes back before you show it. When an answer is keyboard mash, a placeholder or empty, or the contact
   is no email or phone, say kindly in one sentence which answer you couldn't use, and call `AskFor` again for only
   those boxes.
2. Show it back with `ShowCard`: the kicker ENTRY FOR THE FREE WEBSITES, their business as the title, a row each for
   the four, the status Not sent yet in amber. What they told you before the form goes on the card too.
3. Say in one sentence that Send it mails this entry to Louis and nothing else.
4. `Offer` Send it (suggested), Change something and Not now.
5. On Send it, call `SendLead` once, with what they want starting "Entry for the free websites", and show the card
   again with the status Sent in green.
6. Tell them Louis reads every entry and calls the two he chooses.

## An example

The form comes back with the name Anna, the business "sadf", the contact anna@example.com and a
reason. You say "I couldn't use what your business does. Tell me in a few words?" and ask again for only that box.
It comes back as "I bake bread and cakes in Malmesbury". Now the card shows all four, with Not sent yet in amber.
