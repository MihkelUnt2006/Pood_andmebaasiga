using System;
using System.Drawing;
using System.Windows.Forms;

namespace Pood_andmebaasiga
{
    public class LoginForm : Form
    {
        TextBox txtUser, txtPass;

        public LoginForm()
        {
            Text = "Pood – sisselogimine";
            ClientSize = new Size(320, 200);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            UI.Silt(this, "Kasutajanimi", 20, 25); txtUser = UI.Tekst(this, 130, 22, 160);
            UI.Silt(this, "Parool", 20, 65); txtPass = UI.Tekst(this, 130, 62, 160);
            txtPass.UseSystemPasswordChar = true;
            AcceptButton = UI.Nupp(this, "Logi sisse", 130, 105, 160, Login);
            UI.Nupp(this, "Registreeri kasutaja", 130, 145, 160, (s, e) =>
            {
                using (var f = new RegisterForm()) f.ShowDialog(this);
            });
        }

        void Login(object sender, EventArgs e)
        {
            var dt = Db.Query("SELECT * FROM Kasutajad WHERE Kasutajanimi=@u", Db.P("@u", txtUser.Text.Trim()));
            if (dt.Rows.Count == 0 ||
                Db.Hash(txtPass.Text, (string)dt.Rows[0]["Sool"]) != (string)dt.Rows[0]["ParoolHash"])
            {
                MessageBox.Show("Vale kasutajanimi või parool!");
                return;
            }
            Session.Kasutaja = (string)dt.Rows[0]["Kasutajanimi"];
            Session.Roll = (string)dt.Rows[0]["Roll"];

            Hide();
            // Müüja näeb ainult Kassat, Omanik näeb menüüd (Tooted, Kassa, Kliendid)
            using (Form f = Session.OnOmanik ? (Form)new MenuForm() : new KassaForm())
                f.ShowDialog();
            Session.Kasutaja = null; Session.Roll = null;
            txtPass.Clear();
            Show();
        }
    }
}
