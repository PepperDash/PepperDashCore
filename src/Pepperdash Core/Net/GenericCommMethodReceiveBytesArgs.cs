using System;

namespace PepperDash.Core.Net
{
    /// <summary>
    /// 
    /// </summary>
    public class GenericCommMethodReceiveBytesArgs : EventArgs
    {
        /// <summary>
        /// 
        /// </summary>
		public byte[] Bytes { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
		public GenericCommMethodReceiveBytesArgs(byte[] bytes)
        {
            Bytes = bytes;
        }

        /// <summary>
        /// S+ Constructor
        /// </summary>
        public GenericCommMethodReceiveBytesArgs() { }
    }
}