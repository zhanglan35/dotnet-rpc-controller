using System.Reflection;
using RpcController.Client.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Data;

namespace RpcController.AspNetCore;

/// <summary>
/// Use this convention to register controller from IRpcController.
/// </summary>
public class RpcControllerConvention : IApplicationModelConvention
{
    static bool IsAssignableTo(Type type, Type toType)
    {
        return toType.IsAssignableFrom(type);
    }

    public void Apply(ApplicationModel application)
    {
        /* Register IRpcController */
        foreach (var controller in application.Controllers)
        {
            /* Get all IRpcController interfaces implemented by the ControllerType  */
            var rpcInterfaces = controller.ControllerType.ImplementedInterfaces
                .Where(x => IsAssignableTo(x, typeof(IRpcController)))
                .Where(x => x.Name != nameof(IRpcController))
                .ToArray();

            /* skip if no interface */
            if (rpcInterfaces.Length == 0)
            {
                continue;
            }
            else if (rpcInterfaces.Length > 1)
            {
                throw new ConstraintException($"RPC implement '{controller.ControllerType.Name}' can only has one 'IRpcController' interface");
            }

            var rpcInterface = rpcInterfaces.First();
            var controllerDefaultSelector = controller.Selectors.First();
            var controllerRouteAttributes = rpcInterface.GetCustomAttributes<RouteAttribute>(true);

            if (controllerRouteAttributes.Count() != 1)
            {
                throw new ConstraintException($"RPC interface '{rpcInterface.Name}' must define only one 'HttpRouteAttribute'");
            }

            controller.Selectors.Clear();

            foreach (var controllerRouteAttribute in controllerRouteAttributes)
            {
                var controllerSelector = new SelectorModel(controllerDefaultSelector)
                {
                    AttributeRouteModel = new AttributeRouteModel(controllerRouteAttribute!)
                };

                controller.Selectors.Add(controllerSelector);
            }

            foreach (var action in controller.Actions)
            {
                var rpcMethod = rpcInterface.GetMethods().FirstOrDefault(x => x.Name == action.ActionMethod.Name);

                if (rpcMethod is null)
                {
                    /* if interface does not contain the action */
                    /* just skip and let ASP.NET Core process it */
                    continue;
                }

                var rpcRouteAttributes = rpcMethod.GetCustomAttributes<HttpMethodAttribute>(true);

                if (rpcRouteAttributes.Count() != 1)
                {
                    throw new ConstraintException($"RPC interface `{rpcInterface.Name}.{rpcMethod.Name}` must define only one HttpMethodAttribute");
                }

                action.Selectors.Clear();

                foreach (var rpcActionRoute in rpcRouteAttributes)
                {
                    if (rpcActionRoute is not null)
                    {
                        var routeModel = new AttributeRouteModel(rpcActionRoute);
                        var selector = new SelectorModel()
                        {
                            AttributeRouteModel = routeModel,
                            ActionConstraints = { new HttpMethodActionConstraint(rpcActionRoute.HttpMethods.ToArray()) }
                        };
                        action.Selectors.Add(selector);
                    }

                    for (var i = 0; i < action.Parameters.Count; i++)
                    {
                        // Replace actionModel.Parameters which defined in interfaces
                        var parameterInfo = rpcMethod.GetParameters()[i];

                        if (parameterInfo.Name != action.Parameters[i].ParameterName)
                        {
                            var interfaceParameter = $"{rpcInterface.Name}.{rpcMethod.Name}.{parameterInfo.Name}";
                            var controllerParameter = $"{controller.ControllerType.Name}.{action.ActionMethod.Name}.{action.Parameters[i].ParameterName}";

                            throw new ConstraintException($"RPC {interfaceParameter} and {controllerParameter} must have the same parameter name.");
                        }

                        var parameterType = parameterInfo.ParameterType;
                        var bindingSourceAttribute = parameterInfo.GetCustomAttributes(true)
                            .FirstOrDefault(x => x is IBindingSourceMetadata) as IBindingSourceMetadata;
                        var parameterModel = action.Parameters[i] = new ParameterModel(parameterInfo, []);

                        parameterModel.Action = action;
                        parameterModel.BindingInfo = null;
                        parameterModel.ParameterName = parameterInfo.Name;

                        // Process BindingSource of Parameter
                        // Default behavior should bs same as ASP.NET Core
                        if (bindingSourceAttribute is not null)
                        {
                            // skip if parameter has BindingSourceAttribute defined
                        }
                        else if (IsAssignableTo(parameterType, typeof(IFormFile)))
                        {
                            bindingSourceAttribute = new FromFormAttribute();
                        }
                        else if (IsAssignableTo(parameterType, typeof(IEnumerable<IFormFile>)))
                        {
                            // Use FornForm if parameters is IFormFile[]
                            bindingSourceAttribute = new FromFormAttribute();
                        }
                        else if (
                            rpcRouteAttributes.Any(x => x?.Template?.Contains("{" + parameterInfo.Name + "}") == true) ||
                            controllerRouteAttributes.Any(x => x?.Template?.Contains("{" + parameterInfo.Name + "}") == true))
                        {
                            // Use FromRoute if there has a route parameter
                            bindingSourceAttribute = new FromRouteAttribute();
                        }
                        else if (
                            !ModelBindingHelper.IsSimpleType(parameterType) &&
                            rpcRouteAttributes!.First()!.HttpMethods.Any(x => ModelBindingHelper.IsMethodSupportBody(x)))
                        {
                            // Use FromBody if parameter is not simple type and method is POST, PUT, PATCH
                            bindingSourceAttribute = new FromBodyAttribute();
                        }
                        else
                        {
                            // Use FromQuery for other conditions
                            bindingSourceAttribute = new FromQueryAttribute();
                        }

                        parameterModel.BindingInfo = BindingInfo.GetBindingInfo([bindingSourceAttribute!]);
                    }
                }
            }
        }
    }
}
