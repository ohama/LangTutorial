# FunLang File-Based Tests
# Run: make -C tests

.PHONY: all test cli emit-tokens emit-ast file-input variables control functions comments strings repl tuples lists pattern-matching build check clean

# Default: run all tests
all: test

# Run all tests (recursively finds all .flt files)
test:
	@cd .. && fslit tests/

# Run tests by category
cli:
	@cd .. && fslit tests/cli/

emit-tokens:
	@cd .. && fslit tests/emit-tokens/

emit-ast:
	@cd .. && fslit tests/emit-ast/

file-input:
	@cd .. && fslit tests/file-input/

variables:
	@cd .. && fslit tests/variables/

control:
	@cd .. && fslit tests/control/

functions:
	@cd .. && fslit tests/functions/

comments:
	@cd .. && fslit tests/comments/

strings:
	@cd .. && fslit tests/strings/

repl:
	@cd .. && fslit tests/repl/

tuples:
	@cd .. && fslit tests/tuples/

lists:
	@cd .. && fslit tests/lists/

pattern-matching:
	@cd .. && fslit tests/pattern-matching/

# Build before testing
build:
	@cd .. && dotnet build FunLang/FunLang.fsproj

# Build and test
check: build test

clean:
	@echo "Nothing to clean"
