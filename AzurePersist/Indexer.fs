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
open Lucene.Net.Documents

type SimpleCollector() =    
    inherit Collector()
    let docIds = HashSet<int>()
    let mutable docBase = 0
           
    override x.SetScorer (_:Scorer) =
        ()
    override x.Collect(doc:int) =
        docIds.Add(docBase + doc) |> ignore
    override x.SetNextReader( _:IndexReader, baseint:int ) = 
        docBase <- baseint
    override x.AcceptsDocsOutOfOrder 
        with get() = true
    member x.DocIds 
        with get() = docIds

type Indexer(indexKey:string,cloudStorageAccount:CloudStorageAccount) =
    let dirBase = __SOURCE_DIRECTORY__ + @"\AppData\"
    //let dirBase = @"C:\Users\Gerard\Dropbox\Visual Studio 2012\Projects\AzurePersist\AzurePersist\AppData\"
    do if not( Directory.Exists(dirBase + indexKey) ) then Directory.CreateDirectory(dirBase + indexKey) |> ignore
    let directory = new AzureDirectory(cloudStorageAccount, indexKey,FSDirectory.Open(dirBase + indexKey ))

    //let types = Cacher<PropertyDef []>(indexKey + "-types",cloudStorageAccount)

    let reader = Lazy(fun ()-> IndexReader.Open(directory,true))
    let searcher = Lazy(fun ()-> new IndexSearcher(reader.Value))
    let writer = Lazy(fun () -> new IndexWriter(directory, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30), true, new Lucene.Net.Index.IndexWriter.MaxFieldLength(IndexWriter.DEFAULT_MAX_FIELD_LENGTH)))
    
    let getDocById id =
        let booleanQuery = new BooleanQuery()
        let query1 = new TermQuery(new Term("id", id))
        booleanQuery.Add(query1, Occur.MUST)
        let hit = searcher.Value.Search(booleanQuery,1);
        if hit.TotalHits = 1 then
            hit.ScoreDocs.[0].Doc
            |> reader.Value.Document
            |> Some   
        else None

    let getAllDocs () = seq { 
        for i in 0 .. reader.Value.MaxDoc - 1 do
            if not ( reader.Value.IsDeleted(i) ) then
                yield reader.Value.Document(i) 
        }

    let getAllDocsProj (fields:string list ) = seq {
        let fl = List<string>(fields.Length)
        fl.AddRange(fields)
        let fs = MapFieldSelector(fl)
        for i in 0 .. reader.Value.MaxDoc - 1 do
            if not ( reader.Value.IsDeleted(i) ) then
                yield reader.Value.Document(i,fs )
        }

    let search query =
        let parser = QueryParser(Util.Version.LUCENE_30, "id", new StandardAnalyzer(Util.Version.LUCENE_30))
        let query = parser.Parse(query)
            
        //MatchAllDocsQuery()
        let sc = SimpleCollector()
        searcher.Value.Search(query,sc)
        
        seq { for docId in sc.DocIds -> (searcher.Value.Doc(docId)) }

    let searchJson query (jw:JsonTextWriter) =
        jw.WriteStartArray()
        let docWriter = JsonDoc.WriteDocToJson jw 
        for doc in search query do
            docWriter doc 
        jw.WriteEndArray()
        jw.Close()  

    let jsonWriterString (fn:JsonTextWriter -> unit) : string =
        let sb = new StringBuilder()
        let sw = new StringWriter(sb)
        let jw = new JsonTextWriter(sw)
        fn jw
        jw.Close()
        sb.ToString()

    let jsonWriterStream (stream:Stream) (fn:JsonTextWriter -> unit) =
        let sw = new StreamWriter(stream)
        let jw = new JsonTextWriter(sw)
        fn jw
        jw.Close()
        stream.Flush() //todo: probably not requried as flushed on jw close

    let fieldData field =
        let termEnum = reader.Value.Terms(Term(field))    // get Term enumerator
        seq {
            if termEnum.Term.Field = field then
                yield termEnum.Term.Text
                while termEnum.Next() do
                    if termEnum.Term.Field = field then
                        yield termEnum.Term.Text                
        }
        
    //member __.Types with get () = types
    
    member __.GetId<'T> id =
        match getDocById id with
        | Some doc -> Some ( JsonDoc.DocToCont<'T> doc )
        | None -> None
                
    member __.GetIdJsonString id =
        match getDocById id with
        | Some doc -> JsonDoc.DocToJsonString doc
        | None -> "{}"

    member __.GetIdJsonStream id stream =
        match getDocById id with
        | Some doc -> JsonDoc.DocToJsonStream doc stream
        | None -> ()
    
    
    //todo: refactor getbyId, search and get all into subtyes and re-use container/stream/string pattern
//    member __.GetAll (stream:Stream) = 
//        jsonWriterStream stream (fun jw -> 
//            let docWrite = JsonDoc.WriteDocToJson jw
//            for i in 0 .. reader.Value.MaxDoc - 1 do
//                if not ( reader.Value.IsDeleted(i) ) then
//                     docWrite (reader.Value.Document(i))
//        )

    member __.GetAllJsonStream (stream:Stream) =
        jsonWriterStream stream (fun jw -> 
            jw.WriteStartArray()
            for doc in getAllDocs () do
                JsonDoc.WriteDocToJson jw doc
            jw.WriteEndArray()
        )      

    member __.GetAllJsonString () =
        jsonWriterString <| fun jw ->
            jw.WriteStartArray()
            for doc in getAllDocs () do
                JsonDoc.WriteDocToJson jw doc 
            jw.WriteEndArray()
        

    member __.GetAllJsonProjString (flds:string list) =
        jsonWriterString ( fun jw ->
            jw.WriteStartArray()
            for doc in getAllDocsProj flds do
                JsonDoc.WriteDocToJson jw doc 
            jw.WriteEndArray()
        )

    member __.Search query = seq { 
        for doc in search query -> JsonDoc.DocToCont<'T> doc
        }

    member x.SearchToJsonStream query (stream:Stream) =
        jsonWriterStream stream (fun jw -> 
            searchJson query jw
        )

    member x.SearchToJsonString query =
        jsonWriterString (fun jw ->
            searchJson query jw
        )

//    member __.Save<'T> (jc:JContainer<'T>) =
//        failwith "not implimented"
//        ()

    member __.SaveJson stream =
        let doc = JsonDoc.JsonToNewDoc stream
        writer.Value.AddDocument(doc)
        writer.Value.Commit()
            
    /// <summary>
    /// This function returns all values that have been saved for that field so can be used for auto complete
    /// </summary>
    /// <param name="field">The field name to get values for</param>
    member __.FieldData field = fieldData field
//        let hs = HashSet<string>()
//        let termEnum = reader.Value.Terms(Term(field))    // get Term enumerator
//        
//        let rec loop () =
//            if termEnum.Term.Field <> field then    // the all terms in alphabetical continous seq so check if still on same term
//                hs.TrimExcess()     // if not done, trim excess
//            else
//                hs.Add( termEnum.Term.Text ) |> ignore      // add value to hashset, ignore bool of whether added
//                if termEnum.Next() then     // enumerate onto next term, if there is a next term keep looping otherwise return result
//                    loop ()
//                else
//                    hs.TrimExcess()
//        loop () //      Start looping through terms, building up hashset till end found
//        
//        let result = Array.zeroCreate<string>(hs.Count)
//        hs.CopyTo(result) // return basic array for results to free memory of no longer needed hashset
//        result