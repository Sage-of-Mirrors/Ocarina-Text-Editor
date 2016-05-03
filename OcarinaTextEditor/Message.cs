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

        public Message(EndianBinaryReader reader, Dictionary<ControlCode, string> controlCodeDict)
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

            GetStringData(reader, controlCodeDict);

            reader.BaseStream.Position = cuPos;
        }

        public void Print()
        {
            string printString = string.Format("ID: {0}\nBox Type: {1}\nBox Pos: {2}\nData:\n{3}\n\n", MessageID, BoxType, BoxPosition, TextData);
            Console.Write(printString);
        }

        private void GetStringData(EndianBinaryReader reader, Dictionary<ControlCode, string> codeDict)
        {
            List<char> charData = new List<char>();

            byte testByte = reader.ReadByte();

            while (testByte != 0x02)
            {
                bool readControlCode = false;

                if (codeDict.ContainsKey((ControlCode)testByte))
                {
                    charData.AddRange(GetControlCode((ControlCode)testByte, reader, codeDict));
                    readControlCode = true;
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

        private char[] GetControlCode(ControlCode code, EndianBinaryReader reader, Dictionary<ControlCode, string> codeDict)
        {
            List<char> codeBank = new List<char>();

            if (codeDict.First(x => x.Key == code).Value.Count() == 1)
            {
                codeBank.AddRange(codeDict[code].ToCharArray());
                return codeBank.ToArray();
            }

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
                    codeInsides = string.Format("{0}:{1}", "Pixels Right", numSpaces);
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
                case ControlCode.Box_Break:
                    return "\n\n".ToCharArray();

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

        public List<byte> ConvertTextData(Dictionary<ControlCode, string> codeDict)
        {
            List<byte> data = new List<byte>();

            for (int i = 0; i < TextData.Count(); i++)
            {
                // Not a control code, copy char to output buffer
                if (TextData[i] != '<')
                {
                    if (codeDict.ContainsValue(TextData[i].ToString()))
                    {
                        data.Add((byte)codeDict.First(x => x.Value == TextData[i].ToString()).Key);
                    }
                    else if (TextData[i] == '\n')
                    {
                        try
                        {
                            if (TextData[i + 2] == '\n')
                                data.Add((byte)ControlCode.Box_Break);
                            else if (TextData[i - 2] == '\n')
                            {
                                // Do nothing
                            }
                            else
                                data.Add((byte)ControlCode.Line_Break);
                        }
                        catch (IndexOutOfRangeException ex)
                        {
                            data.Add((byte)ControlCode.Line_Break);
                        }
                    }
                    else if (TextData[i] == '\r')
                    {
                        // Do nothing
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

            switch (code[0].ToLower())
            {
                case "line break":
                    output.Add((byte)ControlCode.Line_Break);
                    break;
                case "box break":
                    output.Add((byte)ControlCode.Box_Break);
                    break;
                case "color":
                    output.Add((byte)ControlCode.Color);
                    switch (code[1].ToLower())
                    {
                        case "white":
                            output.Add((byte)Color.White);
                            break;
                        case "red":
                            output.Add((byte)Color.Red);
                            break;
                        case "green":
                            output.Add((byte)Color.Green);
                            break;
                        case "blue":
                            output.Add((byte)Color.Blue);
                            break;
                        case "light_blue":
                            output.Add((byte)Color.Cyan);
                            break;
                        case "pink":
                            output.Add((byte)Color.Magenta);
                            break;
                        case "yellow":
                            output.Add((byte)Color.Yellow);
                            break;
                        case "black":
                            output.Add((byte)Color.Black);
                            break;
                    }
                    break;
                case "pixels right":
                    output.Add((byte)ControlCode.Spaces);
                    output.Add(Convert.ToByte(code[1]));
                    break;
                case "jump":
                    output.Add((byte)ControlCode.Jump);
                    byte[] jumpIDBytes = BitConverter.GetBytes(Convert.ToInt16(code[1]));
                    output.Add(jumpIDBytes[1]);
                    output.Add(jumpIDBytes[0]);
                    break;
                case "draw instant":
                    output.Add((byte)ControlCode.Draw_Instant);
                    break;
                case "draw char":
                    output.Add((byte)ControlCode.Draw_Char);
                    break;
                case "shop description":
                    output.Add((byte)ControlCode.Shop_Description);
                    break;
                case "event":
                    output.Add((byte)ControlCode.Event);
                    break;
                case "delay":
                    output.Add((byte)ControlCode.Delay);
                    output.Add(Convert.ToByte(code[1]));
                    break;
                case "fade":
                    output.Add((byte)ControlCode.Fade);
                    output.Add(Convert.ToByte(code[1]));
                    break;
                case "player":
                    output.Add((byte)ControlCode.Player);
                    break;
                case "ocarina":
                    output.Add((byte)ControlCode.Ocarina);
                    break;
                case "sound":
                    output.Add((byte)ControlCode.Sound);
                    byte[] soundIDBytes = BitConverter.GetBytes(Convert.ToInt16(code[1]));
                    output.Add(soundIDBytes[1]);
                    output.Add(soundIDBytes[0]);
                    break;
                case "icon":
                    output.Add((byte)ControlCode.Icon);
                    output.Add(Convert.ToByte(code[1]));
                    break;
                case "speed":
                    output.Add((byte)ControlCode.Speed);
                    output.Add(Convert.ToByte(code[1]));
                    break;
                case "background":
                    output.Add((byte)ControlCode.Background);
                    //byte[] backgroundIDBytes = BitConverter.GetBytes(Convert.ToInt32(code[1]));
                    //output.Add(backgroundIDBytes[3]);
                    //output.Add(backgroundIDBytes[2]);
                    //output.Add(backgroundIDBytes[1]);
                    break;
                case "marathon time":
                    output.Add((byte)ControlCode.Marathon_Time);
                    break;
                case "race time":
                    output.Add((byte)ControlCode.Race_Time);
                    break;
                case "points":
                    output.Add((byte)ControlCode.Points);
                    break;
                case "gold skulltulas":
                    output.Add((byte)ControlCode.Gold_Skulltulas);
                    break;
                case "no skip":
                    output.Add((byte)ControlCode.No_Skip);
                    break;
                case "two choices":
                    output.Add((byte)ControlCode.Two_Choices);
                    break;
                case "three choices":
                    output.Add((byte)ControlCode.Three_Choices);
                    break;
                case "fish weight":
                    output.Add((byte)ControlCode.Fish_Weight);
                    break;
                case "high score":
                    output.Add((byte)ControlCode.High_Score);
                    //output.Add(Convert.ToByte(code[1]));
                    break;
                case "time":
                    output.Add((byte)ControlCode.Time);
                    break;
                case "dash":
                    output.Add((byte)ControlCode.Dash);
                    break;
                case "a button":
                    output.Add((byte)ControlCode.A_Button);
                    break;
                case "b button":
                    output.Add((byte)ControlCode.B_Button);
                    break;
                case "c button":
                    output.Add((byte)ControlCode.C_Button);
                    break;
                case "l button":
                    output.Add((byte)ControlCode.L_Button);
                    break;
                case "r button":
                    output.Add((byte)ControlCode.R_Button);
                    break;
                case "z button":
                    output.Add((byte)ControlCode.Z_Button);
                    break;
                case "c up":
                    output.Add((byte)ControlCode.C_Up);
                    break;
                case "c down":
                    output.Add((byte)ControlCode.C_Down);
                    break;
                case "c left":
                    output.Add((byte)ControlCode.C_Left);
                    break;
                case "c right":
                    output.Add((byte)ControlCode.C_Right);
                    break;
                case "triangle":
                    output.Add((byte)ControlCode.Triangle);
                    break;
                case "control stick":
                    output.Add((byte)ControlCode.Control_Stick);
                    break;
                case "d pad":
                    output.Add((byte)ControlCode.D_Pad);
                    break;
            }

            return output;
        }
    }
}
