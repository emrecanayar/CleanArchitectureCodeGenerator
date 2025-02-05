using Core.CodeGen.TemplateEngine;

namespace Domain.ValueObjects;

public class CrudTemplateData : ITemplateData
{
    public Entity Entity { get; set; } = default!;
    public string DbContextName { get; set; } = string.Empty;
    public bool IsSecuredOperationUsed { get; set; }
    public string ProjectName { get; set; } = string.Empty;
}
