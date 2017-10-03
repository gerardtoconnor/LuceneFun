namespace AzurePersist

open System.Collections.Generic
open Newtonsoft.Json

open System.Text

open Microsoft.WindowsAzure.Storage
//open Microsoft.WindowsAzure.Storage.Blob

/// <summary>
/// Dictionary cache that is saved to blob storage on updates and loads cloud data on startup
/// </summary>
type Cacher<'T>(cacheKey:string,cloudStorageAccount:CloudStorageAccount) =
    let cloud = Logger(cacheKey,cloudStorageAccount)
    let data = Dictionary<string,'T>()

    let init () = 
        for (id,doc) in cloud.GetAll<'T>() do
            data.Add(id,doc)

    do init () // initialise
    
    member __.GetId<'T> id =
        data.[id]

    member __.GetIdJson id =
        JsonConvert.SerializeObject(data.[id])
    
    member __.GetAll() = 
        seq { for kvp in data -> (kvp.Key,kvp.Value) }
    
    member __.GetAllJson() =
        let sb = StringBuilder().Append('[')
        for kvp in data do 
            sb.AppendFormat("{id:{0},value:{1}},",kvp.Key,kvp.Value) |> ignore
        sb.Insert(sb.Length - 1,']').ToString()

    member __.Save id doc =
        data.Add(id,doc)
        cloud.Save id doc

    member __.SaveJson id json =
        data.Add(id,JsonConvert.DeserializeObject<'T>(json))
        cloud.SaveJson id json
