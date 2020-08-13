using System;
using System.Collections.Generic;
using System.Text;

namespace GrpcServiceProvider
{
    class ProtoGenerator
    {

        //This contains Assembly name with original class to be used in service
        private string BaseAssemblyName;


        private static string GenerateMethodDefinition(string Name)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("rpc ");
            sb.Append(Name);
            sb.Append("(");
            sb.Append(Name);
            sb.Append("Request");
            sb.Append(") returns (");
            sb.Append(Name);
            sb.Append("Reply");
            sb.Append(");\r\n");
            return sb.ToString();
        }





        private string ParseSimpleType(Type t, int i = 1, string name = "result")
        {
            StringBuilder sb = new StringBuilder();
            switch (t.Name)
            {
                case "String":
                    sb.Append("  string ");
                    break;
                case "Int32":
                case "int":
                    sb.Append("  sint32 ");
                    break;
                case "DateTime":
                    sb.Append("  google.protobuf.Timestamp ");
                    break;
                case "Double":
                case "Decimal":
                    sb.Append("  double ");
                    break;
                case "Boolean":
                    sb.Append("  bool ");
                    break;

                case "Single":
                    sb.Append("  float ");
                    break;

                case "Char":
                    sb.Append("  string ");
                    break;

                case "Int64":
                    sb.Append("  sint64 ");
                    break;

                default:
                    throw new Exception("Unsuported type:" + t.Name);
            }

            sb.Append(name);
            sb.Append(" = ");
            sb.Append(i);
            sb.Append(";\r\n");
            return sb.ToString();
        }


        private string ParseClassType(Type t)
        {
            int i = 1;
            StringBuilder sb = new StringBuilder();
            StringBuilder ChildMessages = new StringBuilder();
            sb.Append("message ");
            sb.Append(t.Name); sb.Append(" {\r\n");

            foreach (var f in t.GetFields())
            {
                if (f.FieldType.Assembly.FullName == BaseAssemblyName)
                {
                    ChildMessages.Append(ParseClassType(f.FieldType));
                    sb.Append("  ");
                    sb.Append(f.FieldType.Name);
                    sb.Append(" ");
                    sb.Append(f.FieldType.Name);
                    sb.Append("Field = ");
                    sb.Append(i);
                    sb.Append(";\r\n");
                }
                else
                {
                    if (f.FieldType.IsGenericType)
                    {
                        var NestedType = f.FieldType.GetGenericArguments()[0];
                        ChildMessages.Append(ParseClassType(NestedType));
                        sb.Append("  repeated ");
                        sb.Append(NestedType.Name);
                        sb.Append(" ");
                        sb.Append(f.Name);
                        sb.Append(" = ");
                        sb.Append(i);
                        sb.Append(";\r\n");
                    }
                    else
                        sb.Append(ParseSimpleType(f.FieldType, i, f.Name));
                }

                i++;
            }
            sb.Append("}\r\n\r\n");
            sb.Append(ChildMessages);
            return sb.ToString();
        }


        private string GenerateMessage(string name, Type ReturnType, bool isReply = false)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("message ");
            bool IsInternal = ReturnType.Assembly.FullName == BaseAssemblyName;
            sb.Append(name);
            if (isReply) sb.Append("Reply");
            sb.Append(" {\r\n");
            if (!IsInternal)
            {
                sb.Append(ParseSimpleType(ReturnType));
            }
            else
            {
                sb.Append(ReturnType.Name);
                sb.Append(" ");
                sb.Append(ReturnType.Name);
                sb.Append("Field = 1;\r\n");
            }
            sb.Append("}\r\n\r\n");

            if (IsInternal)
            {
                sb.Append(ParseClassType(ReturnType));
            }

            return sb.ToString();
        }


        //This method converts imput type to proto file
        public string GenerateProtoss(Type T)
        {
            BaseAssemblyName = T.Assembly.FullName;
            StringBuilder Messages = new StringBuilder(); //Message definitions
            StringBuilder ServiceDefinition = new StringBuilder(); //Service definitions

            string BaseName = T.Name;
            string NameSpace = T.Namespace;
            string ServiceName = T.Name + "Service";

            //proto file header
            ServiceDefinition.Append("syntax = \"proto3\";\r\nimport \"timestamp.proto\";\r\noption csharp_namespace = \"");
            ServiceDefinition.Append(BaseName);
            ServiceDefinition.Append("\";\r\npackage ");
            ServiceDefinition.Append(BaseName);
            ServiceDefinition.Append(";\r\nservice ");
            ServiceDefinition.Append(ServiceName);
            ServiceDefinition.Append("{\r\n");

            //Interate through original class methods 
            foreach (var v in T.GetMethods())
            {
                if (v.Module != T.Module) continue;// exclude native methods (ToString, GetHashCode etc...)

                //For each method we need Method definition and Request and Reply messages
                ServiceDefinition.Append(GenerateMethodDefinition(v.Name));
                Messages.Append(GenerateMessage(v.Name, v.ReturnType, true));//Reply - method execution result
                Messages.Append(GenerateMessageRequest(v.Name, v.GetParameters()));//Request - method parameters
            }
            ServiceDefinition.Append("}\r\n\r\n");

            ServiceDefinition.Append(Messages);
            return ServiceDefinition.ToString(); ;
        }

        //This method constructs grpc message for orignal method parameters
        private string GenerateMessageRequest(string name, System.Reflection.ParameterInfo[] parameters)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("message ");
            sb.Append(name);
            sb.Append("Request {\r\n");

            int i = 1;
            foreach (var p in parameters)
                switch (p.ParameterType.Name)
                {
                    case "string":
                    case "String":
                        sb.Append("  string " + p.Name + " = " + i + ";\r\n");
                        i++;
                        break;
                    case "int":
                    case "Int32":
                        sb.Append("  sint32 " + p.Name + " = " + i + ";\r\n");
                        i++;
                        break;
                    case "Int64":
                    case "long":
                        sb.Append("  sint64 " + p.Name + " = " + i + ";\r\n");
                        i++;
                        break;

                    case "Decimal":
                        sb.Append("  double " + p.Name + " = " + i + ";\r\n");
                        i++;
                        break;

                    case "DateTime":
                        sb.Append("  google.protobuf.Timestamp " + p.Name + " = " + i + ";\r\n");
                        i++;
                        break;

                    case "Boolean":
                        sb.Append("  bool " + p.Name + " = " + i + ";\r\n");
                        i++;
                        break;

                    default:
                        throw new Exception("Unknown type: " + p.ParameterType.Name);
                }

            sb.Append("}\r\n\r\n");
            return sb.ToString();
        }




    }
}
