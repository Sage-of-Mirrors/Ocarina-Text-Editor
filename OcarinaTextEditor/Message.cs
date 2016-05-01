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

            byte testByte = reader.ReadByte();

            while (testByte != 0x02)
            {
                bool readControlCode = false;

                foreach (ControlCode code in Enum.GetValues(typeof(ControlCode)))
                {
                    if ((ControlCode)testByte == code)
                    {
                        charData.AddRange(GetControlCode((ControlCode)testByte, reader));
                        readControlCode = true;
                    }
                }

                if (!readControlCode)
                {
                    if (char.IsLetterOrDigit((char)testByte) || char.IsWhiteSpace((char)testByte) || char.IsPunctuation((char)testByte))
                    {
                        charData.Add((char)testByte);
                    }
                }

                testByte = reader.ReadByte();
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

        public void WriteMessage(EndianBinaryWriter writer)
        {
            writer.Write(m_messageID);

            int type = (int)BoxType;
            int pos = (int)BoxPosition;
            type = type << 4;
            type = type | pos;

            writer.Write((byte)type);
            writer.Write((byte)0);
            writer.Write((int)0);
        }

        public List<byte> ConvertTextData()
        {
            List<byte> data = new List<byte>();

            for (int i = 0; i < TextData.Count(); i++)
            {
                // Not a control code, copy char to output buffer
                if (TextData[i] != '<')
                {
                    if (TextData[i] == '\n')
                    {
                        data.Add(1);
                    }
                    else if (TextData[i] == '\r')
                    {

                    }
                    else
                    {
                        data.Add((byte)TextData[i]);
                    }
                    continue;
                }
                // Control code end tag. This should never be encountered on its own.
                else if (TextData[i] == '>')
                {
                    // This should be an error handler
                }
                // We've got a control code
                else
                {
                    // Buffer for the control code
                    List<char> controlCode = new List<char>();

                    while (TextData[i] != '>')
                    {
                        // Add code chars to the buffer
                        controlCode.Add(TextData[i]);
                        // Increase i so we can skip the code when we're done parsing
                        i++;
                    }

                    // Remove the < chevron from the beginning of the code
                    controlCode.RemoveAt(0);

                    string parsedCode = new string(controlCode.ToArray());

                    data.AddRange(GetControlCode(parsedCode.Split(':')));
                }
            }

            return data;
        }

        private List<byte> GetControlCode(string[] code)
        {
            List<byte> output = new List<byte>();

            switch (code[0])
            {
                case "Line Break":
                    output.Add((byte)ControlCode.Line_Break);
                    break;
                case "Box Break":
                    output.Add((byte)ControlCode.Box_Break);
                    break;
                case "Color":
                    output.Add((byte)ControlCode.Color);
                    switch (code[1])
                    {
                        case "White":
                            output.Add((byte)Color.White);
                            break;
                        case "Red":
                            output.Add((byte)Color.Red);
                            break;
                        case "Green":
                            output.Add((byte)Color.Green);
                            break;
                        case "Blue":
                            output.Add((byte)Color.Blue);
                            break;
                        case "Light_Blue":
                            output.Add((byte)Color.Light_Blue);
                            break;
                        case "Pink":
                            output.Add((byte)Color.Pink);
                            break;
                        case "Yellow":
                            output.Add((byte)Color.Yellow);
                            break;
                        case "Black":
                            output.Add((byte)Color.Black);
                            break;
                    }
                    break;
                case "Spaces":
                    output.Add((byte)ControlCode.Spaces);
                    output.Add(Convert.ToByte(code[1]));
                    break;
                case "Jump":
                    output.Add((byte)ControlCode.Jump);
                    byte[] jumpIDBytes = BitConverter.GetBytes(Convert.ToInt16(code[1]));
                    output.Add(jumpIDBytes[1]);
                    output.Add(jumpIDBytes[0]);
                    break;
                case "Draw Instant":
                    output.Add((byte)ControlCode.Draw_Instant);
                    break;
                case "Draw Char":
                    output.Add((byte)ControlCode.Draw_Char);
                    break;
                case "Shop Description":
                    output.Add((byte)ControlCode.Shop_Description);
                    break;
                case "Event":
                    output.Add((byte)ControlCode.Event);
                    break;
                case "Delay":
                    output.Add((byte)ControlCode.Delay);
                    output.Add(Convert.ToByte(code[1]));
                    break;
                case "Fade":
                    output.Add((byte)ControlCode.Fade);
                    output.Add(Convert.ToByte(code[1]));
                    break;
                case "Player":
                    output.Add((byte)ControlCode.Player);
                    break;
                case "Ocarina":
                    output.Add((byte)ControlCode.Ocarina);
                    break;
                case "Sound":
                    output.Add((byte)ControlCode.Sound);
                    byte[] soundIDBytes = BitConverter.GetBytes(Convert.ToInt16(code[1]));
                    output.Add(soundIDBytes[1]);
                    output.Add(soundIDBytes[0]);
                    break;
                case "Icon":
                    output.Add((byte)ControlCode.Icon);
                    output.Add(Convert.ToByte(code[1]));
                    break;
                case "Speed":
                    output.Add((byte)ControlCode.Speed);
                    output.Add(Convert.ToByte(code[1]));
                    break;
                case "Background":
                    output.Add((byte)ControlCode.Background);
                    //byte[] backgroundIDBytes = BitConverter.GetBytes(Convert.ToInt32(code[1]));
                    //output.Add(backgroundIDBytes[3]);
                    //output.Add(backgroundIDBytes[2]);
                    //output.Add(backgroundIDBytes[1]);
                    break;
                case "Marathon Time":
                    output.Add((byte)ControlCode.Marathon_Time);
                    break;
                case "Race Time":
                    output.Add((byte)ControlCode.Race_Time);
                    break;
                case "Points":
                    output.Add((byte)ControlCode.Points);
                    break;
                case "Gold Skulltulas":
                    output.Add((byte)ControlCode.Gold_Skulltulas);
                    break;
                case "No Skip":
                    output.Add((byte)ControlCode.No_Skip);
                    break;
                case "Two Choices":
                    output.Add((byte)ControlCode.Two_Choices);
                    break;
                case "Three Choices":
                    output.Add((byte)ControlCode.Three_Choices);
                    break;
                case "Fish Weight":
                    output.Add((byte)ControlCode.Fish_Weight);
                    break;
                case "High Score":
                    output.Add((byte)ControlCode.High_Score);
                    //output.Add(Convert.ToByte(code[1]));
                    break;
                case "Time":
                    output.Add((byte)ControlCode.Time);
                    break;
                case "Dash":
                    output.Add((byte)ControlCode.Dash);
                    break;
                case "A Button":
                    output.Add((byte)ControlCode.A_Button);
                    break;
                case "B Button":
                    output.Add((byte)ControlCode.B_Button);
                    break;
                case "C Button":
                    output.Add((byte)ControlCode.C_Button);
                    break;
                case "L Button":
                    output.Add((byte)ControlCode.L_Button);
                    break;
                case "R Button":
                    output.Add((byte)ControlCode.R_Button);
                    break;
                case "Z Button":
                    output.Add((byte)ControlCode.Z_Button);
                    break;
                case "C Up":
                    output.Add((byte)ControlCode.C_Up);
                    break;
                case "C Down":
                    output.Add((byte)ControlCode.C_Down);
                    break;
                case "C Left":
                    output.Add((byte)ControlCode.C_Left);
                    break;
                case "C Right":
                    output.Add((byte)ControlCode.C_Right);
                    break;
                case "Triangle":
                    output.Add((byte)ControlCode.Triangle);
                    break;
                case "Control Stick":
                    output.Add((byte)ControlCode.Control_Stick);
                    break;
                case "D Pad":
                    output.Add((byte)ControlCode.D_Pad);
                    break;
            }

            return output;
        }
    }
}
