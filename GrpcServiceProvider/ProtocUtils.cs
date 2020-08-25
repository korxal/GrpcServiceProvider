using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace GrpcServiceProvider
{
    class ProtocUtils
    {
        static bool isWindows = System.Runtime.InteropServices.RuntimeInformation
                                               .IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);

        //This method invokes protoc executable to generate cs files from proto file
        public static void BuildGrpcSourceFiles(string Protoss, string ServiceName, out string protossSourcePath, out string grpcSourcePath, string ProtocPath, string GrpcPluginPath)
        {

            if (isWindows) BuildGrpcSourceFilesWindows(Protoss, ServiceName, out protossSourcePath, out grpcSourcePath,ProtocPath, GrpcPluginPath);
            else BuildGrpcSourceFilesLinux(Protoss, ServiceName, out protossSourcePath, out grpcSourcePath, ProtocPath, GrpcPluginPath);

        }


        public static void BuildGrpcSourceFilesLinux(string Protoss, string ServiceName, out string protossSourcePath, out string grpcSourcePath, string ProtocPath, string GrpcPluginPath)
        {
            var home = AppDomain.CurrentDomain.BaseDirectory;
            if (!File.Exists(GrpcPluginPath)) throw new FileNotFoundException("grpc_csharp_plugin is missing from " + home);

            //Generated proto file will be stored in this folder
            string protossDirPath = home + @"protos";
            string protossPath = home + @"protos/" + ServiceName + ".proto";
            protossSourcePath = home + @"out/" + ServiceName + ".cs";
            grpcSourcePath = home + @"out/" + ServiceName + "Grpc.cs";
            if (!Directory.Exists(home + "/protos")) Directory.CreateDirectory(home + "/protos");
            if (!Directory.Exists(home + "/out")) Directory.CreateDirectory(home + "/out");
            File.WriteAllText(protossPath, Protoss);
            if (File.Exists(protossSourcePath)) File.Delete(protossSourcePath);
            if (File.Exists(grpcSourcePath)) File.Delete(grpcSourcePath);

            //Call protoc executable
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = "protoc";
            //startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = " --csharp_out out  \"" + protossPath + "\" --proto_path \"" + protossDirPath + "\"  --grpc_out out --grpc_opt=no_client --plugin=protoc-gen-grpc=\"" + GrpcPluginPath + "\"";
            startInfo.WorkingDirectory = home;

            using (Process exeProcess = Process.Start(startInfo))
            {
                exeProcess.WaitForExit();
            }
            if (!File.Exists(protossSourcePath)) throw new Exception("Protoc compilation failed, check console log for errors. Missing file:" + protossSourcePath);

        }


        public static void BuildGrpcSourceFilesWindows(string Protoss, string ServiceName, out string protossSourcePath, out string grpcSourcePath, string ProtocPath, string GrpcPluginPath)
        {
            var home = AppDomain.CurrentDomain.BaseDirectory;
            ProtocPath = Environment.ExpandEnvironmentVariables(ProtocPath);
            GrpcPluginPath = Environment.ExpandEnvironmentVariables(GrpcPluginPath);


            if (!File.Exists(ProtocPath)) throw new FileNotFoundException("protoc is missing from " + ProtocPath);
            if (!File.Exists(GrpcPluginPath)) throw new FileNotFoundException("grpc_csharp_plugin is missing from " + GrpcPluginPath);

            //Generated proto file will be stored in this folder
            string protossDirPath = home + @"Protos";
            string protossPath = home + @"Protos\" + ServiceName + ".proto";
            protossSourcePath = home + @"out\" + ServiceName + ".cs";
            grpcSourcePath = home + @"out\" + ServiceName + "Grpc.cs";
            if (!Directory.Exists(home + "\\Protos")) Directory.CreateDirectory(home + "\\Protos");
            if (!Directory.Exists(home + "\\Out")) Directory.CreateDirectory(home + "\\Out");
            File.WriteAllText(protossPath, Protoss);
            if (File.Exists(protossSourcePath)) File.Delete(protossSourcePath);
            if (File.Exists(grpcSourcePath)) File.Delete(grpcSourcePath);

            //Call protoc executable
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = ProtocPath;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = " --csharp_out Out  \"" + protossPath + "\" --proto_path \"" + protossDirPath + "\"  --grpc_out Out --grpc_opt=no_client --plugin=protoc-gen-grpc=\"" + GrpcPluginPath + "\"";
            startInfo.WorkingDirectory = home;

            using (Process exeProcess = Process.Start(startInfo))
            {
                exeProcess.WaitForExit();
            }
            if (!File.Exists(protossSourcePath)) throw new Exception("Protoc compilation failed, check console log for errors. Missing file:" + protossSourcePath);

        }
    }
}
