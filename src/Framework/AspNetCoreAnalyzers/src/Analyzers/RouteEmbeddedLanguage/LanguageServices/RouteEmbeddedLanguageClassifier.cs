// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Classification;
using RoutePatternToken = Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Common.EmbeddedSyntaxToken<Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.RoutePatternKind>;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.LanguageServices;

[ExportAspNetCoreEmbeddedLanguageClassifier(name: "Route", language: LanguageNames.CSharp)]
internal class RouteEmbeddedLanguageClassifier : IAspNetCoreEmbeddedLanguageClassifier
{
    public void RegisterClassifications(AspNetCoreEmbeddedLanguageClassificationContext context)
    {
        var parser = RoutePatternParser.TryParse(context.VirtualCharSequence);

        var visitor = new Visitor(context);
        AddClassifications(parser.Root, visitor);
    }

    private static void AddClassifications(RoutePatternNode node, Visitor visitor)
    {
        node.Accept(visitor);

        foreach (var child in node)
        {
            if (child.IsNode)
            {
                AddClassifications(child.Node, visitor);
            }
        }
    }

    private class Visitor : IRoutePatternNodeVisitor
    {
        public AspNetCoreEmbeddedLanguageClassificationContext _context;

        public Visitor(AspNetCoreEmbeddedLanguageClassificationContext context)
        {
            _context = context;
        }

        public void Visit(RoutePatternCompilationUnit node)
        {
            // Nothing to highlight.
        }

        public void Visit(RoutePatternSegmentNode node)
        {
            // Nothing to highlight.
        }

        public void Visit(RoutePatternReplacementNode node)
        {
            // Nothing to highlight.
        }

        public void Visit(RoutePatternParameterNode node)
        {
            AddClassification(node.OpenBraceToken, ClassificationTypeNames.RegexCharacterClass);
            AddClassification(node.CloseBraceToken, ClassificationTypeNames.RegexCharacterClass);
        }

        public void Visit(RoutePatternLiteralNode node)
        {
            // Nothing to highlight.
        }

        public void Visit(RoutePatternSegmentSeperatorNode node)
        {
            // Nothing to highlight.
        }

        public void Visit(RoutePatternOptionalSeperatorNode node)
        {
            // Nothing to highlight.
        }

        public void Visit(RoutePatternCatchAllParameterPartNode node)
        {
            // Nothing to highlight.
        }

        public void Visit(RoutePatternNameParameterPartNode node)
        {
            // Nothing to highlight.
        }

        public void Visit(RoutePatternPolicyParameterPartNode node)
        {
            // Nothing to highlight.
        }

        public void Visit(RoutePatternPolicyFragmentEscapedNode node)
        {
            // Nothing to highlight.
        }

        public void Visit(RoutePatternPolicyFragment node)
        {
            // Nothing to highlight.
        }

        public void Visit(RoutePatternOptionalParameterPartNode node)
        {
            AddClassification(node.QuestionMarkToken, ClassificationTypeNames.RegexAnchor);
        }

        public void Visit(RoutePatternDefaultValueParameterPartNode node)
        {
            // Nothing to highlight.
        }

        private void AddClassification(RoutePatternToken token, string typeName)
        {
            if (!token.IsMissing)
            {
                _context.AddClassification(typeName, token.GetSpan());
            }
        }

        private void ClassifyWholeNode(RoutePatternNode node, string typeName)
        {
            foreach (var child in node)
            {
                if (child.IsNode)
                {
                    ClassifyWholeNode(child.Node, typeName);
                }
                else
                {
                    AddClassification(child.Token, typeName);
                }
            }
        }

