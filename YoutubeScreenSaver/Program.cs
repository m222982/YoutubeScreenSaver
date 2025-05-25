using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace YoutubeScreenSaver
{
    internal static class Program
    {
        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        /// <summary>
        /// 應用程式的主要進入點。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length > 0 && args[0].ToLower() == "/p" && args.Length > 1)
            {
                // 取得傳入的視窗句柄
                IntPtr previewHandle = new IntPtr(long.Parse(args[1]));

                var previewForm = new PreviewForm();

                // 把視窗父視窗設定為 previewHandle，讓它嵌入
                SetParent(previewForm.Handle, previewHandle);

                // 取得父視窗大小並設定子視窗大小
                GetClientRect(previewHandle, out RECT rect);
                previewForm.Location = new Point(0, 0);
                previewForm.Size = new Size(rect.Right - rect.Left, rect.Bottom - rect.Top);

                Application.Run(previewForm);
            }
            else if ((args.Length > 0) && args[0].ToLower().StartsWith("/s"))
            {
                //test 或預設
                Application.Run(new FormSetting());
            }
            else
            {
                //setup
                Application.Run(new FormSetting(true));
            }
        }
    }

    public class PreviewForm : Form
    {
        private Image image;

        public PreviewForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;

            image = Resource1.logo;
            this.Paint += PreviewForm_Paint;
        }

        private void PreviewForm_Paint(object sender, PaintEventArgs e)
        {
            if (image != null)
            {
                e.Graphics.DrawImage(image, this.ClientRectangle);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && image != null)
            {
                image.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
