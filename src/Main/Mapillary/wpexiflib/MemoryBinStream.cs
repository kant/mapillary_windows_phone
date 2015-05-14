using System.IO;

namespace ExifLibrary.Phone
{
    public class MemoryBinStream : MemoryStream
    {
        private readonly AsciiEncoding _encoding = new AsciiEncoding();

        internal void Write(Bin bin)
        {
            var b = _encoding.GetBytes(bin.ToString());

            this.Write(b, 0, b.Length);
        }
    }
}
