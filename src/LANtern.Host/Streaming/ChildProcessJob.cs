using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Management;
using Microsoft.Win32.SafeHandles;

namespace LANtern.Host.Streaming;

/// <summary>
/// Keeps native sidecars tied to the Host lifetime. Windows terminates every
/// assigned process even when Visual Studio stops the Host abruptly.
/// </summary>
public sealed class ChildProcessJob : IDisposable
{
    private const uint JobObjectLimitKillOnJobClose = 0x00002000;
    private readonly SafeFileHandle _handle;

    public ChildProcessJob()
    {
        _handle = CreateJobObject(IntPtr.Zero, null);
        if (_handle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error(), "Alt süreç Job Object oluşturulamadı.");

        var limits = new JobObjectExtendedLimitInformation
        {
            BasicLimitInformation = new JobObjectBasicLimitInformation { LimitFlags = JobObjectLimitKillOnJobClose }
        };
        var size = Marshal.SizeOf<JobObjectExtendedLimitInformation>();
        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(limits, buffer, false);
            if (!SetInformationJobObject(_handle, 9, buffer, (uint)size))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Alt süreç Job Object yapılandırılamadı.");
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    public void Add(Process process)
    {
        if (!AssignProcessToJobObject(_handle, process.Handle))
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"{process.ProcessName} Host yaşam döngüsüne bağlanamadı.");
    }

    public void StopOrphanedLANternProcesses()
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT ProcessId, ParentProcessId, Name, CommandLine FROM Win32_Process WHERE Name='ffmpeg.exe' OR Name='mediamtx.exe'");

        foreach (ManagementObject item in searcher.Get())
        {
            var commandLine = item["CommandLine"] as string ?? string.Empty;
            var isOurs = commandLine.Contains("mediamtx.runtime.yml", StringComparison.OrdinalIgnoreCase) ||
                         commandLine.Contains("127.0.0.1:8554/display", StringComparison.OrdinalIgnoreCase);
            if (!isOurs) continue;

            var processId = Convert.ToInt32((uint)item["ProcessId"]);
            var parentId = Convert.ToInt32((uint)item["ParentProcessId"]);
            if (IsLiveLANternHost(parentId)) continue;

            try
            {
                using var orphan = Process.GetProcessById(processId);
                orphan.Kill(true);
                orphan.WaitForExit(3000);
            }
            catch (ArgumentException) { }
            catch (InvalidOperationException) { }
        }
    }

    private static bool IsLiveLANternHost(int processId)
    {
        try
        {
            using var parent = Process.GetProcessById(processId);
            return parent.ProcessName.Equals("LANtern.Host", StringComparison.OrdinalIgnoreCase) ||
                   parent.ProcessName.Equals("LANtern", StringComparison.OrdinalIgnoreCase) ||
                   (parent.ProcessName.Equals("dotnet", StringComparison.OrdinalIgnoreCase) &&
                    ReadCommandLine(processId).Contains("LANtern.Host", StringComparison.OrdinalIgnoreCase));
        }
        catch (ArgumentException) { return false; }
        catch (InvalidOperationException) { return false; }
    }

    private static string ReadCommandLine(int processId)
    {
        using var searcher = new ManagementObjectSearcher($"SELECT CommandLine FROM Win32_Process WHERE ProcessId={processId}");
        return searcher.Get().Cast<ManagementObject>().FirstOrDefault()?["CommandLine"] as string ?? string.Empty;
    }

    public void Dispose() => _handle.Dispose();

    [StructLayout(LayoutKind.Sequential)]
    private struct IoCounters
    {
        public ulong ReadOperationCount, WriteOperationCount, OtherOperationCount;
        public ulong ReadTransferCount, WriteTransferCount, OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JobObjectBasicLimitInformation
    {
        public long PerProcessUserTimeLimit, PerJobUserTimeLimit;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSetSize, MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint PriorityClass, SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JobObjectExtendedLimitInformation
    {
        public JobObjectBasicLimitInformation BasicLimitInformation;
        public IoCounters IoInfo;
        public UIntPtr ProcessMemoryLimit, JobMemoryLimit, PeakProcessMemoryUsed, PeakJobMemoryUsed;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateJobObject(IntPtr securityAttributes, string? name);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetInformationJobObject(SafeFileHandle job, int informationClass, IntPtr information, uint length);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AssignProcessToJobObject(SafeFileHandle job, IntPtr process);
}
