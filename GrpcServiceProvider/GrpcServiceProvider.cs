using GrpcServiceProvider;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace GrpcService
{

    //Main project class
    public class GrpcServiceProvider
    {

        private readonly string protocPath;
        private readonly string grpcPluginPath;


        public GrpcServiceProvider(string ProtocPath, string GrpcPluginPath)
        {
            protocPath = ProtocPath;
            grpcPluginPath = GrpcPluginPath;
        }

        public string Proto;
        public string ServiceName;

        public object CreateService(Type T)
        {

            ProtoGenerator pg = new ProtoGenerator();
            GrpcCodeGenerator cg = new GrpcCodeGenerator();
            ServiceName = T.Name;
            //Step 1 - build proto file for our class, 
            Proto = pg.GenerateProtoss(T);
            //Step 2 -  Call 'protoc' executable to build *.cs files for our proto file. Cs files is written to 'out' folder of main executable
            ProtocUtils.BuildGrpcSourceFiles(Proto, ServiceName, out string protossSourcePath, out string grpcSourcePath, protocPath, grpcPluginPath);
            //Step 3 - Write code for our Service. This method provides source code for our grpc service
            string GrpcOverrideSource = cg.CreateServiceSource(T);
            //Step 4 - Compile everything on runtime
            Assembly GrpcServiceAssembly = GrpcCodeCompiler.CompileGrpcServiceSources(protossSourcePath, grpcSourcePath, GrpcOverrideSource, T, ServiceName);
            //Step 5 - return instance of compiled assembly
            return GrpcServiceAssembly.CreateInstance(T.Name + "GRPCService." + T.Name + "ServiceProvider");
        }

        //This method just calls vanilla extension method -  MapGrpcService
        //Since it requires Type to be defined we call it via reflection
        public void MapEndpoint<T>(IEndpointRouteBuilder endpoints, object source)
        {
            var ts = CreateService(typeof(T));
            var m = typeof(GrpcEndpointRouteBuilderExtensions).GetMethod("MapGrpcService");
            var mm = m.MakeGenericMethod(ts.GetType());
            mm.Invoke(source, new object[] { endpoints });
        }

    }
}