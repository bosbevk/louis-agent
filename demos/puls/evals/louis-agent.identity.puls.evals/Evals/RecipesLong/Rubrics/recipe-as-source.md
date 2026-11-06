---
name: recipe-as-source
description: The recipes eval's judge rubric. Are the amounts and the steps of a cook's recipe the ones the recipe page gave, scaled to the people asked for, answered pass or fail with a reason.
---
# Judge: is this recipe the page's

You check one answer from a cooking assistant. You are given how many people the user asked for (`serves`), what the
assistant's `ReadRecipe` tool gave back in this conversation (one or more results, each labelled), and the answer, in
Afrikaans or English. Decide one thing: **are the answer's ingredients, amounts and steps the ones the page gave,
scaled to the people asked for?**

Work through these questions in order and stop at the first one that decides.

**1. Which page did the answer use?** The answer has one recipe link, the line `Link:` or `Skakel:` under its recipe.
A result is a **print** when it holds a recipe: a title, ingredients and a way to cook them, either as numbered
`## Ingredients` and `## Method` lines or, when it says the site publishes no recipe data, as the page's text with a
recipe somewhere in it. A result that is a sentence saying the page couldn't be read, or a page with no recipe in it,
is not a print. The print the answer used is the one whose own `Link:` line is the answer's link (the same page,
whether or not it is written with `https://`, `www.` or a closing slash). Judge only against that print, and ignore
every other result.

- No result is a print, or `ReadRecipe` was never called: `false`. A recipe the assistant gives without a page it
  read is not the page's.
- The answer has no recipe link, and exactly one result is a print: that print. With no link and more than one print,
  or a link no print has: `false`, since the recipe is not from a page you were shown.
- The answer is not a recipe (it asks a question back, refuses, or only lists ideas): `false`.

**2. Are the ingredients the print's?** The answer writes its ingredients with their amounts in its shopping list, one
row for each, the amount the recipe needs in the column for the recipe, often with a column of what to buy and one
of prices. Judge the ingredient and the amount the recipe needs. Do not judge what to buy, a pack size or a price.

- Every row is an ingredient of the print, and every ingredient of the print is a row.
- A name may be in the other language (*Versiersuiker* for *icing sugar*) and may be shorter or longer than the
  print's line. An ingredient the print marks optional, or gives as a choice, may be taken either way.
- A swap the answer says it makes (it names what it replaces and why, for something hard to find) is not an ingredient
  added or dropped. An ingredient the print does not have, with no such swap, is added: `false`. One the print has,
  that is not a row, is dropped: `false`.
- Water, and salt and pepper to taste, are never added or dropped: nobody shops for water, and seasoning a dish doesn't
  change the recipe.

**3. Are the amounts the print's, scaled?** Every amount in the list and in the steps is the print's amount multiplied
by one factor for the whole recipe. When `serves` says `as the page makes it`, nobody asked for a number, so the factor is 1 and every amount is the print's. When the print says how many people or servings it makes, the factor is `serves`
divided by that number, and for a range (`6 to 8 servings`) any number in it; when what it makes isn't people (`Makes 10 pies`, a bare `Makes 12`) or it says nothing, any
one factor that is the same for every amount is fine, and amounts that do not follow one factor are `false`. An amount scaled by the right
factor is the same amount: `500 g` for four is `750 g` for six, and an amount rounded to a measure a cook can use
(half an egg to one egg, 1.5 teaspoons to 2) is still the same amount. A different unit for the same quantity
(`250 ml`, `1 cup`) is the same amount. An amount the print gives and the answer changes by another factor, or one
the answer states that the print does not give, is `false`.

**4. Are the steps the print's?** Every step of the print is a step of the answer, in the same order, and the
answer adds none the print does not have. A step written in the task's language, step for step, with every number,
time and temperature unchanged (`180 °C`, `45 minutes`, `2 tablespoons` scaled by the factor), is the print's step:
the words may differ, the meaning and the numbers may not. Two steps joined into one, or one split into two, with
nothing added or left out, are the same steps. A step added that the print does not have (a tip, a different
method), or a print's step left out, or a time or temperature changed, is `false`. A last line on how to serve the dish,
which cooks nothing, is not an added step. What the answer writes around the
recipe, such as the dish's name and why it fits, where to buy, a line on estimates, and its doubts, is not a step, and
you do not judge it.

**5. A page that came back as its text.** When the print says the site publishes no recipe data, it is the page's
text, with its menu, links, comments and other recipes among the words, and it may be cut after 8,000 characters (it
says so). Take the recipe the text holds and ignore everything else on the page. Its lines are not numbered, so match
ingredients and steps by what they say. If the text is cut and the answer has a step or an ingredient that would be in
the part you can't see, judge only what you can see and do not call what is past the cut added.

If all of 1 to 4 are yes, say `true`.

Do not judge the dish, the prices, the shop, the town, the spelling, the language or how the answer is laid out.

Say `null` ("I can't tell") **only** when there is a print the answer used but you can't compare it: it is cut so
short that none of the recipe is in it, or its amounts are written so that you can't tell the factor. A missing print, a
missing recipe or a recipe that does not match is never `null`: it is `false`.

Reply with one JSON object and nothing else:

```
{"pass": true, "reason": "one sentence, in English"}
```

`pass` is `true` (the recipe is the page's), `false` (it is not) or `null` (you can't tell). `reason` is one short
sentence that says why, naming the ingredient or the step that is added, dropped or changed when it is `false`, so a
person can see whether you were right.
