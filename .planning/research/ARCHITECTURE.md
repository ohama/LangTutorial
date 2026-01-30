# Architecture Research

## Overview

An interpreter built with fslex/fsyacc follows a classic multi-stage pipeline architecture. Each stage transforms the input from one representation to another, culminating in program execution. This document outlines the typical components, their responsibilities, data flow, and recommended structure for an F# interpreter implementation.

## Components

### 1. Lexer (Lexical Analyzer)
**File**: `Lexer.fsl`
**Generated Output**: `Lexer.fs`

**Responsibilities**:
- Converts raw source code (string) into a stream of tokens
- Performs lexical analysis using pattern matching (similar to regular expressions)
- Filters out whitespace and comments (typically)
- Reports lexical errors (invalid characters, malformed tokens)
- Uses fslex to generate F# code from .fsl specification

**Key Concepts**:
- Token: A categorized unit of the source code (e.g., NUMBER, IDENTIFIER, PLUS, LPAREN)
- Lexeme: The actual text matched by a token pattern (e.g., "42", "myVar", "+", "(")

### 2. Parser (Syntax Analyzer)
**File**: `Parser.fsy`
**Generated Output**: `Parser.fs` and `Parser.fsi`

**Responsibilities**:
- Consumes token stream from lexer
- Validates syntax according to formal grammar rules
- Constructs Abstract Syntax Tree (AST) from valid token sequences
- Reports syntax errors (unexpected tokens, malformed expressions)
- Uses fsyacc to generate F# code from .fsy specification

**Key Concepts**:
- Grammar rules: Define valid token sequences (e.g., expression, statement, declaration)
- Productions: Individual grammar rules that describe language constructs
- Parse tree: Hierarchical representation of token sequences (often simplified to AST)

### 3. Abstract Syntax Tree (AST)
**File**: `Ast.fs`

**Responsibilities**:
- Defines discriminated unions representing language constructs
- Captures program structure without syntax details (no whitespace, brackets, etc.)
- Serves as the primary data structure passed between parser and evaluator
- Should be immutable and purely data-focused

**Structure**:
```fsharp
// Example AST types
type Expr =
    | Number of int
    | Variable of string
    | BinOp of Expr * Operator * Expr
    | FunCall of string * Expr list

type Operator = Add | Sub | Mul | Div

type Statement =
    | Assignment of string * Expr
    | Print of Expr
    | IfThen of Expr * Statement list
```

**Key Concepts**:
- Nodes: Individual AST elements (expressions, statements, declarations)
- Tree structure: Parent-child relationships representing program structure
- Type safety: F# discriminated unions provide compile-time guarantees

### 4. Environment (Symbol Table / Context)
**File**: `Environment.fs`

**Responsibilities**:
- Manages variable bindings (name -> value mappings)
- Handles scope (local vs global variables, nested scopes)
- Stores function definitions if applicable
- May track type information for static analysis

**Common Implementations**:
- Simple: `Map<string, Value>` for single-scope languages
- Scoped: Stack of maps or persistent data structures for nested scopes
- Immutable: Return new environment on each binding (functional style)
- Mutable: Update in-place for performance (imperative style)

**Key Concepts**:
- Binding: Association between identifier and value
- Scope: Region where a binding is valid
- Shadowing: Inner scope binding hides outer scope binding

### 5. Evaluator (Interpreter Engine)
**File**: `Evaluator.fs`

**Responsibilities**:
- Traverses AST nodes and executes them
- Evaluates expressions to produce values
- Executes statements for their side effects
- Manages environment during execution
- Handles runtime errors (division by zero, undefined variables)

**Common Patterns**:
- Visitor pattern: Dispatch on AST node type
- Recursive evaluation: Evaluate sub-expressions before parent expressions
- Pattern matching: F# native support for discriminated unions

