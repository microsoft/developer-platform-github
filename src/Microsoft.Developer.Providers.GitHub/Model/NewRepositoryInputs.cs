// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Developer.Providers.GitHub.Model;

public class NewRepositoryInputs(string name, bool @private, string? description, string? gitignore = null, string? license = null)
{
    public string Name => name;

    public bool Private => @private;

    public string? Description => description;

    public string? Gitignore => gitignore;

    public string? License => license;
}