---
name: check-a-page
description: Look at what a visitor sees before it is shown or pushed. Use it when a page, a component, a style or a print layout changed, when Louis asks how something looks, and after a deploy of such a change. It holds how to run a site locally, how to look at exactly 1920 by 1080 and at 390 px wide, how to print a real PDF, and the look at the live page.
---

# Check a page

A test can pass on a page that looks wrong, so a change a visitor sees is looked at. Five stops come first.

1. **Look before you show the step, and before you ask for the push.** Louis, 2026-11-08: "can't you test it
   locally first, in the browser". The deploy is never the check.
2. **Restart the local site after a build.** An old run still answers 200 and still sends a page. But a fresh
   browser can show Blazor's error bar on it, and a print of it came out as one page where there are four.
3. **Never load the site inside one of its own pages in Louis's Chrome.** Chrome crashed twice during such checks,
   and a crash takes his whole browser down while he works in it.
4. **No real key and no real mail on a local run.** Start it with `RESEND_API_KEY=not-a-real-key` and a made-up
   `ADMIN_API_KEY`. Never type a real key into a page.
5. **Say what you did not look at.** A size, a page or a browser that was not checked is named when the step is
   shown. It is Louis's to look at.

## Run it locally

| Site | Command | Address |
|---|---|---|
| The framework's site | `dotnet run --project src/site/louis-agent.site-api --launch-profile site` | `http://127.0.0.1:5083` |
| Code review | `dotnet run --project demos/louis-agent/hosts/louis-agent.coding-api --launch-profile api` | `http://127.0.0.1:5081` |

Start it in the background, with the two settings of stop 4 before the command, and wait for its `Now listening`
line.

## What to look for

- **At 390 px wide**: no sideways scroll, and the menu wrapped.
- **At 1920 by 1080**: every page and every section. Louis, 2026-11-09: "the agent should page through each step
  remember that when checking 1080p". A section that pages sideways is gone through page by page, 1 of 3, 2 of 3,
  3 of 3, and not only its first.
- **Nothing cut off, and nothing on top of something else.**
- **No error in the browser's console.**

## Three ways to look

### Louis's Chrome, for clicking through

The browser tools drive his Chrome. Use it to click, to page through a section and to run the guide. Open a new
tab, and leave his tabs alone.

Its window does not change size while it is maximised. So it is not a check at an exact size: say which size it
was looked at.

The guide's walkthrough of a page is part of the look at 1920 by 1080, since it must page through each step of a
paged section too. It runs the guide, which is a model call: run it, and report what it cost.

### Headless Chrome, for an exact size

A second Chrome with no window, which touches nobody's browser. It takes a picture of the first screen of an
address. Every section has its own address, so a section further down is shot at its own.

The site is shown inside a frame of exactly the size wanted, held by `frame.html` beside this file. The frame is
what makes the size exact, so the same line serves the desktop and the phone.

```bash
CH="/c/Program Files/Google/Chrome/Application/chrome.exe"
OUT="<a folder in the scratchpad>"
python -m http.server 5098 --bind 127.0.0.1 --directory .claude/skills/check-a-page    # in the background

# Desktop: the page at exactly 1920 by 1080.
"$CH" --headless=new --disable-gpu --user-data-dir="$OUT/profile" --window-size=2000,1200 \
  --virtual-time-budget=25000 --screenshot="$OUT/desktop.png" \
  "http://127.0.0.1:5098/frame.html?w=1920&h=1080&u=http://127.0.0.1:5083/demos"

# Phone: the page at exactly 390 wide.
"$CH" --headless=new --disable-gpu --user-data-dir="$OUT/profile" --window-size=600,1000 \
  --virtual-time-budget=25000 --screenshot="$OUT/phone.png" \
  "http://127.0.0.1:5098/frame.html?w=390&h=844&u=http://127.0.0.1:5083/demos"
```

Then read each picture. The site is its top left corner, on grey.

- **Start a check with a new `$OUT/profile`, and keep it for that check.** One kept from before a build serves
  the old stylesheet, so delete it first.
- **Run a line once to warm the profile, and take the picture on the second run.** A cold run can catch the
  site on its loading screen, `BOOTING LOUIS AGENT`. Three of twelve cold runs did, and none of seven warm
  ones.
- **Look at every picture.** One that shows the loading screen is taken again.
- **The window is only asked to be larger than the frame.** A window's size is not the page's size. Asked for 390
  wide, this Chrome lays a page out at 500. Asked for 1920 by 1080, it lays it out at 1904 by 985. The picture is
  the window's size either way, so it hides the difference.
- **The frame's scrollbar is inside the frame**, so the page gets a little less than the width asked for, as it
  does in a desktop browser's window.
- **`frame.html` works only over http.** Opened as a file, or written into the address, the site inside it never
  gets past its loading screen.

### Headless Chrome, for print

A page that is printed, or saved as a PDF, is checked by a real print.

```bash
"$CH" --headless=new --disable-gpu --user-data-dir="$OUT/profile" --virtual-time-budget=25000 \
  --print-to-pdf="$OUT/page.pdf" http://127.0.0.1:5083/cv
```

Start with a new `$OUT/profile` here too. Then read the PDF: count its pages and its links, and look at each
page. A print that is one page where more were expected was made before the app had drawn: restart the site
if it has been built since it started, and print again.

The live site can't be printed this way. Over a real network headless Chrome prints before the app has started.
A print of the live page is Louis's, from his own print dialog, and so is scanning a QR code with a phone.

## After a deploy

`gh run list --workflow deploy-puls.yml --limit 1` shows the deploy that followed the merge. When it has
succeeded, open the changed page on `https://louis-agent.dev` in a new tab of Louis's Chrome and look at it.
