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
using System.Windows;

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

            // We need the char table, with an index of -4, at the start of all the entries. So we'll find it and put it at the top.
            for (int i = 0; i < messageList.Count; i++)
            {
                if (messageList[i].MessageID == -4)
                {
                    // Message is already at the start, we do nothing here
                    if (i == 0)
                        break;

                    Message charTable = messageList[i]; // Copy char table to buffer
                    messageList.Insert(0, charTable); // Insert at the top of the list
                    messageList.RemoveAt(i + 1); // Delete the original message
                }
            }

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
                            ExportToPatch(messageTableStream, stringData);
                            break;
                        case ExportType.OriginalROM:
                            ExportToOriginalROM(messageTableStream, stringData);
                            break;
                    }
                }
            }
        }

        public Exporter(ObservableCollection<Message> messageList, string fileName, ExportType exportType, Dictionary<ControlCode, string> codeDict, MemoryStream inputFile)
        {
            byte[] alphabetStartOffset;
            byte[] alphabetEndOffset;

            m_messageList = messageList;
            m_fileName = fileName;

            // We need the char table, with an index of -4, at the start of all the entries. So we'll find it and put it at the top.
            for (int i = 0; i < messageList.Count; i++)
            {
                if (messageList[i].MessageID == -4)
                {
                    // Message is already at the start, we do nothing here
                    if (i == 0)
                        break;

                    Message charTable = messageList[i]; // Copy char table to buffer
                    messageList.Insert(0, charTable); // Insert at the top of the list
                    messageList.RemoveAt(i + 1); // Delete the original message
                }
            }

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

                // Write end-of-list message
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
                            ExportToPatch(messageTableStream, stringData);
                            break;
                        case ExportType.OriginalROM:
                            ExportToOriginalROM(messageTableStream, stringData);
                            break;
                        case ExportType.NewROM:
                            ExportToNewRom(messageTableStream, stringData, inputFile);
                            break;
                    }
                }
            }
        }

        private void ExportToNewRom(MemoryStream table, MemoryStream stringBank, MemoryStream inputFile)
        {
            try
            {
                using (FileStream romFile = new FileStream(m_fileName, FileMode.Create, FileAccess.Write))
                {
                    inputFile.Position = 0;
                    inputFile.CopyTo(romFile);
                    romFile.Position = 0;
                    EndianBinaryWriter writer = new EndianBinaryWriter(romFile, Endian.Big);

                    romFile.Position = 0x00BC24C0;
                    table.CopyTo(romFile);

                    romFile.Position = 0x8C6000;
                    stringBank.CopyTo(romFile);

                    // Since OoT uses a character table for the title screen, the file select, and Link's name,
                    // And we might move this table's offset, we're going to hack the game a bit.
                    // What we'll do is make the char table the first message (like we did in the constructor above)
                    // And change the code so that it gets a start address of 0x07000000
                    // And an end address of 0x07000048.

                    romFile.Position = 0xAE60B6; // Set position to start address LUI lower half
                    writer.Write((short)0x0700); // Overwrite 0x0704 with 0x0700
                    romFile.Position = 0xAE60BA; // Set position to start address ADDIU lower half
                    writer.Write((short)0); // Overwite 0x80D4 with 0

                    romFile.Position = 0xAE60C6; // Set position to LUI lower half
                    writer.Write((short)0x0700); // Overwite 0x0704 with 0x0700
                    romFile.Position = 0xAE60F2; // Set position to end address ADDIU lower half
                    writer.Write((short)0x48); // Overwrite 0x811C with 0x0048
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show("The ROM you are trying to save to is open in another program. Please close that program and try to save it again.", "ROM is In Use");
                return;
            }
        }

        private void ExportToOriginalROM(MemoryStream table, MemoryStream stringBank)
        {
            try
            {
                using (FileStream romFile = new FileStream(m_fileName, FileMode.Open))
                {
                    EndianBinaryWriter writer = new EndianBinaryWriter(romFile, Endian.Big);

                    romFile.Position = 0x00BC24C0;
                    table.CopyTo(romFile);

                    romFile.Position = 0x8C6000;
                    stringBank.CopyTo(romFile);

                    // Since OoT uses a character table for the title screen, the file select, and Link's name,
                    // And we might move this table's offset, we're going to hack the game a bit.
                    // What we'll do is make the char table the first message (like we did in the constructor above)
                    // And change the code so that it gets a start address of 0x07000000
                    // And an end address of 0x07000048.

                    romFile.Position = 0xAE60B6; // Set position to start address LUI lower half
                    writer.Write((short)0x0700); // Overwrite 0x0704 with 0x0700
                    romFile.Position = 0xAE60BA; // Set position to start address ADDIU lower half
                    writer.Write((short)0); // Overwite 0x80D4 with 0

                    romFile.Position = 0xAE60C6; // Set position to LUI lower half
                    writer.Write((short)0x0700); // Overwite 0x0704 with 0x0700
                    romFile.Position = 0xAE60F2; // Set position to end address ADDIU lower half
                    writer.Write((short)0x48); // Overwrite 0x811C with 0x0048
                }
            }

            catch (IOException ex)
            {
                MessageBox.Show("The ROM you are trying to save to is open in another program. Please close that program and try to save it again.", "ROM is In Use");
                return;
            }
        }

        private void ExportToPatch(MemoryStream table, MemoryStream stringBank)
        {
            EndianBinaryReader tableReader = new EndianBinaryReader(table, Endian.Big);
            EndianBinaryReader stringReader = new EndianBinaryReader(stringBank, Endian.Big);

            using (FileStream patchFile = new FileStream(m_fileName, FileMode.Create))
            {
                EndianBinaryWriter writer = new EndianBinaryWriter(patchFile, Endian.Big);

                writer.Write("PPF30".ToArray());
                writer.Write((byte)2);
                writer.Write("This patch was made by Ocarina Text Editor.       ".ToArray());
                writer.Write((int)0);

                int numChunks = (int)Math.Floor((double)stringBank.Length / 255) + 1;

                long offset = 0x8C6000;

                for (int i = 0; i < numChunks; i++)
                {
                    writer.CurrentEndian = Endian.Little;
                    writer.Write(offset);
                    writer.CurrentEndian = Endian.Big;

                    writer.Write((byte)255);

                    for (int j = 0; j < 255; j++)
                    {
                        if (stringReader.BaseStream.Position != stringReader.BaseStream.Length - 1)
                            writer.Write(stringReader.ReadByte());
                        else
                            writer.Write((byte)0);
                    }

                    offset += 255;
                }

                numChunks = (int)Math.Floor((double)table.Length / 255) + 1;

                offset = 0x00BC24C0;

                for (int i = 0; i < numChunks; i++)
                {
                    writer.CurrentEndian = Endian.Little;
                    writer.Write(offset);
                    writer.CurrentEndian = Endian.Big;

                    writer.Write((byte)255);

                    for (int j = 0; j < 255; j++)
                    {
                        if (tableReader.BaseStream.Position != tableReader.BaseStream.Length - 1)
                            writer.Write(tableReader.ReadByte());
                        else
                            writer.Write((byte)0);
                    }

                    offset += 255;
                }

                writer.CurrentEndian = Endian.Little;
                writer.Write((long)0xAE60B6);
                writer.CurrentEndian = Endian.Big;
                writer.Write((byte)2);
                writer.Write((short)0x0700);

                writer.CurrentEndian = Endian.Little;
                writer.Write((long)0xAE60BA);
                writer.CurrentEndian = Endian.Big;
                writer.Write((byte)2);
                writer.Write((short)0x0000);

                writer.CurrentEndian = Endian.Little;
                writer.Write((long)0xAE60C6);
                writer.CurrentEndian = Endian.Big;
                writer.Write((byte)2);
                writer.Write((short)0x0700);

                writer.CurrentEndian = Endian.Little;
                writer.Write((long)0xAE60F2);
                writer.CurrentEndian = Endian.Big;
                writer.Write((byte)2);
                writer.Write((short)0x0048);
            }
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
