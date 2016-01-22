using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MouseClicker {
    public partial class Form1 : Form {
        const int MOD_ALT = 0x0001;
        const int MOD_CONTROL = 0x0002;
        const int MOD_SHIFT = 0x0004;

        const int WM_HOTKEY = 0x0312;

        const int HOTKEY_ID = 0x0100;

        const int MOUSEEVENTF_LEFTDOWN = 0x2;
        const int MOUSEEVNETF_LEFTUP = 0x4;

        const bool STOP = false;
        const bool RUN = true;

        public bool status = STOP;

        public int num = 0;

        // Task中断用
        public CancellationTokenSource tokenSource = new CancellationTokenSource();

        [DllImport("user32.dll")]
        extern static int RegisterHotKey(IntPtr HWnd, int id, int mod_key, int key);

        [DllImport("user32.dll")]
        extern static int UnregisterHotKey(IntPtr HWnd, int id);

        [DllImport("user32.dll")]
        extern static void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        public Form1() {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e) {
            // ホットキーの登録
            RegisterHotKey(this.Handle, HOTKEY_ID, MOD_ALT | MOD_SHIFT, (int)Keys.A);
        }

        private void Form1_Closed(object sender, System.EventArgs e) {
            // ホットキーの登録解除      
            UnregisterHotKey(this.Handle, HOTKEY_ID);
        }

        protected override void WndProc(ref Message m) {
            base.WndProc(ref m);

            if (m.Msg == WM_HOTKEY && (int)m.WParam == HOTKEY_ID) {
                if (tokenSource == null)
                    tokenSource = new CancellationTokenSource();

                var token = tokenSource.Token;

                // ホットキーが押される度に実行/停止を切替え
                status = !status;

                if (status == RUN) {
                    Task.Factory.StartNew(() => {
                        try {
                            // クリック間隔は秒単位
                            var clickDuration = double.Parse(clickDurationBox.Text) * 1000;

                            // マウス左クリック
                            // ホットキーが押されたらループを抜ける
                            while (!token.IsCancellationRequested) {
                                System.Threading.Thread.Sleep((int)clickDuration);
                                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                                mouse_event(MOUSEEVNETF_LEFTUP, 0, 0, 0, 0);
                            }
                        }
                        catch (Exception ex) {
                            MessageBox.Show(ex.Message);
                        }
                    }, token).ContinueWith(t => {
                        tokenSource.Dispose();
                        tokenSource = null;
                        status = STOP;
                    });
                }
                else {
                    tokenSource.Cancel(true);
                }
            }
        }
    }
}
