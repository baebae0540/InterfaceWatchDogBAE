using InterfaceWatchDog.Core.Actions;
using InterfaceWatchDog.Core.Models;

namespace InterfaceWatchDog.UI.Forms;

public class LogViewerForm : Form
{
    private readonly LogWriter _log;
    private ComboBox _dateCombo = null!;
    private ListView _logList = null!;

    public LogViewerForm(LogWriter log)
    {
        _log = log;
        InitializeComponent();
        LoadDates();
    }

    private void InitializeComponent()
    {
        Text = "InterfaceWatchDog - 로그 뷰어";
        Size = new Size(860, 560);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("맑은 고딕", 9f);

        var toolPanel = new Panel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(8, 6, 8, 0) };

        var dateLbl = new Label { Text = "날짜:", Location = new Point(8, 12), AutoSize = true };
        _dateCombo = new ComboBox
        {
            Location = new Point(44, 8),
            Size = new Size(140, 24),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _dateCombo.SelectedIndexChanged += (_, _) => LoadLogs();

        var btnRefresh = new Button
        {
            Text = "새로고침",
            Location = new Point(196, 7),
            Size = new Size(80, 28),
            FlatStyle = FlatStyle.Flat
        };
        btnRefresh.Click += (_, _) => { LoadDates(); LoadLogs(); };

        var btnOpenFolder = new Button
        {
            Text = "폴더 열기",
            Location = new Point(284, 7),
            Size = new Size(80, 28),
            FlatStyle = FlatStyle.Flat
        };
        btnOpenFolder.Click += (_, _) =>
        {
            if (Directory.Exists(LogWriter.LogDirectoryPath))
                System.Diagnostics.Process.Start("explorer.exe", LogWriter.LogDirectoryPath);
        };

        toolPanel.Controls.AddRange([dateLbl, _dateCombo, btnRefresh, btnOpenFolder]);

        _logList = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = false,
            Font = new Font("Consolas", 8.5f),
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.FromArgb(200, 200, 200),
            BorderStyle = BorderStyle.None
        };
        _logList.Columns.Add("시간", 130);
        _logList.Columns.Add("레벨", 60);
        _logList.Columns.Add("소스", 160);
        _logList.Columns.Add("메시지", 460);

        Controls.Add(_logList);
        Controls.Add(toolPanel);
    }

    private void LoadDates()
    {
        var selected = _dateCombo.SelectedItem?.ToString();
        _dateCombo.Items.Clear();

        var dates = _log.GetAvailableLogDates();
        foreach (var d in dates)
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

        var entries = _log.ReadLogsByDate(date);

        foreach (var entry in entries)
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
