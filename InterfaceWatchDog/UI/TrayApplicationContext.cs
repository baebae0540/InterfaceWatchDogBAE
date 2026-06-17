using InterfaceWatchDog.Core;
using InterfaceWatchDog.Core.Actions;
using InterfaceWatchDog.Core.Models;
using InterfaceWatchDog.UI.Forms;

namespace InterfaceWatchDog.UI;

public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly WatchDogEngine _engine;
    private readonly LogWriter _log;
    private readonly AppConfig _config;
    private MainStatusForm? _statusForm;

    private HealthStatus _worstStatus = HealthStatus.Unknown;
    private readonly Dictionary<string, HealthStatus> _prevStatus = new();
    private bool _disposed;

    public TrayApplicationContext(WatchDogEngine engine, LogWriter log, AppConfig config)
    {
        _engine = engine;
        _log = log;
        _config = config;

        _trayIcon = new NotifyIcon
        {
            Icon = CreateTrayIcon(HealthStatus.Unknown),
            Text = "InterfaceWatchDog - 감시 중",
            Visible = true,
            ContextMenuStrip = BuildContextMenu()
        };

        _trayIcon.DoubleClick += (_, _) => ShowStatusForm();

        _engine.ProgramStatusChanged += OnProgramStatusChanged;

        _log.Info("Tray", "트레이 앱 시작");
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();

        var itemTitle = new ToolStripMenuItem("InterfaceWatchDog") { Enabled = false };
        itemTitle.Font = new Font(itemTitle.Font, FontStyle.Bold);

        var itemStatus = new ToolStripMenuItem("상태 대시보드");
        itemStatus.Click += (_, _) => ShowStatusForm();

        var itemLog = new ToolStripMenuItem("로그 보기");
        itemLog.Click += (_, _) => new LogViewerForm(_log).Show();

        var itemSettings = new ToolStripMenuItem("설정");
        itemSettings.Click += (_, _) => OpenSettings();

        var itemSvcInstall = new ToolStripMenuItem("Windows 서비스 등록");
        itemSvcInstall.Click += (_, _) => InstallService();

        var itemSvcUninstall = new ToolStripMenuItem("Windows 서비스 해제");
        itemSvcUninstall.Click += (_, _) => UninstallService();

        var itemSep1 = new ToolStripSeparator();
        var itemSep2 = new ToolStripSeparator();

        var itemExit = new ToolStripMenuItem("종료");
        itemExit.Click += (_, _) => ExitApp();

        menu.Items.AddRange([itemTitle, itemSep1, itemStatus, itemLog, itemSettings,
                             itemSep2, itemSvcInstall, itemSvcUninstall, new ToolStripSeparator(), itemExit]);
        return menu;
    }

    private void OnProgramStatusChanged(ProgramStatus status)
    {
        var (erweka, tab, _) = _engine.GetCurrentStatus();
        _worstStatus = Severity(erweka.Status) >= Severity(tab.Status) ? erweka.Status : tab.Status;

        if (_disposed) return;

        _trayIcon.Icon?.Dispose();
        _trayIcon.Icon = CreateTrayIcon(_worstStatus);
        _trayIcon.Text = $"InterfaceWatchDog - {WorstStatusText()}";

        _prevStatus.TryGetValue(status.Key, out var prev);
        _prevStatus[status.Key] = status.Status;
        if (status.Status == prev) return;

        if (status.Status == HealthStatus.Failed)
        {
            var msg = status.Key == "Erweka"
                ? $"{status.DisplayName} 프로세스 중지됨 — 수동 확인 필요"
                : $"{status.DisplayName} 복구 실패 — 수동 확인 필요";
            _trayIcon.ShowBalloonTip(5000, "InterfaceWatchDog 경고", msg, ToolTipIcon.Error);
        }
        else if (status.Status is HealthStatus.Warning or HealthStatus.Restarting)
        {
            _trayIcon.ShowBalloonTip(3000, "InterfaceWatchDog",
                $"{status.DisplayName} 이상 감지", ToolTipIcon.Warning);
        }
    }

    private string WorstStatusText() => _worstStatus switch
    {
        HealthStatus.Healthy => "정상",
        HealthStatus.Warning => "경고",
        HealthStatus.Restarting => "재시작 중",
        HealthStatus.Failed => "장애 발생",
        HealthStatus.Disabled => "감시 안함",
        _ => "감시 중"
    };

    // 상태 비교 우선순위 — Disabled(감시 안 함)는 가장 낮은 우선순위로 취급해
    // 다른 프로그램의 실제 경고/실패 상태를 가리지 않도록 한다
    private static int Severity(HealthStatus status) => status switch
    {
        HealthStatus.Failed => 5,
        HealthStatus.Restarting => 4,
        HealthStatus.Warning => 3,
        HealthStatus.Healthy => 2,
        HealthStatus.Unknown => 1,
        HealthStatus.Disabled => 0,
        _ => 0
    };

    private void ShowStatusForm()
    {
        if (_statusForm == null || _statusForm.IsDisposed)
            _statusForm = new MainStatusForm(_engine, _log, _config);

        _statusForm.Show();
        _statusForm.BringToFront();
        _statusForm.WindowState = FormWindowState.Normal;
    }

    private void OpenSettings()
    {
        var config = ConfigManager.Load();
        using var form = new SettingsForm(config);
        if (form.ShowDialog() == DialogResult.OK)
        {
            _engine.ReloadConfig(ConfigManager.Load());
            _log.Info("Tray", "설정 변경 적용됨");
        }
    }

    private static void InstallService()
    {
        var result = MessageBox.Show(
            "Windows 서비스로 등록하면 로그인 없이 자동 실행됩니다.\n계속하시겠습니까?",
            "서비스 등록", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result != DialogResult.Yes) return;

        // 서비스 등록은 관리자 권한 필요 → UAC 요청하여 재실행
        RunElevated("--install", "서비스 등록이 완료되었습니다.", "서비스 등록 완료");
    }

    private static void UninstallService()
    {
        var result = MessageBox.Show(
            "Windows 서비스를 해제하시겠습니까?",
            "서비스 해제", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result != DialogResult.Yes) return;

        RunElevated("--uninstall", "서비스가 해제되었습니다.", "서비스 해제 완료");
    }

    private static void RunElevated(string args, string successMsg, string successTitle)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = Application.ExecutablePath,
                Arguments = args,
                Verb = "runas",          // UAC 관리자 권한 요청
                UseShellExecute = true
            };
            using var p = System.Diagnostics.Process.Start(psi);
            p?.WaitForExit(10000);
            MessageBox.Show(successMsg, successTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // 사용자가 UAC 취소
        }
        catch (Exception ex)
        {
            MessageBox.Show($"실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExitApp()
    {
        _engine.Stop();
        _engine.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Application.Exit();
    }

    // 트레이 아이콘을 GDI+로 동적 생성
    private static Icon CreateTrayIcon(HealthStatus status)
    {
        var color = status switch
        {
            HealthStatus.Healthy => Color.FromArgb(76, 175, 80),
            HealthStatus.Warning => Color.FromArgb(255, 152, 0),
            HealthStatus.Restarting => Color.FromArgb(33, 150, 243),
            HealthStatus.Failed => Color.FromArgb(244, 67, 54),
            _ => Color.FromArgb(158, 158, 158)
        };

        using var bitmap = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, 1, 1, 14, 14);

        var hIcon = bitmap.GetHicon();
        var icon = (Icon)Icon.FromHandle(hIcon).Clone();
        DestroyIcon(hIcon);
        return icon;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _disposed = true;
            _trayIcon.Dispose();
        }
        base.Dispose(disposing);
    }
}
