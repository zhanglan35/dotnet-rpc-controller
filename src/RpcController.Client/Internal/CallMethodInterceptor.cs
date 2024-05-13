using System.Reflection;
using Castle.DynamicProxy;

namespace RpcController.Client.Core;

internal class CallMethodException : Exception
{
    public MethodInfo Method { get; }
    public object[] Arguments { get; }

    public CallMethodException(MethodInfo method, object[] arguments)
    {
        Method = method;
        Arguments = arguments;
    }
}

/* Castle Dynamic Proxy */
/* This is a workround to get the calling method and arguments by throw the Exception */
internal class CallMethodInterceptor : IInterceptor
{
    public void Intercept(IInvocation invocation)
    {
        throw new CallMethodException(invocation.Method, invocation.Arguments);
    }
}
