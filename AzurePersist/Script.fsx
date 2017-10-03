
#r @"..\..\AzurePersist\packages\Newtonsoft.Json.8.0.3\lib\net45\Newtonsoft.Json.dll"
#r @"..\..\AzurePersist\packages\WindowsAzure.Storage.7.0.0\lib\net40\Microsoft.WindowsAzure.Storage.dll"
#r @"..\..\AzurePersist\packages\Lucene.Net.3.0.3\lib\NET40\Lucene.Net.dll"
#r @"..\..\AzurePersist\packages\Microsoft.Azure.KeyVault.Core.1.0.0\lib\net40\Microsoft.Azure.KeyVault.Core.dll"
#r @"..\..\AzurePersist\packages\Lucene.Net.Store.Azure.3.0.5553.21100\lib\net45\Lucene.Net.Store.Azure.dll"

#load "JsonDoc.fs"
#load "Logger.fs"
#load "Cacher.fs"
#load "Indexer.fs"
#load "Catalog.fs"


#load "AzurePersist.fs"


open AzurePersist
open System.IO
open Microsoft.WindowsAzure.Storage

#time
// Define your library scripting code here

let sdir v = __SOURCE_DIRECTORY__ + v
let odir v = sdir (@"\AppData\outputs\" + v )

let connString = "DefaultEndpointsProtocol=https;AccountName={accountname};AccountKey={accountkey};BlobEndpoint=https://{accountname}.blob.core.windows.net/;TableEndpoint=https://{accountname}.table.core.windows.net/;QueueEndpoint=https://{accountname}.queue.core.windows.net/;FileEndpoint=https://{accountname}.file.core.windows.net/"

//let success,cloudStorageAccount = CloudStorageAccount.TryParse(connString)
//let blobClient = cloudStorageAccount.CreateCloudBlobClient()
//let container = blobClient.GetContainerReference("customer1-assets-idx")
//do container.CreateIfNotExists() |> ignore // |> not then failwith "unable to create container"
//container.ListBlobs()
//|> Seq.iter (fun b -> printfn "%A" b.Uri)

let ap = new AzurePersist(connString)
let cust1 = ap.Catalog("customer1") // gets customer1 catalog
let assetIndex = cust1.Index("assets") // access' / creates seachable flexible document store
//let healthLog = cust1.Log("health") // access / creates log store for non/slow-search data

//for testing we can use file streams on local disc, in web app we swap with request/response body stream

//save json files in folder into asset index
for fileName in Directory.GetFiles(sdir @"\AppData\") do
    File.OpenRead(fileName)
    |> assetIndex.SaveJson

//test get by id function to retrieve by id
let id = "XDFHIHDSIUH"
let stream = File.OpenWrite(odir "XDFHIHDSIUH.json")
assetIndex.GetIdJsonStream id stream

//test deep data property query
let query = "data.homescreen.color:blue"
let qstream = File.OpenWrite(odir "homescreen-search.json")
assetIndex.SearchToJsonStream query qstream

//test multiple results on common type
let jquery = "jType:workstation"
let jstream = File.OpenWrite(odir "jTypesearch.json")
assetIndex.SearchToJsonStream jquery jstream

//test array search 
let q1 = "data.citytype:quiet"
let s1 = "app_search.json" |> odir |> File.OpenWrite
assetIndex.SearchToJsonStream q1 s1

let q = "created:>2016-03-10"
let s2 = File.OpenWrite(odir "date_search.json")
assetIndex.SearchToJsonStream q s2

//
assetIndex.FieldData "data.apps"

//test get all document function, returns (and saves) json array of all docs
let astream = File.OpenWrite(odir "all docs.json")
assetIndex.GetAllJsonStream astream

//test get all document function, returns (and saves) json array of all docs
assetIndex.GetAllJsonString () |> JsonDoc.PrettyJson


//test get all document function, returns (and saves) json array of all docs
assetIndex.SearchToJsonString "data.chairs:comfortable" |> JsonDoc.PrettyJson
