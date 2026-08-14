using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AmenoLink.Managers.Program;

#pragma warning disable SYSLIB1054
internal static class JobObjectPInvoke
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string? lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetInformationJobObject(IntPtr hJob, int JobObjectInfoClass, IntPtr lpJobObjectInfo, int cbJobObjectInfoLength);

    public const int JobObjectExtendedLimitInformation = 9;
    public const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;

    [StructLayout(LayoutKind.Sequential)]
    public struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public nint MinimumWorkingSetSize;
        public nint MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public nint Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct IO_COUNTERS
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public nint ProcessMemoryLimit;
        public nint JobMemoryLimit;
        public nint PeakProcessMemoryUsed;
        public nint PeakJobMemoryUsed;
    }
}
#pragma warning restore SYSLIB1054

internal static class ChildProcessTracker
{
    private static readonly IntPtr s_jobHandle;

    static ChildProcessTracker()
    {
        if (!OperatingSystem.IsWindows())
            return;

        s_jobHandle = JobObjectPInvoke.CreateJobObject(IntPtr.Zero, null);

        var info = new JobObjectPInvoke.JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            BasicLimitInformation = new JobObjectPInvoke.JOBOBJECT_BASIC_LIMIT_INFORMATION
            {
                LimitFlags = JobObjectPInvoke.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
            }
        };

        int length = Marshal.SizeOf<JobObjectPInvoke.JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
        IntPtr extendedInfoPtr = Marshal.AllocHGlobal(length);

        try
        {
            Marshal.StructureToPtr(info, extendedInfoPtr, false);
            JobObjectPInvoke.SetInformationJobObject(
                s_jobHandle,
                JobObjectPInvoke.JobObjectExtendedLimitInformation,
                extendedInfoPtr,
                length
            );
        }
        finally
        {
            Marshal.FreeHGlobal(extendedInfoPtr);
        }
    }

    public static void AddProcess(Process process)
    {
        if (!OperatingSystem.IsWindows())
            return;

        if (s_jobHandle != IntPtr.Zero && process is { HasExited: false })
        {
            try
            {
                JobObjectPInvoke.AssignProcessToJobObject(s_jobHandle, process.Handle);
            }
            catch { }
        }
    }
}
