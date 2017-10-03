namespace AzurePersist
//
//open System.Collections.Generic
//open Newtonsoft.Json
//open System.IO
//
//type ValType =
//    | IntegerT  = 0uy
//    | DecimalT  = 1uy
//    | DateT     = 2uy
//    | TextT     = 3uy
//    | IdStringT = 4uy
//    | BooleanT  = 5uy
//
//type ValContainer =
//    | Integer of int
//    | Decimal of float
//    | Date of System.DateTime
//    | Text of string
//    | IdString of string
//    | Boolean of bool
//
//type PropType(n:string,vt:ValType) =
//    new() = PropType("",ValType.TextT)
//    member val Name = n with get,set
//    member val ValType = vt with get,set
//
//type PropContainer(n:string,vt:ValContainer)  =
//    new() = PropContainer("",Text(""))
//    member val Name = n with get,set
//    member val ValContainer = vt with get,set
//
//type Schema(t:string,f:PropType []) =
//    new () = Schema("",[||])
//    member val Type = t with get,set 
//    member val Fields = f with get,set
//    
//type IContainerBase(id:string,t:string) =
//    new() = IContainerBase("","") 
//    member val Id = id with get,set
//    member val Type = t with get,set
//
//type IContainer() =
//    inherit IContainerBase()
//    member val Fields = Dictionary<string,ValContainer>() with get,set
//
//type JsonStream =
//    static member Deserialise<'T> (stream:Stream) =
//        use sr = new StreamReader(stream)
//        use reader = new JsonTextReader(sr)
//        let serializer = new JsonSerializer()
//        serializer.Deserialize<'T>(reader)
//    static member Serialise<'T> (stream:Stream,v:'T) =
//        let serializer = new JsonSerializer()
//        use writer = new StreamWriter(stream)
//        serializer.Serialize(writer,v)
//            



