using System;

namespace PepperDash.Core.Intersystem.Tokens
{
    public sealed class XSigDigitalToken : XSigToken
    {
        private readonly bool _value;

        public XSigDigitalToken(int index, bool value)
            : base(index)
        {
            // 12-bits available for digital encoded data
            if (index >= 4096 || index < 0)
                throw new ArgumentOutOfRangeException("index");

            _value = value;
        }

        public bool Value
        {
            get { return _value; }
        }

        public override XSigTokenType TokenType
        {
            get { return XSigTokenType.Digital; }
        }

        public override byte[] GetBytes()
        {
            return new[] {
                (byte)(0x80 | (Value ? 0 : 0x20) | ((Index - 1) >> 7)),
                (byte)((Index - 1) & 0x7F)
            };
        }

        public override XSigToken GetTokenWithOffset(int offset)
        {
            if (offset == 0) return this;
            return new XSigDigitalToken(Index + offset, Value);
        }

        public override string ToString()
        {
            return Index + " = " + (Value ? "High" : "Low");
        }

        public string ToString(IFormatProvider formatProvider)
        {
            return Value.ToString(formatProvider);
        }
    }
}