namespace Hmz.Kolafi.Core.FeatureFlags;

/// <summary>
/// Tags a class (endpoint, Mediator request, etc.) with the module feature flag it belongs to.
/// Used by the endpoint filter and Mediator pipeline behavior to gate module access.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class ModuleFeatureAttribute(string featureName) : Attribute
{
  public string FeatureName { get; } = featureName;
}
