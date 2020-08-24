using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace GrpcServiceProvider
{
    class GrpcCodeCompiler
    {

        //This method compiles everything in one assembly
        public static Assembly CompileGrpcServiceSources(string protossSourcePath, string grpcSourcePath, string GrpcOverrideSource, Type BaseServiceType, string ServiceName)
        {
            var home = AppDomain.CurrentDomain.BaseDirectory;
            File.WriteAllText(home + "Out\\" + ServiceName + "OverrideSource.cs", GrpcOverrideSource);
            string ProtossTreeSource = File.ReadAllText(protossSourcePath);//Output of protoc executable, 
            string GrpcTreeSource = File.ReadAllText(grpcSourcePath);//Output of protoc executable
            SyntaxTree GrpcTree = CSharpSyntaxTree.ParseText(GrpcTreeSource); //Output of runtime code writer
            SyntaxTree ProtossTree = CSharpSyntaxTree.ParseText(ProtossTreeSource);
            SyntaxTree GrpcOverrideTree = CSharpSyntaxTree.ParseText(GrpcOverrideSource);

            //References to include in our new assembly
            List<MetadataReference> references = new List<MetadataReference>();

            
                
            references.Add(MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location));
            references.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Core").Location));
            references.Add(MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location));
            references.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location));
            references.Add(MetadataReference.CreateFromFile(Assembly.Load("Grpc.Core.Api").Location));
            references.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Threading.Tasks").Location));
            references.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location));


            references.Add(MetadataReference.CreateFromFile(typeof(string).GetTypeInfo().Assembly.Location));
            references.Add(MetadataReference.CreateFromFile(typeof(Google.Protobuf.MessageParser).GetTypeInfo().Assembly.Location));

            //Adds reference to our original class from which we create grpc service
            references.Add(MetadataReference.CreateFromFile(BaseServiceType.Assembly.Location));

            CSharpCompilation compilation = CSharpCompilation.Create(
            BaseServiceType + "Service", //Assembly Name
            syntaxTrees: new[] { ProtossTree, GrpcTree, GrpcOverrideTree }, //Sources to include 
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));


            using (var ms = new MemoryStream())
            {
                EmitResult compiled = compilation.Emit(ms); //This is where compilation happens
                if (!compiled.Success)
                {
                    IEnumerable<Diagnostic> failures = compiled.Diagnostics.Where(diagnostic =>
                        diagnostic.Severity == DiagnosticSeverity.Error);
                    StringBuilder sb = new StringBuilder();
                    sb.Append("Failed to compile protoss:\r\n");
                    foreach (Diagnostic diagnostic in failures)
                    {
                        sb.Append(diagnostic.Location + ":" + diagnostic.GetMessage());
                        sb.Append("\r\n");
                    }
                    throw new Exception(sb.ToString());//Bad case
                }
                else
                {
                    ms.Seek(0, SeekOrigin.Begin);//Reset stream position to beginnig
                    Assembly assembly = AssemblyLoadContext.Default.LoadFromStream(ms);//Load new assembly to memory
                    return assembly;
                }
            }
        }
    }
}
