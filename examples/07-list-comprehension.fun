// List Comprehension Patterns
// Common list manipulation using map, filter, fold

let range = fun low -> fun high ->
    let rec helper i = fun acc ->
        if i < low
        then acc
        else helper (i - 1) (i :: acc)
    in
    helper high []
in

let rec zip xs = fun ys ->
    match xs with
    | [] -> []
    | x :: xrest ->
        match ys with
        | [] -> []
        | y :: yrest -> (x, y) :: zip xrest yrest
in

let sum = fun xs -> fold (fun acc -> fun x -> acc + x) 0 xs in
let product = fun xs -> fold (fun acc -> fun x -> acc * x) 1 xs in

let maximum = fun xs ->
    match xs with
    | [] -> 0
    | h :: t -> fold (fun acc -> fun x -> if x > acc then x else acc) h t
in

let flatten = fun xss ->
    fold (fun acc -> fun xs -> append acc xs) [] xss
in

let concatMap = fun f -> fun xs ->
    flatten (map f xs)
in

let cartesian = fun xs -> fun ys ->
    concatMap (fun x -> map (fun y -> (x, y)) ys) xs
in

let isEven = fun x -> x / 2 * 2 = x in

let nums = range 1 10 in
let evens = filter isEven nums in
let squares = map (fun x -> x * x) nums in
let total = sum nums in
let prod5 = product (range 1 5) in
let cart = cartesian [1, 2] [10, 20] in
let maxNum = maximum nums in

(evens, squares, total, prod5, cart, maxNum)
