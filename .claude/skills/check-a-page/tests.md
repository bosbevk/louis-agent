# Test jobs for check-a-page

Three jobs to try this skill on, in a fresh session that did not write it (`/write-a-skill`, step 7). Each ask is
said the way Louis says it. The skill passes a job when the session loads it from the ask alone, everything in
*Must happen* happens, and nothing in *Must not* does.

| # | Where, and what Louis says | Must happen | Must not |
|---|---|---|---|
| 1 | A fresh cloud thread, on a branch that changed a heading's size on the framework's own site. He says: "check how the How it works page looks" | The framework's own site is the one started, `LouisAgent.SiteFrameworkApi` on port 5085, with made-up keys. Playwright opens the page at 1920 by 1080 and at 390 wide, in light and in dark, after the loading screen has left. If Bootstrap's host is refused, a local copy is handed to the page before any picture counts. Every picture is read, and what was not looked at is said | louis-agent.dev's site on 5083 looked at in its place. A page with no grid reported as a broken layout. A real key. The live site taken as the check |
| 2 | Rider on Windows, after a build that changed the CV's print styles. He says: "does the cv still print on four pages" | The local site is restarted first, since it was built after it started. A real print with headless Chrome, on a new profile. The PDF is read: its pages counted, and each page looked at | A print from the run that started before the build. A print of the live site. An answer from the stylesheet alone |
| 3 | A cloud thread, after a merge that changed the footer. He says: "it's deployed, have a look" | `gh run list --workflow deploy-sites.yml --limit 1` is read first, and the look waits for a deploy that succeeded. The changed page is opened on the site it belongs to. Where the container can't reach the live site, that is said, and the look is made in a browser on his computer or named as his | A look before the deploy finished. The local site passed off as the live one. Silence about a page that could not be reached |
