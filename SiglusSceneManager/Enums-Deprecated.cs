using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SiglusSceneManager {
    public enum Register {
        //MovZX
        EAX = 0x05, ECX = 0x0D,
        EDX = 0x15, EBX = 0x1D,
        ESP = 0x25, EBP = 0x2D,
        ESI = 0x35, EDI = 0x3D,
        //Mov Long
        AL = 0x84, BL = 0x9C,
        CL = 0x8C, DL = 0x94,
        //Unknowk
        Unk = -1
    }
}
