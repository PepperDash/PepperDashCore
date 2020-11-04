using System;

namespace PepperDash.Core.Intersystem.Tokens
{
    public sealed class XSigAnalogToken : XSigToken, IFormattable
    {
        private readonly ushort _value;

        public XSigAnalogToken(int index, ushort value)
            : base(index)
        {
            // 10-bits available for analog encoded data
            if (index >= 1024 || index < 0)
                throw new ArgumentOutOfRangeException("index");

            _value = value;
        }

        public ushort Value
        {
            get { return _value; }
        }

        public override XSigTokenType TokenType
        {
            get { return XSigTokenType.Analog; }
        }

        public override byte[] GetBytes()
        {
            return new[] {
                (byte)(0xC0 | ((Value & 0xC000) >> 10) | (Index >> 7)),
                (byte)((Index - 1) & 0x7F),
                (byte)((Value & 0x3F80) >> 7),
                (byte)(Value & 0x7F)
            };
        }

        public override XSigToken GetTokenWithOffset(int offset)
        {
            if (offset == 0) return this;
            return new XSigAnalogToken(Index + offset, Value);
        }

        public override string ToString()
        {
            return Index + " = 0x" + Value.ToString("X4");
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return Value.ToString(format, formatProvider);
        }
    }
}