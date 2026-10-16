using System;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace Pood_andmebaasiga
{
    public class OstuRida
    {
        public int Id { get; set; }
        public string Nimi { get; set; }
        public decimal Hind { get; set; }
        public int Kogus { get; set; }
        public int Max { get; set; }
        public decimal Summa { get { return Hind * Kogus; } }
    }

    public class KassaForm : Form
    {
        const decimal BoonuseMaar = 0.05m;   // 5% ostusummast kogutakse boonusena

        ComboBox cmbKategooria = new ComboBox { Left = 90, Top = 10, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
        TextBox txtOtsi;
        FlowLayoutPanel flow = new FlowLayoutPanel { Left = 10, Top = 45, Width = 780, Height = 640, AutoScroll = true, BorderStyle = BorderStyle.FixedSingle };
        DataGridView gridOst = new DataGridView
        {
            Left = 810, Top = 45, Width = 390, Height = 300, ReadOnly = true, BackgroundColor = Color.White,
            AllowUserToAddRows = false, AllowUserToDeleteRows = false, MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false
        };
        BindingList<OstuRida> cart = new BindingList<OstuRida>();
        TextBox txtKaart;
        Label lblKlient, lblKokku;
        CheckBox chkBoonus = new CheckBox { Left = 810, Top = 450, Width = 390, Text = "Kasuta kliendi boonust", Enabled = false };

        int? klientId; string klientNimi; decimal klientBoonus;
        bool laadimine;

        public KassaForm()
        {
            Text = "Kassa – müüja: " + Session.Kasutaja;
            ClientSize = new Size(1220, 700);
            StartPosition = FormStartPosition.CenterScreen;

            UI.Silt(this, "Kategooria", 10, 14, 75);
            UI.Silt(this, "Otsi", 310, 14, 40);
            txtOtsi = UI.Tekst(this, 350, 10, 200);
            Controls.Add(cmbKategooria); Controls.Add(flow); Controls.Add(gridOst); Controls.Add(chkBoonus);

            UI.Silt(this, "Ostukorv", 810, 18, 100).Font = new Font(Font, FontStyle.Bold);
            UI.Nupp(this, "+", 810, 350, 60, (s, e) => Muuda(+1));
            UI.Nupp(this, "−", 875, 350, 60, (s, e) => Muuda(-1));
            UI.Nupp(this, "Eemalda", 940, 350, 100, (s, e) => Muuda(-9999));
            UI.Nupp(this, "Tühjenda", 1045, 350, 100, (s, e) => { cart.Clear(); UuendaSumma(); });

            UI.Silt(this, "Kliendikaart", 810, 399, 90);
            txtKaart = UI.Tekst(this, 905, 395, 150);
            UI.Nupp(this, "Leia klient", 1060, 392, 100, LeiaKlient);
            lblKlient = UI.Silt(this, "Klient valimata", 810, 425, 390);

            lblKokku = UI.Silt(this, "", 810, 485, 390, 90);
            lblKokku.Height = 90; lblKokku.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            var osta = UI.Nupp(this, "OSTA ja loo tšekk (PDF)", 810, 590, 390, Osta);
            osta.Height = 55; osta.Font = new Font("Segoe UI", 11, FontStyle.Bold);

            gridOst.DataSource = cart;
            gridOst.Columns["Id"].Visible = false;
            gridOst.Columns["Max"].Visible = false;
            gridOst.Columns["Nimi"].HeaderText = "Toode";

            chkBoonus.CheckedChanged += (s, e) => UuendaSumma();
            cmbKategooria.SelectedIndexChanged += (s, e) => { if (!laadimine) LaadiKaardid(); };
            txtOtsi.TextChanged += (s, e) => LaadiKaardid();

            Load += (s, e) => { LaadiKategooriad(); LaadiKaardid(); UuendaSumma(); };
        }

        void LaadiKategooriad()
        {
            laadimine = true;
            var dt = Db.Query("SELECT Id, Nimetus FROM Kategooriad ORDER BY Nimetus");
            var r = dt.NewRow(); r["Id"] = 0; r["Nimetus"] = "Kõik kategooriad";
            dt.Rows.InsertAt(r, 0);
            cmbKategooria.DataSource = dt;
            cmbKategooria.DisplayMember = "Nimetus";
            cmbKategooria.ValueMember = "Id";
            laadimine = false;
        }

        // Visuaalne kassa: toodete kaardid (pildivõrk)
        void LaadiKaardid()
        {
            foreach (Control c in flow.Controls.Cast<Control>().ToList()) c.Dispose();
            flow.Controls.Clear();

            int kat = cmbKategooria.SelectedValue == null ? 0 : Convert.ToInt32(cmbKategooria.SelectedValue);
            var dt = Db.Query(
                "SELECT Id, Toodenimetus, Kogus, Hind, Pilt FROM Tooted WHERE (@k=0 OR Kategooria_Id=@k) AND Toodenimetus LIKE @s ORDER BY Toodenimetus",
                Db.P("@k", kat), Db.P("@s", "%" + txtOtsi.Text.Trim() + "%"));

            foreach (DataRow row in dt.Rows)
            {
                int id = Convert.ToInt32(row["Id"]);
                string nimi = (string)row["Toodenimetus"];
                int kogus = Convert.ToInt32(row["Kogus"]);
                decimal hind = Convert.ToDecimal(row["Hind"]);

                var kaart = new Panel { Width = 140, Height = 195, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(5), Cursor = Cursors.Hand };
                var pic = new PictureBox { Left = 4, Top = 4, Width = 130, Height = 100, SizeMode = PictureBoxSizeMode.Zoom, Image = UI.LaePilt(row["Pilt"] as string) };
                var lblNimi = new Label { Left = 4, Top = 108, Width = 130, Height = 38, Text = nimi, Font = new Font(Font, FontStyle.Bold) };
                var lblHind = new Label { Left = 4, Top = 148, Width = 130, Height = 18, Text = hind.ToString("0.00") + " €" };
                var lblLadu = new Label { Left = 4, Top = 168, Width = 130, Height = 18, Text = kogus > 0 ? "Laos: " + kogus + " tk" : "OTSAS", ForeColor = kogus > 0 ? Color.DimGray : Color.Red };
                kaart.Controls.AddRange(new Control[] { pic, lblNimi, lblHind, lblLadu });
                if (kogus == 0) kaart.BackColor = Color.Gainsboro;

                EventHandler h = (s, e) => LisaOstukorvi(id, nimi, hind, kogus);
                kaart.Click += h; pic.Click += h; lblNimi.Click += h; lblHind.Click += h; lblLadu.Click += h;
                flow.Controls.Add(kaart);
            }
        }

        void LisaOstukorvi(int id, string nimi, decimal hind, int laos)
        {
            var olemas = cart.FirstOrDefault(x => x.Id == id);
            if (olemas != null)
            {
                if (olemas.Kogus + 1 > olemas.Max) { MessageBox.Show("Laos on ainult " + olemas.Max + " tk!"); return; }
                olemas.Kogus++;
            }
            else
            {
                if (laos < 1) { MessageBox.Show("Toode on otsas!"); return; }
                cart.Add(new OstuRida { Id = id, Nimi = nimi, Hind = hind, Kogus = 1, Max = laos });
            }
            cart.ResetBindings();
            UuendaSumma();
        }

        void Muuda(int delta)
        {
            var it = gridOst.CurrentRow == null ? null : gridOst.CurrentRow.DataBoundItem as OstuRida;
            if (it == null) return;
            if (delta > 0 && it.Kogus + delta > it.Max) { MessageBox.Show("Laos on ainult " + it.Max + " tk!"); return; }
            it.Kogus += delta;
            if (it.Kogus <= 0) cart.Remove(it);
            cart.ResetBindings();
            UuendaSumma();
        }

        void LeiaKlient(object sender, EventArgs e)
        {
            var dt = Db.Query("SELECT Id, Nimi, Boonus FROM Kliendid WHERE Kliendikaart=@k", Db.P("@k", txtKaart.Text.Trim()));
            if (dt.Rows.Count == 0)
            {
                klientId = null; klientNimi = null; klientBoonus = 0;
                lblKlient.Text = "Klienti ei leitud";
                chkBoonus.Checked = false; chkBoonus.Enabled = false;
            }
            else
            {
                klientId = Convert.ToInt32(dt.Rows[0]["Id"]);
                klientNimi = (string)dt.Rows[0]["Nimi"];
                klientBoonus = Convert.ToDecimal(dt.Rows[0]["Boonus"]);
                lblKlient.Text = klientNimi + " – boonus: " + klientBoonus.ToString("0.00") + " €";
                chkBoonus.Enabled = klientBoonus > 0;
            }
            UuendaSumma();
        }

        decimal Kokku() { return cart.Sum(x => x.Summa); }
        decimal BoonusKasutatud() { return (chkBoonus.Checked && klientId.HasValue) ? Math.Min(klientBoonus, Kokku()) : 0m; }

        void UuendaSumma()
        {
            decimal kokku = Kokku(), kasut = BoonusKasutatud();
            lblKokku.Text = "Summa: " + kokku.ToString("0.00") + " €\n" +
                            "Boonus: -" + kasut.ToString("0.00") + " €\n" +
                            "Maksta: " + (kokku - kasut).ToString("0.00") + " €";
        }

        void Osta(object sender, EventArgs e)
        {
            if (cart.Count == 0) { MessageBox.Show("Ostukorv on tühi!"); return; }

            decimal kokku = Kokku(), kasut = BoonusKasutatud(), maksta = kokku - kasut;
            decimal saadud = klientId.HasValue ? Math.Round(maksta * BoonuseMaar, 2) : 0m;
            int muukId;

            using (var c = new SqlConnection(Db.ConnStr))
            {
                c.Open();
                using (var tr = c.BeginTransaction())
                {
                    try
                    {
                        // Laoseis väheneb; ostu ei saa teha rohkem kui laos on
                        foreach (var it in cart)
                        {
                            using (var cmd = new SqlCommand("UPDATE Tooted SET Kogus = Kogus - @k WHERE Id=@id AND Kogus >= @k", c, tr))
                            {
                                cmd.Parameters.AddWithValue("@k", it.Kogus);
                                cmd.Parameters.AddWithValue("@id", it.Id);
                                if (cmd.ExecuteNonQuery() == 0)
                                {
                                    tr.Rollback();
                                    MessageBox.Show("Toodet „" + it.Nimi + "” ei ole laos piisavalt! Ost katkestati.");
                                    LaadiKaardid();
                                    return;
                                }
                            }
                        }

                        using (var cmd = new SqlCommand(
                            "INSERT INTO Muugid (Muuja, Klient_Id, Summa, BoonusKasutatud, BoonusSaadud) OUTPUT INSERTED.Id VALUES (@m,@kl,@s,@bk,@bs)", c, tr))
                        {
                            cmd.Parameters.AddWithValue("@m", Session.Kasutaja ?? "");
                            cmd.Parameters.AddWithValue("@kl", klientId.HasValue ? (object)klientId.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@s", maksta);
                            cmd.Parameters.AddWithValue("@bk", kasut);
                            cmd.Parameters.AddWithValue("@bs", saadud);
                            muukId = (int)cmd.ExecuteScalar();
                        }

                        foreach (var it in cart)
                        {
                            using (var cmd = new SqlCommand("INSERT INTO MuugiRead (Muuk_Id, Toode, Kogus, Hind) VALUES (@m,@t,@k,@h)", c, tr))
                            {
                                cmd.Parameters.AddWithValue("@m", muukId);
                                cmd.Parameters.AddWithValue("@t", it.Nimi);
                                cmd.Parameters.AddWithValue("@k", it.Kogus);
                                cmd.Parameters.AddWithValue("@h", it.Hind);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        if (klientId.HasValue)
                        {
                            using (var cmd = new SqlCommand("UPDATE Kliendid SET Boonus = Boonus - @bk + @bs WHERE Id=@id", c, tr))
                            {
                                cmd.Parameters.AddWithValue("@bk", kasut);
                                cmd.Parameters.AddWithValue("@bs", saadud);
                                cmd.Parameters.AddWithValue("@id", klientId.Value);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        tr.Commit();
                    }
                    catch (Exception ex)
                    {
                        try { tr.Rollback(); } catch { }
                        MessageBox.Show("Ostu salvestamine ebaõnnestus: " + ex.Message);
                        return;
                    }
                }
            }

            string fail = null;
            try
            {
                fail = KirjutaTsekk(muukId, kokku, kasut, maksta, saadud);
                Process.Start(new ProcessStartInfo(fail) { UseShellExecute = true });
            }
            catch (Exception ex) { MessageBox.Show("Ost tehtud, kuid tšeki PDF ebaõnnestus: " + ex.Message); }

            // Lähtesta kassa
            cart.Clear();
            klientId = null; klientNimi = null; klientBoonus = 0; txtKaart.Clear();
            lblKlient.Text = "Klient valimata"; chkBoonus.Checked = false; chkBoonus.Enabled = false;
            UuendaSumma(); LaadiKaardid();
            if (fail != null) MessageBox.Show("Ost tehtud! Tšekk salvestati:\n" + fail);
        }

        // Tšekk salvestatakse PDF-failina kausta „Arved”
        string KirjutaTsekk(int id, decimal kokku, decimal kasut, decimal maksta, decimal saadud)
        {
            Directory.CreateDirectory(Db.ArveteKaust);
            string tee = Path.Combine(Db.ArveteKaust, "Tsekk_" + id + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf");

            var doc = new PdfDocument();
            doc.Info.Title = "Tšekk nr " + id;
            var page = doc.AddPage();
            const double w = 227.0;
            page.Width = w;
            page.Height = 260.0 + cart.Count * 26.0;

            var gfx = XGraphics.FromPdfPage(page);
            var opt = new XPdfFontOptions(PdfFontEncoding.Unicode);
            var f = new XFont("Arial", 9, XFontStyle.Regular, opt);
            var fb = new XFont("Arial", 11, XFontStyle.Bold, opt);
            double y = 15;

            Action<string, string, XFont> rida = (vasak, parem, font) =>
            {
                var rect = new XRect(10, y, w - 20, 14);
                gfx.DrawString(vasak, font, XBrushes.Black, rect, XStringFormats.TopLeft);
                if (!string.IsNullOrEmpty(parem)) gfx.DrawString(parem, font, XBrushes.Black, rect, XStringFormats.TopRight);
                y += 14;
            };

            rida("POOD – TŠEKK nr " + id, "", fb); y += 4;
            rida(DateTime.Now.ToString("dd.MM.yyyy HH:mm"), "", f);
            rida("Müüja: " + Session.Kasutaja, "", f);
            if (klientId.HasValue) rida("Klient: " + klientNimi, "", f);
            y += 4; rida("----------------------------------------", "", f);

            foreach (var it in cart)
            {
                string nimi = it.Nimi.Length > 30 ? it.Nimi.Substring(0, 30) : it.Nimi;
                rida(nimi, "", f);
                rida("", it.Kogus + " x " + it.Hind.ToString("0.00") + " = " + it.Summa.ToString("0.00") + " €", f);
            }

            rida("----------------------------------------", "", f);
            rida("Summa", kokku.ToString("0.00") + " €", f);
            if (kasut > 0) rida("Boonus kasutatud", "-" + kasut.ToString("0.00") + " €", f);
            rida("MAKSTA", maksta.ToString("0.00") + " €", fb);
            if (klientId.HasValue) rida("Boonust saadud", "+" + saadud.ToString("0.00") + " €", f);
            y += 8; rida("Aitäh ostu eest!", "", f);

            gfx.Dispose();
            doc.Save(tee);
            return tee;
        }
    }
}
