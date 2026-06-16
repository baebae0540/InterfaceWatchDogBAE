using System.Management;
using System.Runtime.InteropServices;
using System.Text;

namespace InterfaceWatchDog.Core;

// 사용자는 "javaw"가 아니라 화면에 보이는 프로그램 창(예: "Export Manager 1.03.0_64-bit")만
// 알고 있는 경우가 많다. 현재 실행 중인 창 목록을 보여주고, 사용자가 그중 하나를 선택하면
// 감시 설정에 필요한 프로세스 이름 / 실행 파일 경로 / 실행 인수를 자동으로 채워준다.
public static class RunningProgramFinder
{
    public record WindowInfo(string Title, int Pid);

    public record LaunchInfo(string ProcessName, string ExecutablePath, string Arguments, List<int> ListeningPorts);

    // 현재 화면에 보이는 프로그램 창 목록 (자기 자신은 제외)
    public static List<WindowInfo> GetVisibleWindows()
    {
        var result = new List<WindowInfo>();
        var selfPid = Environment.ProcessId;

        NativeMethods.EnumWindows((hWnd, _) =>
        {
            if (!NativeMethods.IsWindowVisible(hWnd)) return true;

            var length = NativeMethods.GetWindowTextLength(hWnd);
            if (length == 0) return true;

            var sb = new StringBuilder(length + 1);
            NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
            var title = sb.ToString();
            if (string.IsNullOrWhiteSpace(title)) return true;

            NativeMethods.GetWindowThreadProcessId(hWnd, out var pid);
            if (pid == selfPid) return true;

            result.Add(new WindowInfo(title, (int)pid));
            return true;
        }, IntPtr.Zero);

        return result;
    }

    // WMI로 PID의 실행 파일 경로/명령행을 조회해 설정값으로 변환한다.
    public static LaunchInfo? GetLaunchInfo(int pid)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                $"SELECT Name, ExecutablePath, CommandLine FROM Win32_Process WHERE ProcessId = {pid}");

            foreach (var obj in searcher.Get().Cast<ManagementObject>())
            {
                var name        = obj["Name"] as string ?? "";
                var exePath     = obj["ExecutablePath"] as string ?? "";
                var commandLine = obj["CommandLine"] as string ?? "";

                return new LaunchInfo(Path.GetFileNameWithoutExtension(name), exePath, ExtractArguments(commandLine), GetListeningPorts(pid));
            }
        }
        catch
        {
            // WMI 조회 실패 — null 반환
        }

        return null;
    }

    // 지정 PID가 TCP LISTEN 상태로 점유 중인 포트 목록을 반환한다 (IPv4).
    public static List<int> GetListeningPorts(int pid)
    {
        var ports = new List<int>();

        var size = 0;
        NativeMethods.GetExtendedTcpTable(IntPtr.Zero, ref size, true, NativeMethods.AF_INET, NativeMethods.TCP_TABLE_OWNER_PID_LISTENER, 0);
        if (size == 0) return ports;

        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            var result = NativeMethods.GetExtendedTcpTable(buffer, ref size, true, NativeMethods.AF_INET, NativeMethods.TCP_TABLE_OWNER_PID_LISTENER, 0);
            if (result != 0) return ports;

            var numEntries = Marshal.ReadInt32(buffer);
            var rowPtr  = IntPtr.Add(buffer, sizeof(int));
            var rowSize = Marshal.SizeOf<NativeMethods.MIB_TCPROW_OWNER_PID>();

            for (var i = 0; i < numEntries; i++)
            {
                var row = Marshal.PtrToStructure<NativeMethods.MIB_TCPROW_OWNER_PID>(IntPtr.Add(rowPtr, i * rowSize));
                if (row.owningPid != (uint)pid) continue;

                // dwLocalPort는 네트워크 바이트 순서(빅엔디안)로 저장되어 있음 — 하위 2바이트를 뒤집어 변환
                var port = ((row.localPort & 0xFF) << 8) | ((row.localPort >> 8) & 0xFF);
                ports.Add((int)port);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

        return ports.Distinct().OrderBy(p => p).ToList();
    }

    // 명령행 문자열에서 실행 파일 부분을 제외한 인수만 추출한다.
    // 예) "C:\...\javaw.exe" -jar "C:\...\Export Manager.exe"  ->  -jar "C:\...\Export Manager.exe"
    private static string ExtractArguments(string commandLine)
    {
        var trimmed = commandLine.TrimStart();

        if (trimmed.StartsWith('"'))
        {
            var endQuote = trimmed.IndexOf('"', 1);
            if (endQuote > 0) return trimmed[(endQuote + 1)..].TrimStart();
        }
        else
        {
            var space = trimmed.IndexOf(' ');
            if (space > 0) return trimmed[(space + 1)..].TrimStart();
        }

        return "";
    }

    private static class NativeMethods
    {
        public const int AF_INET = 2;
        public const int TCP_TABLE_OWNER_PID_LISTENER = 3;

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int pdwSize, bool bOrder, int ulAf, int tableClass, int reserved);

        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_TCPROW_OWNER_PID
        {
            public uint state;
            public uint localAddr;
            public uint localPort;
            public uint remoteAddr;
            public uint remotePort;
            public uint owningPid;
        }
    }
}
