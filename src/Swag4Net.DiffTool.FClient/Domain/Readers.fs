module Swag4Net.Core.Domain.Readers

open System
open System.IO
open System.Net.Http
open Microsoft.OpenApi.Models
open Microsoft.OpenApi.Readers
open Swag4Net.Core.Domain.Converters

  
let read  (file: FileInfo) =
  use file = File.OpenRead file.FullName
  let reader = new OpenApiStreamReader()
  let document, diags = reader.Read(file)
  parse document
  
let loadAsync(url: Uri) =
  use client = new HttpClient()
  let reader = new OpenApiStreamReader()
  async {
    let! responseStream = client.GetStreamAsync(url) |> Async.AwaitTask
    let document, diags = reader.Read(responseStream)
    return parse document
  } 
  
let parse(specification) =
  let reader = new OpenApiStringReader()
  let document, diags = reader.Read(specification)
  parse document
