# The recipe sites

The South African recipe sites the recipes pack may search and read. A site is here when it *allows us*, which means
what was checked: its `robots.txt` (the `*` group and the groups for AI readers), the robots meta and `X-Robots-Tag` of
a recipe page (`noai`, `noimageai`), and any text-and-data-mining reservation. All were read on the day in the `checked`
column, before the site was added (ADR 0023, ADR 0068). A site's terms of use are a separate matter, not part of that
check: Louis's decision on them is in the plan's F35.2 *Approved during the build* (2026-11-04). A site that later says
no is taken out of this table, or its row changed: that is a content change, not a code change.

Each row is one site. The columns are:

- `slug`: the short lower-case name `RECIPE_SOURCES` uses (`*` turns every row on).
- `read by`: `data` when a recipe page publishes a whole recipe in its schema.org data, else `text`. It records what
  was seen on one page of the site; nothing acts on it yet.
- `search`: `wordpress` when the site's own `/?s=` search page is allowed and gives results we can read, else `web`:
  the web search provider, asked for that site only.
- `signals`: what the site's files or pages said that is worth keeping, blank when there is nothing.

| slug | name | home | read by | search | checked | signals |
|---|---|---|---|---|---|---|
| taste | Woolworths TASTE | https://taste.co.za | data | wordpress | 2026-11-04 | |
| foodandhome | Food & Home | https://www.foodandhome.co.za | data | wordpress | 2026-11-04 | |
| bibbyskitchenat36 | Bibby's Kitchen @ 36 | https://www.bibbyskitchenat36.com | data | wordpress | 2026-11-04 | |
| tantalisemytastebuds | Tantalise My Taste Buds | https://www.tantalisemytastebuds.com | data | wordpress | 2026-11-04 | |
| wandercapetown | Wander Cape Town | https://wandercapetown.com | data | wordpress | 2026-11-04 | |
| thecinnamonjar | The Cinnamon Jar | https://thecinnamonjar.com | data | wordpress | 2026-11-04 | |
| saltyginger | Salty Ginger | https://saltyginger.com | data | web | 2026-11-04 | |
| thebigtastybite | The Big Tasty Bite | https://thebigtastybite.com | data | web | 2026-11-04 | |
| bitsofcarey | Bits of Carey | https://bitsofcarey.com | data | web | 2026-11-04 | |
| simply-delicious-food | Simply Delicious | https://simply-delicious-food.com | data | web | 2026-11-04 | |
| feastandwest | Feast and West | https://feastandwest.com | data | web | 2026-11-04 | |
| foodleclub | Foodle Club | https://www.foodleclub.com | data | web | 2026-11-04 | |
| woolworths | Woolworths | https://www.woolworths.co.za | data | web | 2026-11-04 | |
| rooirose | Rooi Rose | https://novanews.co.za/rooirose | text | web | 2026-11-04 | Content-Signal: ai-input=yes, ai-train=no |
| thelazymakoti | The Lazy Makoti | https://thelazymakoti.com | text | wordpress | 2026-11-04 | |
| thekatetin | The Kate Tin | https://thekatetin.com | text | wordpress | 2026-11-04 | recipe in microdata, not read |
| cooksister | Cooksister | https://www.cooksister.com | text | wordpress | 2026-11-04 | |
| vrouekeur | Vrouekeur | https://vrouekeur.co.za | text | wordpress | 2026-11-04 | |
| thefoodfox | The Food Fox | https://thefoodfox.com | text | wordpress | 2026-11-04 | |
| mykitchen | MyKitchen | https://mykitchen.co.za | text | web | 2026-11-04 | |
| cupcakesandcouscous | Cupcakes and Couscous | https://cupcakesandcouscous.com | text | wordpress | 2026-11-04 | |
| sarahgraham | Sarah Graham | https://sarahgraham.co.za | text | wordpress | 2026-11-04 | recipe in microdata, not read |
| zolanene | Zola Nene | https://www.zolanene.com | text | wordpress | 2026-11-04 | |
| sibamtongana | Siba Mtongana | https://sibamtongana.com | text | wordpress | 2026-11-04 | |
| braai | Jan Braai | https://braai.com | text | wordpress | 2026-11-04 | |
| realmealrevolution | Real Meal Revolution | https://realmealrevolution.com | text | wordpress | 2026-11-04 | |
| rhodesquality | Rhodes Quality | https://rhodesquality.com | text | wordpress | 2026-11-04 | |
| foodloversmarket | Food Lover's Market | https://foodloversmarket.co.za | text | web | 2026-11-04 | |
| thesouthafrican | The South African | https://www.thesouthafrican.com | text | web | 2026-11-04 | |
| finglobal | Fin Global | https://www.finglobal.com | text | wordpress | 2026-11-04 | |
| thestarlitpath | The Starlit Path | https://thestarlitpath.com | text | web | 2026-11-04 | |
| lookatwhatimade | Look At What I Made | https://lookatwhatimade.net | text | web | 2026-11-04 | |
| inspiredlivingsa | Inspired Living SA | https://www.inspiredlivingsa.co.za | text | web | 2026-11-04 | |
| clover | Clover | https://www.clover.co.za | text | web | 2026-11-04 | |
| luckystar | Lucky Star | https://luckystar.co.za | text | web | 2026-11-04 | |
| spar | SPAR | https://www.spar.co.za | text | web | 2026-11-04 | |
| pnp | Pick n Pay | https://www.pnp.co.za | text | web | 2026-11-04 | Crawl-delay: 10 |
| paarman | Ina Paarman | https://paarman.co.za | text | web | 2026-11-04 | |
| thedaleyplate | The Daley Plate | https://thedaleyplate.com | text | web | 2026-11-04 | |
| jackiecameron | Jackie Cameron School of Food and Wine | https://www.jackiecameron.co.za | text | web | 2026-11-04 | |
| foodiesofsa | Foodies of SA | https://www.foodiesofsa.com | text | web | 2026-11-04 | |
| koesister | Koesister | https://www.koesister.com | text | web | 2026-11-04 | |
| shirleybanting | Shirley Banting | https://shirleybanting.blogspot.com | text | web | 2026-11-04 | |
| thesouthafricancookbook | The South African Cookbook | https://thesouthafricancookbook.blogspot.com | text | web | 2026-11-04 | |
| bestrecipes | Best Recipes | https://www.bestrecipes.co.za | data | wordpress | 2026-11-05 | |
