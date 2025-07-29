using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace maorc287.RBRDataExtPlugin
{
    public static class MemoryReader
    {
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


        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [Out] byte[] buffer,
            uint size,
            out int bytesRead);

        internal static T ReadValue<T>(IntPtr hProcess, IntPtr address) where T : struct
        {
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
            int ptrValue = ReadInt(hProcess, address);
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

        internal static float ReadFloat(IntPtr hProcess, IntPtr address) => ReadValue<float>(hProcess, address);
        internal static int ReadInt(IntPtr hProcess, IntPtr address) => ReadValue<int>(hProcess, address);
        internal static uint ReadUInt(IntPtr hProcess, IntPtr address) => ReadValue<uint>(hProcess, address);
        internal static short ReadShort(IntPtr hProcess, IntPtr address) => ReadValue<short>(hProcess, address);
        internal static byte ReadByte(IntPtr hProcess, IntPtr address) => ReadValue<byte>(hProcess, address);
    }
}
