using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace umekan
{
#pragma warning disable RS2008

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;
    using System.Collections.Immutable;
    using System.Linq;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ReadonlyInScriptAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Rule = new(
            id: "ReadonlyInScript",
            title: "ReadonlyInScriptAnalyzer",
            messageFormat: "スクリプト上での値の書き込みは禁止されています。",
            category: "Read/Write",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "エディタ等でのみ編集可能なフィールドを使いたいとき");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            // お約束。
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // binary assignment expression全部
            var kinds = new SyntaxKind[]
            {
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxKind.AddAssignmentExpression,
                SyntaxKind.SubtractAssignmentExpression,
                SyntaxKind.MultiplyAssignmentExpression,
                SyntaxKind.DivideAssignmentExpression,
                SyntaxKind.ModuloAssignmentExpression,
                SyntaxKind.AndAssignmentExpression,
                SyntaxKind.ExclusiveOrAssignmentExpression,
                SyntaxKind.OrAssignmentExpression,
                SyntaxKind.LeftShiftAssignmentExpression,
                SyntaxKind.RightShiftAssignmentExpression,
                SyntaxKind.CoalesceAssignmentExpression,
                SyntaxKind.PreIncrementExpression,
                SyntaxKind.PreDecrementExpression,
                SyntaxKind.PostIncrementExpression,
                SyntaxKind.PostDecrementExpression
            };
            
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, kinds);
        }

        private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var syntax = context.Node switch
            {
                AssignmentExpressionSyntax assignExpression => assignExpression.Left,
                PostfixUnaryExpressionSyntax postUnary => postUnary.Operand,
                PrefixUnaryExpressionSyntax preUnary => preUnary.Operand,
                _ => default
            };

            if (syntax == default) return;

            var symbol = context.SemanticModel
                .GetSymbolInfo(syntax, context.CancellationToken)
                .Symbol;

            var result = symbol switch
            {
                IFieldSymbol fieldSymbol
                    => fieldSymbol.GetAttributes()
                        .Any(x => x.AttributeClass.Name.Contains("ReadonlyInScriptAttribute")),
                IPropertySymbol propertySymbol 
                    => IsAutoProperty(propertySymbol) && HasReadonlyAttribute(propertySymbol),
                _ => false,
            };

            if (result)
            {
                // Diagnosticを作ってReportDiagnosticに詰める。
                var diagnostic = Diagnostic.Create(Rule, context.Node.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }

            bool IsAutoProperty(IPropertySymbol propertySymbol)
            {
                var node = propertySymbol.DeclaringSyntaxReferences.First().GetSyntax();
                if (node is not PropertyDeclarationSyntax propertyDeclarationSyntax) return false;
                return propertyDeclarationSyntax.AccessorList.Accessors.All(a => a.Body == null);
            }

            bool HasReadonlyAttribute(IPropertySymbol propertySymbol)
            {
                var node = propertySymbol.DeclaringSyntaxReferences.First().GetSyntax();
                if (node is not PropertyDeclarationSyntax propertyDeclarationSyntax) return false;
                return propertyDeclarationSyntax.DescendantNodes()
                    .OfType<AttributeSyntax>()
                    .Any(a => a.Name.ToString().Contains("ReadonlyInScript"));
            }
        }
    }
}