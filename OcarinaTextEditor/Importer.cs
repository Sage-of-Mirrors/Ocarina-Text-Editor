using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameFormatReader.Common;
using System.IO;
using OcarinaTextEditor.Enums;

namespace OcarinaTextEditor
{
    class Importer
    {
        private ObservableCollection<Message> m_messageList;

        public Importer()
        {
            m_messageList = new ObservableCollection<Message>();
        }

        public Importer(string fileName, Dictionary<ControlCode, string> controlCodeDict)
        {
            m_messageList = new ObservableCollection<Message>();

            using (FileStream stream = new FileStream(fileName, FileMode.Open))
            {
                EndianBinaryReader reader = new EndianBinaryReader(stream, Endian.Big);

                reader.BaseStream.Seek(0x00BC24C0, 0);

                while (reader.PeekReadInt16() != -1)
                {
                    Message mes = new Message(reader, controlCodeDict);

                    m_messageList.Add(mes);
                }
            }
        }

        public ObservableCollection<Message> GetMessageList()
        {
            return m_messageList;
        }
    }
}