**Example Structure**:
```fsharp
let rec eval (env: Environment) (expr: Expr) : Value =
    match expr with
    | Number n -> IntValue n
    | Variable name -> Environment.lookup name env
    | BinOp (left, op, right) ->
        let leftVal = eval env left
        let rightVal = eval env right
        applyOperator op leftVal rightVal
    | FunCall (name, args) ->
        let argVals = List.map (eval env) args
        callFunction name argVals env
```

### 6. Value System
**File**: `Values.fs`

**Responsibilities**:
- Defines runtime value types (integers, booleans, strings, functions, etc.)
- Provides operations on values
- Handles type coercion if needed
- May include error values for runtime errors

**Example**:
```fsharp
type Value =
    | IntValue of int
    | BoolValue of bool
    | StringValue of string
    | FunctionValue of (Value list -> Value)
    | NullValue
```

### 7. REPL (Read-Eval-Print Loop) [Optional]
**File**: `Repl.fs` or `Main.fs`

**Responsibilities**:
- Provides interactive shell for the interpreter
- Reads user input line by line
- Parses and evaluates each input
- Prints results or error messages
- Maintains session state (environment across inputs)

**Pattern**:
```fsharp
let rec repl env =
    printf "> "
    let input = Console.ReadLine()
    match input with
    | null | "exit" -> ()
    | code ->
        try
            let tokens = Lexer.tokenize code
            let ast = Parser.parse tokens
            let result, newEnv = Evaluator.eval env ast
            printfn "%A" result
            repl newEnv
        with
        | ex ->
            printfn "Error: %s" ex.Message
            repl env
```

### 8. Error Handling
**File**: `Errors.fs`

**Responsibilities**:
- Defines custom exception types for different error categories
- Provides user-friendly error messages
- Tracks source location for errors (line, column)
- Distinguishes lexical, syntax, and runtime errors

**Example**:
```fsharp
type InterpreterError =
    | LexicalError of string * Position
    | SyntaxError of string * Position
    | RuntimeError of string * Position
    | UndefinedVariable of string
    | TypeMismatch of expected: string * actual: string

exception InterpreterException of InterpreterError
```

## Data Flow

The interpreter processes source code through the following pipeline:

```
Source Code (string)
    |
    v
[LEXER (Lexer.fsl)]
    |
    v
Token Stream (Token list)
    |
    v
[PARSER (Parser.fsy)]
    |
    v
Abstract Syntax Tree (AST)
    |
    v
[EVALUATOR (Evaluator.fs)]
    |
    v
Result Value + Updated Environment
    |
    v
Output (printed or returned)
```

### Detailed Flow:

1. **Source to Tokens (Lexing)**
   - Input: `"let x = 5 + 3"`
   - Output: `[LET; IDENTIFIER "x"; EQUALS; NUMBER 5; PLUS; NUMBER 3]`

2. **Tokens to AST (Parsing)**
   - Input: Token stream
   - Output: `Assignment("x", BinOp(Number 5, Add, Number 3))`

3. **AST to Value (Evaluation)**
   - Input: AST + Environment
   - Process:
     - Evaluate `Number 5` → `IntValue 5`
     - Evaluate `Number 3` → `IntValue 3`
     - Apply `Add` operator → `IntValue 8`
     - Bind `"x"` to `IntValue 8` in environment
   - Output: `IntValue 8` + Updated environment with `x=8`

### Error Flow:

Errors can occur at each stage and should be reported with context:

- **Lexical Error**: Invalid character → Report position in source
- **Syntax Error**: Unexpected token → Report expected vs actual
- **Runtime Error**: Undefined variable → Report variable name and usage location

## Component Dependencies

### Build Order (Bottom-Up):

The following order respects F# build constraints (files must be compiled in dependency order):

1. **Ast.fs** (or Types.fs)
   - Defines AST types
   - No dependencies
   - Used by: Parser, Evaluator

2. **Values.fs**
   - Defines runtime value types
   - No dependencies (or depends only on Ast.fs)
   - Used by: Evaluator, Environment

