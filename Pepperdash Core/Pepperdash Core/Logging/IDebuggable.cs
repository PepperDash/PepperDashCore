using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronLogger;

namespace PepperDash.Core
{
    /// <summary>
    /// Indicates a class capabel of console debugging and logging
    /// </summary>
    public interface IDebuggable : IKeyName
    {
        /// <summary>
        /// The class that handles console debugging and logging
        /// </summary>
        DeviceDebug Debug { get; }
    }

    /// <summary>
    /// A class to handle implementation of IDebuggable
    /// </summary>
    public class DeviceDebug
    {
        Device _parentDevice;

        /// <summary>
        /// The current Debug Level for the device instance
        /// </summary>
        public int DebugLevel;

        public DeviceDebug(Device parentDevice)
        {
            _parentDevice = parentDevice;
        }

        public void Console(uint level, string format, params object[] items)
        {
            if (DebugLevel >= level)
            {
                Debug.Console(level, format, items);
            }
        }

        public void Console(uint level, IKeyed dev, string format, params object[] items)
        {
            if (DebugLevel >= level)
            {
                Debug.Console(level, _parentDevice, format, items);
            }
        }

        public void Console(uint level, ErrorLogLevel errorLogLevel, string format, params object[] items)
        {
            if (DebugLevel >= level)
            {
                Debug.Console(level, errorLogLevel, format, items);
            }
        }

        public void ConsoleWithLog(uint level, string format, params object[] items)
        {
            if (DebugLevel >= level)
            {
                Debug.Console(level, format, items);
            }
        }

        public void ConsoleWithLog(uint level, IKeyed dev, string format, params object[] items)
        {
            if (DebugLevel >= level)
            {
                Debug.Console(level, _parentDevice, format, items);
            }
        }

        public void LogError(ErrorLogLevel errorLogLevel, string str)
        {
            Debug.LogError(errorLogLevel, str);
        }
    }
}