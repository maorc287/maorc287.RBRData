using maorc287.RBRDataExtPlugin;
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace maorc287.RBRDataExtPlugin
{
    public static class MemoryReader
    {
        private static IntPtr _cachedHandle = IntPtr.Zero;
        private static int _cachedProcessId = 0;
        private static string _cachedProcessName = null;
        private static readonly Dictionary<string, IntPtr> _cachedDllBases = new Dictionary<string, IntPtr>();


        [Flags]
        public enum ProcessAccessFlags : uint
        {
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            VirtualMemoryOperation = 0x00000008
        }

        // Import necessary functions from kernel32.dll for process memory access
        [DllImport("kernel32.dll")]
        internal static extern IntPtr OpenProcess(ProcessAccessFlags access, bool inheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool CloseHandle(IntPtr hObject);

        internal static uint GetProcessIdByName(string processName)
        {
            var processes = Process.GetProcessesByName(processName);
            return (uint)(processes.Length > 0 ? processes[0].Id : 0);
        }

        internal static string RBRGamePath { get; private set; } = null; 

        internal static void UpdateRBRGamePath()
        {
            if (_cachedProcessId == 0 || _cachedHandle == IntPtr.Zero) return;

            try
            {
                var process = Process.GetProcessById(_cachedProcessId);
                if (process.HasExited) return;

                // EXACT: richardburnsrally_SSE.exe folder path
                RBRGamePath = Path.GetDirectoryName(process.MainModule.FileName);
            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Warn($"[RBRDataExt] Failed find RBR Game Path: {ex.Message}");
                return;
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [Out] byte[] buffer,
            uint size,
            out int bytesRead);

        internal static T ReadValue<T>(IntPtr hProcess, IntPtr address) where T : struct
        {
            if (hProcess == IntPtr.Zero || address == IntPtr.Zero || address.ToInt64() < 0x1000)
            {
                return default(T); // Null/zero address = safe default
            }

            int size;

            if (typeof(T) == typeof(bool) || typeof(T) == typeof(byte))
                size = 1;
            else if (typeof(T) == typeof(short))
                size = sizeof(short);
            else if (typeof(T) == typeof(int))
                size = sizeof(int);
            else if (typeof(T) == typeof(uint))
                size = sizeof(uint);
            else if (typeof(T) == typeof(float))
                size = sizeof(float);
            else
                throw new NotSupportedException($"Type {typeof(T)} is not supported.");

            byte[] buffer = new byte[size];
            if (!ReadProcessMemory(hProcess, address, buffer, (uint)size, out _))
                throw new InvalidOperationException($"Failed to read memory at 0x{address.ToInt64():X}");

            if (typeof(T) == typeof(float)) return (T)(object)BitConverter.ToSingle(buffer, 0);
            if (typeof(T) == typeof(int)) return (T)(object)BitConverter.ToInt32(buffer, 0);
            if (typeof(T) == typeof(uint)) return (T)(object)BitConverter.ToUInt32(buffer, 0);
            if (typeof(T) == typeof(short)) return (T)(object)BitConverter.ToInt16(buffer, 0);
            if (typeof(T) == typeof(byte)) return (T)(object)buffer[0];

            throw new NotSupportedException($"Type {typeof(T)} is not supported.");
        }

        internal static IntPtr ReadPointer(IntPtr hProcess, IntPtr address)
        {
            uint ptrValue = ReadUInt(hProcess, address);
            return new IntPtr(ptrValue);
        }

        internal static IntPtr ResolvePointerChain(IntPtr baseAddress, int[] offsets, Func<IntPtr, IntPtr> readPointer)
        {
            IntPtr currentAddress = baseAddress;

            foreach (int offset in offsets)
            {
                currentAddress = readPointer(currentAddress); // Dereference
                if (currentAddress == IntPtr.Zero)
                    return IntPtr.Zero; // Invalid pointer
                currentAddress += offset;
            }

            return currentAddress;
        }

        internal static IntPtr GetOrOpenProcessHandle(string processName)
        {
            if (_cachedHandle != IntPtr.Zero)
            {
                UpdateRBRGamePath();

                try
                {
                    var proc = Process.GetProcessById(_cachedProcessId);
                    if (!proc.HasExited)
                    {
                        return _cachedHandle;
                    }
                }
                catch (Exception ex)
                {
                    // Process probably exited, or ID is invalid
                    SimHub.Logging.Current.Warn($"[RBRDataExt] Failed find RBR ProcessID: {ex.Message}");
                    CloseCachedHandle();
                    return IntPtr.Zero;
                }
                // Process exited, close handle
                CloseCachedHandle();
            }

            var processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0) return IntPtr.Zero;

            var process = processes[0];
            _cachedHandle = OpenProcess(ProcessAccessFlags.VirtualMemoryRead, false, process.Id);
            if (_cachedHandle != IntPtr.Zero)
            {
                _cachedProcessId = process.Id;
                _cachedProcessName = process.ProcessName;
            }

            return _cachedHandle;
        }

        internal static void CloseCachedHandle()
        {
            if (_cachedHandle != IntPtr.Zero)
            {
                CloseHandle(_cachedHandle);
                _cachedHandle = IntPtr.Zero;
                _cachedProcessId = 0;
                _cachedProcessName = null;
                _cachedDllBases.Clear(); // Clear DLL cache
                TelemetryData.pointerCache.ClearAllCache(); // Clear cached pointers when handle is closed
            }
        }

        internal static IntPtr TryGetOrCacheDllBaseAddress(string dllName)
        {
            if (_cachedProcessId == 0 || _cachedHandle == IntPtr.Zero)
                return IntPtr.Zero;

            if (_cachedDllBases.TryGetValue(dllName, out var cachedBase) && cachedBase != IntPtr.Zero)
                return cachedBase;

            try
            {
                var process = Process.GetProcessById(_cachedProcessId);
                if (process.HasExited) return IntPtr.Zero;

                var module = process.Modules.Cast<ProcessModule>()
                    .FirstOrDefault(m => m.ModuleName.Equals(dllName, StringComparison.OrdinalIgnoreCase));

                if (module == null) return IntPtr.Zero;

                _cachedDllBases[dllName] = module.BaseAddress;
                return module.BaseAddress;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        internal static bool TryReadFromDll<T>(string dllName, uint offset, out T value) where T : struct
        {
            value = default;

            if (_cachedHandle == IntPtr.Zero || _cachedProcessId == 0)
            {
                return false; // no process handle
            }

            IntPtr dllBase = TryGetOrCacheDllBaseAddress(dllName);
            if (dllBase == IntPtr.Zero)
            {
                return false; // DLL not found
            }

            try
            {
                IntPtr finalAddress = IntPtr.Add(dllBase, (int)offset);
                value = ReadValue<T>(_cachedHandle, finalAddress);
                return true;
            }
            catch
            {
                return false; // read failed (invalid offset, etc.)
            }
        }

        internal static float[] ReadFloatArray(IntPtr hProcess, IntPtr address, int count)
        {
            int size = sizeof(float) * count;
            byte[] buffer = new byte[size];

            if (!ReadProcessMemory(hProcess, address, buffer, (uint)size, out _))
                return new float[count]; // Read failed

            float[] result = new float[count];
            for (int i = 0; i < count; i++)
                result[i] = BitConverter.ToSingle(buffer, i * sizeof(float));

            return result;
        }

        internal static float ReadFloat(IntPtr hProcess, IntPtr address) => ReadValue<float>(hProcess, address);
        internal static int ReadInt(IntPtr hProcess, IntPtr address) => ReadValue<int>(hProcess, address);
        internal static uint ReadUInt(IntPtr hProcess, IntPtr address) => ReadValue<uint>(hProcess, address);
        internal static short ReadShort(IntPtr hProcess, IntPtr address) => ReadValue<short>(hProcess, address);
        internal static byte ReadByte(IntPtr hProcess, IntPtr address) => ReadValue<byte>(hProcess, address);
    }
}
