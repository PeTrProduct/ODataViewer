using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ODataViewer
{
    public static partial class JsonExtensions
    {
        public static IEnumerable<JsonElement> DescendantPropertyValues(this JsonElement element, string name, StringComparison comparison = StringComparison.Ordinal)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return DescendantPropertyValues(element, n => name.Equals(n, comparison));
        }

        public static IEnumerable<JsonElement> DescendantPropertyValues(this JsonElement element, Predicate<string> match)
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            IEnumerable<JsonElement> query = RecursiveEnumerableExtensions.Traverse(
                (Name: (string)null, Value: element),
                t =>
                {
                    switch (t.Value.ValueKind)
                    {
                        case JsonValueKind.Array:
                            return t.Value.EnumerateArray().Select(i => ((string)null, i));
                        case JsonValueKind.Object:
                            return t.Value.EnumerateObject().Select(p => (p.Name, p.Value));
                        default:
                            return Enumerable.Empty<(string, JsonElement)>();
                    }
                }, false)
                .Where(t => t.Name != null && match(t.Name))
                .Select(t => t.Value);
            return query;
        }

        public static IEnumerable<(string Name, JsonElement Value)> DescendantPropertyValues(this JsonElement element)
        {
            IEnumerable<(string Name, JsonElement Value)> query = RecursiveEnumerableExtensions.Traverse(
                (Name: (string)null, Value: element),
                t =>
                {
                    switch (t.Value.ValueKind)
                    {
                        case JsonValueKind.Array:
                            return t.Value.EnumerateArray().Select(i => ((string)null, i));
                        case JsonValueKind.Object:
                            return t.Value.EnumerateObject().Select(p => (p.Name, p.Value));
                        default:
                            return Enumerable.Empty<(string, JsonElement)>();
                    }
                }, false);
            return query;
        }
    }
}
