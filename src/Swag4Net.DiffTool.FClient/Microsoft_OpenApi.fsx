open Microsoft.OpenApi.Readers

#load "../../.paket/load/netstandard2.1/Microsoft.OpenApi.fsx"
#load "../../.paket/load/netstandard2.1/Microsoft.OpenApi.Readers.fsx"

open Microsoft.OpenApi.Models


let reader = new OpenApiStringReader(new OpenApiReaderSettings())
let spec = new OpenApiDocument()

//spec.Paths.["op01"].Operations.[OperationType.Delete].Responses.["foo"].Content.[""].Schema.