3. **Errors.fs**
   - Defines error types
   - May depend on: Ast.fs (for Position types)
   - Used by: All components

4. **Parser.fsy** → **Parser.fs/fsi** (generated)
   - Defines grammar and AST construction
   - Depends on: Ast.fs
   - Generates: Token type definitions
   - Used by: Lexer (for token types), Main

5. **Lexer.fsl** → **Lexer.fs** (generated)
   - Defines token patterns
   - Depends on: Parser.fs (for token types)
   - Used by: Main, REPL

6. **Environment.fs**
   - Manages variable bindings
   - Depends on: Values.fs
   - Used by: Evaluator

7. **Evaluator.fs**
   - Executes AST
   - Depends on: Ast.fs, Values.fs, Environment.fs, Errors.fs
   - Used by: Main, REPL

8. **Repl.fs** or **Main.fs**
   - Entry point
   - Depends on: All components
   - Top of dependency chain

### Critical Build Notes:

- **fsyacc must run before fslex**: The lexer needs token type definitions from the parser
- **Generated files must be included in project**: Parser.fs and Lexer.fs are source files
- **Build order matters in F#**: Files earlier in the list cannot reference files later in the list

### Dependency Graph:

```
        Main.fs / Repl.fs
              |
              v
         Evaluator.fs
         /    |     \
        /     |      \
       v      v       v
    Ast.fs  Values.fs Environment.fs
       ^      ^         ^
       |      |         |
       +------+---------+
              |
         Parser.fs (generated)
              ^
              |
         Parser.fsy
              ^
              |
         Lexer.fs (generated)
              ^
              |
         Lexer.fsl
```

## File Organization

### Recommended Project Structure:

```
InterpreterProject/
├── src/
│   ├── Core/
│   │   ├── Ast.fs              # AST type definitions
│   │   ├── Values.fs           # Runtime value types
│   │   └── Errors.fs           # Error types and exceptions
│   │
│   ├── Parsing/
│   │   ├── Parser.fsy          # Parser specification (fsyacc)
│   │   ├── Lexer.fsl           # Lexer specification (fslex)
│   │   ├── Parser.fs           # Generated by fsyacc (git-ignore or commit)
│   │   └── Lexer.fs            # Generated by fslex (git-ignore or commit)
│   │
│   ├── Runtime/
│   │   ├── Environment.fs      # Variable bindings and scope
│   │   └── Evaluator.fs        # AST evaluation logic
│   │
│   └── Interactive/
│       ├── Repl.fs             # REPL implementation
│       └── Main.fs             # Entry point (CLI or REPL)
│
├── tests/
│   ├── LexerTests.fs
│   ├── ParserTests.fs
│   └── EvaluatorTests.fs
│
├── examples/
│   ├── hello.lang              # Example programs
│   └── fibonacci.lang
│
├── docs/
│   └── tutorial/               # Tutorial chapters
│       ├── 01-lexer.md
│       ├── 02-parser.md
│       └── 03-evaluator.md
│
├── InterpreterProject.fsproj   # F# project file
└── README.md
```

### Alternative: Flat Structure (Simpler for Tutorials):

```
InterpreterProject/
├── Ast.fs                      # AST definitions
├── Values.fs                   # Value types
├── Errors.fs                   # Error handling
├── Parser.fsy                  # Parser spec
├── Lexer.fsl                   # Lexer spec
├── Environment.fs              # Environment/scope
├── Evaluator.fs                # Interpreter
├── Repl.fs                     # REPL
├── Main.fs                     # Entry point
├── InterpreterProject.fsproj
└── README.md
```

### F# Project File Considerations:

The .fsproj file must list files in dependency order:

