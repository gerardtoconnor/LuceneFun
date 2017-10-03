//namespace AzurePersist
//
//open Lucene.Net
//open Lucene.Net.Index
////open Lucene.Net.Linq
//open Lucene.Net.Store.Azure
//open Microsoft.WindowsAzure.Storage
//open Lucene.Net.Analysis.Standard
//open Lucene.Net.Documents
//open System
//open Lucene.Net.Index
//open Lucene.Net.Store
//open Lucene.Net.QueryParsers
//open Lucene.Net.Search
//open System.Collections.Generic
//open System.IO
//open Newtonsoft.Json
//open System.Text
//open System.Threading
//
//type SimpleCollector() =    
//    inherit Collector()
//    let docIds = HashSet<int>()
//    let mutable docBase = 0
//           
//    override x.SetScorer (scorer:Scorer) =
//        ()
//    override x.Collect(doc:int) =
//        docIds.Add(docBase + doc) |> ignore
//    override x.SetNextReader( _:IndexReader, baseint:int ) = 
//        docBase <- baseint
//    override x.AcceptsDocsOutOfOrder 
//        with get() = true
//    member x.DocIds 
//        with get() = docIds
//
//type AzurePersist(blobStorage:string,catalogName:string) = 
//
//    //for worker roles, create async pool factory of indexers using same storage account
//    let azureDirectoryOption = 
//        //let cloudStorageAccount = CloudStorageAccount.DevelopmentStorageAccount;
//        let success,cloudStorageAccount = CloudStorageAccount.TryParse(blobStorage)
//        if success then
//            Some(new AzureDirectory(cloudStorageAccount, catalogName,FSDirectory.Open(@"~\" + catalogName)))
//        else
//            None
//
//    let getIndexWriter ad = new IndexWriter(ad, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30), true, new Lucene.Net.Index.IndexWriter.MaxFieldLength(IndexWriter.DEFAULT_MAX_FIELD_LENGTH))
//    
//    //Convert Document to Json ?? currently done with stringbuilder may update to stream at later date
//    let docToJson (sb:StringBuilder) (doc:Document)  = 
//        sb.Append("{") |> ignore
//        for fld in doc.GetFields() do
//            sb.AppendFormat("{0}:{1},",fld.Name,fld.StringValue) |> ignore
//        sb.Remove(sb.Length - 1 ,1).Append("}") |> ignore
//
//    //Convert Document Seq to Json array ?? currently done with stringbuilder may update to stream at later date
//    let docSeqToJson (docs:Document seq) = 
//        let sb = StringBuilder()
//        sb.Append "[" |> ignore
//        let docFn = docToJson sb
//       
//        if not ( Seq.isEmpty docs ) then
//            docs
//            |> Seq.iter (fun doc ->  docFn doc; sb.Append "," |> ignore) 
//            //trim last comma off
//            sb.Remove(sb.Length - 1, 1) |> ignore
//        
//        sb.Append("]").ToString()
//        
//    let getById id =
//        match azureDirectoryOption with
//        | Some azureDirectory ->
//            let searcher = new IndexSearcher(azureDirectory)           
//            let parser = QueryParser(Util.Version.LUCENE_30, "Id", new StandardAnalyzer(Util.Version.LUCENE_30))
//            let query = parser.Parse(id)
//            
//            let topDoc = searcher.Search(query,1)
//            
//            if topDoc.TotalHits > 0 then
//                topDoc.ScoreDocs.[0].Doc
//                |> searcher.Doc
//                |> Some
//            else
//                None
//
//        | None ->
//            None
//    
//
//    let getAll (fields:string [] option) = 
//        match azureDirectoryOption with
//        | Some azureDirectory ->
//            let reader = IndexReader.Open(azureDirectory,true)
//            let mapFn =
//                match fields with
//                | Some mfs -> 
//                    let fs = MapFieldSelector(mfs)
//                    fun id -> reader.Document(id,fs)
//                | None ->
//                    fun id -> reader.Document(id)
//
//            seq { for i in 0 .. reader.MaxDoc - 1 do
//                    if not ( reader.IsDeleted(i) ) then yield mapFn i }
//        | None -> Seq.empty
//
//
//    let search f q (mfso:string [] option) =
//        match azureDirectoryOption with
//        | Some azureDirectory ->
//            let searcher = new IndexSearcher(azureDirectory)           
//            let parser = QueryParser(Util.Version.LUCENE_30, f, new StandardAnalyzer(Util.Version.LUCENE_30))
//            let query = parser.Parse(q)
//            
//            //MatchAllDocsQuery()
//            let sc = SimpleCollector()
//            searcher.Search(query,sc)
//                        
//            let mapFn =
//                match mfso with
//                | Some mfs -> 
//                    let fs = MapFieldSelector(mfs)
//                    fun id -> searcher.Doc(id,fs)
//                | None -> 
//                    fun id -> searcher.Doc(id)            
//            
//            sc.DocIds
//            |> Seq.map mapFn
//        | None ->
//            Seq.empty
//
//
//    interface IDisposable with
//        member __.Dispose () =
//            match azureDirectoryOption with
//            | Some ad -> ad.Dispose()
//            | None -> ()
//
//    member __.Save<'T> (v:'T) =
//        match azureDirectoryOption with
//        | Some azureDirectory ->            
//            let indexWriter = getIndexWriter azureDirectory           
//            let doc = new Document();            
//            
//            let pis = typeof<'T>.GetProperties()
//
//            for pi in pis do
//                let pv = pi.GetValue(v)
//
//                match pv with 
//                | :? int as x ->
//                    let nf = NumericField(pi.Name,0,Field.Store.YES,true)
//                    doc.Add( nf.SetIntValue x )
//                | :? int64 as x ->
//                    let nf = NumericField(pi.Name,0,Field.Store.YES,true)
//                    doc.Add( nf.SetLongValue x )
//                | :? float as x ->
//                    let nf = NumericField(pi.Name,6,Field.Store.YES,true)
//                    doc.Add( nf.SetDoubleValue x )
//                | :? float32 as x ->
//                    let nf = NumericField(pi.Name,4,Field.Store.YES,true)
//                    doc.Add( nf.SetFloatValue x )
//                | :? String as str ->
//                    doc.Add(new Field(pi.Name, str, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.NO));
//                | :? System.DateTime as dt ->
//                    doc.Add(new Field(pi.Name, dt.ToUniversalTime().ToString("o"), Field.Store.YES, Field.Index.NOT_ANALYZED, Field.TermVector.NO));
//                | x ->
//                    doc.Add(new Field(pi.Name, x.ToString(), Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.NO));            
//
////            doc.Add(new Field("Title", "this is my title", Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.NO));            
////            doc.Add(new Field("Body", "This is my body", Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.NO));            
//
//            indexWriter.AddDocument(doc);
//                     
//            indexWriter.Dispose()
//            true
//        | None ->
//            false
//
//    member __.GetById id =
//        getById id 
//        
//    member __.Search f q (mfso:string [] option) =
//        search f q (mfso:string [] option)
//
//    member __.Search f q (mfso:string [] option) =
//        search f q mfso
//        |> docSeqToJson 
//
//    /// <summary>
//    /// This function returns all values that have been saved for that field so can be used for auto complete
//    /// </summary>
//    /// <param name="field">The field name to get values for</param>
//    member __.FieldData field = 
//        match azureDirectoryOption with
//        | Some azureDirectory ->
//            let reader = IndexReader.Open(azureDirectory,true)
//            
//            let termEnum = reader.Terms(Term(field))
//            let rec loop acc =
//                let currentTerm = ( termEnum.Term )
//                if currentTerm.Field <> field then
//                    acc
//                else
//                    let nacc = ( currentTerm.Text :: acc ) 
//                    if termEnum.Next() then
//                        loop nacc
//                    else
//                        nacc
//            loop []
//        | None ->
//            []
//
//    /// <summary>
//    /// Get a sequence of all documents in the index
//    /// </summary>
//    /// <param name="fields"></param>
//    member __.GetAll (fields:string [] option) = 
//        getAll fields
//
//    /// <summary>
//    /// Get a document by ID in json format (when served directly to client and not processed internally) 
//    /// </summary>
//    /// <param name="id"></param>
//    member __.JsonGetById (id) = 
//        match getById id with
//        | Some doc -> 
//            let sb = StringBuilder()
//            docToJson sb doc
//            sb.ToString() 
//            |> Some 
//        | None -> None
//
//    /// <summary>
//    /// If saving directly from the post body stream, can be saved directly from JSON stream efficiently, if saved returns true
//    /// </summary>
//    /// <param name="readStream"></param>
//    member __.JsonSave (readStream:Stream) =
//        match azureDirectoryOption with
//        | Some azureDirectory ->            
//            let indexWriter = new IndexWriter(azureDirectory, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30), true, new Lucene.Net.Index.IndexWriter.MaxFieldLength(IndexWriter.DEFAULT_MAX_FIELD_LENGTH))          
//            let doc = new Document()
//            
//            let sr = new StreamReader(readStream)
//            let reader = new JsonTextReader(sr)
//            //let ser = new JsonSerializer()
//            if not(reader.Read()) || reader.TokenType <> JsonToken.StartArray then
//                failwith "Expected start of array"
//
//            let rec crawl pName ad od =
//                
//                let propAssign (fMap:string->IFieldable) =
//                    match pName with
//                    | Some pn ->
//                        doc.Add(fMap pn)
//                        crawl None ad od
//                    | None -> failwith "cannot set value with no Prop name"
//                                
//                if reader.Read() then
//                    match reader.TokenType with
//                    | JsonToken.StartArray -> crawl pName (ad + 1) od 
//                    | JsonToken.EndArray -> crawl pName (ad - 1) od
//                    | JsonToken.StartObject -> crawl pName ad ( od + 1 )
//                    | JsonToken.EndObject -> crawl pName ad ( od - 1)
//                    | JsonToken.PropertyName -> crawl (Some (reader.Value :?> string)) ad od
//                    | JsonToken.Boolean -> propAssign (fun pn -> Field(pn,reader.ReadAsString(),Field.Store.YES,Field.Index.NOT_ANALYZED ) :> IFieldable )
//                    | JsonToken.Float -> propAssign (fun pn -> NumericField(pn,6,Field.Store.YES,true ).SetDoubleValue(reader.Value) )
//                    | JsonToken.String -> propAssign (fun pn -> Field(pn ))
//                    | JsonToken.Date -> propAssign (fun pn -> NumericField( ))
//                    | JsonToken.Integer -> propAssign (fun pn -> NumericField( ))
//                    | _ -> () //skip
//                    
//                else
//                    ()
//                        
//            doc.Add(new Field("id", DateTime.Now.ToFileTimeUtc().ToString(), Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.NO));            
//            doc.Add(new Field("Title", "this is my title", Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.NO));            
//            doc.Add(new Field("Body", "This is my body", Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.NO));            
//            indexWriter.AddDocument(doc);
//                     
//            indexWriter.Dispose()
//            true
//        | None ->
//            false
//    
//    /// <summary>
//    /// Write Json into a stream for maximum performance throughput
//    /// </summary>
//    /// <param name="fields"></param>
//
//    member __.JsonGetAll (fields:string [] option) (writeStream:Stream) = 
//
//        let sw = new StreamWriter(writeStream)
//        let writer = new JsonTextWriter(sw)
//        writer.WriteStartArray();
//        
//        let docs = getAll fields
//        
//        for doc in docs do
//            writer.WriteStartObject()
//
//            for fld in doc.GetFields() do
//                writer.WritePropertyName(fld.Name)
//                writer.WriteValue(fld.TokenStreamValue) //HACK: figure out correct way to map these
//                
//            writer.WriteEndObject()
//
//            writer.Flush()
//            //Thread.Sleep(500) //HACK: verify if this is required
//        
//        writer.WriteEndArray();
//
//        writer.WriteEnd()
//        writer.Flush()