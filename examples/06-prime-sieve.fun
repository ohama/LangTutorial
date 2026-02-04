// Prime Number Sieve (Sieve of Eratosthenes)

let rec range low = fun high ->
    if low > high
    then []
    else low :: range (low + 1) high
in

let divisibleBy = fun p -> fun x ->
    x / p * p = x
in

let rec removeMultiples p = fun xs ->
    match xs with
    | [] -> []
    | h :: t ->
        if h = p
        then h :: removeMultiples p t
        else if divisibleBy p h
        then removeMultiples p t
        else h :: removeMultiples p t
in

let rec sieve xs =
    match xs with
    | [] -> []
    | p :: rest -> p :: sieve (removeMultiples p rest)
in

let primesUpTo = fun n ->
    sieve (range 2 n)
in

let primes50 = primesUpTo 50 in
let count = length primes50 in

(primes50, count)
