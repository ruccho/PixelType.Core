using System;
using System.Runtime.InteropServices;

namespace PixelType.TrueType
{
    public abstract class TrueTypeTableFixed<T> : TrueTypeTable where T : unmanaged
    {
        public T Data { get; set; }

        public override long GetSize()
        {
            return Marshal.SizeOf<T>();
        }

        public override void Serialize(Span<byte> dest)
        {
            var data = Data;
            MemoryMarshal.Write(dest, ref data);
        }

        public override void Deserialize(DeserializationContext context, ref BufferReader data)
        {
            data.ReadUnaligned(out T result);
            Data = result;
        }
    }
}