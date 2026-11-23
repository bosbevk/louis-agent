---
name: plan-the-marketing
description: Plans how a product is sold and advertised, as a marketer would, from a goal in numbers to the channels tried, each with a small test. Use when Louis asks how we will sell, market, advertise, launch or get users or downloads for a product, asks whether or where to run ads or what to spend, asks to boost a post or to message a group, asks for a marketing or launch plan, or brings back the numbers of a campaign. Holds the goal worked back to downloads, the nineteen channels and how three are picked, the small test each gets, the South African rules on messages and adverts, the store page's rules, and the plan handed over. What the product says and who it is for is research-the-market's, and a reply to one person is answer-a-message's.
---

# Plan the marketing

Marketing here is finding the one or two ways people really come to a product, by trying small and measuring. The
plan is for Louis to carry out, and he is a developer with no business background, so every action in it is an
exact step. Six stops come first.

1. **Claude plans and drafts, Louis sends and pays.** Never post, message, mail, book an advert or sign up to a
   service on his behalf.
2. **No money without his yes.** An advert, a boost, a paid listing or a gift to someone who posts is put to him
   as a budget through `/put-a-finding`: the amount as a cap, the day it ends, and the cost per install that stops
   it early. On his yes, that test and its pass mark go into the plan the same day.
3. **Research first.** Who it is for and what it says come from `/research-the-market`'s work, read before
   anything is planned. A channel's own rules, a price and a law are read at their source on the web that day.
4. **Only true claims.** The product's brand guide lists what is never said. A number in an advert is one we
   measured. Nothing promises a result, such as money back.
5. **A message to a person needs their yes first.** Mail, SMS, a direct message or a call that sells is allowed
   only with consent, or to a customer about a similar product of ours (*The rules*).
6. **The pass mark is written before the test.** A test's number and its end date are in the plan before it
   starts, and are not changed after the result.

## The steps

1. **Read what we have.** The product's brand guide, its market research and sizing, its price, and what was
   tried before and what came of it. For the bill checker these are `docs/brand/README.md` in `bosbevk/bill-checker`
   and `/mnt/project-files/research/bill-checker/`. What is missing is asked of `/research-the-market` first.
2. **Say the goal as a number by a date**, and work it back. A product not yet in a store counts its dates from
   the day the store opens, and the plan names what that day waits on. Paying subscribers come from trials,
   trials from downloads, downloads from visits to the store page, visits from people reached. Use our own measured rate
   where there is one. Where there is none, use a benchmark, name it, and mark it as a guess to replace.
3. **Go through all nineteen channels** in [channels.md](channels.md), and write one real idea for this product
   under each. The ones that look unlikely too: "Everyone starts off with biases", the book says, and "it is
   important that you not dismiss any traction channel in this step".
4. **Rank them in three rings**, and pick three for the middle ring: the most promising to test now.
5. **Give each of the three a small test.** What is done, what it costs in rand and in Louis's hours, how long it
   runs, the one number that is counted, and the pass mark. A test answers the book's three questions: what a
   customer costs through this channel, how many customers it holds, and whether they are the ones we want.
6. **Find the real places, by name.** For each picked channel, search the web that day for where these customers
   really are: the Facebook groups of the town with their member counts and what their rules say about adverts,
   the papers with their newsroom's address, the associations with who to ask. A place that could not be found or
   opened is said so, never guessed. Facebook's groups can't be searched from the open web, so the plan gives
   Louis the searches to run in the Facebook app and a table to fill: the group's name, its link, its members and
   its rule on adverts. Then read each channel's own rules at their source (*The rules*), and write into the
   plan what they allow.
7. **Give each channel its own link**, so a download is counted against where it came from (*Counting*).
8. **Write the plan** (*What the plan holds*), and have the words and pictures made by their own skills:
   `/write-the-copy` for words, `/design-a-graphic` for a card or an advert's picture, `/answer-a-message` for a
   reply.
9. **When the numbers come back**, hold them against the pass marks. Say keep, change or drop for each channel,
   with why, and write it in the plan's record with its date. Once one channel passes its test twice, the plan
   narrows to that one.

## How channels are picked

The method is the Bullseye framework from *Traction* (Weinberg and Mares). Its outer ring is what is possible: all
nineteen channels, with an idea each. Its middle ring is what is probable: a few channels tested cheaply and at
the same time. Its inner ring is what is working: the one channel that moves the number, and all the effort goes
there. The book says a rough idea of a channel takes "at most a thousand dollars and a month of time", and to
"run four Facebook ads versus forty". Our tests are smaller still: most cost only Louis's hours.

A new product starts with what doesn't scale. Louis recruits the first users one by one and talks to each, as
`/answer-a-message` describes under *Think like a startup*. So a channel that costs only his hours is tested
before one that costs money.

## The rules

These were read on 2026-11-23. They change, so read them again at the source on the day they are used. Each is
research to check, not legal advice, and the plan says so.

