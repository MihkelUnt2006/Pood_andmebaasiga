using System;
using System.Windows.Forms;

namespace Pood_andmebaasiga
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try { Db.Init(); }
            catch (Exception ex)
            {
                MessageBox.Show("Andmebaasi loomine ebaõnnestus (kas SQL Server LocalDB on installitud?):\n\n" + ex.Message);
                return;
            }
            Application.Run(new LoginForm());
        }
    }
}
