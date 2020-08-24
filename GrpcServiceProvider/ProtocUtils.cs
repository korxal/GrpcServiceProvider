using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace GrpcServiceProvider
{
    class ProtocUtils
    {
        //This method invokes protoc executable to generate cs files from proto file
        public static void BuildGrpcSourceFiles(string Protoss, string ServiceName,out string protossSourcePath, out string grpcSourcePath)
        {
            var home = AppDomain.CurrentDomain.BaseDirectory;
            if (!File.Exists(home + "protoc.exe")) throw new FileNotFoundException("protoc is missing from " + home); 
            if (!File.Exists(home + "grpc_csharp_plugin.exe")) throw new FileNotFoundException("grpc_csharp_plugin is missing from " + home);

            //Generated proto file will be stored in this folder
            string protossDirPath = home + @"Protos";
            string protossPath = home + @"Protos\"+ServiceName+".proto";
            protossSourcePath = home + @"out\"+ServiceName+".cs";
            grpcSourcePath = home + @"out\"+ServiceName+"Grpc.cs";
            if (!Directory.Exists(home + "\\Protos")) Directory.CreateDirectory(home + "\\Protos");
            if (!Directory.Exists(home + "\\Out")) Directory.CreateDirectory(home + "\\Out");
            File.WriteAllText(protossPath, Protoss);
            if (File.Exists(protossSourcePath)) File.Delete(protossSourcePath);
            if (File.Exists(grpcSourcePath)) File.Delete(grpcSourcePath);

            //Call protoc executable
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = "protoc.exe";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = " --csharp_out Out  \"" + protossPath + "\" --proto_path \"" + protossDirPath + "\"  --grpc_out Out --grpc_opt=no_client --plugin=protoc-gen-grpc=\"" + home + "grpc_csharp_plugin.exe\"";
            startInfo.WorkingDirectory = home;

            using (Process exeProcess = Process.Start(startInfo))
            {
                exeProcess.WaitForExit();
            }
            if (!File.Exists(protossSourcePath)) throw new Exception("Protoc compilation failed, check console log for errors. Missing file:" + protossSourcePath);

        }
    }
}
