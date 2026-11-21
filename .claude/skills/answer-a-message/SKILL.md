---
name: answer-a-message
description: Drafts Louis's reply to a question someone sends louis-agent.dev on Facebook, Messenger, WhatsApp, a comment or an email, so a prospect gets a true, short, friendly answer that moves them one step closer. Use when Louis shares a message or a screenshot from a customer or prospect, asks how to answer it, or asks for a social reply or an FAQ answer. Holds what we sell today, how a reply is shaped, how to say no honestly, the answers to the questions people keep asking, and how a reply is handed over.
---

# Answer a message

Someone asked louis-agent.dev a question in a public or private message. Claude drafts the reply, and Louis sends
it as himself, as a founder of a small startup would (Louis, 2026-11-21: "we should still think like a startup").
Five stops come first.

1. **Claude drafts, Louis sends.** Never post, comment or message anyone on his behalf. The draft goes to him,
   and he sends it from his own Page or phone.
2. **Say only what is true today.** What the site and its guide sell is the source: the guide's
   `what-we-sell.md` skill. What is only a plan is said to be a plan, or left out. A draft plan Louis has not
   approved is never promised to a customer.
3. **No price in a reply.** Every quote is Louis's, after a short call, as the site and its guide say. A
   different rule is Louis's to set, never the draft's.
4. **A no is said plainly.** If we don't do what they ask, the reply says so in the first two lines, with why and
   what we do instead. A no hidden at the end, or a yes that isn't quite true, loses the customer later.
5. **His voice, not ours.** Plain, warm, short, in the first person ("I build it"), British English, no emojis
   (Louis, 2026-11-18: "I do not like writing in emojies"), no exclamation marks in a row, no buzzwords.

## The steps

1. **Read the message**, and what the person can't say in it: who they likely are (an owner of a small business,
   a developer, a job seeker) and what they want behind the question. "Can I customise the design?" often means
   "will it look like my business?" or "will I be stuck paying someone for every change?".
2. **Check the facts** the answer rests on in `origin/main` after `git fetch origin main`: the guide's
   `src/site/LouisAgent.Identities.Guide/prompts/agent/louis-agent-guide/skills/what-we-sell.md` and `voice.md`,
   and `/mnt/project-files/brand/facebook/page-text.md` for what the Page itself says. An offer such as the two
   free websites is checked before it is mentioned, since it can close.
3. **Write the reply in four parts**, most often four to six short sentences in all:
   - **Their name and the answer first.** "Hi Lindiwe, yes." or "Hi Lindiwe, not like Wix, no." The answer they
     asked for is in the first line.
   - **The why, or the how**, in one or two sentences, in their words, not ours.
   - **What we do instead**, when the answer is a no or a partly.
   - **One question back** that moves them a step and teaches us something: about their business and what they
     do today, never "would you use…?" (*Think like a startup*). One question, never a list.
4. **Read it aloud**, as `write-the-copy` asks. Cut every word that adds nothing, and any word they wouldn't use
   ("agent" may need "a chat assistant on your site that answers your customers" the first time).
5. **Hand it over** as below, with what the message taught us. Add the question to *The questions people keep
   asking*, or count it again there.

## Think like a startup

louis-agent.dev is new and small, so each message is worth more than it would be to a big company: it is a
possible customer and a free lesson in what the market wants. Three sources shape how a reply works for that.

- **Do things that don't scale.** Early on, a founder recruits users one by one and gives each one an experience
  a big company can't (Paul Graham). So Louis answers himself, by name, the same day, and offers a short call.
  Being small is the honest selling point: "I build and look after every site myself".
- **Ask about their life, not our idea.** People are polite about an idea, so "Would you use…?" learns nothing.
  Ask about what they do today and what it costs them: "How do customers reach you now?", "What would you change
  most often?" (Rob Fitzpatrick, *The Mom Test*).
- **Look for a step forward.** A good conversation ends with a commitment: a call booked, their business
  details, an entry for a free website. "Sounds great, I'll think about it" is not one (*The Mom Test*). The
  question back aims at the next real step.

A question asked often is a signal for what to build. "Can I change it myself?" asked again and again is the case
for the self-serve sites in the TODO (F133). Count it in the table below, and give Louis the count when he plans.

## Where the reply goes

| They wrote | Reply |
|---|---|
| A private message (Messenger, WhatsApp, email) | In the same thread, the full answer |
| A public comment asking something general | A short public answer, so others see it |
| A public comment with private details, a complaint or a price question | A short public line that you've seen it, then the rest in a private message. Sprout Social's 2025 survey found 51% of people who reach out in public expect a public acknowledgement first |
| Spam, abuse or a scam link | Nothing. Hide or delete it, and Louis decides |

## How fast

