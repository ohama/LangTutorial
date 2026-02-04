// Quicksort Implementation
// Average case: O(n log n), Worst case: O(n^2)

let rec quicksort xs =
    match xs with
    | [] -> []
    | pivot :: rest ->
        let less = filter (fun x -> x < pivot) rest in
        let greater = filter (fun x -> x >= pivot) rest in
        append (quicksort less) (pivot :: quicksort greater)
in

quicksort [5, 2, 8, 1, 9, 3, 7, 4, 6]
