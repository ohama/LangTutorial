# Phase 5: Functions & Abstraction - Research

**Researched:** 2026-01-30
**Domain:** Functional language interpreter - functions, closures, recursion
**Confidence:** HIGH

## Summary

Phase 5 adds first-class functions to the FunLang interpreter, enabling function definition, application, closures (environment capture), and recursion including mutual recursion. This research investigated standard patterns for implementing lambda calculus semantics in tree-walking interpreters using F#.

The standard approach uses discriminated unions to extend the Value type with a FunctionValue case that captures three components: parameter name, function body (as Expr), and the lexical environment at definition time. Function application creates a new environment extending the captured closure environment (not the call-site environment), binding the parameter to the argument value. Recursive functions require special handling through `let rec` which uses environment mutation or delayed closure construction.

Key architectural decisions: (1) Closures must capture the definition-time environment to implement lexical scoping correctly, (2) Each function call gets its own environment to support recursion, (3) The `let rec` construct requires either mutable environment references or a two-phase binding process to handle self-references before the function value exists.

**Primary recommendation:** Extend the Value discriminated union with a FunctionValue case containing parameter name, body expression, and captured environment; implement function application by creating a fresh environment with the closure's environment as parent, binding parameters to arguments.

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| FsLexYacc | 11.3.0 | Lexer/parser generation | Already in use, handles function syntax well |
| F# Pattern Matching | Built-in | AST traversal and value matching | Idiomatic F# for discriminated unions |
| F# Map | Built-in | Immutable environment representation | Standard functional data structure for variable bindings |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Expecto | Current | Testing framework | Already in use for Phase 4, excellent for property-based closure tests |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Immutable Map environment | Mutable Dictionary | Mutable state simplifies `let rec` but breaks functional style and testing clarity |
| Environment-based eval | Substitution-based eval | Substitution avoids closure complexity but has exponential performance for nested functions |
| Discriminated union Value | Object-oriented class hierarchy | OOP would work but discriminated unions provide exhaustive pattern matching guarantees |

**Installation:**
No new packages required. FsLexYacc 11.3.0 already installed.

## Architecture Patterns

### Recommended Project Structure
Current structure remains optimal:
```
FunLang/
├── Ast.fs           # Add Lambda, App, LetRec cases to Expr; FunctionValue to Value
├── Parser.fsy       # Add grammar rules for function syntax
├── Lexer.fsl        # Add FUN, ARROW tokens
├── Eval.fs          # Add function application and closure logic
├── Format.fs        # Add formatValue case for FunctionValue
└── Program.fs       # No changes needed
```

### Pattern 1: Value Type Extension with Closures

**What:** Extend the Value discriminated union to include function values that capture their definition environment.

**When to use:** This is the universal pattern for first-class functions in tree-walking interpreters.

**Example:**
```fsharp
// Source: Crafting Interpreters book + F# discriminated union patterns
// https://craftinginterpreters.com/functions.html
// https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions

type Value =
    | IntValue of int
    | BoolValue of bool
    | FunctionValue of param: string * body: Expr * closure: Env
```

**Key insight:** The closure field stores "the environment...that is active when the function is declared not when it's called" to implement lexical scoping correctly.

### Pattern 2: Environment Chain for Function Application

**What:** Create a new environment for each function call, with the closure environment as parent, not the call-site environment.

**When to use:** Every function application, including recursive and nested calls.

**Example:**
```fsharp
// Source: Crafting Interpreters patterns adapted to F#
// https://craftinginterpreters.com/functions.html

| App (func, arg) ->
    match eval env func with
    | FunctionValue (param, body, closureEnv) ->
        let argValue = eval env arg  // Evaluate in call-site env
        // Create new env extending CLOSURE env (not call-site env)
        let callEnv = Map.add param argValue closureEnv
        eval callEnv body
    | _ -> failwith "Type error: attempted to call non-function"
```

**Critical detail:** Using `closureEnv` as the base (not `env`) is what makes closures work. "The function's body out through the environments where the function is declared, all the way out to the global scope."

### Pattern 3: Let Rec with Environment Mutation