        /*
        public void Visit(RegexCompilationUnit node)
        {
            // Nothing to highlight.
        }

        public void Visit(RegexSequenceNode node)
        {
            // Nothing to highlight.   
        }

        #region Character classes

        public void Visit(RegexWildcardNode node)
            => AddClassification(node.DotToken, ClassificationTypeNames.RegexCharacterClass);

        public void Visit(RegexCharacterClassNode node)
        {
            AddClassification(node.OpenBracketToken, ClassificationTypeNames.RegexCharacterClass);
            AddClassification(node.CloseBracketToken, ClassificationTypeNames.RegexCharacterClass);
        }

        public void Visit(RegexNegatedCharacterClassNode node)
        {
            AddClassification(node.OpenBracketToken, ClassificationTypeNames.RegexCharacterClass);
            AddClassification(node.CaretToken, ClassificationTypeNames.RegexCharacterClass);
            AddClassification(node.CloseBracketToken, ClassificationTypeNames.RegexCharacterClass);
        }

        public void Visit(RegexCharacterClassRangeNode node)
            => AddClassification(node.MinusToken, ClassificationTypeNames.RegexCharacterClass);

        public void Visit(RegexCharacterClassSubtractionNode node)
            => AddClassification(node.MinusToken, ClassificationTypeNames.RegexCharacterClass);

        public void Visit(RegexCharacterClassEscapeNode node)
            => ClassifyWholeNode(node, ClassificationTypeNames.RegexCharacterClass);

        public void Visit(RegexCategoryEscapeNode node)
            => ClassifyWholeNode(node, ClassificationTypeNames.RegexCharacterClass);

        #endregion

        #region Quantifiers

        public void Visit(RegexZeroOrMoreQuantifierNode node)
            => AddClassification(node.AsteriskToken, ClassificationTypeNames.RegexQuantifier);

        public void Visit(RegexOneOrMoreQuantifierNode node)
            => AddClassification(node.PlusToken, ClassificationTypeNames.RegexQuantifier);

        public void Visit(RegexZeroOrOneQuantifierNode node)
            => AddClassification(node.QuestionToken, ClassificationTypeNames.RegexQuantifier);

        public void Visit(RegexLazyQuantifierNode node)
            => AddClassification(node.QuestionToken, ClassificationTypeNames.RegexQuantifier);

        public void Visit(RegexExactNumericQuantifierNode node)
        {
            AddClassification(node.OpenBraceToken, ClassificationTypeNames.RegexQuantifier);
            AddClassification(node.FirstNumberToken, ClassificationTypeNames.RegexQuantifier);
            AddClassification(node.CloseBraceToken, ClassificationTypeNames.RegexQuantifier);
        }

        public void Visit(RegexOpenNumericRangeQuantifierNode node)
        {
            AddClassification(node.OpenBraceToken, ClassificationTypeNames.RegexQuantifier);
            AddClassification(node.FirstNumberToken, ClassificationTypeNames.RegexQuantifier);
            AddClassification(node.CommaToken, ClassificationTypeNames.RegexQuantifier);
            AddClassification(node.CloseBraceToken, ClassificationTypeNames.RegexQuantifier);
        }

        public void Visit(RegexClosedNumericRangeQuantifierNode node)
        {
            AddClassification(node.OpenBraceToken, ClassificationTypeNames.RegexQuantifier);
            AddClassification(node.FirstNumberToken, ClassificationTypeNames.RegexQuantifier);
            AddClassification(node.CommaToken, ClassificationTypeNames.RegexQuantifier);
            AddClassification(node.SecondNumberToken, ClassificationTypeNames.RegexQuantifier);
            AddClassification(node.CloseBraceToken, ClassificationTypeNames.RegexQuantifier);
        }

        #endregion

        #region Groupings

        public void Visit(RegexSimpleGroupingNode node)
            => ClassifyGrouping(node);

        public void Visit(RegexSimpleOptionsGroupingNode node)
            => ClassifyGrouping(node);

        public void Visit(RegexNestedOptionsGroupingNode node)
            => ClassifyGrouping(node);

        public void Visit(RegexNonCapturingGroupingNode node)
            => ClassifyGrouping(node);

        public void Visit(RegexPositiveLookaheadGroupingNode node)
            => ClassifyGrouping(node);

        public void Visit(RegexNegativeLookaheadGroupingNode node)
            => ClassifyGrouping(node);

        public void Visit(RegexPositiveLookbehindGroupingNode node)
            => ClassifyGrouping(node);

        public void Visit(RegexNegativeLookbehindGroupingNode node)
            => ClassifyGrouping(node);

        public void Visit(RegexAtomicGroupingNode node)
            => ClassifyGrouping(node);

        public void Visit(RegexCaptureGroupingNode node)
            => ClassifyGrouping(node);

        public void Visit(RegexBalancingGroupingNode node)
            => ClassifyGrouping(node);

        public void Visit(RegexConditionalCaptureGroupingNode node)
            => ClassifyGrouping(node);

        public void Visit(RegexConditionalExpressionGroupingNode node)
            => ClassifyGrouping(node);

        // Captures and backreferences refer to groups.  So we classify them the same way as groups.
        public void Visit(RegexCaptureEscapeNode node)
            => ClassifyWholeNode(node, ClassificationTypeNames.RegexGrouping);

        public void Visit(RegexKCaptureEscapeNode node)
            => ClassifyWholeNode(node, ClassificationTypeNames.RegexGrouping);

        public void Visit(RegexBackreferenceEscapeNode node)
            => ClassifyWholeNode(node, ClassificationTypeNames.RegexGrouping);

        private void ClassifyGrouping(RegexGroupingNode node)
        {
            foreach (var child in node)
            {
                if (!child.IsNode)
                {
                    AddClassification(child.Token, ClassificationTypeNames.RegexGrouping);
                }
            }
        }

        #endregion

        #region Other Escapes

        public void Visit(RegexControlEscapeNode node)
            => ClassifyOtherEscape(node);

        public void Visit(RegexHexEscapeNode node)
            => ClassifyOtherEscape(node);

        public void Visit(RegexUnicodeEscapeNode node)
            => ClassifyOtherEscape(node);

        public void Visit(RegexOctalEscapeNode node)
            => ClassifyOtherEscape(node);

        public void ClassifyOtherEscape(RoutePatternNode node)
            => ClassifyWholeNode(node, ClassificationTypeNames.RegexOtherEscape);

        #endregion

        #region Anchors

        public void Visit(RegexAnchorNode node)
            => AddClassification(node.AnchorToken, ClassificationTypeNames.RegexAnchor);

        public void Visit(RegexAnchorEscapeNode node)
            => ClassifyWholeNode(node, ClassificationTypeNames.RegexAnchor);

        #endregion

        public void Visit(RegexTextNode node)
            => AddClassification(node.TextToken, ClassificationTypeNames.RegexText);

        public void Visit(RegexPosixPropertyNode node)
        {
            // The .NET parser just interprets the [ of the node, and skips the rest. So
            // classify the end part as a comment.
            _context.AddClassification(ClassificationTypeNames.RegexText, node.TextToken.VirtualChars[0].Span);
            _context.AddClassification(
                ClassificationTypeNames.RegexComment,
                EmbeddedSyntaxHelpers.GetSpan(node.TextToken.VirtualChars[1], node.TextToken.VirtualChars.Last()));
        }

        public void Visit(RegexAlternationNode node)
        {
            for (var i = 1; i < node.SequenceList.NodesAndTokens.Length; i += 2)
            {
                AddClassification(node.SequenceList.NodesAndTokens[i].Token, ClassificationTypeNames.RegexAlternation);
            }
        }

        public void Visit(RegexSimpleEscapeNode node)
            => ClassifyWholeNode(node,
                //node.IsSelfEscape()
                //? ClassificationTypeNames.RegexSelfEscapedCharacter
                //:
                ClassificationTypeNames.RegexOtherEscape);
        */
    }


    // IAspNetCoreEmbeddedLanguageClassifier is internal and tests don't have access to it. Provide a way to get its assembly.
    // Just for unit tests. Don't use in production code.
    internal static class TestAccessor
    {
        public static Assembly ExternalAccessAssembly => typeof(IAspNetCoreEmbeddedLanguageClassifier).Assembly;
    }
}
