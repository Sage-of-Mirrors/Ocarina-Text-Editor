using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OcarinaTextEditor.Enums
{
    public enum ControlCode
    {
        Line_Break = 0x01,
        End = 0x02,
        Box_Break = 0x04,
        Color = 0x05,
        Spaces = 0x06,
        Jump = 0x07,
        Draw_Instant = 0x08,
        Draw_Char = 0x09,
        Shop_Description = 0x0A,
        Event = 0x0B,
        Delay = 0x0C,
        Unused1 = 0x0D,
        Fade = 0x0E,
        Player = 0x0F,
        Ocarina = 0x10,
        Unused2 = 0x11,
        Sound = 0x12,
        Icon = 0x13,
        Speed = 0x14,
        Background = 0x15,
        Marathon_Time = 0x16,
        Race_Time = 0x17,
        Points = 0x18,
        Gold_Skulltulas = 0x19,
        No_Skip = 0x1A,
        Two_Choices = 0x1B,
        Three_Choices = 0x1C,
        Fish_Weight = 0x1D,
        High_Score = 0x1E,
        Time = 0x1F,

        Dash = 0x7F,

        A_Button = 0x9F,
        B_Button = 0xA0,
        C_Button = 0xA1,
        L_Button = 0xA2,
        R_Button = 0xA3,
        Z_Button = 0xA4,
        C_Up = 0xA5,
        C_Down = 0xA6,
        C_Left = 0xA7,
        C_Right = 0xA8,
        Triangle = 0xA9,
        Control_Stick = 0xAA,
        D_Pad = 0xAB
    }
}