**What:** For recursive functions, bind the function name in the environment BEFORE constructing the closure, using a mutable reference or two-phase process.

**When to use:** Implementing `let rec` for self-referential and mutually recursive functions.

**Example:**
```fsharp
// Source: Principles of Programming Languages - Recursion and Mutation
// https://bguppl.github.io/interpreters/class_material/2.8RecursionMutation.html

// Option A: Mutable reference (simpler but impure)
| LetRec (name, param, body, inExpr) ->
    // Create placeholder reference
    let placeholder = ref (IntValue 0)  // temporary value
    // Extend environment with placeholder
    let recEnv = Map.add name (RefValue placeholder) env
    // Build function value in environment where name is bound
    let funcValue = FunctionValue (param, body, recEnv)
    // Update placeholder to actual function
    placeholder := funcValue
    // Evaluate body expression in recursive environment
    eval recEnv inExpr

// Option B: Delayed closure (pure but more complex)
// Store parameter and body without environment
// Construct full closure at lookup time using current env
```

**Warning:** This requires adding a RefValue case to Value type or using F#'s `ref` type. The mutation approach is pragmatic for educational interpreters.

### Pattern 4: Tail Recursion Detection (Future Enhancement)

**What:** F# pattern for recursive functions with accumulators to enable tail call optimization.

**When to use:** When implementing interpreter optimizations (not required for Phase 5).

**Example:**
```fsharp
// Source: F# Recursive Functions documentation
// https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/functions/recursive-functions-the-rec-keyword

let factorial n =
    let rec loop acc n =
        match n with
        | 0 -> acc
        | n -> loop (acc * n) (n - 1)  // Tail recursive
    loop 1 n
```

**Note:** This is how to write tail-recursive F# code in the interpreter implementation itself, not how to implement tail-call optimization in FunLang (which is out of scope for Phase 5).

### Anti-Patterns to Avoid

- **Using call-site environment as closure parent:** This implements dynamic scoping, not lexical scoping. Always use the captured closure environment.
- **Forgetting to capture environment in Lambda case:** Evaluating a lambda expression must return FunctionValue with the current env, not just the parameter and body.
- **Creating single shared environment for all calls:** Each invocation must get its own environment. "Each function call gets its own environment, otherwise recursion would break."
- **Allowing let (non-rec) to self-reference:** The name being bound must NOT be in scope on the right-hand side for regular `let`. Only `let rec` permits self-reference.

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Mutable references | Custom mutable box type | F# `ref` type | Built-in, well-tested, interoperates with F# semantics for closure capture |
| Environment lookup chains | Manual recursive search | Map with Map.tryFind | Efficient, immutable, standard library |
| Parser for lambda syntax | Complex precedence hacking | FsLexYacc grammar rules | Declarative, handles precedence correctly |
| Tail call optimization | Custom stack management | F# `[<TailCall>]` attribute | Compiler handles it for the interpreter implementation itself |

**Key insight:** The interpreter will not magically optimize user's FunLang tail-recursive code. However, writing the interpreter's own F# code in tail-recursive style prevents stack overflow in the interpreter when evaluating deeply recursive FunLang programs.

## Common Pitfalls

### Pitfall 1: Dynamic Scoping Leak

**What goes wrong:** Using the call-site environment instead of the closure environment when applying functions, causing variables to resolve in the caller's scope rather than the definition scope.

**Why it happens:** It's easier to implement - just use the current `env` parameter. The distinction between definition-time and call-time environments is subtle.

**How to avoid:** Always create the call environment by extending `closureEnv` (from the FunctionValue), never by extending the current `env`.

**Warning signs:** Closures don't work - inner functions can't access outer function parameters. Example that fails: `let f x = let g y = x + y in g` - if `g` is called elsewhere, it won't see `x`.

### Pitfall 2: Recursion Without Separate Environments

**What goes wrong:** Reusing the same environment for recursive calls causes parameters to overwrite each other, breaking recursion.

**Why it happens:** Trying to optimize by sharing environments, or misunderstanding that each call needs its own bindings.

**How to avoid:** Every function application creates a fresh `Map.add param argValue closureEnv` - a new map, not mutation of an existing one.