Most people expect an answer within a day on social (Sprout Social Index), and Meta lets a Page answer a message
freely for 24 hours after it arrives, then only with a tag for a person's reply for up to 7 days (Messenger
Platform policy). The Page text says to show "Always open" only when messages are answered within a day. So the
draft is made the same day it is asked for, and an old message gets a short line saying sorry for the wait, no
excuse.

## Saying no

From Help Scout's and Intercom's advice on telling a customer no, kept to what fits a small business:

- **Say it early and honestly.** Better to disappoint them now than leave them hoping (Help Scout, quoting Buffer).
- **Say why**, so they see it is a choice, not a mistake (Intercom). "I build and look after each site myself"
  explains why there is no drag-and-drop editor.
- **Answer the job, not only the question.** Ask or guess what they want done, and say how we get them there
  (Intercom, *Focus on the job to be done*).
- **Offer the next best thing**, even when it isn't ours. A person who needs to change every page daily is better
  served by a builder like Wix, and saying so keeps trust (Help Scout, *Offer alternatives*).
- **Keep the tone of a friend.** A stiff reply reads as cold (Intercom).

## The questions people keep asking

Each answer is the truth today, checked in step 2. *Times* counts how often a question came in since 2026-11-21:
add one each time, and a new row the first time.

| They ask | Times | The honest answer |
|---|---|---|
| Can I customise my website's design? | 1 | Yes: Louis builds it to the design they want, their colours, logo, photos and pages. No: they don't edit the pages themselves like on Wix. A change later is a message to Louis. What the agent knows (hours, prices, services) is kept in files they can change |
| What does it cost? | 0 | Every quote is made after a short call, since it depends on the business. Offer the call |
| What is the agent? | 0 | A chat assistant on their site that answers their customers day and night from what it knows about the business, and passes enquiries on to them |
| Can it take bookings or payments? | 0 | Not as part of a website. Reaching a diary or a till is a custom agent, quoted on its own |
| Is my data safe? | 0 | Each agent runs in its own closed container, sees only what it is given, and can run in Azure's South African region |
| Can I get one for free? | 0 | Only while the two free websites are open: enter at louis-agent.dev/free-websites, and Louis picks two |

## Checks before it is shown

- The first line answers the question they asked.
- Every claim is in `what-we-sell.md` or the Page text on `main` today, and no price is named.
- At most six short sentences, one question at the end, no emojis, British English.
- It reads like Louis talking to a neighbour, not like a company.

## Handing it over

Give Louis the reply ready to paste, then one line on why it is shaped that way, and a second version only when a
real choice sits between them (a shorter one, or one that mentions the free websites). A new FAQ row, or a rule
here that turned out wrong, is a change to this file, committed through `/ship-it` on his word.

## An example

On 2026-11-21 a prospect asked the Page on Messenger "Can I customize my website's design?". Louis's own draft was
"Hi, yes you can give it the design you want. I will build it the way you want it. But changing it yourself like
Wix, is no". The facts were right, and the reply needed the no said kindly, a why, what she can change, and a
question back:

> Hi Lindiwe, yes. I build your site in the design you want, with your own colours, logo, photos and the pages
> you need. You won't drag things around yourself like on Wix, though, because I build and look after each site
> myself. When you want something changed, you send me a message and I change it. What the assistant on your site
> knows, like your hours and prices, you can update yourself. What kind of business is it for, and what would you
> want to change on it most often?

The last question does two jobs: it opens the conversation toward a call, and her answer says whether owners
want to change the look, or only their hours and prices, which is the question behind F133.

## References

- `what-we-sell.md` and `voice.md` in `src/site/LouisAgent.Identities.Guide/prompts/agent/louis-agent-guide/`,
  what we sell and the rule that every quote follows a call
- `/mnt/project-files/brand/facebook/page-text.md`, what the Page says about itself
- [The brand guide](../../../src/docs/brand/README.md#voice), the voice
- Sprout Social, [Social media response time](https://sproutsocial.com/insights/social-media-response-time/) and
  [Social media customer service statistics](https://sproutsocial.com/insights/social-media-customer-service-statistics/):
  a day at most, a public line before a private one (vendor surveys, so a guide, not a law)
- Meta, [Messenger Platform policy, responsiveness](https://developers.facebook.com/docs/messenger-platform/policy/responsiveness):
  the 24-hour window and the 7-day tag for a person's reply
- Paul Graham, [Do things that don't scale](https://paulgraham.com/ds.html): recruit and delight users by hand
- Rob Fitzpatrick, *The Mom Test* ([a summary](https://mtlynch.io/book-reports/the-mom-test/)): ask about their
  life, not your idea, and look for a commitment
- Help Scout, [How to tell a customer no](https://helpscout.com/blog/how-to-tell-a-customer-no), and Intercom,
  [How do you say no to customers?](https://www.intercom.com/blog/say-no-to-customers/)
- `/write-the-copy`, for how a sentence is written and read aloud
