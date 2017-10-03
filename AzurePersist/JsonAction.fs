namespace AzurePersist

open System
open System.Collections.Generic

type JResult =
    | Valid
    | Mapping of result:string * dest:string
    | Pending of string list
    | Error of string

type JValidate(fn:Func<obj [],bool>,dependencies:string [],msg:string) =    
    member x.Run (dict:Dictionary<string,obj>) =
        //prepare empty input array for func call
        let input = Array.zeroCreate<obj>(dependencies.Length)
        let rec loop ls index =
            //loop in decending to zero index
            if index > 0 then
                //check and see if dependency is in object dictionary
                if dict.ContainsKey dependencies.[index] then
                    //if in object dictionary then 
                    input.[index] <- dict.[dependencies.[index]]
                    loop ls (index - 1)
                else
                    loop (dependencies.[index] :: ls ) (index - 1)
            //end of dependencies list reached so no check if any pending required or can validation function execute
            else
                match ls with
                | [] -> if fn.Invoke(input) then Valid else Error(msg)    
                | _ -> Pending(ls)
        loop [] (dependencies.Length - 1)

type JMap(fn:Func<obj [],string>,dependencies:string [],dest:string) =
    member x.Run (dict:Dictionary<string,obj>) =
        let input = Array.zeroCreate<obj>(dependencies.Length)
        let rec loop ls index =
            if index > 0 then
                if dict.ContainsKey dependencies.[index] then
                    input.[index] <- dict.[dependencies.[index]]
                    loop ls (index - 1)
                else
                    loop (dependencies.[index] :: ls ) (index - 1)
            else
                match ls with
                | [] -> Mapping(fn.Invoke(input),dest) 
                | _ -> Pending(ls)
        loop [] (dependencies.Length - 1)