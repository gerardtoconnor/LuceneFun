namespace AzurePersist

open System.Collections.Generic
open Microsoft.WindowsAzure.Storage

/// <summary>
/// Catalog is a segregated area of storage functionality, provides seperation for multi-tenancy
/// </summary>
type Catalog(catalogName:string,cloudStorageAccount:CloudStorageAccount) =
    let indexes = Dictionary<string,Indexer>()
    let loggers = Dictionary<string,Logger>()
    //member val Directory = directory with get
    member __.Index indexName =
        if indexes.ContainsKey indexName then
            indexes.[indexName]
        else
            printfn "creating new inder called %s-%s-idx" catalogName indexName
            let nIndex = Indexer(catalogName + "-" + indexName + "-idx",cloudStorageAccount)
            indexes.Add(indexName,nIndex)
            nIndex

    member __.Log logName = 
        if loggers.ContainsKey logName then
            loggers.[logName]
        else
            let nLog = Logger(catalogName + "-" + logName + "-log",cloudStorageAccount)
            loggers.Add(logName,nLog)
            nLog

