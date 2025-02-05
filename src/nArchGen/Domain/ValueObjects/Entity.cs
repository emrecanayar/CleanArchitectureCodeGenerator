using Core.CodeGen.Code.CSharp.ValueObjects;

namespace Domain.ValueObjects;

public class Entity
{
    public string Name { get; set; } = string.Empty;
    public string IdType { get; set; } = string.Empty;
    public ICollection<PropertyInfo> Properties { get; set; }

    public Entity()
    {
        Properties = Array.Empty<PropertyInfo>();
    }
}
