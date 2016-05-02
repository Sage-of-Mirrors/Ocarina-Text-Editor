using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OcarinaTextEditor;
using OcarinaTextEditor.Enums;
using GameFormatReader.Common;

namespace OcarinaTextEditor
{
    class Exporter
    {
        private ObservableCollection<Message> m_messageList;
        private string m_fileName;

        public Exporter()
        {

        }

        public Exporter(ObservableCollection<Message> messageList, string fileName, ExportType exportType, Dictionary<ControlCode, string> codeDict)
        {
            byte[] alphabetStartOffset;
            byte[] alphabetEndOffset;

            m_messageList = messageList;
            m_fileName = fileName;

            List<byte> stringBank = new List<byte>();

            using (MemoryStream messageTableStream = new MemoryStream())
            {
                EndianBinaryWriter messageTableWriter = new EndianBinaryWriter(messageTableStream, Endian.Big);

                foreach (Message mes in messageList)
                {
                    mes.WriteMessage(messageTableWriter);

                    messageTableWriter.BaseStream.Seek(-4, SeekOrigin.Current);

                    int stringOffset = stringBank.Count();

                    byte[] decompOffset = BitConverter.GetBytes(stringOffset);
                    decompOffset[3] = 0x07;

                    if (mes.MessageID == -3)
                        alphabetStartOffset = decompOffset;

                    for (int i = 3; i > -1; i--)
                    {
                        messageTableWriter.Write(decompOffset[i]);
                    }

                    stringBank.AddRange(mes.ConvertTextData(codeDict));
                    stringBank.Add(0x02);

                    ExtensionMethods.PadByteList4(stringBank);
                }

                messageTableWriter.Write((short)-1);
                messageTableWriter.Write((short)0);
                messageTableWriter.Write((int)0);

                messageTableStream.Position = 0;

                using (MemoryStream stringData = new MemoryStream())
                {
                    EndianBinaryWriter stringWriter = new EndianBinaryWriter(stringData, Endian.Big);
                    stringWriter.Write(stringBank.ToArray());

                    stringData.Position = 0;

                    switch (exportType)
                    {
                        case ExportType.File:
                            ExportToFile(messageTableWriter, stringWriter);
                            break;
                        case ExportType.Patch:
                            break;
                        case ExportType.ROM:
                            ExportToRom(messageTableStream, stringData);
                            break;
                    }
                }
            }
        }

        private void ExportToRom(MemoryStream table, MemoryStream stringBank)
        {
            using (FileStream romFile = new FileStream(m_fileName, FileMode.Open))
            {
                romFile.Position = 0x00BC24C0;
                table.CopyTo(romFile);

                romFile.Position = 0x8C6000;
                stringBank.CopyTo(romFile);
            }
        }

        private void ExportToPatch()
        {

        }

        private void ExportToFile(EndianBinaryWriter messageTableWriter, EndianBinaryWriter stringWriter)
        {
            using (FileStream tableFile = new FileStream(string.Format(@"{0}\MessageTable.tbl", m_fileName), FileMode.Create))
            {
                messageTableWriter.BaseStream.CopyTo(tableFile);
                tableFile.Close();
            }

            using (FileStream textFile = new FileStream(string.Format(@"{0}\StringData.bin", m_fileName), FileMode.Create))
            {
                stringWriter.BaseStream.CopyTo(textFile);
                textFile.Close();
            }
        }
    }
}
