using System;

namespace PixelType.TrueType
{
    public abstract class TrueTypeTable
    {
        public virtual Type[] DeserializationDependencies => Array.Empty<Type>();
        public virtual Type[] ValidationDependencies => Array.Empty<Type>();
        public abstract uint Tag { get; }
        public abstract long GetSize();
        public abstract void Serialize(Span<byte> dest);
        public abstract void Deserialize(DeserializationContext context, ref BufferReader data);

        public virtual void Validate(ValidationContext context)
        {
        }
    }
}