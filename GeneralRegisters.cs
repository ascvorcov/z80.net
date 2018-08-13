using System;

namespace z80emu
{
    class GeneralRegisters
    {
      public GeneralRegisters()
      {
        this.BC = new WordRegister();
        this.DE = new WordRegister();
        this.HL = new WordRegister();
        this.WZ = new WordRegister();
      }

      public WordRegister BC {get;}
      public WordRegister DE {get;}
      public WordRegister HL {get;}
      public WordRegister WZ {get;}

      public void Dump(string suffix)
      {
        BC.Dump("BC" + suffix);
        DE.Dump("DE" + suffix);
        HL.Dump("HL" + suffix);
        WZ.Dump("WZ" + suffix);
      }
    }
}