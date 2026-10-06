# Code Review Skills

Skills for performing and managing code reviews.

**Note:** These are placeholder/example skills. For production code review, prefer using the native `code-review` skill or integrate with dedicated code review tools. These markdown skills are primarily useful for demonstrating skill execution patterns.

## Skill: AnalyzeChanges

- Description: Analyze code changes for issues and improvements
- Parameters:
  - `filePath` (optional): File path to analyze (default: all changed files)
  - `focusArea` (optional): Focus on specific issues (e.g., "security", "performance", "correctness")
- Execution:

```bash
# This is a placeholder skill
# Implementation delegates to code review analysis tools
echo "Analyzing code changes in $SKILL_ARG_filePath"
```

## Skill: CheckCompliance

- Description: Check code for compliance with project standards
- Parameters:
  - `standard` (optional): Compliance standard (e.g., "style", "tests", "documentation")
  - `strict` (optional): Enforce strict rules (true/false)
- Execution:

```bash
# This is a placeholder skill
# Implementation delegates to compliance checking tools
echo "Checking compliance against standard: $SKILL_ARG_standard"
```

## Skill: GenerateReview

- Description: Generate a code review summary
- Parameters:
  - `filePattern` (optional): File pattern to include (e.g., "*.cs")
  - `detailLevel` (optional): Detail level (brief, standard, detailed)
- Execution:

```bash
# This is a placeholder skill
# Implementation delegates to review generation tools
echo "Generating code review with detail level: $SKILL_ARG_detailLevel"
```
