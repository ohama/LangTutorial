// Recursive Functions with let rec
// FunLang uses let rec for recursion

let rec factorial n =
    if n <= 1
    then 1
    else n * factorial (n - 1)
in

let rec fibonacci n =
    if n <= 1
    then n
    else fibonacci (n - 1) + fibonacci (n - 2)
in

let rec sumList xs =
    match xs with
    | [] -> 0
    | h :: t -> h + sumList t
in

let rec gcd a = fun b ->
    if b = 0
    then a
    else gcd b (a - a / b * b)
in

let fact5 = factorial 5 in
let fact10 = factorial 10 in
let fib10 = fibonacci 10 in
let total = sumList [1, 2, 3, 4, 5] in
let gcd12_8 = gcd 12 8 in

(fact5, fact10, fib10, total, gcd12_8)
