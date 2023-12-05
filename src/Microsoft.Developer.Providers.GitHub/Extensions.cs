// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.RegularExpressions;

namespace Microsoft.Developer.Providers.GitHub;

public static partial class Extensions
{
    [GeneratedRegex(@"(\p{Ll})(\P{Ll})")]
    private static partial Regex SplitCamelCaseRegexA();
    [GeneratedRegex(@"(\P{Ll})(\P{Ll}\p{Ll})")]
    private static partial Regex SplitCamelCaseRegexB();

    public static string SplitCamelCase(this string str)
        => SplitCamelCaseRegexA().Replace(SplitCamelCaseRegexB().Replace(str, "$1 $2"), "$1 $2");

    public static string UppercaseFirst(this string str)
        => char.ToUpper(str[0]) + str[1..];
}