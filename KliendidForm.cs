using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace Pood_andmebaasiga
{
    // Administraatori paneel: klientide ja kliendikaartide haldus
    public class KliendidForm : Form
    {
        TextBox txtNimi, txtEmail, txtTelefon, txtKaart, txtOtsi;
        NumericUpDown numBoonus = new NumericUpDown { Minimum = 0, Maximum = 100000, DecimalPlaces = 2, Left = 100, Top = 130, Width = 200 };
        DataGridView grid = new DataGridView
        {
            Left = 320, Top = 50, Width = 650, Height = 500, ReadOnly = true, BackgroundColor = Color.White,
            AllowUserToAddRows = false, AllowUserToDeleteRows = false, MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        int? valitudId;

        public KliendidForm()
        {
            Text = "Administraatori paneel – kliendid";
            ClientSize = new Size(990, 570);
            StartPosition = FormStartPosition.CenterScreen;

            UI.Silt(this, "Nimi", 10, 18); txtNimi = UI.Tekst(this, 100, 15, 200);
            UI.Silt(this, "E-post", 10, 53); txtEmail = UI.Tekst(this, 100, 50, 200);
            UI.Silt(this, "Telefon", 10, 88); txtTelefon = UI.Tekst(this, 100, 85, 200);
            UI.Silt(this, "Boonus (€)", 10, 133); Controls.Add(numBoonus);
            UI.Silt(this, "Kliendikaart", 10, 173); txtKaart = UI.Tekst(this, 100, 170, 200);
            UI.Nupp(this, "Genereeri kaardi nr", 100, 205, 200, (s, e) => txtKaart.Text = UusKaart());

            UI.Nupp(this, "Lisa klient", 10, 260, 140, Lisa);
            UI.Nupp(this, "Uuenda", 160, 260, 140, Uuenda);
            UI.Nupp(this, "Kustuta", 10, 300, 140, Kustuta);
            UI.Nupp(this, "Tühjenda väljad", 160, 300, 140, (s, e) => Tuhjenda());

            UI.Silt(this, "Otsi (nimi / kaart)", 320, 18, 130); txtOtsi = UI.Tekst(this, 455, 15, 250);
            Controls.Add(grid);

            txtOtsi.TextChanged += (s, e) => Laadi();
            grid.CellClick += GridClick;
            Load += (s, e) =>
            {
                if (!Session.OnOmanik) { MessageBox.Show("Juurdepääs keelatud!"); Close(); return; }
                Laadi();
            };
        }

        string UusKaart() { return "K" + new Random().Next(10000000, 99999999); }

        void Laadi()
        {
            grid.DataSource = Db.Query(
                "SELECT Id, Nimi, Email, Telefon, Kliendikaart, Boonus FROM Kliendid WHERE Nimi LIKE @s OR Kliendikaart LIKE @s ORDER BY Nimi",
                Db.P("@s", "%" + txtOtsi.Text.Trim() + "%"));
            grid.Columns["Id"].Visible = false;
        }

        void Tuhjenda()
        {
            valitudId = null; txtNimi.Clear(); txtEmail.Clear(); txtTelefon.Clear(); txtKaart.Clear(); numBoonus.Value = 0;
        }

        static object Tyhi(string s) { return string.IsNullOrWhiteSpace(s) ? null : s.Trim(); }

        void GridClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var r = grid.Rows[e.RowIndex];
            valitudId = Convert.ToInt32(r.Cells["Id"].Value);
            txtNimi.Text = Convert.ToString(r.Cells["Nimi"].Value);
            txtEmail.Text = Convert.ToString(r.Cells["Email"].Value);
            txtTelefon.Text = Convert.ToString(r.Cells["Telefon"].Value);
            txtKaart.Text = Convert.ToString(r.Cells["Kliendikaart"].Value);
            numBoonus.Value = Convert.ToDecimal(r.Cells["Boonus"].Value);
        }

        void Lisa(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNimi.Text)) { MessageBox.Show("Sisesta kliendi nimi!"); return; }
            if (string.IsNullOrWhiteSpace(txtKaart.Text)) txtKaart.Text = UusKaart();
            ValidateChildren();
            try
            {
                Db.Exec("INSERT INTO Kliendid (Nimi, Email, Telefon, Kliendikaart, Boonus) VALUES (@n,@e,@t,@k,@b)",
                    Db.P("@n", txtNimi.Text.Trim()), Db.P("@e", Tyhi(txtEmail.Text)), Db.P("@t", Tyhi(txtTelefon.Text)),
                    Db.P("@k", txtKaart.Text.Trim()), Db.P("@b", numBoonus.Value));
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            { MessageBox.Show("Selline kliendikaardi number on juba olemas!"); return; }
            Tuhjenda(); Laadi();
        }

        void Uuenda(object sender, EventArgs e)
        {
            if (valitudId == null) { MessageBox.Show("Vali tabelist klient!"); return; }
            if (string.IsNullOrWhiteSpace(txtNimi.Text) || string.IsNullOrWhiteSpace(txtKaart.Text))
            { MessageBox.Show("Nimi ja kliendikaart on kohustuslikud!"); return; }
            ValidateChildren();
            try
            {
                Db.Exec("UPDATE Kliendid SET Nimi=@n, Email=@e, Telefon=@t, Kliendikaart=@k, Boonus=@b WHERE Id=@id",
                    Db.P("@n", txtNimi.Text.Trim()), Db.P("@e", Tyhi(txtEmail.Text)), Db.P("@t", Tyhi(txtTelefon.Text)),
                    Db.P("@k", txtKaart.Text.Trim()), Db.P("@b", numBoonus.Value), Db.P("@id", valitudId.Value));
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            { MessageBox.Show("Selline kliendikaardi number on juba olemas!"); return; }
            Tuhjenda(); Laadi();
        }

        void Kustuta(object sender, EventArgs e)
        {
            if (valitudId == null) { MessageBox.Show("Vali tabelist klient!"); return; }
            if (MessageBox.Show("Kustutada klient?", "Kinnitus", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            Db.Exec("UPDATE Muugid SET Klient_Id = NULL WHERE Klient_Id=@id", Db.P("@id", valitudId.Value));
            Db.Exec("DELETE FROM Kliendid WHERE Id=@id", Db.P("@id", valitudId.Value));
            Tuhjenda(); Laadi();
        }
    }
}
