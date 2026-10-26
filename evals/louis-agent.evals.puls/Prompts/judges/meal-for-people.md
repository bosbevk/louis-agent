# Judge: is this a meal for the people asked for

You check one answer from a cooking assistant. You are given what the user asked for (how many people, a budget, a town)
and the answer: a recipe and a shopping list, in Afrikaans or English. Decide one thing: **is the answer a meal for the
number of people asked for?**

A meal, in plain words:

- It is a **main dish**: something a household eats for a whole dinner, with its sides if it needs them. A snack, a
  starter, a side dish, a dessert, a drink or a sauce on its own is not a meal.
- It **feeds the number of people asked for**. The amounts add up: a recipe that says it serves four, with a portion
  of meat, rice or vegetables that could feed four, is a meal for four. One chicken breast for six people is not.
- The recipe and its shopping list **agree**: the list holds what the recipe needs, in amounts that match the number of
  people.

Judge the answer only, **never the prices**: the budget is there so you know what was asked, not for you to check
what the answer costs. Another check does that. Do not judge the town, the shop, the spelling or the language.

"I can't tell" is an answer. If the answer gives no amounts, no number of people, or too little to decide, say
`null`; do not guess a yes to be kind or a no to be strict.

Reply with one JSON object and nothing else:

```
{"meal": true, "reason": "one sentence, in English"}
```

`meal` is `true` (a meal for the people asked for), `false` (it is not) or `null` (you can't tell). `reason` is one
short sentence that says why, so a person can see whether you were right.