```xml
<ItemGroup>
  <Compile Include="Ast.fs" />
  <Compile Include="Values.fs" />
  <Compile Include="Errors.fs" />
  <FsYacc Include="Parser.fsy">
    <OtherFlags>--module Parser</OtherFlags>
  </FsYacc>
  <Compile Include="Parser.fsi" />
  <Compile Include="Parser.fs" />
  <FsLex Include="Lexer.fsl">
    <OtherFlags>--unicode</OtherFlags>
  </FsLex>
  <Compile Include="Lexer.fs" />
  <Compile Include="Environment.fs" />
  <Compile Include="Evaluator.fs" />
  <Compile Include="Repl.fs" />
  <Compile Include="Main.fs" />
</ItemGroup>
```

## Suggested Build Order for Tutorial

Given the teaching context, here's a recommended chapter progression:

### Phase 1: Foundation (Chapters 1-3)
1. **Chapter 1: AST Definition**
   - Create Ast.fs with simple expression types
   - No dependencies, students see the target structure first
   - Example: Number, BinOp (Add, Sub, Mul, Div)

2. **Chapter 2: Lexer**
   - Create Lexer.fsl with token definitions
   - Introduce fslex tool and token concept
   - Test: "2 + 3" → [NUMBER 2; PLUS; NUMBER 3]

3. **Chapter 3: Parser**
   - Create Parser.fsy with grammar rules
   - Introduce fsyacc and grammar concepts
   - Test: Tokens → AST (BinOp structure)

### Phase 2: Execution (Chapters 4-5)
4. **Chapter 4: Values and Simple Evaluator**
   - Create Values.fs
   - Create basic Evaluator.fs for arithmetic
   - Test: Evaluate AST to produce integer results

5. **Chapter 5: Variables and Environment**
   - Add variable expressions to AST
   - Create Environment.fs
   - Extend evaluator for variable lookup
   - Test: "let x = 5" then "x + 3" → 8

### Phase 3: Enhancement (Chapters 6-8)
6. **Chapter 6: Control Flow**
   - Add if/else to AST
   - Add boolean values
   - Extend evaluator for conditionals

7. **Chapter 7: Functions**
   - Add function definitions and calls to AST
   - Add function values
   - Implement function evaluation with scope

8. **Chapter 8: Error Handling and REPL**
   - Create Errors.fs
   - Add error handling throughout
   - Create Repl.fs for interactive use

### Dependencies Between Chapters:

- Chapters 1-3 are sequential (parser needs AST, generates tokens for lexer)
- Chapter 4 depends on chapters 1-3
- Chapter 5 depends on chapter 4
- Chapters 6-7 extend chapters 4-5 incrementally
- Chapter 8 integrates everything

## Design Patterns and Best Practices

