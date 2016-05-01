using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OcarinaTextEditor.Enums;
using GameFormatReader.Common;

namespace OcarinaTextEditor
{
    public class Message : INotifyPropertyChanged
    {
        #region NotifyPropertyChanged overhead
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region public short MessageID
        public short MessageID
        {
            get { return m_messageID; }
            set
            {
                if (value != m_messageID)
                {
                    m_messageID = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private short m_messageID;
        #endregion

        #region public TextboxType BoxType
        public TextboxType BoxType
        {
            get { return m_boxType; }
            set
            {
                if (value != m_boxType)
                {
                    m_boxType = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private TextboxType m_boxType;
        #endregion

        #region public TextboxPosition BoxPosition
        public TextboxPosition BoxPosition
        {
            get { return m_boxPosition; }
            set
            {
                if (value != m_boxPosition)
                {
                    m_boxPosition = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private TextboxPosition m_boxPosition;
        #endregion

        #region public string TextData
        public string TextData
        {
            get { return m_textData; }
            set
            {
                if (value != m_textData)
                {
                    m_textData = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private string m_textData;
        #endregion

        public Message()
        {
            TextData = "";
        }

        public Message(EndianBinaryReader reader)
        {
            MessageID = reader.ReadInt16();

            byte typePosField = reader.ReadByte();

            BoxType = (TextboxType)((typePosField & 0xF0) >> 4);
            BoxPosition = (TextboxPosition)(typePosField & 0x0F);

            reader.SkipByte();

            uint offset = reader.ReadUInt32();

            offset = offset & 0x00ffffff;

            int cuPos = (int)reader.BaseStream.Position;

            reader.BaseStream.Position = 0x8C6000 + offset;

            GetStringData(reader);

            reader.BaseStream.Position = cuPos;
        }

        public void Print()
        {
            string printString = string.Format("ID: {0}\nBox Type: {1}\nBox Pos: {2}\nData:\n{3}\n\n", MessageID, BoxType, BoxPosition, TextData);
            Console.Write(printString);
        }

        private void GetStringData(EndianBinaryReader reader)
        {
            List<char> charData = new List<char>();

            char testChar = reader.ReadChar();

            while (testChar != 0x02)
            {
                bool readControlCode = false;

                foreach (ControlCode code in Enum.GetValues(typeof(ControlCode)))
                {
                    if ((ControlCode)testChar == code)
                    {
                        charData.AddRange(GetControlCode((ControlCode)testChar, reader));
                        readControlCode = true;
                    }
                }

                if (!readControlCode)
                {
                    if (char.IsLetterOrDigit(testChar) || char.IsWhiteSpace(testChar) || char.IsPunctuation(testChar))
                    {
                        charData.Add(testChar);
                    }
                }

                testChar = reader.ReadChar();
            }

            TextData = new String(charData.ToArray());
        }

        private char[] GetControlCode(ControlCode code, EndianBinaryReader reader)
        {
            List<char> codeBank = new List<char>();

            string codeInsides = "";

            switch (code)
            {
                case ControlCode.Color:
                    Color col = (Color)reader.ReadByte();
                    codeInsides = string.Format("{0}:{1}", "Color", col.ToString());
                    break;
                case ControlCode.Icon:
                    byte iconID = reader.ReadByte();
                    codeInsides = string.Format("{0}:{1}", "Icon", iconID);
                    break;
                case ControlCode.Line_Break:
                    return "\n".ToCharArray();
                case ControlCode.Spaces:
                    byte numSpaces = reader.ReadByte();
                    codeInsides = string.Format("{0}:{1}", "Spaces", numSpaces);
                    break;
                case ControlCode.Delay:
                    byte numFrames = reader.ReadByte();
                    codeInsides = string.Format("{0}:{1}", "Delay", numFrames);
                    break;
                case ControlCode.Fade:
                    byte numFramesFade = reader.ReadByte();
                    codeInsides = string.Format("{0}:{1}", "Fade", numFramesFade);
                    break;
                case ControlCode.Sound:
                    short soundID = reader.ReadInt16();
                    codeInsides = string.Format("{0}:{1}", "Sound", soundID);
                    break;
                case ControlCode.Speed:
                    byte speed = reader.ReadByte();
                    codeInsides = string.Format("{0}:{1}", "Speed", speed);
                    break;
                case ControlCode.High_Score:
                    short scoreID = reader.ReadInt16();
                    codeInsides = string.Format("{0}:{1}", "High Score", scoreID);
                    break;
                case ControlCode.Jump:
                    short msgID = reader.ReadInt16();
                    codeInsides = string.Format("{0}:{1}", "Jump", msgID);
                    break;
                default:
                    codeInsides = code.ToString().Replace("_", " ");
                    break;
            }

            codeBank.AddRange(string.Format("<{0}>", codeInsides).ToCharArray());

            return codeBank.ToArray();
        }
    }
}
