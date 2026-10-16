using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Pood_andmebaasiga
{
    public class ToodedForm : Form
    {
        TextBox txtNimetus;
        NumericUpDown numKogus = new NumericUpDown { Minimum = 0, Maximum = 1000000, Left = 100, Top = 50, Width = 200 };
        NumericUpDown numHind = new NumericUpDown { Minimum = 0, Maximum = 1000000, DecimalPlaces = 2, Increment = 0.1m, Left = 100, Top = 85, Width = 200 };
        ComboBox cmbKategooria = new ComboBox { Left = 100, Top = 120, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
        PictureBox picPilt = new PictureBox { Left = 100, Top = 185, Width = 200, Height = 150, SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle };
        DataGridView dataGridTooted = new DataGridView
        {
            Left = 320, Top = 15, Width = 650, Height = 550,
            ReadOnly = true, BackgroundColor = Color.White, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };

        int? valitudId;
        string uusPilt;          // värskelt valitud pildifail (täielik tee)
        string praeguminePilt;   // andmebaasis olev failinimi

        public ToodedForm()
        {
            Text = "Tooted";
            ClientSize = new Size(990, 580);
            StartPosition = FormStartPosition.CenterScreen;

            UI.Silt(this, "Nimetus", 10, 18); txtNimetus = UI.Tekst(this, 100, 15, 200);
            UI.Silt(this, "Kogus", 10, 53);
            UI.Silt(this, "Hind (€)", 10, 88);
            UI.Silt(this, "Kategooria", 10, 123);
            UI.Silt(this, "Pilt", 10, 185);
            Controls.Add(numKogus); Controls.Add(numHind); Controls.Add(cmbKategooria);
            Controls.Add(picPilt); Controls.Add(dataGridTooted);

            UI.Nupp(this, "Lisa kategooria", 100, 150, 98, btnLisaKategooria_Click);
            UI.Nupp(this, "Kustuta kat.", 202, 150, 98, btnKustutaKategooria_Click);
            UI.Nupp(this, "Otsi pildifail...", 100, 340, 200, btnOtsiFail_Click);
            UI.Nupp(this, "Lisa", 10, 385, 140, btnLisa_Click);
            UI.Nupp(this, "Uuenda", 160, 385, 140, btnUuenda_Click);
            UI.Nupp(this, "Kustuta", 10, 425, 290, btnKustuta_Click);
            UI.Silt(this, "Punane rida = laos ≤ 5 tk", 10, 470, 290);

            dataGridTooted.CellClick += dataGridTooted_CellClick;
            Load += (s, e) =>
            {
                if (!Session.OnOmanik) { MessageBox.Show("Juurdepääs keelatud!"); Close(); return; }
                LaadiKategooriad(); LaadiTooted();
            };
        }

        void LaadiKategooriad()
        {
            var dt = Db.Query("SELECT Id, Nimetus FROM Kategooriad ORDER BY Nimetus");
            cmbKategooria.DataSource = dt;
            cmbKategooria.DisplayMember = "Nimetus";
            cmbKategooria.ValueMember = "Id";
        }

        void LaadiTooted()
        {
            dataGridTooted.DataSource = Db.Query(
                @"SELECT t.Id, t.Toodenimetus, t.Kogus, t.Hind, t.Pilt, t.Kategooria_Id, k.Nimetus AS Kategooria
                  FROM Tooted t LEFT JOIN Kategooriad k ON k.Id = t.Kategooria_Id ORDER BY t.Toodenimetus");
            dataGridTooted.Columns["Id"].Visible = false;
            dataGridTooted.Columns["Kategooria_Id"].Visible = false;
            dataGridTooted.Columns["Toodenimetus"].HeaderText = "Toode";
            foreach (DataGridViewRow r in dataGridTooted.Rows)
                if (Convert.ToInt32(r.Cells["Kogus"].Value) <= 5) r.DefaultCellStyle.BackColor = Color.MistyRose;
        }

        void Tuhjenda()
        {
            valitudId = null; uusPilt = null; praeguminePilt = null;
            txtNimetus.Clear(); numKogus.Value = 0; numHind.Value = 0;
            if (picPilt.Image != null) { picPilt.Image.Dispose(); picPilt.Image = null; }
        }

        // Sisestuskontroll: negatiivseid väärtusi ei lubata
        bool Loe(out int kogus, out decimal hind)
        {
            ValidateChildren();
            kogus = (int)numKogus.Value; hind = numHind.Value;
            if (string.IsNullOrWhiteSpace(txtNimetus.Text)) { MessageBox.Show("Sisesta toote nimetus!"); return false; }
            if (kogus < 0) { MessageBox.Show("Kogus ei tohi olla negatiivne!"); return false; }
            if (hind < 0) { MessageBox.Show("Hind ei tohi olla negatiivne!"); return false; }
            if (cmbKategooria.SelectedValue == null) { MessageBox.Show("Vali kategooria!"); return false; }
            return true;
        }

        // Kopeerib uue pildi projekti kausta Pildid ja tagastab failinime
        string SalvestaPilt()
        {
            if (uusPilt == null) return praeguminePilt ?? "";
            Directory.CreateDirectory(Db.PildiKaust);
            string nimi = Guid.NewGuid().ToString("N") + Path.GetExtension(uusPilt);
            File.Copy(uusPilt, Path.Combine(Db.PildiKaust, nimi));
            return nimi;
        }

        void btnLisa_Click(object sender, EventArgs e)
        {
            int kogus; decimal hind;
            if (!Loe(out kogus, out hind)) return;
            Db.Exec("INSERT INTO Tooted (Toodenimetus, Kogus, Hind, Pilt, Kategooria_Id) VALUES (@n,@k,@h,@p,@kat)",
                Db.P("@n", txtNimetus.Text.Trim()), Db.P("@k", kogus), Db.P("@h", hind),
                Db.P("@p", SalvestaPilt()), Db.P("@kat", cmbKategooria.SelectedValue));
            MessageBox.Show("Toode lisatud!");
            Tuhjenda(); LaadiTooted();
        }

        void btnUuenda_Click(object sender, EventArgs e)
        {
            if (valitudId == null) { MessageBox.Show("Vali tabelist toode!"); return; }
            int kogus; decimal hind;
            if (!Loe(out kogus, out hind)) return;
            Db.Exec("UPDATE Tooted SET Toodenimetus=@n, Kogus=@k, Hind=@h, Pilt=@p, Kategooria_Id=@kat WHERE Id=@id",
                Db.P("@n", txtNimetus.Text.Trim()), Db.P("@k", kogus), Db.P("@h", hind),
                Db.P("@p", SalvestaPilt()), Db.P("@kat", cmbKategooria.SelectedValue), Db.P("@id", valitudId.Value));
            MessageBox.Show("Toode uuendatud!");
            Tuhjenda(); LaadiTooted();
        }

        void btnKustuta_Click(object sender, EventArgs e)
        {
            if (valitudId == null) { MessageBox.Show("Vali tabelist toode!"); return; }
            if (MessageBox.Show("Kustutada valitud toode?", "Kinnitus", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            Db.Exec("DELETE FROM Tooted WHERE Id=@id", Db.P("@id", valitudId.Value));
            Tuhjenda(); LaadiTooted();
        }

        void btnOtsiFail_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog { Filter = "Pildifailid|*.jpg;*.jpeg;*.png" })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                uusPilt = dlg.FileName;
                if (picPilt.Image != null) picPilt.Image.Dispose();
                picPilt.Image = UI.LaeFailist(dlg.FileName);
            }
        }

        void dataGridTooted_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var rida = dataGridTooted.Rows[e.RowIndex];
            valitudId = Convert.ToInt32(rida.Cells["Id"].Value);
            txtNimetus.Text = Convert.ToString(rida.Cells["Toodenimetus"].Value);
            numKogus.Value = Convert.ToDecimal(rida.Cells["Kogus"].Value);
            numHind.Value = Convert.ToDecimal(rida.Cells["Hind"].Value);

            object kat = rida.Cells["Kategooria_Id"].Value;
            if (kat == null || kat == DBNull.Value) cmbKategooria.SelectedIndex = -1;
            else cmbKategooria.SelectedValue = kat;

            uusPilt = null;
            praeguminePilt = rida.Cells["Pilt"].Value as string;
            if (picPilt.Image != null) picPilt.Image.Dispose();
            picPilt.Image = UI.LaePilt(praeguminePilt);
        }

        void btnLisaKategooria_Click(object sender, EventArgs e)
        {
            string uus = Microsoft.VisualBasic.Interaction.InputBox("Sisesta uue kategooria nimi:", "Lisa kategooria", "");
            if (string.IsNullOrWhiteSpace(uus)) return;
            Db.Exec("INSERT INTO Kategooriad (Nimetus) VALUES (@n)", Db.P("@n", uus.Trim()));
            LaadiKategooriad();
        }

        void btnKustutaKategooria_Click(object sender, EventArgs e)
        {
            if (cmbKategooria.SelectedValue == null) return;
            if (MessageBox.Show("Kustutada kategooria? Selle toodetel jääb kategooria tühjaks.", "Kinnitus", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            Db.Exec("DELETE FROM Kategooriad WHERE Id=@id", Db.P("@id", cmbKategooria.SelectedValue));
            LaadiKategooriad(); LaadiTooted();
        }
    }
}
