namespace Domain.Constants;

public static class Templates
{
    public static class Paths
    {
        public const string Root = "Templates";
        public static readonly string Crud = Path.Combine(Root, "CRUD");
        public static readonly string Command = Path.Combine(Root, "Command");
        public static readonly string Query = Path.Combine(Root, "Query");
    }
}
