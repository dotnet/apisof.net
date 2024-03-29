﻿using Microsoft.CodeAnalysis;

namespace Terrajobst.ApiCatalog;

internal static class AssemblyStream
{
    public static async Task<MetadataReference> CreateAsync(Stream stream, string path)
    {
        if (!stream.CanSeek)
        {
            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            stream.Dispose();
            stream = memoryStream;
        }

        return MetadataReference.CreateFromStream(stream, filePath: path);
    }
}