﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Crestron.SimplSharp;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;

namespace PepperDash.Core.Logging
{
    /// <summary>
    /// Represents a debugging context
    /// </summary>
    public static class DebugContext
    {
        private static readonly CTimer SaveTimer;
        private static readonly Dictionary<string, DebugContextData> CurrentData;

        public static readonly string ApplianceFilePath = Path.Combine("/", "user", "debug", $"app{InitialParametersClass.ApplicationNumber.ToString().PadLeft(2, '0')}-debug.json");
        public static readonly string ServerFilePath = Path.Combine("/", "User", "debug", $"{InitialParametersClass.RoomId}-debug.json");

        /// <summary>
        /// The name of the file containing the current debug settings.
        /// </summary>
        public static readonly string FileName = CrestronEnvironment.DevicePlatform switch
        {
            eDevicePlatform.Appliance => ApplianceFilePath,
            eDevicePlatform.Server => ServerFilePath,
            _ => string.Empty
        };

        static DebugContext()
        {
            CurrentData = LoadData();
            SaveTimer = new CTimer(_ => SaveData(), Timeout.Infinite);

            CrestronEnvironment.ProgramStatusEventHandler += args =>
            {
                if (args == eProgramStatusEventType.Stopping)
                {
                    using (SaveTimer)
                    {
                        SaveData();
                        SaveTimer.Stop();
                    }
                }
            };
        }

        public static bool TryGetDataForKey(string key, out DebugContextData data) =>
            CurrentData.TryGetValue(key, out data);

        public static DebugContextData GetOrCreateDataForKey(string key, LogEventLevel defaultLevel) =>
            CurrentData.TryGetValue(key, out var data) ? data : new DebugContextData(defaultLevel, false, []);

        public static void SetDataForKey(string key, LogEventLevel defaultLevel, bool logAllDevices = false, string[] devices = null)
        {
            if (CurrentData.ContainsKey(key))
            {
                CurrentData[key] = new DebugContextData(defaultLevel, logAllDevices, devices);
            }
            else
            {
                CurrentData.Add(key, new DebugContextData(defaultLevel, logAllDevices, devices));
            }

            SaveTimer.Reset(5000);
        }

        private static void SaveData()
        {
            Log.Information("Saving debug data to file:{File}", FileName);

            try
            {
                var debugDirectory = Path.GetDirectoryName(FileName) ??
                                     throw new Exception("File directory is root");

                Directory.CreateDirectory(debugDirectory);
                var json = JsonConvert.SerializeObject(CurrentData);
                File.WriteAllText(FileName, json);
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to save debug data");
            }
        }

        private static Dictionary<string, DebugContextData> LoadData()
        {
            if (!File.Exists(FileName))
            {
                return new Dictionary<string, DebugContextData>();
            }

            var json = File.ReadAllText(FileName);
            return JsonConvert.DeserializeObject<Dictionary<string, DebugContextData>>(json);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public record DebugContextData(LogEventLevel Level, bool LogAllDevices = false, string[] Devices = null);
}
