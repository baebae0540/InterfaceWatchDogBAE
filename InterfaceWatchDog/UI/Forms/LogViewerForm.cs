using InterfaceWatchDog.Core.Actions;
using InterfaceWatchDog.Core.Models;

namespace InterfaceWatchDog.UI.Forms;

public class LogViewerForm : Form
{
    private readonly LogWriter _log;
    private ComboBox  _dateCombo = null!;
    private ListView  _logList   = null!;

    public LogViewerForm(LogWriter log)
    {
        _log = log;
        AutoScaleMode = AutoScaleMode.Dpi;
        InitializeComponent();
        LoadDates();
    }

    private void InitializeComponent()
    {
        Text          = "InterfaceWatchDog — 로그 뷰어";
        Size          = new Size(1000, 652);
        MinimumSize   = new Size(800, 492);
        StartPosition = FormStartPosition.CenterScreen;
        Font          = new Font("맑은 고딕", 9.5f);
        BackColor     = Color.FromArgb(245, 246, 250);

        // ── 툴바 (FlowLayoutPanel으로 절대좌표 제거) ─────────────────────────
        var toolbar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 64,                                // 높이 확대 (날짜/버튼 잘림 방지)
            BackColor = Color.White,
            Padding   = new Padding(14, 12, 14, 12)
        };
        var toolDivider = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 1,
            BackColor = Color.FromArgb(215, 218, 228)
        };

        var flow = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents  = false,
            AutoSize      = false
        };

        var dateLbl = new Label
        {
            Text      = "날짜:",
            AutoSize  = true,
            Font      = new Font("맑은 고딕", 9.5f),
            ForeColor = Color.FromArgb(50, 56, 76),
            Margin    = new Padding(0, 9, 8, 0)
        };

        _dateCombo = new ComboBox
        {
            Width         = 170,
            Height        = 32,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font          = new Font("맑은 고딕", 9.5f),
            Margin        = new Padding(0, 4, 12, 0)
        };
        _dateCombo.SelectedIndexChanged += (_, _) => LoadLogs();

        var btnRefresh = ToolBtn("새로고침");
        btnRefresh.Click += (_, _) => { LoadDates(); LoadLogs(); };

        var btnFolder = ToolBtn("폴더 열기");
        btnFolder.Click += (_, _) =>
        {
            if (Directory.Exists(LogWriter.LogDirectoryPath))
                System.Diagnostics.Process.Start("explorer.exe", LogWriter.LogDirectoryPath);
        };

        flow.Controls.AddRange([dateLbl, _dateCombo, btnRefresh, btnFolder]);
        toolbar.Controls.AddRange([toolDivider, flow]);

        // ── 로그 목록 ─────────────────────────────────────────────────────────
        _logList = new ListView
        {
            Dock        = DockStyle.Fill,
            View        = View.Details,
            FullRowSelect  = true,
            GridLines      = false,
            HeaderStyle    = ColumnHeaderStyle.Nonclickable,
            Font        = new Font("Consolas", 9f),
            BackColor   = Color.FromArgb(22, 24, 30),
            ForeColor   = Color.FromArgb(195, 200, 215),
            BorderStyle = BorderStyle.None
        };
        _logList.Columns.Add("시간",   140);
        _logList.Columns.Add("레벨",    62);
        _logList.Columns.Add("소스",   170);
        _logList.Columns.Add("메시지", 580);

        Controls.Add(_logList);
        Controls.Add(toolbar);
    }

    private static Button ToolBtn(string text) => new()
    {
        Text      = text,
        Size      = new Size(100, 32),
        FlatStyle = FlatStyle.Flat,
        Font      = new Font("맑은 고딕", 9f),
        BackColor = Color.FromArgb(232, 235, 242),
        ForeColor = Color.FromArgb(46, 52, 72),
        Cursor    = Cursors.Hand,
        Margin    = new Padding(8, 4, 0, 0)
    };

    private void LoadDates()
    {
        var selected = _dateCombo.SelectedItem?.ToString();
        _dateCombo.Items.Clear();

        foreach (var d in _log.GetAvailableLogDates())
            _dateCombo.Items.Add(d);

        if (_dateCombo.Items.Count > 0)
        {
            var idx = selected != null ? _dateCombo.Items.IndexOf(selected) : 0;
            _dateCombo.SelectedIndex = idx >= 0 ? idx : 0;
        }
    }

    private void LoadLogs()
    {
        _logList.Items.Clear();

        var date = _dateCombo.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(date)) return;

        foreach (var entry in _log.ReadLogsByDate(date))
        {
            var item = new ListViewItem(entry.Timestamp.ToString("HH:mm:ss.fff"));
            item.SubItems.Add(entry.LevelText);
            item.SubItems.Add(entry.Source);
            item.SubItems.Add(entry.Message);
            item.ForeColor = entry.LevelColor;
            _logList.Items.Add(item);
        }

        if (_logList.Items.Count > 0)
            _logList.EnsureVisible(_logList.Items.Count - 1);
    }
}
