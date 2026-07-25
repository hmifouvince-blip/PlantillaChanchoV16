using Guna.UI2.WinForms;
using PlantillaChanchoV16.Products;
using PlantillaChanchoV16.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlantillaChanchoV16.Template
{
    // Menu "Windows PaiPai" : outils d'optimisation, rangés par catégorie, avec icônes et
    // cases à cocher. On coche plusieurs outils puis "Apply selected". Chaque commande
    // vient de Products/WindowsPaiTweaks.cs.
    internal class WindowsPaiScreen : Form
    {
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        private static readonly string[] Categories = { "All", "Cleaning", "Performance", "Privacy", "Network" };

        private Panel _scroll;
        private Label _status;
        private Guna2Button _applyBtn;
        private Guna2Button _vpnBtn;
        private Label _vpnStatus;
        private bool _vpnBusy;
        private string _filter = "All";
        private readonly List<Guna2Button> _catButtons = new List<Guna2Button>();
        private readonly List<(WindowsPaiTweaks.Tweak tweak, Guna2CustomCheckBox check)> _checks
            = new List<(WindowsPaiTweaks.Tweak, Guna2CustomCheckBox)>();

        private const int Pad = 34;

        public WindowsPaiScreen()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(840, 640);
            BackColor = Colors.bgColor;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            using (var p = Rounded(new Rectangle(0, 0, Width, Height), 14)) Region = new Region(p);

            // Header.
            MakeLbl("Pai", Color.White, new Font("Inter Semibold", 12f), new Point(Pad, 22), true);
            float pW = TextRenderer.MeasureText("Pai", new Font("Inter Semibold", 12f)).Width;
            MakeLbl("Pai", Colors.mainColor, new Font("Inter Semibold", 12f), new Point(Pad + (int)pW - 6, 22), true);
            MakeLbl("Windows PaiPai", Colors.mainColor, new Font("Inter Semibold", 18f), new Point(Pad, 48), true);
            MakeLbl("Check the tools you want, then Apply.", Color.FromArgb(170, 255, 255, 255),
                new Font("Inter Medium", 10f), new Point(Pad, 80), true);

            var close = new WindowButton(WindowButton.Glyph.Close, Color.FromArgb(255, 95, 87))
            { Parent = this, Location = new Point(Width - 42, 18) };
            close.Clicked += (s, e) => Close();

            BuildVpnBanner();

            // Barre de catégories.
            int cx = Pad, cy = 184;
            foreach (var cat in Categories)
            {
                var b = new Guna2Button
                {
                    Parent = this, Text = cat, Font = new Font("Inter Semibold", 9.5f),
                    ForeColor = cat == _filter ? Color.White : Color.FromArgb(150, 152, 168),
                    FillColor = cat == _filter ? Colors.mainColor : Colors.scColor,
                    BorderRadius = 9, BorderThickness = 0, Size = new Size(104, 34),
                    Location = new Point(cx, cy), Cursor = Cursors.Hand, Animated = true,
                    UseTransparentBackground = true, Tag = cat
                };
                b.HoverState.FillColor = cat == _filter ? Colors.mainColor : ControlPaint.Light(Colors.scColor, 0.3f);
                b.Click += (s, e) => { _filter = (string)((Guna2Button)s).Tag; RefreshCatButtons(); BuildCards(); };
                _catButtons.Add(b);
                cx += 110;
            }

            // Zone défilable.
            _scroll = new Panel
            {
                Parent = this,
                Location = new Point(Pad, 230),
                Size = new Size(Width - Pad * 2, Height - 230 - 84),
                BackColor = Colors.bgColor,
                AutoScroll = true
            };

            // Bas : Apply + statut.
            _applyBtn = new Guna2Button
            {
                Parent = this, Text = "Apply selected", Font = new Font("Inter Semibold", 10.5f),
                ForeColor = Color.White, FillColor = Colors.mainColor, BorderRadius = 10, BorderThickness = 0,
                Size = new Size(170, 42), Cursor = Cursors.Hand, Animated = true, UseTransparentBackground = true,
                Location = new Point(Width - Pad - 170, Height - 62)
            };
            _applyBtn.HoverState.FillColor = ControlPaint.Light(Colors.mainColor, 0.2f);
            _applyBtn.Click += async (s, e) => await ApplySelected();

            _status = new Label
            {
                Parent = this, Text = "Ready.", ForeColor = Color.FromArgb(190, 255, 255, 255),
                BackColor = Color.Transparent, Font = new Font("Inter Medium", 9.5f),
                AutoSize = false, AutoEllipsis = true,
                Size = new Size(Width - Pad * 2 - 190, 40), Location = new Point(Pad, Height - 60)
            };

            BuildCards();
            this.MouseDown += Drag;
        }

        private void BuildVpnBanner()
        {
            var banner = new Guna2Panel
            {
                Parent = this, Location = new Point(Pad, 108), Size = new Size(Width - Pad * 2, 62),
                FillColor = Colors.scColor, BorderRadius = 12, BorderThickness = 0, UseTransparentBackground = true,
                CustomBorderThickness = new Padding(3, 0, 0, 0), CustomBorderColor = Colors.mainColor
            };
            new CategoryIcon("Network") { Parent = banner, Location = new Point(14, (62 - 44) / 2), Size = new Size(44, 44) };
            new Guna2HtmlLabel
            {
                Parent = banner, Text = "Cloudflare WARP — VPN", ForeColor = Color.White,
                Font = new Font("Inter Semibold", 11.5f), AutoSize = true, BackColor = Color.Transparent,
                IsSelectionEnabled = false, Location = new Point(72, 12)
            };
            _vpnStatus = new Label
            {
                Parent = banner, Text = "Checking…", ForeColor = Color.FromArgb(170, 255, 255, 255),
                BackColor = Color.Transparent, Font = new Font("Inter Medium", 9.5f), AutoSize = true,
                Location = new Point(72, 36)
            };

            _vpnBtn = new Guna2Button
            {
                Parent = banner, Text = "Connect", Font = new Font("Inter Semibold", 10f), ForeColor = Color.White,
                FillColor = Colors.mainColor, BorderRadius = 9, BorderThickness = 0, Size = new Size(130, 40),
                Cursor = Cursors.Hand, Animated = true, UseTransparentBackground = true,
                Location = new Point(banner.Width - 130 - 16, (62 - 40) / 2)
            };
            _vpnBtn.HoverState.FillColor = ControlPaint.Light(Colors.mainColor, 0.2f);
            _vpnBtn.Click += async (s, e) => await ToggleVpn();

            RefreshVpnStatusAsync();
        }

        private async void RefreshVpnStatusAsync()
        {
            bool connected = await Task.Run(() => WarpVpn.IsInstalled() && WarpVpn.IsConnected());
            SetVpnUi(connected);
        }

        private void SetVpnUi(bool connected)
        {
            if (connected)
            {
                _vpnStatus.ForeColor = Colors.mainColor;
                _vpnStatus.Text = "PaiPai is connected to VPN";
                _vpnBtn.Text = "Disconnect";
                _vpnBtn.FillColor = Colors.scColor;
                _vpnBtn.HoverState.FillColor = ControlPaint.Light(Colors.scColor, 0.3f);
            }
            else
            {
                _vpnStatus.ForeColor = Color.FromArgb(170, 255, 255, 255);
                _vpnStatus.Text = "Not connected";
                _vpnBtn.Text = "Connect";
                _vpnBtn.FillColor = Colors.mainColor;
                _vpnBtn.HoverState.FillColor = ControlPaint.Light(Colors.mainColor, 0.2f);
            }
        }

        private async Task ToggleVpn()
        {
            if (_vpnBusy) return;
            _vpnBusy = true;
            _vpnBtn.Enabled = false;

            bool connected = await Task.Run(() => WarpVpn.IsInstalled() && WarpVpn.IsConnected());

            if (connected)
            {
                _vpnStatus.ForeColor = Color.FromArgb(170, 255, 255, 255);
                _vpnStatus.Text = "Disconnecting…";
                await Task.Run(() => WarpVpn.Disconnect());
            }
            else
            {
                if (!await Task.Run(() => WarpVpn.IsInstalled()))
                {
                    _vpnStatus.Text = "Installing Cloudflare WARP… (first time only)";
                    bool ok = await Task.Run(() => WarpVpn.Install());
                    if (!ok)
                    {
                        SakuraMessageBox.Show(
                            "Could not install Cloudflare WARP automatically.\nInstall it from " + WarpVpn.OfficialUrl,
                            "VPN", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(WarpVpn.OfficialUrl) { UseShellExecute = true }); } catch { }
                        SetVpnUi(false);
                        _vpnBusy = false; _vpnBtn.Enabled = true;
                        return;
                    }
                }
                _vpnStatus.Text = "Connecting…";
                await Task.Run(() => WarpVpn.Connect());
            }

            bool nowConnected = await Task.Run(() => WarpVpn.IsInstalled() && WarpVpn.IsConnected());
            SetVpnUi(nowConnected);
            _vpnBusy = false;
            _vpnBtn.Enabled = true;
        }

        private void RefreshCatButtons()
        {
            foreach (var b in _catButtons)
            {
                bool sel = (string)b.Tag == _filter;
                b.FillColor = sel ? Colors.mainColor : Colors.scColor;
                b.ForeColor = sel ? Color.White : Color.FromArgb(150, 152, 168);
                b.HoverState.FillColor = sel ? Colors.mainColor : ControlPaint.Light(Colors.scColor, 0.3f);
            }
        }

        private void BuildCards()
        {
            _scroll.Controls.Clear();
            _checks.Clear();

            int y = 0, cardW = _scroll.Width - 22, cardH = 78;
            foreach (var tweak in WindowsPaiTweaks.Tweaks)
            {
                string cat = CategoryOf(tweak);
                if (_filter != "All" && cat != _filter) continue;

                var card = new Guna2Panel
                {
                    Parent = _scroll, Location = new Point(0, y), Size = new Size(cardW, cardH),
                    FillColor = Colors.scColor, BorderRadius = 10, BorderThickness = 0,
                    UseTransparentBackground = true,
                    CustomBorderThickness = new Padding(3, 0, 0, 0), CustomBorderColor = Colors.mainColor
                };

                // Icône de catégorie (dessinée).
                var icon = new CategoryIcon(cat) { Parent = card, Location = new Point(14, (cardH - 44) / 2), Size = new Size(44, 44) };

                new Guna2HtmlLabel
                {
                    Parent = card, Text = tweak.Name, ForeColor = Color.White,
                    Font = new Font("Inter Semibold", 11f), AutoSize = true,
                    BackColor = Color.Transparent, IsSelectionEnabled = false, Location = new Point(72, 15)
                };
                new Guna2HtmlLabel
                {
                    Parent = card, Text = tweak.Description, ForeColor = Color.FromArgb(165, 255, 255, 255),
                    Font = new Font("Inter Medium", 9f), AutoSize = true,
                    BackColor = Color.Transparent, IsSelectionEnabled = false, Location = new Point(72, 42)
                };

                var chk = new Guna2CustomCheckBox
                {
                    Parent = card, Size = new Size(22, 22),
                    Location = new Point(cardW - 40, (cardH - 22) / 2), Animated = true
                };
                chk.CheckedState.FillColor = Colors.mainColor;
                chk.CheckedState.BorderColor = Colors.mainColor;
                chk.UncheckedState.FillColor = Colors.bgColor;
                chk.UncheckedState.BorderColor = Color.FromArgb(90, 92, 108);

                _checks.Add((tweak, chk));
                y += cardH + 10;
            }

            if (_checks.Count == 0)
            {
                _scroll.Controls.Add(new Label
                {
                    Text = "No tools in this category.", ForeColor = Color.FromArgb(150, 152, 168),
                    BackColor = Color.Transparent, AutoSize = true, Location = new Point(4, 8),
                    Font = new Font("Inter Medium", 10f)
                });
            }
        }

        private async Task ApplySelected()
        {
            var selected = _checks.FindAll(c => c.check.Checked);
            if (selected.Count == 0)
            {
                _status.ForeColor = Color.FromArgb(254, 188, 46);
                _status.Text = "Check at least one tool first.";
                return;
            }

            // Confirmation si un outil sensible est coché.
            foreach (var (tweak, _) in selected)
                if (tweak.NeedsConfirm)
                {
                    var r = SakuraMessageBox.Show("Some selected tools change system settings. Continue?",
                        "Windows PaiPai", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (r != DialogResult.Yes) return;
                    break;
                }

            _applyBtn.Enabled = false;
            int done = 0;
            foreach (var (tweak, _) in selected)
            {
                done++;
                _status.ForeColor = Color.FromArgb(190, 255, 255, 255);
                _status.Text = $"Applying ({done}/{selected.Count}): {tweak.Name}…";
                await Task.Run(() => WindowsPaiTweaks.Run(tweak.Command));
            }
            _applyBtn.Enabled = true;
            _status.ForeColor = Colors.mainColor;
            _status.Text = $"Done — {selected.Count} tool(s) applied. 🌸".Replace("🌸", "");
        }

        private static string CategoryOf(WindowsPaiTweaks.Tweak t)
        {
            string s = ((t.Name ?? "") + " " + (t.Description ?? "")).ToLowerInvariant();
            if (s.Contains("dns") || s.Contains("tcp") || s.Contains("reseau") || s.Contains("network") || s.Contains("vpn"))
                return "Network";
            if (s.Contains("telemetr") || s.Contains("diagtrack") || s.Contains("confidential") || s.Contains("privacy"))
                return "Privacy";
            if (s.Contains("temp") || s.Contains("cache") || s.Contains("corbeille") || s.Contains("recycle")
                || s.Contains("clean") || s.Contains("hibernation") || s.Contains("store"))
                return "Cleaning";
            return "Performance";
        }

        // ---- Icône de catégorie dessinée (pas de fichier image requis) ----
        private class CategoryIcon : Control
        {
            private readonly string _cat;
            public CategoryIcon(string cat)
            {
                _cat = cat;
                SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint
                         | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
                DoubleBuffered = true;
                BackColor = Color.Transparent;
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var rc = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var bg = new SolidBrush(Color.FromArgb(45, Colors.mainColor)))
                    g.FillEllipse(bg, rc);
                float cx = Width / 2f, cy = Height / 2f;
                using (var pen = new Pen(Colors.mainColor, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                using (var br = new SolidBrush(Colors.mainColor))
                {
                    switch (_cat)
                    {
                        case "Cleaning": // corbeille
                            g.DrawLine(pen, cx - 8, cy - 6, cx + 8, cy - 6);
                            g.DrawRectangle(pen, cx - 6, cy - 6, 12, 13);
                            g.DrawLine(pen, cx - 3, cy - 6, cx - 3, cy + 5);
                            g.DrawLine(pen, cx + 3, cy - 6, cx + 3, cy + 5);
                            break;
                        case "Privacy": // bouclier
                            using (var path = new GraphicsPath())
                            {
                                path.AddLines(new[] { new PointF(cx, cy - 9), new PointF(cx + 8, cy - 5),
                                    new PointF(cx + 8, cy + 2), new PointF(cx, cy + 9), new PointF(cx - 8, cy + 2),
                                    new PointF(cx - 8, cy - 5) });
                                path.CloseFigure();
                                g.DrawPath(pen, path);
                            }
                            break;
                        case "Network": // globe
                            g.DrawEllipse(pen, cx - 8, cy - 8, 16, 16);
                            g.DrawEllipse(pen, cx - 3, cy - 8, 6, 16);
                            g.DrawLine(pen, cx - 8, cy, cx + 8, cy);
                            break;
                        default: // Performance : éclair
                            using (var path = new GraphicsPath())
                            {
                                path.AddLines(new[] { new PointF(cx + 2, cy - 9), new PointF(cx - 6, cy + 1),
                                    new PointF(cx, cy + 1), new PointF(cx - 2, cy + 9), new PointF(cx + 6, cy - 1),
                                    new PointF(cx, cy - 1) });
                                path.CloseFigure();
                                g.FillPath(br, path);
                            }
                            break;
                    }
                }
            }
        }

        private Label MakeLbl(string text, Color color, Font font, Point loc, bool drag)
        {
            var l = new Label { Parent = this, Text = text, ForeColor = color, BackColor = Color.Transparent, Font = font, AutoSize = true, Location = loc };
            if (drag) l.MouseDown += Drag;
            return l;
        }

        private void Drag(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(this.Handle, 0xA1, 0x2, 0); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, Width, Height);
            using (var bg = new LinearGradientBrush(rect, Colors.bgColor, Color.FromArgb(46, 24, 40), LinearGradientMode.ForwardDiagonal))
                g.FillRectangle(bg, rect);
            using (var glow = new GraphicsPath())
            {
                glow.AddEllipse(Width - 240, -160, 340, 320);
                using (var pgb = new PathGradientBrush(glow))
                {
                    pgb.CenterColor = Color.FromArgb(45, Colors.mainColor);
                    pgb.SurroundColors = new[] { Color.FromArgb(0, Colors.mainColor) };
                    g.FillPath(pgb, glow);
                }
            }
            using (var path = Rounded(new Rectangle(0, 0, Width - 1, Height - 1), 14))
            using (var pen = new Pen(Color.FromArgb(90, Colors.mainColor), 1f))
                g.DrawPath(pen, path);
        }

        private static GraphicsPath Rounded(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
