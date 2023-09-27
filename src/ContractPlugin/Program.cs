using Google.Protobuf;
using Google.Protobuf.Compiler;

namespace ContractPlugin;

// assume current directory is the output directory, and it contains protoc cli.
// protoc --plugin=protoc-gen-contract_plugin_csharp --contract_plugin_csharp_out=./ --proto_path=%userprofile%\.nuget\packages\google.protobuf.tools\3.21.1\tools --proto_path=./ chat.proto

internal class Program
{
    private static void Main()
    {
        // you can attach debugger
        // System.Diagnostics.Debugger.Launch();

        Stream stream = Console.OpenStandardInput();

        ContractGenerator.ContractGenerator contractGenerator = new ContractGenerator.ContractGenerator();
        CodeGeneratorResponse response = contractGenerator.Generate(stream);

        // set result to standard output
        using var stdout = Console.OpenStandardOutput();
        response.WriteTo(stdout);
    }
}
