using InterfaceWatchDog.Core;

namespace InterfaceWatchDog.UI.Forms;

// 현재 화면에 보이는 프로그램 창 목록에서 사용자가 직접 선택하도록 하는 대화상자.
// (사용자는 "javaw" 같은 실제 프로세스 이름이 아니라, 화면에 보이는 창 제목만 알고 있음)
public class ProcessPickerForm : Form
{
    private readonly ListView _list;
    private readonly TextBox  _search;
    private List<(RunningProgramFinder.WindowInfo w, string procName)> _items    = [];
    private List<(RunningProgramFinder.WindowInfo w, string procName)> _filtered = [];

    public RunningProgramFinder.WindowInfo? Selected { get; private set; }

    public ProcessPickerForm()
    {
        Text            = "실행 중인 프로그램 선택";
        Size            = new Size(860, 580);
        MinimumSize     = new Size(620, 420);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox     = false;
        MinimizeBox     = false;
        Font            = new Font("맑은 고딕", 9.5f);
        BackColor       = Color.FromArgb(245, 246, 250);

        var windows = RunningProgramFinder.GetVisibleWindows();
        _items = windows.Select(w =>
        {
            var procName = "";
            try { procName = System.Diagnostics.Process.GetProcessById(w.Pid).ProcessName; } catch { }
            return (w, procName);
        }).ToList();
        _filtered = [.. _items];

        // ── 안내 문구 ──────────────────────────────────────────────────────
        var hint = new Label
        {
            Dock      = DockStyle.Top,
            Height    = 38,
            Text      = "감시할 프로그램의 창을 선택하세요.",
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(14, 0, 0, 0),
            BackColor = Color.White,
            ForeColor = Color.FromArgb(70, 80, 100)
        };
        var divHint = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(210, 215, 228) };

        // ── 검색 바 ────────────────────────────────────────────────────────
        var searchRow = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 50,
            BackColor = Color.White,
            Padding   = new Padding(14, 10, 14, 10)
        };
        var searchLbl = new Label
        {
            Text      = "검색",
            Dock      = DockStyle.Left,
            Width     = 80,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(70, 80, 100)
        };
        _search = new TextBox { Dock = DockStyle.Fill, Font = new Font("맑은 고딕", 9.5f) };
        _search.TextChanged += (_, _) => ApplyFilter();
        // Controls.Add 역순: 마지막에 추가된 컨트롤이 Fill로 먼저 처리됨
        searchRow.Controls.Add(_search);
        searchRow.Controls.Add(searchLbl);

        var divSearch = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(210, 215, 228) };

        // ── 목록 ───────────────────────────────────────────────────────────
        _list = new ListView
        {
            Dock          = DockStyle.Fill,
            View          = View.Details,
            FullRowSelect = true,
            MultiSelect   = false,
            GridLines     = false,
            BorderStyle   = BorderStyle.None,
            Font          = new Font("맑은 고딕", 9.5f),
            BackColor     = Color.White
        };
        _list.Columns.Add("창 제목",       -2);
        _list.Columns.Add("프로세스 이름", 150);
        _list.DoubleClick += (_, _) => Accept();
        PopulateList();

        // ── 하단 버튼 바 ───────────────────────────────────────────────────
        var btnBar  = new Panel { Dock = DockStyle.Bottom, Height = 58, BackColor = Color.White };
        var divBtn  = new Panel { Dock = DockStyle.Top,    Height = 1,  BackColor = Color.FromArgb(210, 215, 228) };
        var btnFlow = new FlowLayoutPanel
        {
            Dock          = DockStyle.Right,
            AutoSize      = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents  = false,
            Padding       = new Padding(0, 10, 14, 0)
        };

        var btnOk = new Button
        {
            Text      = "선택",
            Size      = new Size(100, 36),
            BackColor = Color.FromArgb(33, 120, 220),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("맑은 고딕", 10f, FontStyle.Bold),
            Cursor    = Cursors.Hand
        };
        btnOk.FlatAppearance.BorderSize = 0;
        btnOk.Click += (_, _) => Accept();

        var btnCancel = new Button
        {
            Text             = "취소",
            Size             = new Size(90, 36),
            BackColor        = Color.FromArgb(228, 231, 238),
            ForeColor        = Color.FromArgb(50, 55, 70),
            FlatStyle        = FlatStyle.Flat,
            Cursor           = Cursors.Hand,
            CausesValidation = false
        };
        btnCancel.FlatAppearance.BorderColor = Color.FromArgb(195, 200, 215);
        btnCancel.Click += (_, _) => Close();

        btnFlow.Controls.Add(btnOk);
        btnFlow.Controls.Add(btnCancel);
        btnBar.Controls.AddRange([divBtn, btnFlow]);

        // Controls.Add 순서: 마지막에 추가된 컨트롤이 가장 먼저 Dock 처리됨
        Controls.Add(_list);      // Fill  — 가장 먼저 추가 → 가장 나중에 처리
        Controls.Add(btnBar);     // Bottom
        Controls.Add(divSearch);  // Top #4
        Controls.Add(searchRow);  // Top #3
        Controls.Add(divHint);    // Top #2
        Controls.Add(hint);       // Top #1 — 가장 나중에 추가 → 가장 먼저 처리

        Load   += (_, _) => ResizeTitleColumn();
        Resize += (_, _) => ResizeTitleColumn();
    }

    private void ResizeTitleColumn()
    {
        if (_list.Columns.Count < 2) return;
        var remaining = _list.ClientSize.Width - _list.Columns[1].Width
                        - SystemInformation.VerticalScrollBarWidth - 2;
        if (remaining > 0)
            _list.Columns[0].Width = remaining;
    }

    private void PopulateList()
    {
        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (var (w, procName) in _filtered)
        {
            var item = new ListViewItem(w.Title) { Tag = w };
            item.SubItems.Add(procName);
            _list.Items.Add(item);
        }
        _list.EndUpdate();
    }

    private void ApplyFilter()
    {
        var q = _search.Text.Trim();
        _filtered = string.IsNullOrEmpty(q)
            ? [.. _items]
            : [.. _items.Where(x => x.w.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
                                 || x.procName.Contains(q, StringComparison.OrdinalIgnoreCase))];
        PopulateList();
    }

    private void Accept()
    {
        if (_list.SelectedItems.Count == 0)
        {
            MessageBox.Show("프로그램을 선택하세요.", "InterfaceWatchDog",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        Selected     = (RunningProgramFinder.WindowInfo)_list.SelectedItems[0].Tag!;
        DialogResult = DialogResult.OK;
        Close();
    }
}
