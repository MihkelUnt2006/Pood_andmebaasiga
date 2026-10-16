using System;
using System.Drawing;
using System.Windows.Forms;

namespace Pood_andmebaasiga
{
    public class RegisterForm : Form
    {
        const string OmanikuKood = "OMANIK2026";   // Omaniku rolli registreerimiseks vajalik kood

        TextBox txtUser, txtPass, txtPass2, txtKood;
        ComboBox cmbRoll;
        Label lblKood;

        public RegisterForm()
        {
            Text = "Kasutaja registreerimine";
            ClientSize = new Size(360, 280);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;

            UI.Silt(this, "Kasutajanimi", 20, 25); txtUser = UI.Tekst(this, 150, 22, 190);
            UI.Silt(this, "Parool", 20, 65); txtPass = UI.Tekst(this, 150, 62, 190);
            UI.Silt(this, "Korda parooli", 20, 105); txtPass2 = UI.Tekst(this, 150, 102, 190);
            txtPass.UseSystemPasswordChar = true; txtPass2.UseSystemPasswordChar = true;

            UI.Silt(this, "Roll", 20, 145);
            cmbRoll = new ComboBox { Left = 150, Top = 142, Width = 190, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbRoll.Items.AddRange(new object[] { "Müüja", "Omanik" });
            cmbRoll.SelectedIndex = 0;
            cmbRoll.SelectedIndexChanged += (s, e) =>
            {
                bool om = cmbRoll.SelectedItem.ToString() == "Omanik";
                lblKood.Visible = om; txtKood.Visible = om;
            };
            Controls.Add(cmbRoll);

            lblKood = UI.Silt(this, "Omaniku kood", 20, 185); txtKood = UI.Tekst(this, 150, 182, 190);
            txtKood.UseSystemPasswordChar = true;
            lblKood.Visible = false; txtKood.Visible = false;

            AcceptButton = UI.Nupp(this, "Registreeri", 150, 225, 190, Registreeri);
        }

        void Registreeri(object sender, EventArgs e)
        {
            string nimi = txtUser.Text.Trim();
            string roll = cmbRoll.SelectedItem.ToString();

            if (nimi.Length < 3) { MessageBox.Show("Kasutajanimi peab olema vähemalt 3 märki!"); return; }
            if (txtPass.Text.Length < 4) { MessageBox.Show("Parool peab olema vähemalt 4 märki!"); return; }
            if (txtPass.Text != txtPass2.Text) { MessageBox.Show("Paroolid ei ühti!"); return; }
            if (roll == "Omanik" && txtKood.Text != OmanikuKood) { MessageBox.Show("Vale omaniku kood!"); return; }

            if ((int)Db.Scalar("SELECT COUNT(*) FROM Kasutajad WHERE Kasutajanimi=@u", Db.P("@u", nimi)) > 0)
            { MessageBox.Show("Selline kasutajanimi on juba olemas!"); return; }

            string sool = Db.UusSool();
            Db.Exec("INSERT INTO Kasutajad (Kasutajanimi, ParoolHash, Sool, Roll) VALUES (@u,@h,@s,@r)",
                Db.P("@u", nimi), Db.P("@h", Db.Hash(txtPass.Text, sool)), Db.P("@s", sool), Db.P("@r", roll));
            MessageBox.Show("Kasutaja loodud rollis: " + roll);
            Close();
        }
    }
}
