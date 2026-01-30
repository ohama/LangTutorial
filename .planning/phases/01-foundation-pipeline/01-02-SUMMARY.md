---
phase: 01-foundation-pipeline
plan: 02
subsystem: compiler-pipeline
tags: [fsyacc, fslex, parser, lexer, code-generation]

# Dependency graph
requires:
  - phase: 01-01
    provides: FunLang project with Ast.fs defining Expr type
provides:
  - Parser.fsy specification generating Parser.fs with token types
  - Lexer.fsl specification generating Lexer.fs with tokenize function
  - Correct build order: Parser.fsy before Lexer.fsl
  - Working lexer/parser pipeline foundation
affects: [01-03, 02-01]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "FsYacc generates Parser.fs with token type definitions"
    - "FsLex opens Parser module to access token types"
    - "Build order ensures Parser generates before Lexer"

key-files:
  created:
    - FunLang/Parser.fsy
    - FunLang/Lexer.fsl
    - FunLang/Parser.fs (generated)
    - FunLang/Parser.fsi (generated)
    - FunLang/Lexer.fs (generated)
  modified:
    - FunLang/FunLang.fsproj

key-decisions:
  - "FsYacc before FsLex in build order (critical for token type access)"
  - "Generated files placed in source directory (FsLexYacc default)"
  - "Added FSharp.Text.Lexing namespace to Lexer.fsl for LexBuffer types"

patterns-established:
  - "Parser.fsy defines tokens with semantic values (%token <type> NAME)"
  - "Lexer.fsl opens Parser module to return token types"
  - "Minimal grammar proves pipeline (single number parsing)"

# Metrics
duration: 2.5min
completed: 2026-01-30
---

# Phase 1 Plan 2: Lexer & Parser Pipeline Summary

**Parser.fsy and Lexer.fsl specifications with FsYacc→FsLex build order, generating working lexer/parser for number literals**

## Performance

- **Duration:** 2.5 min
- **Started:** 2026-01-30T01:57:20Z
- **Completed:** 2026-01-30T02:00:11Z
- **Tasks:** 3
- **Files modified:** 4

## Accomplishments
- Created Parser.fsy with NUMBER token and minimal grammar
- Created Lexer.fsl with tokenize rule for number literals
- Configured correct build order ensuring Parser.fs generates before Lexer.fs compiles
- Verified FsLexYacc pipeline works end-to-end

## Task Commits

Each task was committed atomically:

1. **Task 1: Create Parser.fsy specification** - `7894e30` (feat)
2. **Task 2: Create Lexer.fsl specification** - `4812af0` (feat)
3. **Task 3: Configure .fsproj with correct build order** - `5fdc2cc` (feat)

## Files Created/Modified
- `FunLang/Parser.fsy` - Parser specification with NUMBER and EOF tokens, single grammar rule
- `FunLang/Lexer.fsl` - Lexer specification with digit pattern returning NUMBER token
- `FunLang/FunLang.fsproj` - Build configuration with FsYacc and FsLex tasks in correct order
- `FunLang/Parser.fs` - Generated parser implementation with token type definitions
- `FunLang/Parser.fsi` - Generated parser interface
- `FunLang/Lexer.fs` - Generated lexer implementation with tokenize function

## Decisions Made

**FsYacc before FsLex in build order**
- Critical: Lexer.fsl opens Parser module, so Parser.fs must exist before Lexer.fsl compiles
- Configured in .fsproj: FsYacc Include before FsLex Include

**Generated files location**
- FsLexYacc generates in source directory by default (not obj/)
- Adjusted .fsproj to reference generated files directly without IntermediateOutputPath

**Added FSharp.Text.Lexing namespace**
- Lexer.fsl needs open FSharp.Text.Lexing for LexBuffer type
- Without it, fslex compilation fails with "Lexing not defined" error

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] Added FSharp.Text.Lexing namespace to Lexer.fsl**
- **Found during:** Task 3 (Build verification)
- **Issue:** Lexer.fsl used Microsoft.FSharp.Text.Lexing.LexBuffer without opening namespace, causing fslex compilation error "Lexing is not defined"
- **Fix:** Added `open FSharp.Text.Lexing` to Lexer.fsl header, simplified LexBuffer references
- **Files modified:** FunLang/Lexer.fsl
- **Verification:** dotnet build succeeds, Lexer.fs generated correctly
- **Committed in:** 5fdc2cc (Task 3 commit)

**2. [Rule 2 - Missing Critical] Corrected generated file paths in .fsproj**
- **Found during:** Task 3 (Build verification)
- **Issue:** Plan expected FsLexYacc to generate files in $(IntermediateOutputPath)/obj/, but actual behavior generates in source directory
- **Fix:** Changed .fsproj to reference Parser.fs and Lexer.fs directly without IntermediateOutputPath variable
- **Files modified:** FunLang/FunLang.fsproj
- **Verification:** Build succeeds with files in correct location
- **Committed in:** 5fdc2cc (Task 3 commit)

---

**Total deviations:** 2 auto-fixed (2 missing critical for build success)
**Impact on plan:** Both fixes necessary to make FsLexYacc work correctly. Standard FsLexYacc behavior, not scope creep.

## Issues Encountered

**FsLexYacc file generation location**
- Expected: Generated files in obj/Debug/net10.0/
- Actual: Generated files in source directory (FunLang/)
- Resolution: This is standard FsLexYacc behavior. Updated .fsproj to match actual behavior.

**Namespace requirements in .fsl files**
- FsLex requires explicit namespace opens for FSharp.Text.Lexing
- Cannot use fully qualified names without open statement
- Resolution: Added open FSharp.Text.Lexing to Lexer.fsl header

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

**Ready for 01-03 (Main Program & CLI):**
- Parser.fs provides token types and start rule
- Lexer.fs provides tokenize function
- Build order established and working
- Can now integrate lexer/parser into Program.fs

**Key integration points:**
- LexBuffer creation from string input
- Calling Lexer.tokenize on LexBuffer
- Calling Parser.start with token stream
- Returns Ast.Expr (Number n)

**Blockers:** None

---
*Phase: 01-foundation-pipeline*
*Completed: 2026-01-30*
