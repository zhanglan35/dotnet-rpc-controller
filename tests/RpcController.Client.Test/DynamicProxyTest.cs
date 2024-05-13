using Castle.DynamicProxy;
using Microsoft.AspNetCore.Mvc;

namespace RpcController.Client;

// RpcClient Dnynamic Proxy Test
// To validate the features required to implement the IRpcClient dynamic proxy
// Basic requirements:
//   1. Given interface ICalculator
//   2. Use IProxyGenerator to dynamically generate a proxy object of ICalculator
//   3. Use CalculatorClient.Call() method to call the method in ICalculator and return CallResult
// Test Validation
//   1. Support proxy interface
//   2. Support synchronous method call
//   3. Support asynchronous method call
//   4. Support exception handling
//   5. Support void method call
[HttpRoute("/api/v1")]
public interface ICalculator : IRpcController
{
    [HttpGet("add")]
    int Add(int a, int b);

    [HttpGet("add-async")]
    Task<int> AddAsync(int a, int b);

    [HttpGet("add-exception")]
    Task<int> AddException(int a, int b);

    [HttpGet("void")]
    void Void(int a, int b);
}

// 统一返回的异常格式
public class CallCalculatorException : Exception
{
    public object[] Arguments { get; }

    public CallCalculatorException(string message, object[] arguments) : base(message)
    {
        Arguments = arguments;
    }
}

// 动态代理拦截器
public class DynamicProxyInterceptor : IInterceptor
{
    public void Intercept(IInvocation invocation)
    {
        var method = invocation.Method;
        var returnType = method.ReturnType;
        var args = invocation.Arguments;
        var result = (int) args[0] + (int) args[1];

        if (method.Name == "AddException")
        {
            throw new CallCalculatorException("Something wrong", args);
        }
        else if (returnType.IsAssignableTo(typeof(Task)))
        {
            invocation.ReturnValue = Task.FromResult(result);
        }
        else
        {
            invocation.ReturnValue = result;
        }
    }
}

// Use RpcClient to call the method of ICalculator
public static class CalculatorClient
{
    static readonly ProxyGenerator ProxyGenerator = new();

    public class CallResult
    {
        public bool IsSuccess { get; set; }
        public object? Result { get; set; }
        public CallCalculatorException? Exception { get; set; }

        public CallResult(object? result)
        {
            IsSuccess = true;
            Result = result;
        }

        public CallResult(CallCalculatorException exception)
        {
            IsSuccess = false;
            Exception = exception;
        }
    }

    public static CallResult Call<T>(Func<ICalculator, T> callback)
    {
        var proxy = ProxyGenerator.CreateInterfaceProxyWithoutTarget<ICalculator>(new DynamicProxyInterceptor());

        try
        {
            var result = callback(proxy);
            return new CallResult(result!);
        }
        catch(CallCalculatorException exception)
        {
            return new CallResult(exception);
        }
    }
}

public class DynamicProxyTest
{
    static readonly ProxyGenerator ProxyGenerator = new();

    [Fact]
    public void Void()
    {
        var interceptor = new DynamicProxyInterceptor();
        var proxy = ProxyGenerator.CreateInterfaceProxyWithoutTarget<ICalculator>(interceptor);

        proxy.Void(0, 0);
    }

    [Fact]
    public void Add()
    {
        var interceptor = new DynamicProxyInterceptor();
        var proxy = ProxyGenerator.CreateInterfaceProxyWithoutTarget<ICalculator>(interceptor);
        var result = proxy.Add(1, 5);

        result.ShouldBe(6);
    }

    [Fact]
    public async Task Add_Async()
    {
        var interceptor = new DynamicProxyInterceptor();
        var proxy = ProxyGenerator.CreateInterfaceProxyWithoutTarget<ICalculator>(interceptor);
        var result = await proxy.AddAsync(2, 2);

        result.ShouldBe(4);
    }

    [Fact]
    public async Task Add_Exception()
    {
        var interceptor = new DynamicProxyInterceptor();
        var proxy = ProxyGenerator.CreateInterfaceProxyWithoutTarget<ICalculator>(interceptor);
        var exception = await Assert.ThrowsAsync<CallCalculatorException>(() => proxy.AddException(2, 2));

        exception.Message.ShouldBe("Something wrong");
        exception.Arguments.Length.ShouldBe(2);
        exception.Arguments[0].ShouldBe(2);
        exception.Arguments[1].ShouldBe(2);
    }

    [Fact]
    public void Call_Add()
    {
        var result = CalculatorClient.Call(proxy => proxy.Add(1, 5));

        result.IsSuccess.ShouldBeTrue();
        result.Result.ShouldBe(6);
    }

    [Fact]
    public void Call_Add_Exception()
    {
        var result = CalculatorClient.Call(proxy => proxy.AddException(1, 5));

        result.IsSuccess.ShouldBeFalse();
        result.Exception.ShouldNotBeNull();
        result.Exception!.Message.ShouldBe("Something wrong");
        result.Exception!.Arguments.Length.ShouldBe(2);
        result.Exception!.Arguments[0].ShouldBe(1);
        result.Exception!.Arguments[1].ShouldBe(5);
    }
}
