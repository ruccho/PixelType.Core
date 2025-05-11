using System.Collections.Generic;

namespace PixelType.TrueType
{
    public class DeserializationContext
    {
        public List<TrueTypeTable> DeserializedTables { get; } = new();
    }
}