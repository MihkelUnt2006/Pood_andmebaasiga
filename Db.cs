using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Security.Cryptography;

namespace Pood_andmebaasiga
{
    public static class Session
    {
        public static string Kasutaja;
        public static string Roll;
        public static bool OnOmanik { get { return Roll == "Omanik"; } }
    }

    public static class Db
    {
        public static readonly string Baas = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string AndmeKaust = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Pood_andmebaasiga", "Andmed");
        public static readonly string PildiKaust = Path.Combine(Baas, "Pildid");
        public static readonly string ArveteKaust = Path.Combine(Baas, "Arved");

        const string MasterConn = @"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True";
        public const string ConnStr = @"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=Pood;Integrated Security=True";

        // Loob kaustad, andmebaasi ja tabelid, kui neid veel pole
        public static void Init()
        {
            Directory.CreateDirectory(AndmeKaust);
            Directory.CreateDirectory(PildiKaust);
            Directory.CreateDirectory(ArveteKaust);

            string mdf = Path.Combine(AndmeKaust, "Pood.mdf");
            string ldf = Path.Combine(AndmeKaust, "Pood_log.ldf");
            string createSql = File.Exists(mdf)
                ? "CREATE DATABASE Pood ON (FILENAME='" + mdf + "') FOR ATTACH"
                : "CREATE DATABASE Pood ON (NAME=Pood, FILENAME='" + mdf + "') LOG ON (NAME=Pood_log, FILENAME='" + ldf + "')";

            // Kas olemasolevasse andmebaasi saab ühenduda?
            bool toimib = false;
            try { using (var t = new SqlConnection(ConnStr)) { t.Open(); toimib = true; } }
            catch { }

            using (var c = new SqlConnection(MasterConn))
            {
                c.Open();
                if (!toimib)
                {
                    // Andmebaas on registreeritud, aga failid puuduvad/on katki -> eemaldame registreeringu
                    Proovi(c, "ALTER DATABASE Pood SET SINGLE_USER WITH ROLLBACK IMMEDIATE");
                    Proovi(c, "DROP DATABASE Pood");
                    Proovi(c, "EXEC sp_detach_db 'Pood', 'true'");
                }
                using (var cmd = new SqlCommand("IF DB_ID('Pood') IS NULL " + createSql, c)) cmd.ExecuteNonQuery();
            }

            const string schema = @"
IF OBJECT_ID('Kategooriad') IS NULL
CREATE TABLE Kategooriad (Id INT IDENTITY(1,1) PRIMARY KEY, Nimetus NVARCHAR(50) NOT NULL);

IF OBJECT_ID('Tooted') IS NULL
CREATE TABLE Tooted (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Toodenimetus NVARCHAR(100) NOT NULL,
    Kogus INT NOT NULL CHECK (Kogus >= 0),
    Hind DECIMAL(10,2) NOT NULL CHECK (Hind >= 0),
    Pilt NVARCHAR(255) NULL,
    Kategooria_Id INT NULL REFERENCES Kategooriad(Id) ON DELETE SET NULL);

IF OBJECT_ID('Kasutajad') IS NULL
CREATE TABLE Kasutajad (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Kasutajanimi NVARCHAR(50) NOT NULL UNIQUE,
    ParoolHash NVARCHAR(100) NOT NULL,
    Sool NVARCHAR(50) NOT NULL,
    Roll NVARCHAR(20) NOT NULL);

IF OBJECT_ID('Kliendid') IS NULL
CREATE TABLE Kliendid (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nimi NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NULL,
    Telefon NVARCHAR(30) NULL,
    Kliendikaart NVARCHAR(20) NOT NULL UNIQUE,
    Boonus DECIMAL(10,2) NOT NULL DEFAULT 0);

IF OBJECT_ID('Muugid') IS NULL
CREATE TABLE Muugid (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Kuupaev DATETIME NOT NULL DEFAULT GETDATE(),
    Muuja NVARCHAR(50) NULL,
    Klient_Id INT NULL,
    Summa DECIMAL(10,2) NOT NULL,
    BoonusKasutatud DECIMAL(10,2) NOT NULL DEFAULT 0,
    BoonusSaadud DECIMAL(10,2) NOT NULL DEFAULT 0);

IF OBJECT_ID('MuugiRead') IS NULL
CREATE TABLE MuugiRead (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Muuk_Id INT NOT NULL REFERENCES Muugid(Id),
    Toode NVARCHAR(100) NOT NULL,
    Kogus INT NOT NULL,
    Hind DECIMAL(10,2) NOT NULL);

IF NOT EXISTS (SELECT 1 FROM Kategooriad)
INSERT INTO Kategooriad (Nimetus) VALUES (N'Toidutooted'), (N'Joogid'), (N'Maiustused');";
            Exec(schema);
        }

        static void Proovi(SqlConnection c, string sql)
        {
            try { using (var cmd = new SqlCommand("IF DB_ID('Pood') IS NOT NULL " + sql, c)) cmd.ExecuteNonQuery(); }
            catch { }
        }

        public static SqlParameter P(string nimi, object v) { return new SqlParameter(nimi, v ?? DBNull.Value); }

        static SqlCommand Cmd(SqlConnection c, string sql, SqlParameter[] p)
        {
            var cmd = new SqlCommand(sql, c);
            if (p != null && p.Length > 0) cmd.Parameters.AddRange(p);
            return cmd;
        }

        public static DataTable Query(string sql, params SqlParameter[] p)
        {
            using (var c = new SqlConnection(ConnStr))
            using (var da = new SqlDataAdapter(Cmd(c, sql, p)))
            {
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public static int Exec(string sql, params SqlParameter[] p)
        {
            using (var c = new SqlConnection(ConnStr))
            {
                c.Open();
                using (var cmd = Cmd(c, sql, p)) return cmd.ExecuteNonQuery();
            }
        }

        public static object Scalar(string sql, params SqlParameter[] p)
        {
            using (var c = new SqlConnection(ConnStr))
            {
                c.Open();
                using (var cmd = Cmd(c, sql, p)) return cmd.ExecuteScalar();
            }
        }

        public static string UusSool()
        {
            var b = new byte[16];
            using (var r = RandomNumberGenerator.Create()) r.GetBytes(b);
            return Convert.ToBase64String(b);
        }

        public static string Hash(string parool, string sool)
        {
            using (var kdf = new Rfc2898DeriveBytes(parool, Convert.FromBase64String(sool), 10000))
                return Convert.ToBase64String(kdf.GetBytes(32));
        }
    }
}
