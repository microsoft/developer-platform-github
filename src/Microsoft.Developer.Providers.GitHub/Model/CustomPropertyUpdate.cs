// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Developer.Providers.GitHub.Model;

public class CustomPropertyUpdate
{
    public List<CustomProperty> Properties { get; set; } = [];

    public static readonly CustomPropertyUpdate Required = new()
    {
        Properties = [
            new CustomProperty
            {
                PropertyName = CustomProperty.Use,
                ValueType = CustomPropertyType.SingleSelect,
                DefaultValue = "ignore",
                Required = true,
                AllowedValues = ["workflows", "config", "ignore", "repo", "template"]
            }
        ]
    };
}
