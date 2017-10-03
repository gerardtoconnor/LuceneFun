namespace AzurePersist
open System

type JContainer<'T>(id,jtype) = 
    member val Id = id with get,set
    member val JType = jtype with get,set
    member val Created = DateTime.UtcNow with get,set
    member val Modified = DateTime.UtcNow with get,set
    member val Data: 'T = Unchecked.defaultof<'T> with get,set
    new() =  JContainer("#","#")

//type JTask<'T,'U> =
//    | Validate of (string*'T -> bool) * string
//    | Transform of (string*'T -> 'U) 

module JsonDoc =
    open System.IO
    open Lucene.Net.Documents
    open Newtonsoft.Json
    open System.Collections.Generic
    //open System.Diagnostics
    open System.Text
    open Microsoft.FSharp.Core

    let DATAPROP = "data"
    //Enums for JStack
    type private JStack =
        | JObject = 0uy
        | JArray = 1uy

    /// <summary>
    /// Prefixer class simplifies building and reduction of prefix on doc build
    /// </summary>
    type private Prefixer() =
        //todo: reimpliment this with a stringbuilder and index stack when you have more time
        let stack = Stack<string>()
        let mutable cachedResult = ""
        let mutable dirty = false
        let jstack = Stack<JStack>()
        
        // calculates the prefix string from the prefix stack
        let cacheUpdate () = 
            cachedResult <-
                stack
                |> Seq.fold (fun acc v -> if acc = "" then v else v + "." + acc) ""
            dirty <- false

        ///<summary>
        ///pops off prefix value, only call once per cycle
        ///</summary> 
        member __.Value () =
            if dirty then cacheUpdate ()
            match jstack.Peek() with
            | JStack.JArray -> () // dont pop prefix as we are in a value array
            | JStack.JObject -> 
                stack.Pop() |> ignore
                dirty <- true
            | x -> failwith (sprintf "unidentified Jstack enum of %A provided" x)
            cachedResult
        member __.Push v =
            dirty <- true
            stack.Push v
        //member __.Count with get() = stack.Count
        member __.StartArray () = jstack.Push(JStack.JArray)
        member __.StartObject () = jstack.Push(JStack.JObject)
        member __.EndArray () =
            if jstack.Pop() <> JStack.JArray then failwith "bad json object construct"
            dirty <- true
            stack.Pop()
        member __.EndObject () =
            if jstack.Pop() <> JStack.JObject then failwith "bad json object construct"
            dirty <- true
            if stack.Count > 0 then
                stack.Pop()
            else
                ""

    type private AddDocFieldAction =
        | StringAdd of string
        | FloatAdd  of float32
        | BoolAdd   of bool
        | DoubleAdd of float
        | DateAdd   of DateTime
        | IntAdd    of int
        | NoAction
            
    let private baseMaps : Map<string,string option -> AddDocFieldAction> = 
        [
            "id", (function Some _ -> NoAction | None -> StringAdd "adfasdfd" )
            "jType",(function | Some _ -> NoAction | None -> NoAction)
            "created",(function | Some _ -> NoAction | None -> DateAdd DateTime.UtcNow )
            "modified",(function | Some _ -> DateAdd DateTime.UtcNow  | None -> DateAdd DateTime.UtcNow )
        ] |> Map.ofList

    ///<summary> field store map (data field) </summary>
    let private fsm dataField = if dataField then Field.Store.NO else Field.Store.YES 
    ///<summary> field Index map (data field) </summary>
    let private fim dataField = if dataField then Field.Index.ANALYZED else Field.Index.NOT_ANALYZED_NO_NORMS 
    
    /// <summary>
    /// helper fn to add field (document, field name, data field ? , AddDocField Action)
    /// </summary>
    let private addDocField (doc:Document,fieldName,dataFld,actn)=
        match actn with
        | StringAdd     v -> doc.Add(Field(fieldName,v,fsm dataFld, fim dataFld))
        | FloatAdd      v -> doc.Add(NumericField(fieldName,fsm dataFld, true).SetFloatValue(v))
        | BoolAdd       v -> doc.Add(NumericField(fieldName, fsm dataFld, true).SetIntValue(if v then 1 else 0))
        | DoubleAdd     v -> doc.Add(NumericField(fieldName,fsm dataFld, true).SetDoubleValue(v))
        | DateAdd       v -> doc.Add(NumericField(fieldName,fsm dataFld, true).SetLongValue(v.Ticks)) //Todo: think of better date container
        | IntAdd        v -> doc.Add(NumericField(fieldName,fsm dataFld, true).SetIntValue(v))
        | NoAction      -> ()
        
    /// <summary>
    /// crawls the given JToken to the specified Document.
    /// </summary>
    /// <param name="doc">
    /// The Document to crawl to.
    /// </param>
    /// <param name="prefix">
    /// The prefix to use for field names.
    /// </param>
    /// <param name="token">
    /// The JToken to crawl.
    /// </param>
    let rec private crawl(doc:Document, prefix:Prefixer, reader:JsonTextReader, data:(JsonTextWriter*StringBuilder) option ) =
                            
        if reader.Read() then
        
            let inline next () = crawl(doc, prefix, reader,data) // reading next token changing function inputs 

            //let value = reader.Value;
            match reader.TokenType with
            
                | JsonToken.StartObject ->
                    prefix.StartObject()
                    match data with
                    | Some (jw,_) -> jw.WriteStartObject()                 
                    | None -> ()
                    next ()
                    
                | JsonToken.StartArray ->
                    prefix.StartArray()
                    match data with
                    | Some (jw,_) -> jw.WriteStartArray()                         
                    | None -> ()
                    next ()
                    
                | JsonToken.EndObject ->
                    let closedObject = prefix.EndObject() 
                    match data with
                    | Some (jw,sb) ->
                        jw.WriteEndObject()
                        if closedObject = DATAPROP then
                            jw.Close()
                            doc.Add(new Field(DATAPROP, sb.ToString(), Field.Store.YES, Field.Index.NO))
                            crawl(doc, prefix, reader,None); // data now set to none
                        else
                            next ()
                    | None -> next ()
                    
                | JsonToken.EndArray ->
                    prefix.EndArray() |> ignore
                    match data with
                    | Some (jw,_) -> jw.WriteEndArray()                         
                    | None -> ()
                    next ()
                    
                | JsonToken.PropertyName ->
                    let propName = reader.Value :?> string 
                    prefix.Push(propName);
                    match data with
                    | Some (jw,_) -> 
                        jw.WritePropertyName(propName) 
                        next () //if already in data object continue, option check faster then string check
                    | None ->
                        if propName = DATAPROP then // if not already in data, check if entering data prop
                            let sb = StringBuilder()
                            crawl(doc, prefix, reader,Some(new JsonTextWriter(new StringWriter(sb)),sb))
                        else
                            next ()
                    
                | JsonToken.Boolean -> //only handles data only
                    let v = reader.Value :?> bool
                    let df = 
                        match data with
                        | Some (jw,_) -> jw.WriteValue(v) ; true
                        | None -> false;
                    addDocField (doc,prefix.Value(), df,BoolAdd v)
                    next ()
                        
                | JsonToken.Date ->
                    let v = (reader.Value :?> DateTime)
                    //let v = reader.Value.ToString().Replace(':','-')
                    let df = 
                        match data with
                        | Some (jw,_) -> jw.WriteValue(v) ; true
                        | None -> false
                    addDocField (doc,prefix.Value(), df,DateAdd v)
                    next ()

                | JsonToken.Float ->
                    let actn , df =
                        match reader.Value with
                        | :? float32 as s ->
                            match data with 
                            | Some (jw,_) -> jw.WriteValue(s) ; FloatAdd( s ), true
                            | None -> FloatAdd( s ), false
                        | :? float as d ->
                            match data with
                            | Some (jw,_) -> jw.WriteValue(d) ; DoubleAdd( d), true
                            | None -> DoubleAdd( d), false
                        | x -> failwith (sprintf "failed to cast Json Float token of type:%A" x) ; StringAdd (x.ToString()),true
                    addDocField (doc,prefix.Value(), df,actn)
                    next ()

                | JsonToken.Integer ->
                    let v = Convert.ToInt32(reader.Value)
                    let df = 
                        match data with
                        | Some (jw,_) -> jw.WriteValue(v) ; true
                        | None -> false
                    addDocField( doc, prefix.Value(), df,IntAdd v)
                    next ()
                                            
                | JsonToken.Null -> 
                    next () // ignore
                    
                | JsonToken.String ->
                    let v = reader.Value :?> string
                    let df = 
                        match data with
                        | Some (jw,_) -> jw.WriteValue(v) ; true
                        | None -> false
                    addDocField( doc, prefix.Value(), df,StringAdd v)
                    next ()

                | _ ->
                    failwith ("Unsupported JValue type -> " + reader.TokenType.ToString())
                    next ()

    let addJsonToDoc (source : Stream,  doc : Document) = 
        use sr = new StreamReader(source)
        use reader = new JsonTextReader(sr)
        //let baseFlds = HashSet<string>(baseMaps.)

        crawl(doc, Prefixer(), reader,None)

    //  , tranlationRules :Dictionary<string,Func<obj,obj>>,validationRules:Dictionary<string,Func<obj,bool>*string>

    let JsonToNewDoc stream =
        let doc = new Document()
        addJsonToDoc(stream, doc)
        doc

    let WriteDocToJson (jw:JsonTextWriter) (doc:Document) =
        
        let writeCheck (fn:string -> unit) prop =
            match doc.Get(prop) with
            | null -> () //prop not found so dont bother writing property
            | x ->
                jw.WritePropertyName(prop)
                fn x
                  
        let writeStr = writeCheck jw.WriteValue
        let writeDt = writeCheck (int64 >> DateTime >> jw.WriteValue)        
        let writeRaw = writeCheck jw.WriteRawValue        
        
        //write document
        jw.WriteStartObject()

        writeStr "id"
        writeStr "jType"
        writeDt "created"
        writeDt "modified"
        
        writeRaw "data"

        jw.WriteEndObject()

    /// <summary>
    /// Takes a document and writes json into a stream (optimal method)
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="stream"></param>
    let DocToJsonStream (doc:Document) (stream:Stream) =
        use sw = new StreamWriter(stream)
        use jw = new JsonTextWriter(sw)
        WriteDocToJson jw doc 
        jw.Close()

    /// <summary>
    /// Takes a document and converts it to a Json string (DocToJsonStream more efficient & preffered)
    /// </summary>
    /// <param name="doc"></param>
    let DocToJsonString (doc:Document) =
        let sb = StringBuilder()
        use sw = new StringWriter(sb)
        use jw = new JsonTextWriter(sw)
        WriteDocToJson jw doc
        sb.ToString()

    let DocToCont<'T> (doc:Document) =
        let jc = JContainer<'T>(doc.Get("id"),doc.Get("jType"))
        jc.Created <- DateTime(int64(doc.Get("created")))
        jc.Modified <- DateTime(int64(doc.Get("modified")))
        jc.Data <- JsonConvert.DeserializeObject<'T>( doc.Get("data") )
        jc

    let PrettyJson json =
        let jt = Newtonsoft.Json.Linq.JToken.Parse(json)
        jt.ToString(Newtonsoft.Json.Formatting.Indented)