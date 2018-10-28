using System;

namespace z80emu.Operations
{
  using word = System.UInt16;

  struct OperationResult
  {
    public int DeltaT; // T-states spent by operation
    public int DeltaR; // R register increment
    public int DeltaPC; // PC register increment
    public OperationResult(int dt, int dr, int dp)
    {
      this.DeltaT = dt;
      this.DeltaR = dr;
      this.DeltaPC = dp;
    }
  }

  class Operation
  {
    public Operation(int opSize, string opName)
    {
      this.OpSize = opSize;
      this.Name = opName;
    }

    public int OpSize {get;}
    public string Name {get;}
    public virtual OperationResult Execute(Memory m)
    {
      return new OperationResult();
    }

    public Handler F => m => (word)Execute(m).DeltaPC;

    protected bool EvenParity(byte a)
    {
        ulong x1 = 0x0101010101010101;
        ulong x2 = 0x8040201008040201;
        return ((((a * x1) & x2) % (ulong)0x1FF) & 1) == 0;
    }

  }
}