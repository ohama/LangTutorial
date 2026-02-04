// Insertion Sort Implementation
// Time complexity: O(n^2) average, O(n) best case

let rec insert x = fun sorted ->
    match sorted with
    | [] -> [x]
    | h :: t ->
        if x <= h
        then x :: sorted
        else h :: insert x t
in

let rec insertionSort xs =
    match xs with
    | [] -> []
    | h :: t -> insert h (insertionSort t)
in

insertionSort [64, 34, 25, 12, 22, 11, 90]
