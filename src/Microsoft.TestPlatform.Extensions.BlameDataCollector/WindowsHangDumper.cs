﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TestPlatform.Extensions.BlameDataCollector
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using Microsoft.Win32.SafeHandles;

    internal class WindowsHangDumper : IHangDumper
    {
        public void Dump(int processId, string outputFile, DumpTypeOption type)
        {
            var process = Process.GetProcessById(processId);
            CollectDump(process, outputFile, type);
        }

        internal static void CollectDump(Process process, string outputFile, DumpTypeOption type)
        {
            // Open the file for writing
            using (var stream = new FileStream(outputFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                NativeMethods.MINIDUMP_EXCEPTION_INFORMATION exceptionInfo = default(NativeMethods.MINIDUMP_EXCEPTION_INFORMATION);

                NativeMethods.MINIDUMP_TYPE dumpType = NativeMethods.MINIDUMP_TYPE.MiniDumpNormal;
                switch (type)
                {
                    case DumpTypeOption.Full:
                        dumpType = NativeMethods.MINIDUMP_TYPE.MiniDumpWithFullMemory |
                                   NativeMethods.MINIDUMP_TYPE.MiniDumpWithDataSegs |
                                   NativeMethods.MINIDUMP_TYPE.MiniDumpWithHandleData |
                                   NativeMethods.MINIDUMP_TYPE.MiniDumpWithUnloadedModules |
                                   NativeMethods.MINIDUMP_TYPE.MiniDumpWithFullMemoryInfo |
                                   NativeMethods.MINIDUMP_TYPE.MiniDumpWithThreadInfo |
                                   NativeMethods.MINIDUMP_TYPE.MiniDumpWithTokenInformation;
                        break;
                    case DumpTypeOption.WithHeap:
                        dumpType = NativeMethods.MINIDUMP_TYPE.MiniDumpWithPrivateReadWriteMemory |
                                   NativeMethods.MINIDUMP_TYPE.MiniDumpWithDataSegs |
                                   NativeMethods.MINIDUMP_TYPE.MiniDumpWithHandleData |
                                   NativeMethods.MINIDUMP_TYPE.MiniDumpWithUnloadedModules |
                                   NativeMethods.MINIDUMP_TYPE.MiniDumpWithFullMemoryInfo |
                                   NativeMethods.MINIDUMP_TYPE.MiniDumpWithThreadInfo |
                                   NativeMethods.MINIDUMP_TYPE.MiniDumpWithTokenInformation;
                        break;
                    case DumpTypeOption.Mini:
                        dumpType = NativeMethods.MINIDUMP_TYPE.MiniDumpWithThreadInfo;
                        break;
                }

                // Retry the write dump on ERROR_PARTIAL_COPY
                for (int i = 0; i < 5; i++)
                {
                    // Dump the process!
                    if (NativeMethods.MiniDumpWriteDump(process.Handle, (uint)process.Id, stream.SafeFileHandle, dumpType, ref exceptionInfo, IntPtr.Zero, IntPtr.Zero))
                    {
                        break;
                    }
                    else
                    {
                        int err = Marshal.GetHRForLastWin32Error();
                        if (err != NativeMethods.ERROR_PARTIAL_COPY)
                        {
                            Marshal.ThrowExceptionForHR(err);
                        }
                    }
                }
            }
        }

        private static class NativeMethods
        {
            public const int ERROR_PARTIAL_COPY = unchecked((int)0x8007012b);

            [DllImport("Dbghelp.dll", SetLastError = true)]
            public static extern bool MiniDumpWriteDump(IntPtr hProcess, uint ProcessId, SafeFileHandle hFile, MINIDUMP_TYPE DumpType, ref MINIDUMP_EXCEPTION_INFORMATION ExceptionParam, IntPtr UserStreamParam, IntPtr CallbackParam);

            [StructLayout(LayoutKind.Sequential, Pack = 4)]
            public struct MINIDUMP_EXCEPTION_INFORMATION
            {
                public uint ThreadId;
                public IntPtr ExceptionPointers;
                public int ClientPointers;
            }

            [Flags]
#pragma warning disable SA1201 // Elements must appear in the correct order
            public enum MINIDUMP_TYPE : uint
#pragma warning restore SA1201 // Elements must appear in the correct order
            {
                MiniDumpNormal = 0,
                MiniDumpWithDataSegs = 1 << 0,
                MiniDumpWithFullMemory = 1 << 1,
                MiniDumpWithHandleData = 1 << 2,
                MiniDumpFilterMemory = 1 << 3,
                MiniDumpScanMemory = 1 << 4,
                MiniDumpWithUnloadedModules = 1 << 5,
                MiniDumpWithIndirectlyReferencedMemory = 1 << 6,
                MiniDumpFilterModulePaths = 1 << 7,
                MiniDumpWithProcessThreadData = 1 << 8,
                MiniDumpWithPrivateReadWriteMemory = 1 << 9,
                MiniDumpWithoutOptionalData = 1 << 10,
                MiniDumpWithFullMemoryInfo = 1 << 11,
                MiniDumpWithThreadInfo = 1 << 12,
                MiniDumpWithCodeSegs = 1 << 13,
                MiniDumpWithoutAuxiliaryState = 1 << 14,
                MiniDumpWithFullAuxiliaryState = 1 << 15,
                MiniDumpWithPrivateWriteCopyMemory = 1 << 16,
                MiniDumpIgnoreInaccessibleMemory = 1 << 17,
                MiniDumpWithTokenInformation = 1 << 18,
                MiniDumpWithModuleHeaders = 1 << 19,
                MiniDumpFilterTriage = 1 << 20,
                MiniDumpWithAvxXStateContext = 1 << 21,
                MiniDumpWithIptTrace = 1 << 22,
                MiniDumpValidTypeFlags = (-1) ^ ((~1) << 22)
            }
        }
    }
}
