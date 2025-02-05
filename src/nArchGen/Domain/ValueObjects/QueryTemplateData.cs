using Core.CodeGen.TemplateEngine;

namespace Domain.ValueObjects;

public class QueryTemplateData : ITemplateData
{
    public string QueryName { get; set; } = string.Empty;
    public string FeatureName { get; set; } = string.Empty;
    public bool IsSecuredOperationUsed { get; set; }
}
