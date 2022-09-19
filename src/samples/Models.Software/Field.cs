using GitObjectDb;
using GitObjectDb.Model;
using System.Collections.Generic;

namespace Models.Software
{
    [GitFolder("Fields")]
    public record Field : Node
    {
#pragma warning disable SA1011 // Closing square brackets should be spaced correctly
        public NestedA[]? A { get; set; }

        public NestedA? SomeValue { get; set; }
    }

#pragma warning disable SA1402 // File may only contain a single type
    public record NestedA
    {
        public NestedB? B { get; set; }
    }

    public record NestedB
    {
        public bool IsVisible { get; set; }
    }

    public static class IConnectionFieldExtensions
    {
        public static IEnumerable<Field> GetFields(this IConnection connection, Table table, string? committish = null) =>
            connection.GetNodes<Field>(table, committish);
    }
}
