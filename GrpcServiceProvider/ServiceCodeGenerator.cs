using Microsoft.CodeAnalysis;
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

        private string ConvertType(FieldInfo ft, string prefix)
        {
            switch (ft.FieldType.FullName)
            {
                case "System.DateTime":
                    return "Timestamp.FromDateTime(" + prefix + "." + ft.Name + ".ToUniversalTime())";

                case "System.Decimal":
                    return "(double)" + prefix + "." + ft.Name;
                case "System.Double":
                case "System.String":
                case "System.Int32":
                case "System.Int64":
                case "System.Boolean":
                case "System.Single":
                    return prefix + "." + ft.Name;
                case "System.Char":
                    return prefix + "." + ft.Name + ".ToString()";

                default: throw new Exception("Unsupported type - " + ft.FieldType.Name);
            }
        }

        // rez.QuoteField.Balances.AddRange(R.Balances.Select(x=> new Money() {



        private string GenerateGenericAssignement(string TargetPrefix, string SourcePrefix, string FieldPostfix, FieldInfo f)
        {
            StringBuilder sb = new StringBuilder();

            var NestedType = f.FieldType.GetGenericArguments()[0];

            sb.AppendLine("if(" + SourcePrefix + f.Name + "!=null)");
            sb.AppendLine(TargetPrefix + f.Name  + ".AddRange(" + SourcePrefix + f.Name + ".Select(x=> new " + NestedType.Name + "(){");

            sb.AppendLine(GenerateFieldAssignement("", "x","", NestedType,true));

            sb.AppendLine("));");

            return sb.ToString();
        }

        private List<StatementSyntax> GenerateGenericAssignements(string TargetPrefix, string SourcePrefix, string FieldPostfix, Type t)
        {
            List<StatementSyntax> GenericAssignements = new List<StatementSyntax>();
            foreach (FieldInfo g in t.GetFields().Where(f => f.FieldType.IsGenericType))
                GenericAssignements.Add(SyntaxFactory.ParseStatement(GenerateGenericAssignement(TargetPrefix+t.Name+ FieldPostfix+".", SourcePrefix,FieldPostfix, g)));
            return GenericAssignements;
        }


        private string GenerateFieldAssignement(string TargetPrefix, string SourcePrefix, string FieldPostfix, Type t,bool NoHeader =false)
        {
            StringBuilder sb = new StringBuilder();

           if(!NoHeader) sb.AppendLine(t.Name + FieldPostfix + " = new " + t.Name + "(){");

            foreach (var f in t.GetFields())
            {
                if (f.FieldType.Module != t.Module && !f.FieldType.IsGenericType)
                {
                    sb.AppendLine(f.Name[0].ToString().ToUpper() + f.Name.Substring(1) + " = " + ConvertType(f, SourcePrefix) + ",");
                }

                if (f.FieldType.Module != t.Module && f.FieldType.IsGenericType)
                {
                    //Skip Generic
                }

                if (f.FieldType.Module == t.Module)
                {
                    //Local class
                    sb.Append(GenerateFieldAssignement(f.Name, SourcePrefix + "." + f.Name, FieldPostfix, f.FieldType));
                    sb.Append(",\r\n");
                }

            }


            sb.AppendLine("}");



            return sb.ToString();
        }

        private MethodDeclarationSyntax CreateMethodSource(MethodInfo m)
        {
            List<StatementSyntax> MethodStatements = new List<StatementSyntax>();


            //Original method parameters
            var LocalParamList = SyntaxFactory.SeparatedList<ArgumentSyntax>();
            foreach (var par in m.GetParameters())
            {
                LocalParamList = LocalParamList.Add(
                    SyntaxFactory.Argument(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("request"),
                            SyntaxFactory.IdentifierName(par.Name[0].ToString().ToUpper() + par.Name.Substring(1))
                            )));
            }

            //if method returns simple type (string, int etc...)
            if (m.ReturnType.Module != m.Module)
            {
                MethodStatements.Add(SyntaxFactory.ParseStatement("return Task.FromResult(new " + m.Name + "Reply() { Result= bl." + m.Name + "(" + LocalParamList + ") });"));
            }
            else
            {
                //if original method returns custom class... 
                MethodStatements.Add(SyntaxFactory.ParseStatement("var R = bl." + m.Name + "(" + LocalParamList + ");"));
                var a = GenerateFieldAssignement("rez.", "R", "Field", m.ReturnType); //Recusivly parse all class tree
                var v = "var rez = new  " + m.Name + "Reply() {" + a + "};";
                MethodStatements.Add(SyntaxFactory.ParseStatement(v));
                MethodStatements.AddRange(GenerateGenericAssignements("rez.", "R.", "Field", m.ReturnType));
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
