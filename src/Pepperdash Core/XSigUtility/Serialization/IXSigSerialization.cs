using PepperDash.Core.XSigUtility.Tokens;
using System.Collections.Generic;

namespace PepperDash.Core.XSigUtility.Serialization
{
    /// <summary>
    /// Interface to determine XSig serialization for an object.
    /// </summary>
    public interface IXSigSerialization
    {
        /// <summary>
        /// Serialize the sig data
        /// </summary>
        /// <returns></returns>
        IEnumerable<XSigToken> Serialize();

        /// <summary>
        /// Deserialize the sig data
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tokens"></param>
        /// <returns></returns>
        T Deserialize<T>(IEnumerable<XSigToken> tokens) where T : class, IXSigSerialization;
    }
}