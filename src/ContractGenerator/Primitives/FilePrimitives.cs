using System.Text;
using Google.Protobuf.Reflection;

namespace ContractGenerator.Primitives;

public static class FilePrimitives
{
    public static bool ContainsEvent(this FileDescriptor file)
    {
        return file.MessageTypes.Any(m => m.IsEventMessageType());
    }

    public static string GetNamespace(this FileDescriptor fileDescriptor)
    {
        return fileDescriptor.GetOptions().HasCsharpNamespace
            ? fileDescriptor.GetOptions().CsharpNamespace
            : fileDescriptor.Package.UnderscoresToCamelCase(true, true);
    }

    /// <summary>
    ///     ServicesFilename generates Services FileName based on the FileDescriptor
    ///     Its based on the C++ original
    ///     https://github.com/AElfProject/contract-plugin/blob/de625fcb79f83603e29d201c8488f101b40f573c/src/contract_csharp_generator_helpers.h#L27
    /// </summary>
    public static string GetOutputCSharpFilename(this FileDescriptor file)
    {
        return FileNameInUpperCamel(file, false) + ".c.cs";
    }

    public static string GetReflectionClassName(this FileDescriptor descriptor)
    {
        var result = descriptor.GetNamespace();
        if (result.Length > 0) result += '.';
        result += GetReflectionClassUnqualifiedName(descriptor);
        return "global::" + result;

        static string GetReflectionClassUnqualifiedName(FileDescriptor descriptor)
        {
            // TODO: Detect collisions with existing messages,
            // and append an underscore if necessary.
            return GetFileNameBase(descriptor) + "Reflection";
        }

        static string GetFileNameBase(IDescriptor descriptor)
        {
            var protoFile = descriptor.Name;
            var lastSlash = protoFile.LastIndexOf('/');
            var stringBase = protoFile[(lastSlash + 1)..];
            return StripDotProto(stringBase).UnderscoresToPascalCase();
        }

        static string StripDotProto(string protoFile)
        {
            var lastIndex = protoFile.LastIndexOf(".", StringComparison.Ordinal);
            return protoFile[..lastIndex];
        }
    }

    #region Private Methods

    private static string FileNameInUpperCamel(IDescriptor file, bool includePackagePath)
    {
        var tokens = StripProto(file.Name).Split('/');
        var result = new StringBuilder();

        if (includePackagePath)
            for (var i = 0; i < tokens.Length - 1; i++)
                result.Append(tokens[i] + "/");

        result.Append(tokens[^1]
            .LowerUnderscoreToUpperCamel()); // Using the "end index" operator to get the last element

        return result.ToString();
    }

    private static string StripProto(string fileName)
    {
        return fileName.EndsWith(".proto") ? fileName[..^".proto".Length] : fileName;
    }

    #endregion Private Methods
}
