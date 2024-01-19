using AElf;
using ContractGenerator.Primitives;
using Google.Protobuf.Reflection;

namespace ContractGenerator;

public partial class Generator : AbstractGenerator
{
    private const string ServiceFieldName = "__ServiceName";
    private readonly ServiceDescriptor _serviceDescriptor;

    private string PublicOrInternal => Options.InternalAccess ? "internal" : "public";

    private string ServiceContainerClassName => $"{_serviceDescriptor.Name}Container";
    private IEnumerable<ServiceDescriptor> FullServices => _serviceDescriptor.GetFullService();

    private IEnumerable<MessageDescriptor> AllReferenceMessages
    {
        get
        {
            var descriptorSet = new HashSet<IDescriptor>();
            var result = new List<MessageDescriptor>();

            foreach (var method in FullMethods)
            {
                if (!descriptorSet.Contains(method.InputType))
                {
                    descriptorSet.Add(method.InputType);
                    result.Add(method.InputType);
                }

                if (descriptorSet.Contains(method.OutputType)) continue;
                descriptorSet.Add(method.OutputType);
                result.Add(method.OutputType);
            }

            return result;
        }
    }

    private IEnumerable<MethodDescriptor> FullMethods =>
        _serviceDescriptor.GetFullService().SelectMany(serviceItem => serviceItem.Methods).ToList();


    public Generator(ServiceDescriptor serviceDescriptor, GeneratorOptions options) : base(options)
    {
        _serviceDescriptor = serviceDescriptor;
    }

    /// <summary>
    ///     Generate will produce a chunk of C# code that serves as the container class of the AElf Contract.
    /// </summary>
    public override string? Generate()
    {
        // GenerateDocCommentBody(serviceDescriptor,)
        _($"{PublicOrInternal} static partial class {ServiceContainerClassName}");
        InBlock(() =>
        {
            _($"""static readonly string {ServiceFieldName} = "{_serviceDescriptor.FullName}";""");

            ___EmptyLine___();
            Marshallers();
            ___EmptyLine___();
            Methods();
            ___EmptyLine___();
            Descriptors();

            if (Options.GenerateContract)
            {
                ___EmptyLine___();
                GenerateContractBaseClass();
                ___EmptyLine___();
                GenerateBindServiceMethod();
            }

            if (Options.GenerateStub)
            {
                ___EmptyLine___();
                GenerateStubClass();
            }

            if (Options.GenerateReference)
            {
                ___EmptyLine___();
                GenerateReferenceClass();
            }
        });
        return Output();
    }

    private void GenerateAllServiceDescriptorsProperty()
    {
        _(
            "public static global::System.Collections.Generic.IReadOnlyList<global::Google.Protobuf.Reflection.ServiceDescriptor> Descriptors"
        );
        InBlock(() =>
        {
            _("get");
            InBlock(() =>
            {
                _(
                    "return new global::System.Collections.Generic.List<global::Google.Protobuf.Reflection.ServiceDescriptor>()");
                InBlockWithSemicolon(() =>
                {
                    foreach (var service in FullServices)
                    {
                        var index = service.Index.ToString();
                        _(
                            $"{service.File.GetReflectionClassName()}.Descriptor.Services[{index}],");
                    }
                });
            });
        });
    }

    private string GetServerClassName()
    {
        return _serviceDescriptor.Name + "Base";
    }

    /// <summary>
    ///     Generates a section of instantiated aelf Marshallers as part of the contract
    /// </summary>
    private void Marshallers()
    {
        InRegion("Marshallers", () =>
        {
            foreach (var usedMessage in AllReferenceMessages)
            {
                var fieldName = GetMarshallerFieldName(usedMessage);
                var t = usedMessage.GetFullTypeName();
                _(
                    $"static readonly aelf::Marshaller<{t}> {fieldName} = aelf::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), {t}.Parser.ParseFrom);");
            }
        });
    }


    private void GenerateServiceDescriptorProperty()
    {
        _(
            "public static global::Google.Protobuf.Reflection.ServiceDescriptor Descriptor");
        _("{");
        _(
            $"  get {{ return {_serviceDescriptor.File.GetReflectionClassName()}.Descriptor.Services[{_serviceDescriptor.Index}]; }}");
        _("}");
    }


    /// <summary>
    ///     GetMarshallerFieldName formats and returns a marshaller-fieldname based on the original C++ logic
    ///     found here
    ///     https://github.com/AElfProject/contract-plugin/blob/de625fcb79f83603e29d201c8488f101b40f573c/src/contract_csharp_generator.cc#L242
    /// </summary>
    private static string GetMarshallerFieldName(IDescriptor message)
    {
        var msgFullName = message.FullName;
        return "__Marshaller_" + msgFullName.Replace(".", "_");
    }

    private static string GetMethodFieldName(MethodDescriptor method)
    {
        return "__Method_" + method.Name;
    }

    private static string GetCSharpMethodType(MethodDescriptor method)
    {
        return IsViewOnlyMethod(method) ? "aelf::MethodType.View" : "aelf::MethodType.Action";
    }


    private static bool IsViewOnlyMethod(MethodDescriptor method)
    {
        if (method.GetOptions() == null)
        {
            return false;
        }
        return method.GetOptions().GetExtension(OptionsExtensions.IsView);
    }


    private enum MethodType
    {
        MethodtypeNoStreaming,
        MethodtypeClientStreaming,
        MethodtypeServerStreaming,
        MethodtypeBidiStreaming
    }
}
