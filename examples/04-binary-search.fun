// Binary Search on Sorted List
// Returns the index of target, or -1 if not found

let rec nth xs = fun n ->
    match xs with
    | [] -> 0 - 1
    | h :: t ->
        if n = 0 then h
        else nth t (n - 1)
in

let rec binarySearchHelper xs = fun target -> fun low -> fun high ->
    if low > high
    then 0 - 1
    else
        let mid = (low + high) / 2 in
        let midVal = nth xs mid in
        if midVal = target
        then mid
        else if midVal < target
        then binarySearchHelper xs target (mid + 1) high
        else binarySearchHelper xs target low (mid - 1)
in

let binarySearch = fun xs -> fun target ->
    binarySearchHelper xs target 0 (length xs - 1)
in

let sorted = [1, 3, 5, 7, 9, 11, 13, 15, 17, 19] in
let find7 = binarySearch sorted 7 in
let find1 = binarySearch sorted 1 in
let find19 = binarySearch sorted 19 in
let find8 = binarySearch sorted 8 in

(find7, find1, find19, find8)