**Warning signs:** Factorial(5) works but Fibonacci(5) fails. Mutual recursion causes "stack overflow" or wrong values. Example: `fib(3)` tries to compute `fib(2)` and `fib(1)`, but the parameter `n` gets overwritten.

### Pitfall 3: Let Rec Forward Reference Problem

**What goes wrong:** Attempting to create a recursive function closure when the function name doesn't exist yet in the environment, causing "undefined variable" errors.

**Why it happens:** In `let rec f x = f (x-1)`, the body `f (x-1)` references `f`, but we're trying to create the value of `f`. Chicken-and-egg problem.

**How to avoid:** Use one of two approaches: (1) Mutable references - bind name to a ref, create function, update ref. (2) Delayed closure - store parameter/body without environment, construct at lookup time.

**Warning signs:** `let rec f x = f (x-1) in f 5` fails with "Undefined variable: f". Simple recursion fails while non-recursive functions work.

### Pitfall 4: Incorrect Arity Checking

**What goes wrong:** Forgetting to validate argument counts, allowing `(fun x -> x + 1) 2 3` to succeed incorrectly.

**Why it happens:** Phase 5 adds single-parameter functions initially; arity checking seems unnecessary.

**How to avoid:** For single-parameter functions, ensure exactly one argument. When extending to multi-parameter functions later, validate `arguments.length == parameters.length` before binding.

**Warning signs:** Extra arguments are silently ignored. Functions fail with cryptic errors when called with too few arguments.

### Pitfall 5: Forgetting to Evaluate Function Expression

**What goes wrong:** In function application `App(funcExpr, argExpr)`, trying to extract parameter/body directly from `funcExpr` without evaluating it first.

**Why it happens:** Confusion between AST nodes (Expr) and runtime values (Value).

**How to avoid:** Always evaluate both function and argument: `eval env funcExpr` must return a `FunctionValue`, then `eval env argExpr` to get the argument value.

**Warning signs:** Type errors when function expression is a variable or nested call. Example: `let f = fun x -> x in f 5` fails because `f` is a `Var`, not a `Lambda`.

## Code Examples

Verified patterns from official sources:

### Extending Value Type for Functions
```fsharp
// Source: F# Discriminated Unions documentation
// https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions

type Value =
    | IntValue of int
    | BoolValue of bool
    | FunctionValue of param: string * body: Expr * closure: Env

// Format function extension
let rec formatValue (v: Value) : string =
    match v with
    | IntValue n -> string n
    | BoolValue b -> if b then "true" else "false"
    | FunctionValue _ -> "<function>"  // Functions not directly printable
```

### Extending Expr Type for Functions
```fsharp
// Add to Ast.fs Expr type
type Expr =
    | Number of int
    | Bool of bool
    | Var of string
    | Let of string * Expr * Expr
    // ... existing cases ...
    | Lambda of param: string * body: Expr      // fun param -> body
    | App of func: Expr * arg: Expr             // func arg
    | LetRec of name: string * param: string * body: Expr * inExpr: Expr
    // let rec name param = body in inExpr
```

### Lambda Evaluation (Closure Creation)
```fsharp
// Source: Crafting Interpreters closure patterns
// https://craftinginterpreters.com/closures.html

| Lambda (param, body) ->
    // Capture current environment when lambda is defined
    FunctionValue (param, body, env)
```

### Function Application (Call)
```fsharp
// Source: Crafting Interpreters function call patterns
// https://craftinginterpreters.com/functions.html

| App (funcExpr, argExpr) ->
    // Evaluate function expression to get closure
    match eval env funcExpr with
    | FunctionValue (param, body, closureEnv) ->
        // Evaluate argument in current (call-site) environment
        let argValue = eval env argExpr
        // Create new environment extending CLOSURE environment
        let callEnv = Map.add param argValue closureEnv
        // Evaluate body in call environment
        eval callEnv body
    | _ -> failwith "Type error: attempted to call non-function"
```

