namespace z80emu.Operations
{
  class And : Operation //todo
  {
    private readonly FlagsRegister f;
    private readonly IReference<byte> dst;
    private readonly IReference<byte> src;

    public And(FlagsRegister f, IReference<byte> dst, IReference<byte> src, byte sz = 1)
      : base(sz, "AND")
    {
      this.f = f;
      this.dst = dst;
      this.src = src;
    }

    public override OperationResult Execute(Memory m)
    {
      byte v1 = dst.Read(m);
      byte v2 = src.Read(m);
      byte res = (byte)(v1 & v2);
      f.Sign = res > 0x7F;
      f.Zero = res == 0;
      f.HalfCarry = true;
      f.ParityOverflow = EvenParity(res);
      f.AddSub = false;
      f.Carry = false;
      dst.Write(m, res);

      return new OperationResult(src.IsRegister ? 4 : 7, this.OpSize == 1 ? 1 : 2, this.OpSize);
    }
  }
}