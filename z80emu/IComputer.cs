namespace z80emu
{
    interface IComputer
    {
        CPU CPU {get;}
        ULA ULA {get;}
        IMemory Memory {get;}
    }
}
