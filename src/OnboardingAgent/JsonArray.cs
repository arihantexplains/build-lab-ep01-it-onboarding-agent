using System.Collections.Generic;
using System.Linq;

namespace OnboardingAgent
{
    /// <summary>A JSON array: a list of parsed values (JsonObject, JsonArray, string, long, double, bool or null).</summary>
    public sealed class JsonArray : List<object>
    {
        /// <summary>Creates an empty array.</summary>
        public JsonArray()
        {
        }

        /// <summary>Creates an array holding the given items.</summary>
        /// <param name="items">The values to copy into the array.</param>
        public JsonArray(IEnumerable<object> items) : base(items)
        {
        }

        /// <summary>Returns only the items that are JSON objects, skipping anything else.</summary>
        public IEnumerable<JsonObject> Objects()
        {
            return this.OfType<JsonObject>();
        }
    }
}
