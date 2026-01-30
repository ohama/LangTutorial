module Lexer

open System
open FSharp.Text.Lexing
open Parser  // Import token types from generated Parser module/// Rule tokenize
val tokenize: lexbuf: LexBuffer<char> -> token
