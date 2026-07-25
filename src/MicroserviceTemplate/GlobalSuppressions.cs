using System.Diagnostics.CodeAnalysis;

// Operation namespaces intentionally mirror the HTTP action used by each feature silo.
// These are application implementation details rather than a public cross-language API.
[assembly: SuppressMessage(
    "Naming",
    "CA1716:Identifiers should not match keywords",
    Justification = "Feature-silo operation namespaces use concise HTTP action names.",
    Scope = "namespaceanddescendants",
    Target = "~N:ModernMicroservice.Features.Tasks.Get")]
