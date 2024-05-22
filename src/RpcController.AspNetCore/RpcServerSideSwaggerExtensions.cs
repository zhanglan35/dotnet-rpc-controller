using System.Reflection;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RpcController.AspNetCore;

public static class RpcServerSideSwaggerExtensions
{
    private struct ControllerInfo
    {
        public Type Type { get; set; }
        public Type Interface { get; set; }
    }

    private static ControllerInfo[]? _controllerTypes = null;
    private static ApiExplorerSettingsAttribute[]? _apiExplorerSettingsAttributes = null;

    /// <summary>
    /// Call this method to register all controllers in the application.
    /// </summary>
    private static void LoadControllers(IServiceProvider services)
    {
        _controllerTypes = services
            .GetRequiredService<IActionDescriptorCollectionProvider>()
            .ActionDescriptors.Items
            .Where(descriptor => descriptor is ControllerActionDescriptor)
            .Select(descriptor =>
            {
                var type = ((ControllerActionDescriptor) descriptor).ControllerTypeInfo.AsType();

                return new ControllerInfo()
                {
                    Type = type,
                    Interface = type.GetTypeInfo().ImplementedInterfaces
                        .Where(x => x.IsAssignableTo(typeof(IRpcController)))
                        .Where(x => x.Name != nameof(IRpcController))
                        .FirstOrDefault()!
                };
            })
            .ToArray();
        _apiExplorerSettingsAttributes ??= _controllerTypes
            .Select(x => x.Type.GetCustomAttribute<ApiExplorerSettingsAttribute>()!)
            .Where(x => x is not null)
            .ToArray();
    }

    private static XPathDocument ConvertXDocumentToXPathDocument(XDocument xdoc)
    {
        using var xmlReader = xdoc.CreateReader();
        var xpathDoc = new XPathDocument(xmlReader);

        xmlReader.Close();

        return xpathDoc;
    }

    public static void IncludeRpcControllerXmlComments(this SwaggerGenOptions options, IServiceProvider services)
    {
        LoadControllers(services);

        var types = _controllerTypes!.Where(x => x.Interface is not null).ToArray();
        var controllerAssemblys = types
            .Select(x => x.Interface.Assembly)
            .Distinct()
            .ToArray();

        foreach (var assembly in controllerAssemblys)
        {
            var documentFile = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");
            var file = File.ReadAllText(documentFile);
            var dom = XDocument.Parse(file);

            foreach (var member in dom.Element("doc")!.Element("members")!.Elements("member"))
            {
                var attribute = member.Attribute("name")!;

                if (attribute.Value.StartsWith("T:"))
                {
                    var typeName = attribute.Value[2..];
                    var type = types.FirstOrDefault(x => x.Interface.FullName == typeName).Type;

                    if (type is not null)
                    {
                        attribute.Value = $"T:{type.FullName}";
                    }
                }
                else if (attribute.Value.StartsWith("M:"))
                {
                    var methodFullName = attribute.Value.Split("(").First();
                    var methodName = methodFullName[(methodFullName.LastIndexOf('.') + 1)..];
                    var typeName = methodFullName[2..methodFullName.LastIndexOf('.')];
                    var type = types.FirstOrDefault(x => x.Interface.FullName == typeName).Type;

                    if (type is not null)
                    {
                        attribute.Value = attribute.Value.Replace(typeName, type.FullName);
                    }

                    var parameters = member.Elements("param").ToArray();

                    for (var i = 0; i < parameters.Length; i++)
                    {
                        var param = parameters[i];
                        var paramName = type!.GetMethod(methodName)!.GetParameters()[i].Name;

                        param.Attribute("name")!.Value = paramName!;
                    }
                }
            }

            options.IncludeXmlComments(() => ConvertXDocumentToXPathDocument(dom), true);
        }
    }

    /// <summary>
    /// Use this method to include the application's XML comments in Swagger.
    /// </summary>
    /// <param name="options"></param>
    public static void IncludeApplicationXmlComments(this SwaggerGenOptions options)
    {
        var path = Path.Combine(AppContext.BaseDirectory, Assembly.GetEntryAssembly()!.GetName().Name + ".xml");

        options.IncludeXmlComments(path, true);
    }

}
