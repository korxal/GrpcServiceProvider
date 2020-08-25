using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


namespace ExampleService
{

    //This class will be exposed as GRPC service
    //Class must be public
    //All public must heave  class or List<T> as return type
    public class Service
    {
        public string SayHello()
        {
            return "Hello world!";
        }

        public string Greet(string Name)
        {
            return "Hello " + Name;
        }
    }


    //Default web server setup
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc();
        }
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {

                //Provider requires paths to protoc and grpc plugin
                //Examples below is for Windows
                GrpcService.GrpcServiceProvider provider = new GrpcService.GrpcServiceProvider(
                    @"%USERPROFILE%\.nuget\packages\grpc.tools\2.31.0\tools\windows_x64\protoc.exe",
                    @"%USERPROFILE%\.nuget\packages\grpc.tools\2.31.0\tools\windows_x64\grpc_csharp_plugin.exe");

                //This is where all the magic happens
                provider.MapEndpoint<Service>(endpoints, this);

                
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Service started! Try to call SayHello grpc method using GRPC client. Proto file can be viewed at /Proto/" + provider.ServiceName + ".proto");
                });

                //For convinience
                endpoints.MapGet("/Proto/"+provider.ServiceName+".proto", async context =>
                {
                    await context.Response.WriteAsync(provider.Proto);
                });


            });
        }
    }

    //Default web server setup
    class Program
    {
        static void Main(string[] args)
        {
            Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureKestrel(options=> {
                    options.ListenLocalhost(5000, listenOptions =>
                     {
                         //required by grpc
                         listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
                     });
                });
                webBuilder.UseStartup<Startup>();
            }).Build().Run();
        }
    }
}
