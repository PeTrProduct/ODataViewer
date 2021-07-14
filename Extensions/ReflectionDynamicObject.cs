using System.Dynamic;
using System.Linq;
using System.Text.Json;

namespace ODataViewer
{
    public class ReflectionDynamicObject : DynamicObject
    {
        public JsonElement RealObject { get; set; }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            // Get the property value
            JsonElement srcData = RealObject.GetProperty(binder.Name);

            result = null;

            switch (srcData.ValueKind)
            {
                case JsonValueKind.Null:
                    result = null;
                    break;
                case JsonValueKind.Number:
                    result = srcData.GetDouble();
                    break;
                case JsonValueKind.False:
                    result = false;
                    break;
                case JsonValueKind.True:
                    result = true;
                    break;
                case JsonValueKind.Undefined:
                    result = null;
                    break;
                case JsonValueKind.String:
                    result = srcData.GetString();
                    break;
                case JsonValueKind.Object:
                    result = new ReflectionDynamicObject
                    {
                        RealObject = srcData
                    };
                    break;
                case JsonValueKind.Array:
                    result = srcData.EnumerateArray()
                        .Select(o => new ReflectionDynamicObject { RealObject = o })
                        .ToArray();
                    break;
            }

            // Always return true; other exceptions may have already been thrown if needed
            return true;
        }
    }
}
