using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace GrpcServiceProvider
{
    class GrpcCodeGenerator
    {
        private ExpressionStatementSyntax GenerateDirectFieldAssignement(string parent, string fieldName, string parameterName, string ParameterPrefix = "")
        {
            // rez.SomeField = parameterName;
            var ss = SyntaxFactory.ExpressionStatement(
                      SyntaxFactory.AssignmentExpression(
                          SyntaxKind.SimpleAssignmentExpression,
                          SyntaxFactory.MemberAccessExpression(
                              SyntaxKind.SimpleMemberAccessExpression,
                              SyntaxFactory.MemberAccessExpression(
                                  SyntaxKind.SimpleMemberAccessExpression,
                                  SyntaxFactory.IdentifierName(parent),
                                  SyntaxFactory.IdentifierName(fieldName + "Field")),
                              SyntaxFactory.IdentifierName(parameterName[0].ToString().ToUpper() + parameterName.Substring(1))),
                      SyntaxFactory.MemberAccessExpression(
                          SyntaxKind.SimpleMemberAccessExpression,
                          SyntaxFactory.IdentifierName("R"),
                          SyntaxFactory.IdentifierName(ParameterPrefix + parameterName))));
            return ss;
        }

        private ExpressionStatementSyntax GenerateToStringFieldAssignement(string parent, string fieldName, string parameterName, string ParameterPrefix = "")
        {
            // rez.SomeField = parameterName;
            var ss = SyntaxFactory.ExpressionStatement(
                      SyntaxFactory.AssignmentExpression(
                          SyntaxKind.SimpleAssignmentExpression,
                          SyntaxFactory.MemberAccessExpression(
                              SyntaxKind.SimpleMemberAccessExpression,
                              SyntaxFactory.MemberAccessExpression(
                                  SyntaxKind.SimpleMemberAccessExpression,
                                  SyntaxFactory.IdentifierName(parent),
                                  SyntaxFactory.IdentifierName(fieldName + "Field")),
                              SyntaxFactory.IdentifierName(parameterName[0].ToString().ToUpper() + parameterName.Substring(1))),
                     SyntaxFactory.InvocationExpression(
                          SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                          SyntaxFactory.MemberAccessExpression(
                             SyntaxKind.SimpleMemberAccessExpression,
                             SyntaxFactory.IdentifierName("R"),
                             SyntaxFactory.IdentifierName(ParameterPrefix + parameterName)),
                           SyntaxFactory.IdentifierName("ToString")))));
            return ss;
        }

        private ExpressionStatementSyntax GenerateDecimalFieldAssignement(string parent, string fieldName, string parameterName, string ParameterPrefix = "")
        {
            // rez.SomeField = (double)parameterName;

            var ss = SyntaxFactory.ExpressionStatement(
                 SyntaxFactory.AssignmentExpression(
                     SyntaxKind.SimpleAssignmentExpression,
                     SyntaxFactory.MemberAccessExpression(
                         SyntaxKind.SimpleMemberAccessExpression,
                         SyntaxFactory.MemberAccessExpression(
                             SyntaxKind.SimpleMemberAccessExpression,
                             SyntaxFactory.IdentifierName(parent),
                             SyntaxFactory.IdentifierName(fieldName + "Field")),
                         SyntaxFactory.IdentifierName(parameterName)),
                     SyntaxFactory.CastExpression(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DoubleKeyword)),
                 SyntaxFactory.MemberAccessExpression(
                     SyntaxKind.SimpleMemberAccessExpression,
                     SyntaxFactory.IdentifierName("R"),
                     SyntaxFactory.IdentifierName(ParameterPrefix + parameterName)))));
            return ss;

        }

        private ExpressionStatementSyntax GenerateDateTimeFieldAssignement(string parent, string fieldName, string parameterName, string ParameterPrefix = "")
        {
            // rez.SomeField = Timestamp.FromDateTime(parameterName);

            var ss = SyntaxFactory.ExpressionStatement(
                      SyntaxFactory.AssignmentExpression(
                          SyntaxKind.SimpleAssignmentExpression,
                          SyntaxFactory.MemberAccessExpression(
                              SyntaxKind.SimpleMemberAccessExpression,
                              SyntaxFactory.MemberAccessExpression(
                                  SyntaxKind.SimpleMemberAccessExpression,
                                  SyntaxFactory.IdentifierName(parent),
                                  SyntaxFactory.IdentifierName(fieldName + "Field")),
                              SyntaxFactory.IdentifierName(parameterName)),

                          SyntaxFactory.InvocationExpression(
                              SyntaxFactory.MemberAccessExpression(
                                  SyntaxKind.SimpleMemberAccessExpression,
                                  SyntaxFactory.IdentifierName("Timestamp"),
                                  SyntaxFactory.IdentifierName("FromDateTime")))
                          .WithArgumentList(
                              SyntaxFactory.ArgumentList(
                                  SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                                      SyntaxFactory.Argument(
                                           SyntaxFactory.InvocationExpression(
                                              SyntaxFactory.MemberAccessExpression(
                                                 SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.MemberAccessExpression(
                                                      SyntaxKind.SimpleMemberAccessExpression,
                                                      SyntaxFactory.IdentifierName("R"),
                                                      SyntaxFactory.IdentifierName(ParameterPrefix + parameterName)),
                                                    SyntaxFactory.IdentifierName("ToUniversalTime")
                                              ))))))));

            return ss;

        }

        private List<ExpressionStatementSyntax> GenerateFieldAssignement(string parent, Type p, string ReturnTypeName, string ParameterNamePrefix = "")
        {
            List<ExpressionStatementSyntax> exs = new List<ExpressionStatementSyntax>();


            var s = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(parent),
                        SyntaxFactory.IdentifierName(ReturnTypeName + "Field")),
                    SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(ReturnTypeName)).WithArgumentList(SyntaxFactory.ArgumentList())));
            exs.Add(s);


            //Iterate through all fields
            foreach (var prop in p.GetFields())
            {
                switch (prop.FieldType.ToString())
                {
                    case "System.DateTime":
                        exs.Add(GenerateDateTimeFieldAssignement(parent, ReturnTypeName, prop.Name, ParameterNamePrefix));
                        break;
                    case "System.Decimal":
                        exs.Add(GenerateDecimalFieldAssignement(parent, ReturnTypeName, prop.Name, ParameterNamePrefix));
                        break;
                    case "System.Double":
                    case "System.String":
                    case "System.Int32":
                    case "System.Boolean":
                    case "System.Single":
                        exs.Add(GenerateDirectFieldAssignement(parent, ReturnTypeName, prop.Name, ParameterNamePrefix));
                        break;

                    case "System.Char":
                        exs.Add(GenerateToStringFieldAssignement(parent, ReturnTypeName, prop.Name, ParameterNamePrefix));
                        break;

                    default:
                        var rz = GenerateFieldAssignement(parent + "." + p.Name + "Field", prop.FieldType, prop.Name, prop.Name + ".");//Recursivly walk all tree
                        exs.AddRange(rz);
                        break;
                }
            }
            return exs;
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

            //Output of original method
            var ResultDeclaration = SyntaxFactory.LocalDeclarationStatement
                           (
                               SyntaxFactory.VariableDeclaration
                               (
                                   SyntaxFactory.IdentifierName("var")
                               )
                               .WithVariables
                               (
                                   SyntaxFactory.SeparatedList<VariableDeclaratorSyntax>().Add(
                                       SyntaxFactory.VariableDeclarator("R")
                                   .WithInitializer(
                                      SyntaxFactory.EqualsValueClause(
                                      SyntaxFactory.InvocationExpression(
                                        SyntaxFactory.MemberAccessExpression(
                                             SyntaxKind.SimpleMemberAccessExpression,
                                              SyntaxFactory.IdentifierName("bl"),
                                             SyntaxFactory.IdentifierName(m.Name)))
                                    .WithArgumentList(
                                       SyntaxFactory.ArgumentList(LocalParamList)
                             ))))));


            MethodStatements.Add(ResultDeclaration);


            var ReturnObjectDeclaration = SyntaxFactory.LocalDeclarationStatement
                         (
                             SyntaxFactory.VariableDeclaration
                             (
                                 SyntaxFactory.IdentifierName("var")
                             )
                             .WithVariables(
                                 SyntaxFactory.SeparatedList<VariableDeclaratorSyntax>().Add(
                                      SyntaxFactory.VariableDeclarator("rez")
                                        .WithInitializer(
                                              SyntaxFactory.EqualsValueClause(
                                                  SyntaxFactory.ObjectCreationExpression(
                                                      SyntaxFactory.IdentifierName(m.Name + "Reply"))
                                                  .WithArgumentList(SyntaxFactory.ArgumentList())))
                                 )));

            MethodStatements.Add(ReturnObjectDeclaration);


            //if method returns simple type (string, int etc...)
            if (m.ReturnType.Module != m.Module)
            {

                var AssignRez = SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("rez"),
                            SyntaxFactory.IdentifierName("Result")
                            ),
                        SyntaxFactory.IdentifierName("R")));

                MethodStatements.Add(AssignRez);
            }
            else
            {
                //if original method returns custom class... 
                var a = GenerateFieldAssignement("rez", m.ReturnType, m.ReturnType.Name); //Recusivly parse all class tree
                foreach (var b in a)
                    MethodStatements.Add(b);
            }

            //  return Task.FromResult(rez);
            var ReturnStatement = SyntaxFactory.ReturnStatement(

                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                             SyntaxKind.SimpleMemberAccessExpression,
                             SyntaxFactory.IdentifierName("Task"),
                             SyntaxFactory.IdentifierName("FromResult")))
                    .WithArgumentList(
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("rez")
                                    )))));


            MethodStatements.Add(ReturnStatement);

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
            sf = sf.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Threading.Tasks")));
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
