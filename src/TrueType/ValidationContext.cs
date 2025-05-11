using System.Collections.Generic;

namespace PixelType.TrueType
{
    public class ValidationContext
    {
        public List<TrueTypeTable> ValidatedTables { get; } = new();
    }
}