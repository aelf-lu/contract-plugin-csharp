using AElf;
using ContractGenerator.Primitives;
using Google.Protobuf.Reflection;

namespace ContractGenerator;

public partial class Generator
{
    /// <summary>
    ///     Generate will produce a chunk of C# code BaseClass for the AElf Contract. based on C++ original
    ///     https://github.com/AElfProject/contract-plugin/blob/453bebfec0dd2fdcc06d86037055c80721d24e8a/src/contract_csharp_generator.cc#L422
    /// </summary>
    protected internal void GenerateContractBaseClass()
    {
        var serverClassName = GetServerClassName();
        _(
            $"/// <summary>Base class for the contract of {serverClassName}</summary>");
        _(
            $"public abstract partial class {serverClassName} : AElf.Sdk.CSharp.CSharpSmartContract<{GetStateTypeName()}>");
        InBlock(() =>
        {
            foreach (var method in FullMethods)
            {
#if VIRTUAL_METHOD
                _(
                    $"public virtual {GetMethodReturnTypeServer(method)} {method.Name}({GetMethodRequestParamServer(method)}{GetMethodResponseStreamMaybe(method)})");
                _("{");
                Indent();
                _("throw new global::System.NotImplementedException();");
                Outdent();
                _("}");
#else
                _(
                    $"public abstract {GetMethodReturnTypeServer(method)} {method.Name}({GetMethodRequestParamServer(method)}{GetMethodResponseStreamMaybe(method)});");
#endif
            }
        });
    }

    private void GenerateBindServiceMethod()
    {
        _($"public static aelf::ServerServiceDefinition BindService({GetServerClassName()} serviceImpl)");
        InBlock(() =>
        {
            _("return aelf::ServerServiceDefinition.CreateBuilder()");
            DoubleIndented(() =>
            {
                _(".AddDescriptors(Descriptors)");
                if (FullMethods.Any())
                {
                    foreach (var method in FullMethods.SkipLast(1))
                    {
                        _($".AddMethod({GetMethodFieldName(method)}, serviceImpl.{method.Name})");
                    }

                    var lastMethod = FullMethods.Last();
                    _($".AddMethod({GetMethodFieldName(lastMethod)}, serviceImpl.{lastMethod.Name}).Build();");
                }
            });
        });
        ___EmptyLine___();
    }

    #region Helper Methods

    private string GetStateTypeName()
    {
        // If there has no option (aelf.csharp_state) = "XXX" in proto files, state name will return empty string. Such as base proto.
        if (_serviceDescriptor.GetOptions() == null)
        {
            return "";
        }
        return _serviceDescriptor.GetOptions().GetExtension(OptionsExtensions.CsharpState);
    }


    private static string GetMethodReturnTypeServer(MethodDescriptor method)
    {
        return method.OutputType.GetFullTypeName();
    }

    private static string GetMethodRequestParamServer(MethodDescriptor method)
    {
        switch (GetMethodType(method))
        {
            case MethodType.MethodtypeNoStreaming:
            case MethodType.MethodtypeServerStreaming:
                return method.InputType.GetFullTypeName() + " input";
            case MethodType.MethodtypeClientStreaming:
            case MethodType.MethodtypeBidiStreaming:
                return $"grpc::IAsyncStreamReader<{method.InputType.GetFullTypeName()}> requestStream";
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static string GetMethodResponseStreamMaybe(MethodDescriptor method)
    {
        switch (GetMethodType(method))
        {
            case MethodType.MethodtypeNoStreaming:
            case MethodType.MethodtypeClientStreaming:
                return "";
            case MethodType.MethodtypeServerStreaming:
            case MethodType.MethodtypeBidiStreaming:
                return $", grpc::IServerStreamWriter<{method.OutputType}> responseStream";
            default:
                throw new ArgumentOutOfRangeException();
        }
    }


    private static MethodType GetMethodType(MethodDescriptor method)
    {
        if (method.IsClientStreaming)
            return method.IsServerStreaming ? MethodType.MethodtypeBidiStreaming : MethodType.MethodtypeClientStreaming;
        return method.IsServerStreaming ? MethodType.MethodtypeServerStreaming : MethodType.MethodtypeNoStreaming;
    }

    #endregion
}
