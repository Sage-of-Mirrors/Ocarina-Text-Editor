using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameFormatReader.Common;
using System.IO;
using OcarinaTextEditor.Enums;
using System.Windows;

namespace OcarinaTextEditor
{
    class Importer
    {
        private ObservableCollection<Message> m_messageList;
        private MemoryStream m_inputFile;

        public Importer()
        {
            m_messageList = new ObservableCollection<Message>();
        }
        
        public Importer(string fileName, Dictionary<ControlCode, string> controlCodeDict)
        {
            List<TableRecord> tableRecordList = new List<TableRecord>();

            try
            {
                using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    m_messageList = new ObservableCollection<Message>();

                    m_inputFile = new MemoryStream();
                    stream.CopyTo(m_inputFile);

                    EndianBinaryReader reader = new EndianBinaryReader(stream, Endian.Big);

                    reader.BaseStream.Seek(0x00BC24C0, 0);

                    //Read in message table records
                    while (reader.PeekReadInt16() != -1)
                    {
                        TableRecord mesRecord = new TableRecord(reader);
                        tableRecordList.Add(mesRecord);
                    }

                    foreach (var mesgRecord in tableRecordList)
                    {
                        reader.BaseStream.Position = 0x8C6000 + mesgRecord.Offset;
                        Message mes = new Message(reader, mesgRecord, controlCodeDict);

                        m_messageList.Add(mes);
                    }
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show("The chosen ROM is open in another program. Please close that program and open the ROM again.", "ROM is Already In Use");
                return;
            }
        }

        public Importer(string tableFileName, string messageDataFileName, Dictionary<ControlCode, string> controlCodeDict)
        {
            m_messageList = new ObservableCollection<Message>();

            List<TableRecord> tableRecordList = new List<TableRecord>();
            
            //Read in message table records
            using (FileStream stream = new FileStream(tableFileName, FileMode.Open))
            {
                EndianBinaryReader reader = new EndianBinaryReader(stream, Endian.Big);

                while (reader.PeekReadInt16() != -1)
                {
                    TableRecord mesRecord = new TableRecord(reader);
                    tableRecordList.Add(mesRecord);
                }
            }

            //Read in message data
            using (FileStream stream = new FileStream(messageDataFileName, FileMode.Open))
            {
                EndianBinaryReader reader = new EndianBinaryReader(stream, Endian.Big);

                foreach (var mesgRecord in tableRecordList)
                {
                    reader.BaseStream.Position = mesgRecord.Offset;
                    Message mes = new Message(reader, mesgRecord, controlCodeDict);

                    m_messageList.Add(mes);
                }
            }
        }

        public ObservableCollection<Message> GetMessageList()
        {
            return m_messageList;
        }

        public MemoryStream GetInputFile()
        {
            return m_inputFile;
        }
    }
}
