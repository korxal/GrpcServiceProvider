﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GrpcServiceProvider
{
    class GrpcCodeGenerator
    {

        class FieldAssignement
        {
            public string Result;
            public List<StatementSyntax> GenericAssignements;
        }


        private string ConvertFieldType(FieldInfo ft, string prefix)
        {
            return ConvertTypeToProto(ft.FieldType.FullName, ft.Name, prefix);
        }


        private string ConvertFieldType(string FieldtypeName, string FieldName, string prefix)
        {
            return ConvertTypeToProto(FieldtypeName, FieldName, "");
        }

        private string ConvertTypeToProto(string FieldtypeName, string FieldName, string prefix = "")
        {
            switch (FieldtypeName)
            {
                case "System.DateTime":
                    return "Timestamp.FromDateTime(" + prefix + FieldName + ".ToUniversalTime())";

                case "System.Decimal":
                    return "(double)" + prefix + FieldName;
                case "System.Double":
                case "System.Int32":
                case "System.Int64":
                case "System.Boolean":
                case "System.Single":
                    return prefix + FieldName;
                case "System.Char":
                    return prefix + FieldName + ".ToString()";

                case "System.String":
                    return prefix + FieldName + " ==null? \"\": " + prefix + FieldName;

                default: throw new Exception("Unsupported type - " + FieldtypeName);
            }
        }

        private string ConvertTypeFromProto(string FieldtypeName, string FieldName, string prefix = "")
        {
            switch (FieldtypeName)
            {
                case "System.DateTime":
                    return prefix + FieldName + ".ToDateTime()";
                case "System.Decimal":
                    return "(decimal)" + prefix + FieldName;
                case "System.Double":
                case "System.String":
                case "System.Int32":
                case "System.Int64":
                case "System.Boolean":
                case "System.Single":
                    return prefix + FieldName;
                case "System.Char":
                    return prefix + FieldName + ".ToString()";

                default: throw new Exception("Unsupported type - " + FieldtypeName);
            }
        }

        // rez.QuoteField.Balances.AddRange(R.Balances.Select(x=> new Money() {



        private string GenerateGenericAssignement(string TargetPrefix, string SourcePrefix, string FieldPostfix, FieldInfo f, int depth = 0)
        {
            StringBuilder sb = new StringBuilder();

            var NestedType = f.FieldType.GetGenericArguments()[0];

            sb.AppendLine("if(" + SourcePrefix + f.Name + "!=null){");

            sb.AppendLine("for( int l" + depth + "=0; l"+depth+"< " + SourcePrefix + f.Name + ".Count;l"+depth+"++){");

            sb.AppendLine(TargetPrefix + f.Name + ".Add( new " + NestedType.Name + "() {");




            sb.AppendLine(GenerateFieldAssignement(SourcePrefix + f.Name+"[l"+depth+"]", FieldPostfix, f.Name+FieldPostfix, TargetPrefix, NestedType, true).Result);

            //foreach end
            sb.AppendLine(");");


            foreach (FieldInfo g in NestedType.GetFields().Where(f => f.FieldType.IsGenericType))
            {
                sb.AppendLine(GenerateGenericAssignement(TargetPrefix + f.Name+ "[l" + depth + "].", SourcePrefix + f.Name + "[l" + depth + "].", FieldPostfix, g, depth + 1));
            }

            sb.AppendLine("}}");
            return sb.ToString();
        }

        private List<StatementSyntax> GenerateGenericAssignements(string TargetPrefix, string SourcePrefix, string FieldPostfix, Type t)
        {
            List<StatementSyntax> GenericAssignements = new List<StatementSyntax>();
            foreach (FieldInfo g in t.GetFields().Where(f => f.FieldType.IsGenericType))
                GenericAssignements.Add(SyntaxFactory.ParseStatement(GenerateGenericAssignement(TargetPrefix + t.Name + FieldPostfix + ".", SourcePrefix, FieldPostfix, g)));
            return GenericAssignements;
        }


        private FieldAssignement GenerateFieldAssignement(string SourcePrefix, string FieldPostfix, string FieldName, string TargetPrefix, Type t, bool NoHeader = false)
        {
            StringBuilder sb = new StringBuilder();
            List<StatementSyntax> ass = new List<StatementSyntax>();

            if (!NoHeader) sb.AppendLine(FieldName[0].ToString().ToUpper() + FieldName.Substring(1) + FieldPostfix + " =  " + SourcePrefix + "==null? null:   new " + t.Name + "(){");

            foreach (var f in t.GetFields())
            {
                if (f.FieldType.Module != t.Module && !f.FieldType.IsGenericType)
                {
                    sb.AppendLine(f.Name[0].ToString().ToUpper() + f.Name.Substring(1) + " = " + ConvertFieldType(f, SourcePrefix + ".") + ",");
                }

                if (f.FieldType.Module != t.Module && f.FieldType.IsGenericType)
                {
                    //Skip Generic
                    // ass.AddRange(GenerateGenericAssignements(TargetPrefix + ".", SourcePrefix + ".", "Field", t));
                }

                if (f.FieldType.Module == t.Module)
                {
                    //Local class
                    var r = GenerateFieldAssignement(SourcePrefix + "." + f.Name, FieldPostfix, f.Name, TargetPrefix, f.FieldType);
                    ass.AddRange(r.GenericAssignements);
                    sb.Append(r.Result);
                    sb.Append(",\r\n");
                }

            }
            sb.AppendLine("}");
            return new FieldAssignement() { Result = sb.ToString(), GenericAssignements = ass };
        }

        private MethodDeclarationSyntax CreateMethodSource(MethodInfo m)
        {
            List<StatementSyntax> MethodStatements = new List<StatementSyntax>();


            //Original method parameters
            StringBuilder LocalParamList = new StringBuilder();
            foreach (var par in m.GetParameters())
            {
                LocalParamList.Append(ConvertTypeFromProto(par.ParameterType.FullName, "request." + par.Name[0].ToString().ToUpper() + par.Name.Substring(1)) + " ,");
            }
            if (LocalParamList.Length > 0) LocalParamList.Length--;


            //if method returns simple type (string, int etc...)
            if (m.ReturnType.Module != m.Module)
            {
                var c = ConvertTypeToProto(m.ReturnType.FullName, "bl." + m.Name + "(" + LocalParamList + ")");
                MethodStatements.Add(SyntaxFactory.ParseStatement("return Task.FromResult(new " + m.Name + "Reply() { Result= " + c + " });"));
            }
            else
            {
                //if original method returns custom class... 
                MethodStatements.Add(SyntaxFactory.ParseStatement("var R = bl." + m.Name + "(" + LocalParamList + ");"));
                var a = GenerateFieldAssignement("R", "Field", m.ReturnType.Name, "rez", m.ReturnType); //Recusivly parse all class tree



                var v = "var rez = new  " + m.Name + "Reply() {" + a.Result + "};";
                MethodStatements.Add(SyntaxFactory.ParseStatement(v));


                MethodStatements.AddRange(GenerateGenericAssignements("rez.", "R.", "Field", m.ReturnType));


                MethodStatements.AddRange(a.GenericAssignements);
                MethodStatements.Add(SyntaxFactory.ParseStatement("return Task.FromResult(rez);"));

            }


            var MethodBody = SyntaxFactory.Block(MethodStatements);

            var MethodReturn = SyntaxFactory.GenericName("Task").WithTypeArgumentList(SyntaxFactory.TypeArgumentList()
                .AddArguments(SyntaxFactory.IdentifierName(m.Name + "Reply"))
                );
            var ps = SyntaxFactory.Parameter(SyntaxFactory.Identifier("request")).WithType(SyntaxFactory.IdentifierName(m.Name + "Request"));
            var context = SyntaxFactory.Parameter(SyntaxFactory.Identifier("context")).WithType(SyntaxFactory.IdentifierName("ServerCallContext"));
            var ParamList = SyntaxFactory.SeparatedList<ParameterSyntax>();
            ParamList = ParamList.Add(ps);
            ParamList = ParamList.Add(context);

            var MethodParams = SyntaxFactory.ParameterList(ParamList);

            var methodDeclaration = SyntaxFactory.MethodDeclaration(MethodReturn, m.Name)
    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
    .AddModifiers(SyntaxFactory.Token(SyntaxKind.OverrideKeyword))
    .WithParameterList(MethodParams)
    .WithBody(MethodBody);

            return methodDeclaration;

        }

        //This method writes GRPC service code to be compiled later
        public string CreateServiceSource(Type T)
        {

            var sf = SyntaxFactory.CompilationUnit();
            sf = sf.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")));
            sf = sf.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Linq")));
            sf = sf.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Threading.Tasks")));
            sf = sf.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Collections")));
            sf = sf.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Collections.Generic")));
            sf = sf.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Grpc.Core")));
            sf = sf.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Google.Protobuf.WellKnownTypes")));
            sf = sf.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(T.Name)));

            var ns = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(T.Name + "GRPCService")).NormalizeWhitespace();
            var cd = SyntaxFactory.ClassDeclaration(T.Name + "ServiceProvider");//Class declaration
            cd = cd.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            cd = cd.AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(T.Name + "Service." + T.Name + "ServiceBase")));


            //This creates an instance of original class to call original methods
            //Original class must have parameterless constructor
            var ci = SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(T.Namespace + '.' + T.Name)) //Class Instance
                .AddVariables(SyntaxFactory.VariableDeclarator("bl")
                .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.ObjectCreationExpression(SyntaxFactory.QualifiedName(
                                            SyntaxFactory.IdentifierName(T.Namespace),
                                            SyntaxFactory.IdentifierName(T.Name)))
                                    .WithArgumentList(SyntaxFactory.ArgumentList()))));
            var fi = SyntaxFactory.FieldDeclaration(ci)//
             .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
             .AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
            cd = cd.AddMembers(fi);

            //Map all GRPC mehods to their methods in original class
            foreach (MethodInfo v in T.GetMethods())
            {
                if (v.Module != T.Module) continue;// exclude  ToString, GetHashCode...
                var methodSource = CreateMethodSource(v);
                cd = cd.AddMembers(methodSource);
            }

            //Wrap up
            ns = ns.AddMembers(cd);
            sf = sf.AddMembers(ns);
            var code = sf
               .NormalizeWhitespace()
               .ToFullString();
            return code;
        }

    }
}
