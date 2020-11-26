using System;
using System.IO;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace Swag4Net.DiffTool.Client
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			try
			{
				if (args.Length < 2)
				{
					await Console.Error.WriteLineAsync("at least two specifications are required for comparison");
				}
				else
				{
					(OpenApiDocument, OpenApiDiagnostic)[]? specs = await Task.WhenAll(
						ReadSpecificationAsync(args[0]), ReadSpecificationAsync(args[1]));

					Console.WriteLine(
						JsonSerializer.Serialize(specs[0].Item1.CompareTo(specs[1].Item1), 
															new JsonSerializerOptions()
															{
																WriteIndented = true,
																IgnoreNullValues = true,
																Converters = { new JsonStringEnumConverter() },
																Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
															}));
				}
			}
			catch (Exception e)
			{
				Console.Error.WriteLine($"unable to compare apis specification, something goes wrong: {e.Message}");
			}
		}
		
		private static async Task<(OpenApiDocument, OpenApiDiagnostic)> ReadSpecificationAsync(string source)
		{
			(OpenApiDocument, OpenApiDiagnostic) specification;
			if (Uri.TryCreate(source, UriKind.Absolute, out Uri? swaggerUri) && swaggerUri.Scheme.StartsWith("http"))
			{
				specification = await ReadSpecificationAsync(swaggerUri).ConfigureAwait(false);
			}
			else
			{
				specification = ReadSpecification(new FileInfo(source));
			}

			return specification;
		}

		private static (OpenApiDocument, OpenApiDiagnostic) ReadSpecification(FileInfo filePath)
		{
			using var file = new FileStream(filePath.FullName, FileMode.Open);
			return ReadSpecification(file);
		}

		private static async Task<(OpenApiDocument, OpenApiDiagnostic)> ReadSpecificationAsync(Uri swaggerUri)
		{
			using var client = new HttpClient(new HttpClientHandler() { UseDefaultCredentials = true });
			Stream stream = await client.GetStreamAsync(swaggerUri);
			return ReadSpecification(stream);
		}

		private static (OpenApiDocument, OpenApiDiagnostic) ReadSpecification(Stream stream)
		{
			var reader = new OpenApiStreamReader();
			var apiDocument = reader.Read(stream, out OpenApiDiagnostic diags);
			return (apiDocument, diags);
		}
	}
}
