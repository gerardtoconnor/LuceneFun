namespace AzurePersist

open Lucene.Net.Store.Azure
open Lucene.Net.Store
open System.Collections.Generic
open Newtonsoft.Json
open System.IO
open System.Text
open Lucene.Net.Search
open Lucene.Net.Index
open Lucene.Net.Analysis.Standard
open Lucene.Net.QueryParsers
open Lucene.Net
open Microsoft.WindowsAzure.Storage
//open Microsoft.WindowsAzure.Storage.Blob

//module JStream =
//    let fold fn iacc (stream:Stream) =
//        let sr = new StreamReader(stream)
//        let reader = new JsonTextReader(sr)
//        let rec loop acc =
//            if reader.Read() then
//                let nacc = fn acc reader
//                loop nacc
//            else
//                acc
//        loop iacc

type ValType =
    | IntegerT  = 0uy
    | DecimalT  = 1uy
    | DateT     = 2uy
    | TextT     = 3uy
    | IdStringT = 4uy
    | BooleanT  = 5uy

type PropertyDef() =
    member val Name = "" with get,set
    member val ValType = ValType.TextT with get,set
    member val Retired = false with get,set
    member val DisplayOrder = 0 with get,set

//type TypeDef() =
//    member val Name = "" with get,set
//    member val PropertyDefs = Dictionary<string,PropertyDef>() with get,set

/// <summary>
/// The base class of data persistance that holds cloud storage connection
/// </summary>
type AzurePersist(blobStorage:string) = 

    //for worker roles, create async pool factory of indexers using same storage account
    //let sucess,cloudStorageAccount = true , CloudStorageAccount.DevelopmentStorageAccount;
    let success,cloudStorageAccount = CloudStorageAccount.TryParse(blobStorage)
    do
        printfn "conn success %A" success //HACK: <<REMOVVE
 
        if not success then
            failwith "unable to connecto to cloud storage"

    let catalogStore = Dictionary<string,Catalog>()

    member __.Catalog catalogName = 
        if catalogStore.ContainsKey catalogName then
            catalogStore.[catalogName]
        else
            printfn "creating new catalog: %A" catalogName //HACK: <<REMOVVE
            let newCatalog = Catalog(catalogName, cloudStorageAccount)
            catalogStore.Add(catalogName,newCatalog)
            newCatalog