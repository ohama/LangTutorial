// Merge Sort Implementation
// Time complexity: O(n log n) in all cases

let rec split xs =
    match xs with
    | [] -> ([], [])
    | x :: [] -> ([x], [])
    | a :: b :: rest ->
        let pair = split rest in
        match pair with
        | (left, right) -> (a :: left, b :: right)
in

let rec merge xs = fun ys ->
    match xs with
    | [] -> ys
    | x :: xrest ->
        match ys with
        | [] -> xs
        | y :: yrest ->
            if x <= y
            then x :: merge xrest ys
            else y :: merge xs yrest
in

let rec mergesort xs =
    match xs with
    | [] -> []
    | x :: [] -> [x]
    | _ ->
        let pair = split xs in
        match pair with
        | (left, right) -> merge (mergesort left) (mergesort right)
in

mergesort [38, 27, 43, 3, 9, 82, 10]
