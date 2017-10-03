namespace AzurePersist

module IdEncoder =
    open System.Text
    // "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-_.~"
    let ALPHABET = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray()
    let BASE = ALPHABET.Length
    let ALPHABET_REVERSE = Map(seq { for i in 0.. BASE - 1 -> (ALPHABET.[i],i) })
    let SIGN_CHARACTER = '$'

    let rec numEncode(n:int) : string =
               
        if n < 0 then
            System.String([| SIGN_CHARACTER |]) + numEncode(-n)
        else
            let nb = n 

            let sb = StringBuilder(6)
            let rec buildStr (ls:char list) =
                match ls with
                | [] ->                            
                    sb.ToString().PadLeft(6,'0')
                | h :: t ->
                    sb.Append h |> ignore
                    buildStr t

            let rec test nv acc =
                let n',r = System.Math.DivRem(nv,BASE)
                let nacc = ALPHABET.[r] :: acc
                if n' = 0 then
                    buildStr nacc
                else
                    test n' nacc
            test nb []


    let rec numDecode (s:string) : int =
        if s.[0] = SIGN_CHARACTER then
            - numDecode(s.[1..])
        else
            let mutable n = 0
            for c in s do
                n <- n * BASE + ALPHABET_REVERSE.[c]
            //n
            n >>> 16 ||| n <<< 16

//numDecode("2LKcb1")
//numDecode("000002")
//
//numEncode(2000)
//numEncode(System.Int32.MaxValue)
