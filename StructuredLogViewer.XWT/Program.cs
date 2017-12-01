using System;
using Xwt;
using Xwt.Drawing;

namespace StructuredLogViewer.XWT
{
    class MainClass
    {
        [STAThread]
        public static void Main()
        {
            Application.Initialize(ToolkitType.Gtk);
            var mainWindow = new MainWindow();
            mainWindow.Show();
            Application.Run();
            mainWindow.Dispose();
        }
    }
}
