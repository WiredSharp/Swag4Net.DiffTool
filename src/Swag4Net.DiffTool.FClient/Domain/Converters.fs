module Swag4Net.Core.Domain.Converters

open Microsoft.OpenApi.Models
open Swag4Net.Core.Domain

let private parseInfo (openApiInfo: OpenApiInfo) : ApiInfo =
  {
    Description = openApiInfo.Description
    Version = failwith "todo"
    Title = failwith "todo"
    TermsOfService = failwith "todo"
    Contact = failwith "todo"
    License = failwith "todo"    
  }

let parse (specification:OpenApiDocument) : Api =
  {
     Info = parseInfo specification.Info
     Paths = failwith "todo"
     Definitions = failwith "todo"
     Servers = failwith "todo"
  }