using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace Swag4Net.DiffTool.Tests.TestHelpers
{
    internal static class OpenApiDocumentExtensions
    {
        public static (OpenApiDocument, OpenApiDiagnostic) ReadSpecification(FileInfo filePath)
        {
            using var file = new FileStream(filePath.FullName, FileMode.Open);
            return ReadSpecification(file);
        }

        public static async Task<(OpenApiDocument, OpenApiDiagnostic)> ReadSpecificationAsync(Uri swaggerUri)
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