using System;
using System.Drawing;
using System.Windows.Forms;

namespace Pood_andmebaasiga
{
    // Omaniku peamenüü
    public class MenuForm : Form
    {
        public MenuForm()
        {
            Text = "Pood – menüü (Omanik)";
            ClientSize = new Size(320, 250);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            UI.Silt(this, "Tere, " + Session.Kasutaja + "!", 20, 15, 280);
            UI.Nupp(this, "Tooted", 40, 50, 240, (s, e) => Ava(new ToodedForm())).Height = 40;
            UI.Nupp(this, "Kassa", 40, 100, 240, (s, e) => Ava(new KassaForm())).Height = 40;
            UI.Nupp(this, "Administraatori paneel (kliendid)", 40, 150, 240, (s, e) => Ava(new KliendidForm())).Height = 40;
            UI.Nupp(this, "Logi välja", 40, 200, 240, (s, e) => Close());
        }

        void Ava(Form f)
        {
            using (f) f.ShowDialog(this);
        }
    }
}
