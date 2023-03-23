using System;

namespace PepperDash.Core.Net.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public class GenericCommMethodReceiveTextArgs : EventArgs
    {
        /// <summary>
        /// 
        /// </summary>
		public string Text { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public string Delimiter { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
		public GenericCommMethodReceiveTextArgs(string text)
        {
            Text = text;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="delimiter"></param>
        public GenericCommMethodReceiveTextArgs(string text, string delimiter)
            : this(text)
        {
            Delimiter = delimiter;
        }

        /// <summary>
        /// S+ Constructor
        /// </summary>
        public GenericCommMethodReceiveTextArgs() { }
    }
}