| What | The rule | Source |
|---|---|---|
| A message that sells, by any electronic communication | Section 69(1) names automatic calling machines, fax, SMS and email. WhatsApp, a direct message and a push notification are our reading of section 1's "any text, voice, sound or image message sent over an electronic communications network", not words of the Act. Allowed only with the person's consent, or to our own customer when all three hold: the details came from a sale, the message is about our own similar product, and the customer could object free of charge then and in every message. Someone who never refused may be asked for consent once. Every message says who sent it and how to stop it. The Regulator's note, which is advisory, counts a phone call as electronic | POPIA sections 1 and 69, and the Information Regulator's guidance note on direct marketing, 3 December 2024 |
| Someone posts about us for money, or for anything given | The post is marked `#ad` or `#sponsored`, where it is seen without opening "read more", also when the pay was goods and not money. The Advertising Regulatory Board held a brand to this in 2026, for a post on a personal account | The ARB's Code of Advertising Practice, as Polity reported it on 26 August 2026; the Code itself was not read |
| The Play store's page | A title of at most 30 characters, a short description of 80, a full one of 4,000. No "free", "new", "#1" or "best", no price and no emoji in the title, the icon or the developer's name. No "free", "discount", "#1" or "best" in the short description. No testimonial without a name, and no comparison with another app | Play Console Help: *Metadata*, and *Best practices for your store listing* |
| Opening the Play store at all | A personal developer account made after 13 November 2023 runs a closed test "with a minimum of 12 testers who have been opted in continuously for at least 14 days" before production opens. A tester who leaves early doesn't count, so a test recruits more than 12 | Play Console Help: *App testing requirements for new personal developer accounts* |
| An advert on Facebook or Instagram | Every campaign says whether it is in a special category. Meta's developer page lists housing, credit, employment, and issues, elections and politics, and says a "financial products and services" category is required for US financial campaigns. Meta's page on what counts as a financial product opened empty, so whether a bill checker is one is not known | Meta's *Special ad categories* |

Not read yet: Apple's rules for a product page, the Consumer Protection Act's sections on marketing, and the
ARB's Code itself on proof for a claim. Read them at the source before a plan leans on them.

## Counting

Google Play gives up to 50 custom store listings, each with its own address, so every channel gets its own: the
newspaper's link, the Facebook card's link, the flyer's QR code. A download is then counted against its channel
with no extra tool. The plan keeps one table, a row per channel per week: reached, visits, downloads, trials,
paying, rand spent. A paying subscriber's cost is the rand spent divided by those who paid. Louis's hours are
written down too, since they are the real cost of a free channel.

## What the plan holds

The plan is a doc, written as `/write-the-docs` asks: the goal and its sum, the three channels and why these, each
test with its pass mark, the rules that apply, the exact steps for Louis with a date each, and the record of what
was tried. It lives beside the product's brand guide, which is a commit in that product's repository, so the
draft stays in `/mnt/project-files/` until Louis says commit. Each step is something he can do in one sitting:
"Mail the Swartland Gazette's newsroom this draft", with the draft, not "do press".

Its centre is the posting table, a row per thing Louis posts or sends: the place by name with its link, the day,
the words, the picture, the link that counts it, and what the place's rules ask. He should be able to work down
it without asking anything.

## Checks before it is shown

- Every number names where it came from, and a guess is marked as one.
- Every one of the nineteen channels has an idea, and the three picked each have a cost, an end date and a pass
  mark.
- No claim is on the brand guide's never-say list, and no line promises a result.
- Every step that reaches a person names the consent it rests on.
- Nothing in it was sent, booked or paid for.

## Handing it over

Show Louis the plan with its sources, the three tests in one line each, and any budget as its own finding. What
could not be confirmed is listed plainly. Save his picks and each test's result to project memory, so a later
thread starts from what was learned.

## An example

The bill checker, sized on 2026-11-23. The middle case was 350 paying households in a year, at 1.4% of downloads
paying (RevenueCat's 2026 median for low-priced apps), so about 25,000 downloads. That is the goal's sum. The
brand work had already named places to try: a closed test of twelve people through a Facebook card, the Novus
papers and The Village NEWS, the ratepayers' groups, and the tariff change every July as a yearly reason to write.
In the framework's words those are four channels: existing platforms, publicity, community building and content.
Each would get a link of its own and a pass mark, such as "twenty testers opted in from the card by the closed
test's first day", so that twelve are still in 14 days later.

## References

- Gabriel Weinberg and Justin Mares, *Traction*: [the Bullseye framework](https://medium.com/@yegg/the-bullseye-framework-for-getting-traction-ef49d05bfd7e)
  and [the nineteen channels](https://medium.com/p/the-19-channels-you-can-use-to-get-traction-93c762d19339)
- The Information Regulator, [guidance note on direct marketing](https://inforegulator.org.za/guidance-notes/),
  3 December 2024
- Play Console Help, [Metadata](https://support.google.com/googleplay/android-developer/answer/9898842) and
  [Best practices for your store listing](https://support.google.com/googleplay/android-developer/answer/13393723);
  Google Play, [custom store listings](https://play.google.com/console/about/customstorelistings/)
- Play Console Help, [App testing requirements for new personal developer accounts](https://support.google.com/googleplay/android-developer/answer/14151465)
- [POPIA section 69](https://popia.co.za/section-69-direct-marketing-by-means-of-unsolicited-electronic-communications/)
  and [section 1](https://popia.co.za/section-1-definitions/), on a third party's copy of the Act
- Polity, [When is a personal post actually an advertisement?](https://www.polity.org.za/article/when-is-a-personal-post-actually-an-advertisement-2026-08-26),
  on the ARB's ruling
- Meta, [Special ad categories](https://developers.facebook.com/docs/marketing-api/audiences/special-ad-category)
- RevenueCat, [State of Subscription Apps 2026](https://www.revenuecat.com/state-of-subscription-apps-2026/), for
  a benchmark where we have no number of our own
- [Answer a message](../answer-a-message/SKILL.md), *Think like a startup*, and
  [Research the market](../research-the-market/SKILL.md)
