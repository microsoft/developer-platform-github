// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Microsoft.Developer.Providers.GitHub.Model;

public partial class WorkflowYaml
{
    [GeneratedRegex(@"(\n|((\\r)?\\n))on:(\s|((\\r)?\\n))+([\s\S])*?workflow_dispatch:?", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture)]
    private static partial Regex WorkflowDispatchRegex();

    internal static readonly Regex WorkflowDispatchPattern = WorkflowDispatchRegex();

    [GeneratedRegex(@"workflow_dispatch:(\n|((\\r)?\\n))+\s*inputs:((\n|((\\r)?\\n)))+", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture)]
    private static partial Regex WorkflowDispatchWithInputsRegex();

    internal static readonly Regex WorkflowDispatchWithInputsPattern = WorkflowDispatchWithInputsRegex();


    public static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .IgnoreUnmatchedProperties()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)  // see height_in_inches in sample yml
        .Build();

    public string? Name { get; set; }

    [YamlMember(Alias = "env", ApplyNamingConventions = false)]
    public Dictionary<string, string> EnvironmentVariables { get; set; } = [];

    public string Description()
        => new Dictionary<string, string>(EnvironmentVariables, StringComparer.OrdinalIgnoreCase)
        .TryGetValue(nameof(Description), out var value) && value is string description
        ? description : $"Runs the {Name} workflow.";
}

public class WorkflowYaml<TOn> : WorkflowYaml
{
    public TOn? On { get; set; }
}

public class WorkflowYamlWithInputs : WorkflowYaml<WorkflowDispatchTrigger> { }

public class WorkflowDispatchTrigger
{
    public WorkflowDispatchConfig? WorkflowDispatch { get; set; }
}

public class WorkflowDispatchConfig
{
    public Dictionary<string, WorkflowDispatchInput>? Inputs { get; set; }
}