### Let Rec Implementation with Mutation
```fsharp
// Source: Principles of Programming Languages - Recursion and Mutation
// https://bguppl.github.io/interpreters/class_material/2.8RecursionMutation.html

// First, extend Value type
type Value =
    | IntValue of int
    | BoolValue of bool
    | FunctionValue of param: string * body: Expr * closure: Env
    | RefValue of Value ref  // For mutable references

// Then implement LetRec
| LetRec (name, param, funcBody, inExpr) ->
    // Create placeholder reference
    let placeholder = ref (IntValue 0)
    // Extend environment with reference BEFORE creating function
    let recEnv = Map.add name (RefValue placeholder) env
    // Now create function in environment where name exists
    let funcValue = FunctionValue (param, funcBody, recEnv)
    // Update the reference to point to actual function
    placeholder := funcValue
    // Evaluate body in recursive environment
    eval recEnv inExpr

// Update Var case to dereference
| Var name ->
    match Map.tryFind name env with
    | Some (RefValue r) -> !r  // Dereference
    | Some value -> value
    | None -> failwithf "Undefined variable: %s" name
```

### FsLexYacc Grammar for Functions
```fsharp
// Lexer.fsl additions
// Source: FsLexYacc documentation
// https://github.com/fsprojects/FsLexYacc

rule tokenize = parse
    | "fun"         { FUN }
    | "rec"         { REC }
    | "->"          { ARROW }
    // ... existing rules ...

// Parser.fsy additions
%token FUN REC ARROW

Expr:
    | LET REC IDENT IDENT EQUALS Expr IN Expr
        { LetRec($3, $4, $6, $8) }  // let rec name param = body in expr
    | FUN IDENT ARROW Expr
        { Lambda($2, $4) }           // fun param -> body
    | Factor Factor
        { App($1, $2) }              // function application (left-associative)
    // ... existing rules ...
```

