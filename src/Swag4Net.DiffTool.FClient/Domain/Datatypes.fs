module Swag4Net.Core.Domain.DataTypes

let IsArrayType (dataType) :bool =
  match dataType with
  | DataType.Array -> true
  | _ -> false

let IsPrimaryType (dataType) :bool =
  match dataType with
  | DataType.Object -> false
  | DataType.Array -> false
  | _ -> true

let IsArray (schema:Schema) :bool =
  IsArrayType schema.Type

let IsPrimary (schema:Schema) :bool =
  IsPrimaryType schema.Type