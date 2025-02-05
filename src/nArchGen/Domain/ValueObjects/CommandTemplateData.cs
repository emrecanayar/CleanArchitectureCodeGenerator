using Core.CodeGen.TemplateEngine;

namespace Domain.ValueObjects;

public class CommandTemplateData : ITemplateData
{
    public string CommandName { get; set; } = string.Empty;
    public string FeatureName { get; set; } = string.Empty;
    public bool IsSecuredOperationUsed { get; set; }
    public string EndPointMethod { get; set; } = string.Empty;
}
