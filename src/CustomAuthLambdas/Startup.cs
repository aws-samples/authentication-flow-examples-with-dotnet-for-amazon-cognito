using Microsoft.Extensions.DependencyInjection;

namespace AuthLambdas;

[Amazon.Lambda.Annotations.LambdaStartup]
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
    }
}
