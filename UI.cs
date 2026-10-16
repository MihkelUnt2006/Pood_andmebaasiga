using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Pood_andmebaasiga
{
    public static class UI
    {
        public static Label Silt(Control p, string tekst, int x, int y, int w = 85, int v = 0)
        {
            var l = new Label { Text = tekst, Left = x, Top = y, Width = w, AutoSize = false, Height = 20 };
            p.Controls.Add(l); return l;
        }

        public static TextBox Tekst(Control p, int x, int y, int w = 180)
        {
            var t = new TextBox { Left = x, Top = y, Width = w };
            p.Controls.Add(t); return t;
        }

        public static Button Nupp(Control p, string tekst, int x, int y, int w, EventHandler h)
        {
            var b = new Button { Text = tekst, Left = x, Top = y, Width = w, Height = 30 };
            b.Click += h;
            p.Controls.Add(b); return b;
        }

        // Laeb pildi failist nii, et faili ei jääks lukku (see lahendab piltidega seotud vead)
        public static Image LaeFailist(string tee)
        {
            try
            {
                if (string.IsNullOrEmpty(tee) || !File.Exists(tee)) return null;
                using (var fs = new FileStream(tee, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var img = Image.FromStream(fs))
                    return new Bitmap(img);
            }
            catch { return null; }
        }

        public static Image LaePilt(string failinimi)
        {
            if (string.IsNullOrWhiteSpace(failinimi)) return null;
            return LaeFailist(Path.Combine(Db.PildiKaust, failinimi));
        }
    }
}