### 1. Immutability
- Keep AST immutable (F# discriminated unions)
- Consider immutable environments (return new environment on updates)
- Benefits: Easier to reason about, safer for concurrent evaluation

### 2. Separation of Concerns
- Lexer handles only tokenization (no parsing logic)
- Parser handles only syntax (no evaluation logic)
- Evaluator handles only execution (no parsing logic)
- Clear boundaries make testing and maintenance easier

### 3. Error Recovery
- Lexer: Continue after lexical errors when possible
- Parser: Provide informative syntax error messages
- Evaluator: Catch runtime errors and provide context

### 4. Extensibility
- Use discriminated unions for easy pattern matching
- Add new AST nodes without breaking existing code
- Use visitor pattern (match expressions) for traversal

### 5. Testing Strategy
- Unit test each component independently
- Integration tests for full pipeline
- Test error cases as thoroughly as success cases

### 6. Visitor Pattern for Traversal
F# pattern matching provides natural visitor pattern implementation:

```fsharp
let rec visitExpr visitor expr =
    match expr with
    | Number n -> visitor.visitNumber n
    | BinOp (l, op, r) ->
        let left = visitExpr visitor l
        let right = visitExpr visitor r
        visitor.visitBinOp left op right
    | Variable name -> visitor.visitVariable name
```

This pattern works for:
- Evaluation (compute values)
- Pretty printing (generate strings)
- Type checking (infer/check types)
- Optimization (transform AST)

## Common Pitfalls and Solutions

### 1. Build Order Issues
**Problem**: Parser.fs doesn't exist when Lexer.fsl tries to reference token types
**Solution**: Run fsyacc before fslex, ensure build order in .fsproj

### 2. Circular Dependencies
**Problem**: Lexer needs Parser tokens, Parser needs Lexer for testing
**Solution**: Parser generates token types, Lexer imports them (unidirectional)

### 3. Mutable State in Functional Code
**Problem**: Global mutable environment breaks functional paradigm
**Solution**: Thread environment through evaluation functions, return updated environment

### 4. Poor Error Messages
**Problem**: "Syntax error" without context is unhelpful
**Solution**: Include position information, expected vs actual tokens, suggestions

### 5. Mixing Concerns
**Problem**: Parser contains evaluation logic
**Solution**: Parser only builds AST, evaluator handles execution

## References and Further Reading

### FsLex/FsYacc Specific:
- [F# Lexing and Parsing - Wikibooks](https://en.wikibooks.org/wiki/F_Sharp_Programming/Lexing_and_Parsing)
- [FsLexYacc Official Documentation](https://fsprojects.github.io/FsLexYacc/)
- [Using FSLexYacc Tutorial](https://thanos.codes/blog/using-fslexyacc-the-fsharp-lexer-and-parser/)
- [Parsing with fslex and fsyacc - Tim Robinson](https://www.partario.com/blog/2009/05/lisp-compiler-in-f-parsing-with-fslex-and-fsyacc.html)
- [Building JSON parser with FsLex and FsYacc](https://github.com/fsprojects/FsLexYacc/blob/master/docs/content/jsonParserExample.md)
- [Lexing and Parsing with F# - Sergey Tihon](https://sergeytihon.com/2014/07/04/lexing-and-parsing-with-f-part-i/)

### General Interpreter Design:
- [Writing an Interpreter From Scratch - Toptal](https://www.toptal.com/scala/writing-an-interpreter)
- [Crafting Interpreters - Evaluating Expressions](https://craftinginterpreters.com/evaluating-expressions.html)
- [Let's Build A Simple Interpreter - Ruslan Spivak](https://ruslanspivak.com/lsbasi-part7/)
- [Understanding Interpreter Architecture Guide](https://diversedaily.com/understanding-interpreter-architecture-a-comprehensive-guide/)

### F# REPL and Patterns:
- [F# Interactive Reference - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/fsharp/tools/fsharp-interactive/)
- [Using REPL - F# by Example](https://camilotk.github.io/fsharp-by-example/chapters/using-repl/)
- [Interpreter Design Pattern - GeeksforGeeks](https://www.geeksforgeeks.org/interpreter-design-pattern/)

### Practical Examples:
- [FsLexYacc GitHub Repository](https://github.com/fsprojects/FsLexYacc)
- [Functional Arithmetic Evaluator in F# and C#](https://github.com/matteobortolazzo/FunctionalArithmeticEvaluator)

## Summary

An fslex/fsyacc-based F# interpreter consists of several well-defined components working in a pipeline:

1. **Lexer** (tokenization) → 2. **Parser** (AST construction) → 3. **Evaluator** (execution)

Supporting components include **AST definitions**, **Value types**, **Environment** (for state), and **Error handling**.

The key to a successful implementation is:
- Respecting build order (Parser before Lexer in generation)
- Maintaining clear separation of concerns
- Using F#'s type system for safety (discriminated unions, pattern matching)
- Providing good error messages with source location context
- Building incrementally (simple expressions first, then variables, then control flow)

For a tutorial context, start with the simplest possible language (arithmetic expressions) and build up chapter by chapter, adding one feature at a time. Each chapter should have clear input/output examples and test cases.
