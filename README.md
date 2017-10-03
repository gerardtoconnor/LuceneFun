# LuceneFun
F# Wrapper around Lucene &amp; AzureDirectory to persist data

**Experimental & Testing only, not to be used in production**

The library is a small basic wrapper around Lucene & AzureDiretory that allows use of local Lucene index as NoSql database with data synced & persisted to azure storage. 

On wiping of machine (webapp), the index is automatically pulled back in again from (cheap) azure storage and continues where left off.

### Helper Functions use:

[AzureDirectory](https://github.com/azure-contrib/AzureDirectory)

[Lucence.net](https://lucenenet.apache.org/)

All saved json items/documents have common base shape with json data on "data" property:

```json
{
    id:"{###}",
    jType:"{user set type of data (shape name)}",
    data:"{json submitted added here to data}"
    created:"{DateTime int64}"
}
```

# Example usage

```fsharp
let sdir v = __SOURCE_DIRECTORY__ + v
let odir v = sdir (@"\AppData\outputs\" + v )

let connString = "DefaultEndpointsProtocol=https;AccountName={accountname};AccountKey={accountkey};BlobEndpoint=https://{accountname}.blob.core.windows.net/;TableEndpoint=https://{accountname}.table.core.windows.net/;QueueEndpoint=https://{accountname}.queue.core.windows.net/;FileEndpoint=https://{accountname}.file.core.windows.net/"

let ap = new AzurePersist(connString)
let cust1 = ap.Catalog("customer1") // gets customer1 catalog
let assetIndex = cust1.Index("assets") // access' / creates seachable flexible document store

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
assetIndex.SearchToJsonStream q1 s1 // saves q1 search to s1 file

let q = "created:>2016-03-10"
let s2 = File.OpenWrite(odir "date_search.json")
assetIndex.SearchToJsonStream q s2  // saves q search to s2 file

//
assetIndex.FieldData "data.apps"

//test get all document function, returns (and saves) json array of all docs
let astream = File.OpenWrite(odir "all docs.json")
assetIndex.GetAllJsonStream astream

//test get all document function, returns (and saves) json array of all docs
assetIndex.GetAllJsonString () |> JsonDoc.PrettyJson


//test get all document function, returns (and saves) json array of all docs
assetIndex.SearchToJsonString "data.chairs:comfortable" |> JsonDoc.PrettyJson
```
