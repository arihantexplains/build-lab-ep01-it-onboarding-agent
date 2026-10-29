using System.Collections.Generic;
using System.Linq;

namespace OnboardingAgent
{
    public sealed class JsonArray : List<object>
    {
        public JsonArray()
        {
        }

        public JsonArray(IEnumerable<object> items) : base(items)
        {
        }

        public IEnumerable<JsonObject> Objects()
        {
            return this.OfType<JsonObject>();
        }
    }
}
