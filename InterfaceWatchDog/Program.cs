using System.ServiceProcess;
using System.Threading;
using InterfaceWatchDog.Core;
using InterfaceWatchDog.Core.Actions;
using InterfaceWatchDog.Service;
using InterfaceWatchDog.UI;
using InterfaceWatchDog.UI.Forms;

namespace InterfaceWatchDog;

static class Program
{
    private static Mutex? _singleInstanceMutex;   // 앱 수명 동안 GC 방지

    [STAThread]
    static void Main(string[] args)
    {
        // ── 서비스 관리 명령행 인수 처리 ─────────────────────────────────────
        if (args.Contains("--install", StringComparer.OrdinalIgnoreCase))
        {
            WatchDogWindowsService.Install();
            return;
        }
        if (args.Contains("--uninstall", StringComparer.OrdinalIgnoreCase))
        {
            WatchDogWindowsService.Uninstall();
            return;
        }

        // ── Windows Service 모드 ──────────────────────────────────────────────
        if (!Environment.UserInteractive)
        {
            ServiceBase.Run(new WatchDogWindowsService());
            return;
        }

        // ── 단일 인스턴스 보장 (서버 전체 1개) ────────────────────────────────
        if (!TryAcquireSingleInstance())
        {
            MessageBox.Show("InterfaceWatchDog가 이미 실행 중입니다.",
                "InterfaceWatchDog", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // ── 트레이 앱 모드 ────────────────────────────────────────────────────
        ApplicationConfiguration.Initialize();
        Application.SetCompatibleTextRenderingDefault(false);

        var config = ConfigManager.Load();
        var log = new LogWriter();

        var engine = new WatchDogEngine(config, log, isInteractiveSession: true);

        // 최초 실행: 설정 화면 자동 표시
        if (ConfigManager.IsFirstRun())
        {
            using var setupForm = new SettingsForm(config);
            setupForm.Text = "InterfaceWatchDog - 초기 설정 (필수)";
            var result = setupForm.ShowDialog();

            if (result != DialogResult.OK)
            {
                MessageBox.Show("설정이 완료되지 않아 종료합니다.",
                    "InterfaceWatchDog", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            config = ConfigManager.Load();
        }

        engine.Start();

        Application.Run(new TrayApplicationContext(engine, log, config));
    }

    // Global 뮤텍스로 서버 전체 단일 인스턴스 보장.
    // 신규 생성 성공 = 첫 실행(true). 이미 존재하거나(다른 세션 보유로) 접근 거부면 = 이미 실행 중(false).
    static bool TryAcquireSingleInstance()
    {
        const string name = @"Global\InterfaceWatchDog.Tray.SingleInstance";
        try
        {
            _singleInstanceMutex = new Mutex(initiallyOwned: true, name, out bool createdNew);
            return createdNew;
        }
        catch (UnauthorizedAccessException)
        {
            // 다른 사용자 세션이 이미 뮤텍스를 보유 → 접근 거부를 '이미 실행 중'으로 간주
            return false;
        }
    }
}
