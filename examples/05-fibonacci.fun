// Fibonacci Numbers - Multiple Implementations

let rec fibNaive n =
    if n <= 1
    then n
    else fibNaive (n - 1) + fibNaive (n - 2)
in

// Tail-recursive with accumulator (linear time)
let fib = fun n ->
    let rec fibHelper a = fun b -> fun count ->
        if count = 0
        then a
        else fibHelper b (a + b) (count - 1)
    in
    fibHelper 0 1 n
in

// Generate first n Fibonacci numbers
let fibList = fun n ->
    let rec helper i = fun acc ->
        if i < 0
        then acc
        else helper (i - 1) (fib i :: acc)
    in
    helper (n - 1) []
in

let first10 = fibList 10 in
let fib20 = fib 20 in

(first10, fib20)