### Mutual Recursion with `and` Keyword
```fsharp
// Source: F# Recursive Functions documentation
// https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/functions/recursive-functions-the-rec-keyword

// FunLang syntax: let rec even x = if x = 0 then true else odd (x - 1)
//                 and odd x = if x = 0 then false else even (x - 1)
//                 in even 10

// AST representation
type Expr =
    | LetRecMutual of bindings: (string * string * Expr) list * body: Expr
    // Each binding: (name, param, funcBody)

// Evaluation (extends LetRec pattern)
| LetRecMutual (bindings, inExpr) ->
    // Create placeholder refs for all functions
    let placeholders =
        bindings
        |> List.map (fun (name, _, _) -> name, ref (IntValue 0))
        |> Map.ofList
    // Extend environment with all placeholders
    let recEnv =
        placeholders
        |> Map.fold (fun e name refVal -> Map.add name (RefValue refVal) e) env
    // Create all function values in mutual environment
    let funcValues =
        bindings
        |> List.map (fun (name, param, body) ->
            name, FunctionValue (param, body, recEnv))
    // Update all placeholders
    funcValues |> List.iter (fun (name, fval) ->
        let refVal = placeholders.[name]
        refVal := fval)
    // Evaluate body
    eval recEnv inExpr
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Substitution-based evaluation | Environment-based with closures | Standard since 1960s lambda calculus interpreters | Environment-based scales better, enables mutation, clearer separation of phases |
| Manual closure conversion | First-class functions with captured environments | Scheme (1975), ML (1973) | Simpler semantics, user-friendly, enables higher-order functions |
| Global scope only | Lexical scoping with environment chains | Algol 60 (1960) | Modularity, local reasoning, prevents action-at-a-distance bugs |
| Tail call optimization optional | Expected in functional languages | Scheme specification (1975) | Required for recursive patterns without stack overflow (but Phase 5 can defer this) |

**Deprecated/outdated:**
- **Dynamic scoping:** Used in early Lisp, now considered harmful. Variables should resolve in definition scope, not caller scope.
- **Single global environment:** Modern interpreters use environment chains. Global-only breaks nested functions.
- **Implicit recursion:** Modern languages require explicit markers (`rec` keyword, `letrec` form) to avoid accidental infinite loops and clarify intent.

## Open Questions

Things that couldn't be fully resolved:

1. **Multi-parameter functions vs currying**
   - What we know: FunLang requirements specify single-parameter syntax `let f x = ...`. Multi-parameter could be `let f x y = ...` (direct) or `let f = fun x -> fun y -> ...` (curried).
   - What's unclear: Which approach is more intuitive for Phase 5 users? Direct multi-param is simpler initially but currying is more compositional.
   - Recommendation: Implement single-parameter for Phase 5 as specified. Document that multi-parameter can be added later via currying or direct syntax extension.

2. **Tail call optimization implementation**
   - What we know: F# compiler optimizes tail calls in the interpreter's own code. FunLang programs don't automatically benefit.
   - What's unclear: Should Phase 5 implement TCO for FunLang programs? Requires trampoline or continuation-passing style, significant complexity.
   - Recommendation: Defer TCO to future optimization phase. Document that deeply recursive FunLang programs may stack overflow. Most tutorial examples will be shallow recursion.

3. **Error messages for type mismatches**
   - What we know: Function application will fail when non-function is called. Current pattern uses `failwith "Type error: ..."`.
   - What's unclear: How detailed should error messages be? Include source location, expected vs actual types?
   - Recommendation: Keep simple string messages for Phase 5. Source locations require parser changes (track line/column in AST). Add as later enhancement.

4. **Anonymous recursion (Y combinator)**
   - What we know: `let rec` provides named recursion. Anonymous recursion requires fixed-point combinators like Y combinator.
   - What's unclear: Should FunLang support this? It's theoretically elegant but practically confusing.
   - Recommendation: Not required. `let rec` covers practical use cases. Y combinator can be implemented as library code if desired, doesn't need language support.

## Sources

### Primary (HIGH confidence)
- [Crafting Interpreters - Functions](https://craftinginterpreters.com/functions.html) - Function implementation patterns, closure semantics, call environments
- [Crafting Interpreters - Closures](https://craftinginterpreters.com/closures.html) - Environment capture timing, dynamic vs static scoping pitfalls
- [Microsoft Learn - F# Discriminated Unions](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions) - Value type patterns, recursive unions, pattern matching
- [Microsoft Learn - F# Recursive Functions](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/functions/recursive-functions-the-rec-keyword) - `let rec` syntax, mutual recursion with `and`, tail recursion patterns
- [Microsoft Learn - F# Values](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/values/) - Function values, closure capture semantics

### Secondary (MEDIUM confidence)
- [Principles of Programming Languages - Recursion and Mutation](https://bguppl.github.io/interpreters/class_material/2.8RecursionMutation.html) - Let rec implementation approaches, Box-env pattern, rec-env pattern
- [FsLexYacc GitHub Documentation](https://github.com/fsprojects/FsLexYacc) - Parser generator syntax, token definitions
- [8th Light - Testing Recursion](https://8thlight.com/insights/testing-recursion) - Recursive function testing strategies
- [Stanford CS106B - Testing Recursive Functions](https://web.stanford.edu/class/archive/cs/cs106b/cs106b.1206/assignments/assign3/warmup) - Debugging recursive functions, stack overflow detection

### Tertiary (LOW confidence - for context)
- [F# for Fun and Profit - Pattern Matching](https://fsharpforfunandprofit.com/posts/match-expression/) - Pattern matching examples
- [Rosetta Code - Mutual Recursion](https://rosettacode.org/wiki/Mutual_recursion) - Cross-language mutual recursion examples
- [Wikipedia - Closure (computer programming)](https://en.wikipedia.org/wiki/Closure_(computer_programming)) - Closure definition and history
- [Wikipedia - Let expression](https://en.wikipedia.org/wiki/Let_expression) - Let and letrec semantics across languages

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - FsLexYacc already in use, F# built-ins well-documented
- Architecture patterns: HIGH - Crafting Interpreters is authoritative, F# docs are official, patterns verified across multiple sources
- Let rec implementation: MEDIUM - Multiple valid approaches exist, mutation vs pure trade-offs depend on project goals
- Pitfalls: HIGH - Well-documented in literature, common issues in interpreter courses
- Testing strategies: MEDIUM - General recursion testing is well-known, closure-specific testing less documented

**Research date:** 2026-01-30
**Valid until:** 2026-03-01 (30 days - stable domain, F# and interpreter patterns don't change rapidly)
