using GameFormatReader.Common;
using OcarinaTextEditor.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OcarinaTextEditor
{
    public class TableRecord
    {
        public short MessageID { get; set; }
        public TextboxType BoxType { get; set; }
        public TextboxPosition BoxPosition { get; set; }
        public uint Offset
        {
            get { return offset; }
            set { offset = value & 0x00FFFFFF; }
        }
        private uint offset;

        public TableRecord(EndianBinaryReader reader)
        {
            MessageID = reader.ReadInt16();

            byte typePosField = reader.ReadByte();

            BoxType = (TextboxType)((typePosField & 0xF0) >> 4);
            BoxPosition = (TextboxPosition)(typePosField & 0x0F);

            reader.SkipByte();

            Offset = reader.ReadUInt32();
        }
    }
}
