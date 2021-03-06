﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis.CSharp;
using ExpressionSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax;
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using CSSyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using Microsoft.CodeAnalysis;
using ICSharpCode.CodeConverter.Util.FromRoslyn;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class LiteralConversions
    {
        public static ExpressionSyntax GetLiteralExpression(object value, string textForUser = null, ITypeSymbol convertedType = null)
        {
            if (value is string valueTextForCompiler) {
                textForUser = textForUser == null
                    ? SymbolDisplay.FormatLiteral(valueTextForCompiler, true)
                    : GetQuotedStringTextForUser(textForUser, valueTextForCompiler);
                return SyntaxFactory.LiteralExpression(CSSyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(textForUser, valueTextForCompiler));
            }

            if (value == null)
                return SyntaxFactory.LiteralExpression(CSSyntaxKind.NullLiteralExpression);
            if (value is bool)
                return SyntaxFactory.LiteralExpression((bool)value ? CSSyntaxKind.TrueLiteralExpression : CSSyntaxKind.FalseLiteralExpression);

            textForUser = ConvertNumericLiteralValueText(textForUser ?? value.ToString(), value, convertedType);

            switch (value)
            {
                case byte b:
                    return SyntaxFactory.LiteralExpression(CSSyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(textForUser, b));
                case sbyte sb:
                    return SyntaxFactory.LiteralExpression(CSSyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(textForUser, sb));
                case short s:
                    return SyntaxFactory.LiteralExpression(CSSyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(textForUser, s));
                case ushort us:
                    return SyntaxFactory.LiteralExpression(CSSyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(textForUser, us));
                case int i:
                    return SyntaxFactory.LiteralExpression(CSSyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(textForUser, i));
                case uint u:
                    return SyntaxFactory.LiteralExpression(CSSyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(textForUser, u));
                case long l:
                    return SyntaxFactory.LiteralExpression(CSSyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(textForUser, l));
                case ulong ul:
                    return SyntaxFactory.LiteralExpression(CSSyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(textForUser, ul));
                case double d:
                    // The value is passed as a double from VB expression: "3.5F" and "3.5M"
                    // Important to use value text, otherwise "10.0" gets coerced to and integer literal of 10 which can change semantics
                    var syntaxToken = convertedType?.SpecialType switch
                    {
                        SpecialType.System_Single => SyntaxFactory.Literal(textForUser, (float)d),
                        SpecialType.System_Decimal => SyntaxFactory.Literal(textForUser, (decimal)d),
                        _ => SyntaxFactory.Literal(textForUser, d)
                    };
                    return SyntaxFactory.LiteralExpression(CSSyntaxKind.NumericLiteralExpression, syntaxToken);
                case float f:
                    return SyntaxFactory.LiteralExpression(CSSyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(textForUser, f));
                case decimal dec:
                    return SyntaxFactory.LiteralExpression(CSSyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(textForUser, dec));
                case char c:
                    return SyntaxFactory.LiteralExpression(CSSyntaxKind.CharacterLiteralExpression, SyntaxFactory.Literal(c));
                case DateTime dt:
                {
                    var valueToOutput = dt.Date.Equals(dt) ? dt.ToString("yyyy-MM-dd") : dt.ToString("yyyy-MM-dd HH:mm:ss");
                    return SyntaxFactory.ParseExpression("DateTime.Parse(\"" + valueToOutput + "\")");
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        internal static string GetQuotedStringTextForUser(string textForUser, string valueTextForCompiler)
        {
            var sourceUnquotedTextForUser = Unquote(textForUser);
            var worthBeingAVerbatimString = IsWorthBeingAVerbatimString(valueTextForCompiler);
            var destQuotedTextForUser =
                $"\"{EscapeQuotes(sourceUnquotedTextForUser, valueTextForCompiler, worthBeingAVerbatimString)}\"";

            return worthBeingAVerbatimString ? "@" + destQuotedTextForUser : destQuotedTextForUser;

        }

        internal static string EscapeQuotes(string unquotedTextForUser, string valueTextForCompiler, bool isVerbatimString)
        {
            if (isVerbatimString) {
                return valueTextForCompiler.Replace("\"", "\"\"");
            } else {
                return unquotedTextForUser.Replace("\"\"", "\\\"");
            }
        }

        private static string Unquote(string quotedText)
        {
            quotedText = quotedText.Replace('”', '"');
            int firstQuoteIndex = quotedText.IndexOf("\"");
            int lastQuoteIndex = quotedText.LastIndexOf("\"");
            var unquoted = quotedText.Substring(firstQuoteIndex + 1, lastQuoteIndex - firstQuoteIndex - 1);
            return unquoted;
        }

        public static bool IsWorthBeingAVerbatimString(string s1)
        {
            return s1.IndexOfAny(new[] {'\r', '\n', '\\'}) > -1;
        }

        /// <summary>
        ///  https://docs.microsoft.com/en-us/dotnet/visual-basic/programming-guide/language-features/data-types/type-characters
        ///  https://stackoverflow.com/a/166762/1128762
        /// </summary>
        private static string ConvertNumericLiteralValueText(string textForUser, object value, ITypeSymbol convertedTypeOrNull)
        {
            var replacements = new Dictionary<string, string> {
                {"C", ""},
                {"I", ""},
                {"%", ""},
                {"UI", "U"},
                {"S", ""},
                {"US", ""},
                {"UL", "UL"},
                {"D", "M"},
                {"@", "M"},
                {"R", "D"},
                {"#", "D"},
                {"F", "F"}, // Normalizes casing
                {"!", "F"},
                {"L", "L"}, // Normalizes casing
                {"&", "L"},
            };

            var isHex = textForUser.StartsWith("&H", StringComparison.OrdinalIgnoreCase);

            // Be careful not to replace only the "S" in "US" for example
            var longestMatchingReplacement = replacements.Where(t => textForUser.EndsWith(t.Key, StringComparison.OrdinalIgnoreCase) && (!isHex || !new[] { "C", "D", "F" }.Contains(t.Key)))
                .GroupBy(t => t.Key.Length).OrderByDescending(g => g.Key).FirstOrDefault()?.SingleOrDefault();


            if (longestMatchingReplacement != null) {
                textForUser = textForUser.ReplaceEnd(longestMatchingReplacement.Value);
            } else if (textForUser.Contains(".")) {
                if (convertedTypeOrNull?.SpecialType == SpecialType.System_Single) textForUser += "F";
                if (convertedTypeOrNull?.SpecialType == SpecialType.System_Decimal) textForUser += "M";
            }

            if (textForUser.Length <= 2 || !textForUser.StartsWith("&")) return textForUser;

            if (isHex)
            {
                return "0x" + textForUser.Substring(2);
            }

            if (textForUser.StartsWith("&B", StringComparison.OrdinalIgnoreCase))
            {
                return "0b" + textForUser.Substring(2);
            }

            // Octal or something unknown that can't be represented with C# literals
            return value.ToString();
        }
    }
}