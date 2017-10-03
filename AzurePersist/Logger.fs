namespace AzurePersist

open Newtonsoft.Json
open System.Text
open Microsoft.WindowsAzure.Storage
open System

/// <summary>
/// Logger is a key/value store for cloud persisting large volumes of non-indexed data
/// </summary>
type Logger(logKey:string,cloudStorageAccount:CloudStorageAccount) =
    //let logKey = catalogName + "-" + logName + "-log"
    let blobClient = cloudStorageAccount.CreateCloudBlobClient()
    let container = blobClient.GetContainerReference(logKey)
    do container.CreateIfNotExists() |> ignore // |> not then failwith "unable to create container"

    member x.GetId<'T> id = 
            async {
            let! json = x.GetIdJson id |> Async.AwaitTask
            return JsonConvert.DeserializeObject<'T>(json) } |> Async.StartAsTask

    member __.GetIdJson id =
        let blobRef = container.GetBlockBlobReference(id)
        blobRef.DownloadTextAsync()

    member __.GetAll<'T>() = 
        Seq.empty<string*'T>

    member __.GetAllJson() = //todo:redo with async stream writers
        let sb = StringBuilder().Append('[')
        for item in container.ListBlobs(null,false) do
            let blob = item :?> Microsoft.WindowsAzure.Storage.Blob.CloudBlockBlob
            sb.Append( blob.DownloadText() ).Append(",") |> ignore
        sb.Insert(sb.Length - 1,']').ToString()

    member __.SlowSearch (field,query) =
        NotImplementedException()
        "" //Seq.empty<DynamicTableEntity>
    
    member __.SlowSearchJson (field,query) =
        NotImplementedException()
        "[]"
    
    member __.Save<'T> id (doc:'T) =
        let json = JsonConvert.SerializeObject(doc)
        let blob = container.GetBlockBlobReference(id)
        blob.UploadTextAsync(json)
            
    member __.SaveJson id json =
        let blob = container.GetBlockBlobReference(id)
        blob.UploadTextAsync(json)