using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameFormatReader.Common;

namespace OcarinaTextEditor
{
    public static class ExtensionMethods
    {
        public static void PadStream32(EndianBinaryWriter writer)
        {
            // Pad up to a 32 byte alignment
            // Formula: (x + (n-1)) & ~(n-1)
            long nextAligned = (writer.BaseStream.Length + 0x1F) & ~0x1F;

            long delta = nextAligned - writer.BaseStream.Length;
            writer.BaseStream.Position = writer.BaseStream.Length;
            for (int i = 0; i < delta; i++)
            {
                writer.Write((byte)0);
            }
        }

        public static void PadByteList4(List<byte> list)
        {
            // Pad up to a 32 byte alignment
            // Formula: (x + (n-1)) & ~(n-1)
            long nextAligned = (list.Count + 0x3) & ~0x3;

            long delta = nextAligned - list.Count;

            for (int i = 0; i < delta; i++)
            {
                list.Add(0);
            }
        }
    }
}
