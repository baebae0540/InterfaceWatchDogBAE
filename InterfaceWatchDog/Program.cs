using System.ServiceProcess;
using InterfaceWatchDog.Core;
using InterfaceWatchDog.Core.Actions;
using InterfaceWatchDog.Service;
using InterfaceWatchDog.UI;
using InterfaceWatchDog.UI.Forms;

namespace InterfaceWatchDog;

static class Program
{
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

        // ── 트레이 앱 모드 ────────────────────────────────────────────────────
        ApplicationConfiguration.Initialize();
        Application.SetCompatibleTextRenderingDefault(false);

        var config = ConfigManager.Load();
        var log = new LogWriter();
        var engine = new WatchDogEngine(config, log);

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
}
