# Phase 4: Prelude (표준 라이브러리) - Research

**Researched:** 2026-02-01
**Domain:** Standard library implementation, prelude loading, recursive list functions
**Confidence:** HIGH

## Summary

A prelude (standard library) provides commonly used functions that are automatically available without explicit import. For FunLang, Phase 4 implements essential list processing functions (map, filter, fold), utility functions (id, const, compose), and list operations (hd, tl, length, reverse, append) in FunLang source code, then automatically loads this file on interpreter startup.

The standard approach follows functional language interpreters (Haskell, OCaml, Scheme): write the prelude in the language itself (demonstrating the language's expressive power), parse and evaluate it into an initial environment, then use that environment for subsequent user code. This "bootstrap" pattern has three key benefits: (1) prelude functions are debuggable FunLang code, (2) no special-casing in the evaluator, (3) demonstrates the language is powerful enough to implement its own standard library.

Implementation complexity is low - FunLang already has all required features (recursive functions, pattern matching, lists). The main architectural decision is where to load the prelude: modify `Repl.startRepl` and `Program.main` to create a "prelude environment" instead of `emptyEnv`, or add a `loadPrelude` function that augments any environment. Both approaches work; the former is simpler and matches how GHCi/Python REPL work.

**Primary recommendation:** Write Prelude.fun with recursive implementations of all required functions, then modify Repl.fs and Program.fs to parse and evaluate Prelude.fun into an initial environment on startup. Use tail-recursive implementations where appropriate (fold, reverse) to avoid stack overflow on large lists.

## Standard Stack

The established tools for implementing prelude/stdlib in interpreters:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| F# (compiler) | .NET 10 | Host language with List module | F#'s List.map/fold/filter guide correct implementations |
| FsLexYacc | 11.3.0 | Parser | Already parses recursive functions and pattern matching needed for prelude |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| System.IO | .NET 10 | File reading | Read Prelude.fun from disk on startup |
| Expecto | 5.1+ | Unit testing | Test individual prelude functions |
| fslit | N/A | Integration testing | End-to-end prelude availability tests |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| File-based prelude | Built-in F# functions | Built-in functions are faster but hide implementation from users; file-based demonstrates language capability |
| Parse/eval on startup | Compile to F# code | Compilation is faster but requires separate build step; parse/eval is simpler and more flexible |
| Single large file | Module system | Modules enable organization but add complexity; single file is sufficient for v3.0 scope |

**Installation:**
No new dependencies required - existing stack sufficient.

**File Location:**
`Prelude.fun` in project root (next to FunLang executable) or embed as resource.

## Architecture Patterns

### Recommended Project Structure
```
LangTutorial/
├── Prelude.fun              # Standard library in FunLang source
├── FunLang/
│   ├── Eval.fs              # Already has recursive function support
│   ├── Repl.fs              # Modify to load prelude
│   └── Program.fs           # Modify to load prelude
└── tests/
    └── prelude/             # NEW: Tests for prelude functions
        ├── map.fun
        ├── filter.fun
        └── fold.fun
```

### Pattern 1: Prelude Loading on Startup

**What:** Parse and evaluate Prelude.fun into initial environment before user code runs
**When to use:** All interpreter modes (REPL, file evaluation, expression evaluation)
**Example:**
```fsharp
// Source: Python initialization, GHCi prelude loading
// In Repl.fs or separate Prelude.fs module

module Prelude

open System.IO
open FSharp.Text.Lexing
open Ast
open Eval

/// Parse a string into an AST
let private parse (input: string) : Expr =
    let lexbuf = LexBuffer<char>.FromString input
    Parser.start Lexer.tokenize lexbuf

/// Load prelude from file and return environment with bindings
let loadPrelude () : Env =
    let preludePath = "Prelude.fun"
    if File.Exists preludePath then
        try
            let source = File.ReadAllText preludePath
            let ast = parse source
            // Evaluate prelude, extracting bindings into environment
            // Prelude structure: let x = ... in let y = ... in ()
            // Or use special eval variant that collects bindings
            evalToEnv emptyEnv ast
        with ex ->
            eprintfn "Warning: Failed to load Prelude.fun: %s" ex.Message
            emptyEnv
    else
        eprintfn "Warning: Prelude.fun not found, starting with empty environment"
        emptyEnv

/// Evaluate expressions that define bindings into an environment
/// Handles: let x = ... in let y = ... in <final expr>
let rec evalToEnv (env: Env) (expr: Expr) : Env =
    match expr with
    | Let (name, binding, body) ->
        let value = eval env binding
        let extendedEnv = Map.add name value env
        evalToEnv extendedEnv body  // Continue collecting bindings
    | LetRec (name, param, funcBody, inExpr) ->
        let funcVal = FunctionValue (param, funcBody, env)
        let recEnv = Map.add name funcVal env
        evalToEnv recEnv inExpr
    | _ ->
        // Final expression (typically unit or dummy value)
        env  // Return accumulated environment
```

**Then modify Repl.fs:**
```fsharp
// In Repl.fs
let startRepl () : int =
    printfn "FunLang REPL"
    printfn "Type '#quit' or Ctrl+D to quit."
    printfn ""
    let initialEnv = Prelude.loadPrelude()  // Changed from emptyEnv
    replLoop initialEnv
    0
```

**And Program.fs for file/expression evaluation:**
```fsharp
// In Program.fs
let main argv =
    // ... existing Argu parsing ...

    // For --expr and file evaluation modes:
    let initialEnv = Prelude.loadPrelude()

    // Example for --expr:
    elif results.Contains Expr then
        let expr = results.GetResult Expr
        try
            let ast = parse expr
            let result = eval initialEnv ast  // Use initialEnv, not emptyEnv
            printfn "%s" (formatValue result)
            0
        with ex ->
            eprintfn "Error: %s" ex.Message
            1
```

### Pattern 2: Recursive List Functions in FunLang

**What:** Implement map, filter, fold using pattern matching on lists
**When to use:** Prelude.fun implementations
**Example:**
```
// Source: ML/F# List module + Cornell CS312 course materials
// In Prelude.fun

// map : ('a -> 'b) -> 'a list -> 'b list
let rec map = fun f -> fun xs ->
    match xs with
    | [] -> []
    | h :: t -> (f h) :: (map f t)
in

// filter : ('a -> bool) -> 'a list -> 'a list
let rec filter = fun pred -> fun xs ->
    match xs with
    | [] -> []
    | h :: t ->
        if pred h then
            h :: (filter pred t)
        else
            filter pred t
in

// fold : ('acc -> 'a -> 'acc) -> 'acc -> 'a list -> 'acc
// Left-associative fold (tail recursive)
let rec fold = fun f -> fun acc -> fun xs ->
    match xs with
    | [] -> acc
    | h :: t -> fold f (f acc h) t
in

// hd : 'a list -> 'a
let hd = fun xs ->
    match xs with
    | h :: _ -> h
    | [] -> () // Error: FunLang has no exceptions yet
in

// tl : 'a list -> 'a list
let tl = fun xs ->
    match xs with
    | _ :: t -> t
    | [] -> []
in

// length : 'a list -> int
let rec length = fun xs ->
    match xs with
    | [] -> 0
    | _ :: t -> 1 + (length t)
in

// reverse : 'a list -> 'a list
// Tail-recursive implementation using accumulator
let reverse = fun xs ->
    let rec rev_helper = fun acc -> fun ys ->
        match ys with
        | [] -> acc
        | h :: t -> rev_helper (h :: acc) t
    in
    rev_helper [] xs
in

// append : 'a list -> 'a list -> 'a list
let rec append = fun xs -> fun ys ->
    match xs with
    | [] -> ys
    | h :: t -> h :: (append t ys)
in

// id : 'a -> 'a
let id = fun x -> x
in

// const : 'a -> 'b -> 'a
let const = fun x -> fun _ -> x
in

// compose : ('b -> 'c) -> ('a -> 'b) -> ('a -> 'c)
let compose = fun f -> fun g -> fun x -> f (g x)
in

()  // Final expression (unit-like value, discarded)
```

**Note:** FunLang doesn't have built-in exceptions, so `hd []` will cause a match failure. Document as runtime error.

### Pattern 3: Tail-Recursive Accumulator Pattern

**What:** Use helper function with accumulator for tail recursion
**When to use:** Functions that process entire list (reverse, fold)
**Example:**
```
// Source: F# List module implementation patterns
// Tail-recursive reverse

let reverse = fun xs ->
    // Helper function with accumulator
    let rec rev_helper = fun acc -> fun ys ->
        match ys with
        | [] -> acc
        | h :: t -> rev_helper (h :: acc) t
    in
    rev_helper [] xs
in
```

**Why tail-recursive:** Prevents stack overflow on large lists. F# compiler optimizes tail calls to iteration.

### Pattern 4: evalToEnv Helper for Environment Collection

**What:** Collect let bindings into environment without evaluating final expression
**When to use:** Loading prelude file which is structured as nested let-in expressions
**Example:**
```fsharp
// Source: Custom pattern for FunLang
// In Eval.fs or Prelude.fs

/// Evaluate nested let-in expressions, accumulating bindings
let rec evalToEnv (env: Env) (expr: Expr) : Env =
    match expr with
    | Let (name, binding, body) ->
        let value = eval env binding
        let extendedEnv = Map.add name value env
        evalToEnv extendedEnv body
    | LetRec (name, param, funcBody, inExpr) ->
        let funcVal = FunctionValue (param, funcBody, env)
        let recEnv = Map.add name funcVal env
        evalToEnv recEnv inExpr
    | _ ->
        // Base case: return environment, ignore final expression
        env
```

**Alternative:** Evaluate to Value, use side-effect to collect bindings (less functional).

### Anti-Patterns to Avoid

- **Don't hard-code prelude functions in F#** - Defeats the purpose of demonstrating language capability; write in FunLang
- **Don't skip error handling for missing Prelude.fun** - Interpreter should warn but continue with empty environment
- **Don't use non-tail-recursive fold** - foldr requires stack proportional to list length; foldl is tail-recursive
- **Don't ignore evaluation order** - Prelude.fun must define functions in dependency order (no forward references without rec)
- **Don't cache parsed prelude globally** - Parse on each interpreter start for simplicity; caching adds state management

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Prelude file location search | Custom path resolution | System.IO.File.Exists with fallback | File I/O already handles relative/absolute paths, no need for custom resolver |
| Currying syntax | Manual nested lambdas | FunLang's existing `fun x -> fun y ->` | Multi-parameter functions require currying in FunLang; explicit nesting is clear |
| Memoization | Custom caching layer | Standard recursive implementation | Prelude functions are stateless; memoization adds complexity without benefit for v3.0 |

**Key insight:** The prelude is source code that gets parsed and evaluated - treat it like any other FunLang program. Don't add special-case logic in the interpreter; use existing eval infrastructure.

## Common Pitfalls

### Pitfall 1: Non-Tail-Recursive Functions Overflow

**What goes wrong:** `length [1..10000]` causes stack overflow
**Why it happens:** Non-tail-recursive `1 + (length t)` builds stack frames proportional to list length
**How to avoid:** Use tail-recursive accumulator pattern for length, or document stack depth limitation
**Warning signs:** Tests with large lists (>1000 elements) crash

**Mitigation:** For v3.0, document that FunLang has stack depth limits. Tail-recursive versions can be added later.

### Pitfall 2: Prelude.fun Parse Errors Silent

**What goes wrong:** Syntax error in Prelude.fun causes silent fallback to empty environment
**Why it happens:** `loadPrelude` catches all exceptions, prints warning, returns emptyEnv
**How to avoid:** Add verbose mode flag to print full parse errors during development
**Warning signs:** Prelude functions not available but no error message

### Pitfall 3: Nested Let-In Structure Fragile

**What goes wrong:** Changing final expression from `()` to actual value breaks evalToEnv
**Why it happens:** evalToEnv returns environment from nested lets but ignores final expression
**How to avoid:** Document Prelude.fun structure requirement, add comment explaining `()` is discarded
**Warning signs:** Prelude.fun refactoring breaks loading

### Pitfall 4: Function Dependency Order

**What goes wrong:** Using `map` inside `filter` definition before `map` is defined
**Why it happens:** FunLang evaluates let-bindings sequentially; no mutual recursion across lets
**How to avoid:** Define functions in dependency order, or use single `let rec ... and ... and ...` if mutual recursion needed
**Warning signs:** "Undefined variable: map" error when loading prelude

### Pitfall 5: Head/Tail on Empty List Errors

**What goes wrong:** `hd []` raises match failure with unclear error message
**Why it happens:** FunLang has no exception mechanism; pattern match failure is generic error
**How to avoid:** Document preconditions in comments, accept runtime errors for v3.0
**Warning signs:** User confusion about cryptic "Match failure" errors

## Code Examples

Verified patterns from official sources:

### Complete Prelude.fun
```
// Prelude.fun - FunLang Standard Library
// Automatically loaded on interpreter startup

// Higher-order list functions
// ===========================

// map : ('a -> 'b) -> 'a list -> 'b list
// Apply function to each element
let rec map = fun f -> fun xs ->
    match xs with
    | [] -> []
    | h :: t -> (f h) :: (map f t)
in

// filter : ('a -> bool) -> 'a list -> 'a list
// Select elements satisfying predicate
let rec filter = fun pred -> fun xs ->
    match xs with
    | [] -> []
    | h :: t ->
        if pred h then h :: (filter pred t)
        else filter pred t
in

// fold : ('acc -> 'a -> 'acc) -> 'acc -> 'a list -> 'acc
// Left-associative fold (tail recursive)
let rec fold = fun f -> fun acc -> fun xs ->
    match xs with
    | [] -> acc
    | h :: t -> fold f (f acc h) t
in

// Basic list operations
// =====================

// hd : 'a list -> 'a
// Get first element (errors on empty list)
let hd = fun xs ->
    match xs with
    | h :: _ -> h
in

// tl : 'a list -> 'a list
// Get all but first element (errors on empty list)
let tl = fun xs ->
    match xs with
    | _ :: t -> t
in

// length : 'a list -> int
// Count elements in list
let rec length = fun xs ->
    match xs with
    | [] -> 0
    | _ :: t -> 1 + (length t)
in

// reverse : 'a list -> 'a list
// Reverse element order (tail recursive)
let reverse = fun xs ->
    let rec rev_acc = fun acc -> fun ys ->
        match ys with
        | [] -> acc
        | h :: t -> rev_acc (h :: acc) t
    in
    rev_acc [] xs
in

// append : 'a list -> 'a list -> 'a list
// Concatenate two lists
let rec append = fun xs -> fun ys ->
    match xs with
    | [] -> ys
    | h :: t -> h :: (append t ys)
in

// Utility functions
// =================

// id : 'a -> 'a
// Identity function
let id = fun x -> x
in

// const : 'a -> 'b -> 'a
// Constant function (returns first arg, ignores second)
let const = fun x -> fun _ -> x
in

// compose : ('b -> 'c) -> ('a -> 'b) -> ('a -> 'c)
// Function composition (f . g) x = f(g(x))
let compose = fun f -> fun g -> fun x -> f (g x)
in

()  // Final expression (discarded by evalToEnv)
```

### Prelude Loading Module
```fsharp
// Source: Pattern synthesis from Python/GHCi initialization
// In FunLang/Prelude.fs (new file)

module Prelude

open System.IO
open FSharp.Text.Lexing
open Ast
open Eval

/// Parse string into AST
let private parse (input: string) : Expr =
    let lexbuf = LexBuffer<char>.FromString input
    Parser.start Lexer.tokenize lexbuf

/// Evaluate nested let-in expressions into environment
let rec private evalToEnv (env: Env) (expr: Expr) : Env =
    match expr with
    | Let (name, binding, body) ->
        let value = eval env binding
        let extendedEnv = Map.add name value env
        evalToEnv extendedEnv body
    | LetRec (name, param, funcBody, inExpr) ->
        let funcVal = FunctionValue (param, funcBody, env)
        let recEnv = Map.add name funcVal env
        evalToEnv recEnv inExpr
    | _ ->
        // Final expression - return accumulated environment
        env

/// Load Prelude.fun and return initial environment
let loadPrelude () : Env =
    let preludePath = "Prelude.fun"
    if File.Exists preludePath then
        try
            let source = File.ReadAllText preludePath
            let ast = parse source
            evalToEnv emptyEnv ast
        with ex ->
            eprintfn "Warning: Failed to load %s: %s" preludePath ex.Message
            eprintfn "Starting with empty environment."
            emptyEnv
    else
        eprintfn "Warning: %s not found, starting with empty environment" preludePath
        emptyEnv
```

### Modified Repl.fs
```fsharp
// Source: Existing Repl.fs + prelude initialization
// In FunLang/Repl.fs

module Repl

open System
open FSharp.Text.Lexing
open Ast
open Eval

/// Parse a string input and return the AST
let private parse (input: string) : Expr =
    let lexbuf = LexBuffer<char>.FromString input
    Parser.start Lexer.tokenize lexbuf

/// REPL loop with environment threading
let rec private replLoop (env: Env) : unit =
    Console.Write "funlang> "
    Console.Out.Flush()

    match Console.ReadLine() with
    | null -> printfn ""  // EOF
    | "#quit" -> ()       // Explicit quit
    | "" -> replLoop env  // Empty line
    | line ->
        try
            let ast = parse line
            let result = eval env ast
            printfn "%s" (formatValue result)
            replLoop env
        with ex ->
            eprintfn "Error: %s" ex.Message
            replLoop env

/// Start the REPL with prelude loaded
let startRepl () : int =
    printfn "FunLang REPL"
    printfn "Type '#quit' or Ctrl+D to quit."
    printfn ""
    let initialEnv = Prelude.loadPrelude()  // CHANGED: load prelude
    replLoop initialEnv
    0
```

### Test Example (fslit format)
```
// Test: Prelude map function
// RUN: dotnet run --project FunLang -- -e %s
map (fun x -> x * 2) [1, 2, 3]
// CHECK: [2, 4, 6]
```

```
// Test: Prelude filter function
// RUN: dotnet run --project FunLang -- -e %s
filter (fun x -> x > 1) [1, 2, 3]
// CHECK: [2, 3]
```

```
// Test: Prelude fold function
// RUN: dotnet run --project FunLang -- -e %s
fold (fun a -> fun b -> a + b) 0 [1, 2, 3]
// CHECK: 6
```

```
// Test: Prelude compose function
// RUN: dotnet run --project FunLang -- -e %s
let double = fun x -> x * 2 in
let succ = fun x -> x + 1 in
(compose double succ) 5
// CHECK: 12
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Built-in primitives (C/Assembly) | Self-hosting stdlib | 1960s (Lisp eval in Lisp) | Demonstrates language power, easier debugging |
| Manual environment construction | Parse/eval stdlib file | 1970s (Scheme) | Stdlib is editable source, not hard-coded |
| Eager prelude loading | Lazy/on-demand imports | 2000s (Python 3, ES6 modules) | Faster startup, smaller namespace; FunLang uses eager for simplicity |
| Global mutable environment | Immutable environment threading | Modern FP languages | Thread-safe, easier to reason about |

**Deprecated/outdated:**
- Writing stdlib in host language (C, F#) - Modern interpreters prefer self-hosting where possible
- Single global namespace - Modern languages use modules; FunLang v3.0 uses flat namespace as baseline

## Open Questions

Things that couldn't be fully resolved:

1. **Prelude.fun file location**
   - What we know: Could be in project root, in FunLang/ dir, or embedded as resource
   - What's unclear: Best practice for packaged/distributed interpreter (dotnet tool install)
   - Recommendation: Start with project root for development; embed as resource for distribution (post-v3.0)

2. **Error handling for hd/tl on empty lists**
   - What we know: FunLang has no exception mechanism yet
   - What's unclear: Whether to add special error messages or accept generic match failure
   - Recommendation: Accept runtime match failure for v3.0; add exceptions in future phase if needed

3. **Performance of non-tail-recursive functions**
   - What we know: F# optimizes tail calls, but stack depth limits still exist
   - What's unclear: Acceptable stack depth for tutorial language (1000? 10000?)
   - Recommendation: Document limitation, provide tail-recursive versions in examples

4. **Extending prelude in user code**
   - What we know: Users might want to add their own utility functions
   - What's unclear: Pattern for extending prelude vs. defining in each file
   - Recommendation: Keep prelude minimal for v3.0; add module system in future for user extensions

## Sources

### Primary (HIGH confidence)
- [F# List Module Documentation](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-collections-listmodule.html) - Official F# Core, signatures and semantics
- [Haskell Prelude](https://www.haskell.org/onlinereport/standard-prelude.html) - Standard Prelude definitions, type signatures
- [Cornell CS312 - Lists, Map, Fold and Tail Recursion](https://www.cs.cornell.edu/courses/cs312/2006sp/recitations/rec04.html) - ML implementations
- [Cornell CS312 - Folding and Tail Recursion](https://www.cs.cornell.edu/courses/cs312/2008sp/recitations/rec05.html) - Implementation patterns
- Existing FunLang codebase (Eval.fs, Program.fs, Repl.fs) - Current evaluation infrastructure

### Secondary (MEDIUM confidence)
- [GHCi User Guide](https://downloads.haskell.org/ghc/latest/docs/users_guide/ghci.html) - Prelude loading behavior
- [Python Interpreter Initialization](https://docs.python.org/3/tutorial/interpreter.html) - Stdlib loading pattern (updated 2026-01-30)
- [OCaml Stdlib Documentation](https://ocaml.org/api/List.html) - List module reference
- [Function Composition in Functional Programming](https://dev.to/biomathcode/composition-of-functions-178g) - Compose/pipe implementations
- [Wikipedia: Function Composition](https://en.wikipedia.org/wiki/Function_composition_(computer_science)) - Theoretical background

### Tertiary (LOW confidence)
- [Yaegi (Go Interpreter)](https://github.com/traefik/yaegi) - Example of Use(stdlib.Symbols) pattern
- [Writing an Interpreter (Toptal)](https://www.toptal.com/developers/scala/writing-an-interpreter) - General interpreter design

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - No new dependencies, System.IO for file reading is standard .NET
- Architecture: HIGH - F# List module provides reference implementations, pattern matching already working
- Pitfalls: MEDIUM - Tail recursion and file loading are well-understood, but FunLang-specific interactions need validation

**Research date:** 2026-02-01
**Valid until:** ~2026-03-01 (30 days - stable domain, fundamental CS patterns unlikely to change)
