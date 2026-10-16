# Files Skills

Guidance for the `files` toolset: reading, writing and managing files in the workspace.

## Tools

### Reading & Searching
- `ReadWorkspaceFile(path)` — Read a UTF-8 text file (max 1 MB)
- `ReadTable(path, sheet, startRow, maxRows)` — A spreadsheet or CSV as a table, a page of rows at a time, with its size and sheets
- `ReadDocument(path)` — Read a PDF, Word document (.docx) or spreadsheet (.xlsx, .xls, .csv) as text, with page and sheet markers; long ones come in parts (`startIndex`). Scanned PDFs have no text (no OCR)
- `ListWorkspaceFiles(relativePath)` — List files and directories in a folder
- `ListDirectoryTree(relativePath, maxDepth)` — Show directory tree structure (depth 1-5)
- `SearchWorkspace(query, maxResults)` — Search files for text patterns
- `FindFiles(pattern, searchPath)` — Find files by name pattern (e.g., `*.cs`, `test*.txt`)
- `PathExists(relativePath)` — Check if a file or directory exists

### Writing & Creating
- `WriteWorkspaceFile(path, content)` — Create or replace a UTF-8 text file
- `AppendToFile(path, content)` — Append text to an existing file (or create it)
- `CreateDirectory(relativePath)` — Create a new directory
- `CopyFile(sourcePath, destinationPath)` — Copy a file to a new location

**Large files: write in chunks.** Every reply has an output limit, and a tool call cut off by it is not run. For any
file over ~300 lines (full web pages, long docs, generated code):
1. `WriteWorkspaceFile(path, firstChunk)` with the first ~300 lines.
2. `AppendToFile(path, nextChunk)` with the next ~300 lines, one call per reply, and repeat until the file is complete.
3. Each chunk ends on a whole line, and the next chunk continues exactly where it stopped. Never put a whole large file
   in one call, and never route it through PythonRun, BashRun or PowerShellRun to get around this.

If a tool result says a call was cut off by the output limit, switch to chunks immediately instead of retrying or
trying another tool.

### File Management
- `DeleteFile(relativePath)` — Delete a file
- `DeleteDirectory(relativePath)` — Delete a directory and all its contents
- `RenameFileOrDirectory(currentPath, newPath)` — Rename files or directories
- `GetFileInfo(relativePath)` — Get file size, creation/modification dates, attributes

## Rules

- Stay inside the workspace root. Don't reach outside that boundary.
- Secrets stay secret (.env, .pem, credentials). Don't read or write them. Templates such as `.env.example` hold no secrets and can be read.
- File operations are bounded: 1 MB reads, 500 files maximum.
