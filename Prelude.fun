(* FunLang Standard Library (Prelude)

   This file contains the standard library functions for FunLang.
   It is automatically loaded when the interpreter starts.

   Functions defined:
   - map, filter, fold (list higher-order functions)
   - length, reverse, append (list utilities)
   - hd, tl (list accessors)
   - id, const, compose (combinators)
*)

let rec map f = fun xs ->
    match xs with
    | [] -> []
    | h :: t -> (f h) :: (map f t)
in

let rec filter pred = fun xs ->
    match xs with
    | [] -> []
    | h :: t -> if pred h then h :: (filter pred t) else filter pred t
in

let rec fold f = fun acc -> fun xs ->
    match xs with
    | [] -> acc
    | h :: t -> fold f (f acc h) t
in

let rec length xs =
    match xs with
    | [] -> 0
    | _ :: t -> 1 + (length t)
in

let reverse = fun xs ->
    let rec rev_acc acc = fun ys ->
        match ys with
        | [] -> acc
        | h :: t -> rev_acc (h :: acc) t
    in
    rev_acc [] xs
in

let rec append xs = fun ys ->
    match xs with
    | [] -> ys
    | h :: t -> h :: (append t ys)
in

let id = fun x -> x
in

let const = fun x -> fun y -> x
in

let compose = fun f -> fun g -> fun x -> f (g x)
in

let hd = fun xs ->
    match xs with
    | h :: _ -> h
in

let tl = fun xs ->
    match xs with
    | _ :: t -> t
in

0
