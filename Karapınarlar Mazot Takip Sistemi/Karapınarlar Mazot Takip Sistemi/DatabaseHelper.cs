using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;


public class DatabaseHelper
{
    private static string dbPath = "Database/mazot_takip.db";


    public static string GetDbPath()
    {
        return dbPath;
    }
    // Veritabanını başlatma ve tablo oluşturma
    public static void InitializeDatabase()
    {
        string directoryPath = "Database";

        // Eğer Database klasörü yoksa oluştur
        if (!System.IO.Directory.Exists(directoryPath))
        {
            System.IO.Directory.CreateDirectory(directoryPath);
            Console.WriteLine("Database klasörü oluşturuldu.");
        }

        // Eğer veritabanı dosyası yoksa oluştur
        if (!System.IO.File.Exists(dbPath))
        {
            SQLiteConnection.CreateFile(dbPath);
            Console.WriteLine("Veritabanı dosyası oluşturuldu.");
        }

        using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
        {
            connection.Open();

            // Islemler tablosu
            string createTableQuery = @"
        CREATE TABLE IF NOT EXISTS Islemler (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Tarih TEXT NOT NULL,
            BelgeNo TEXT NOT NULL,
            Aciklama TEXT NOT NULL,
            Plaka TEXT NOT NULL,
            Borc REAL NOT NULL DEFAULT 0,
            Alacak REAL NOT NULL DEFAULT 0,
            GuncelBorc REAL NOT NULL
        );";
            new SQLiteCommand(createTableQuery, connection).ExecuteNonQuery();

            Console.WriteLine("Tablo oluşturuldu.");
        }
    }


    // Son güncel borcu getiren metot
    public static decimal GetLastBalance()
    {
        using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
        {
            connection.Open();
            string query = "SELECT GuncelBorc FROM Islemler ORDER BY Id DESC LIMIT 1;";
            var command = new SQLiteCommand(query, connection);
            var result = command.ExecuteScalar();

            return result != null ? Convert.ToDecimal(result) : 0; // Eğer işlem yoksa başlangıç borcu 0 kabul edilir
        }
    }

    // İşlem ekleme metodu (borç, alacak ve güncel toplam borç hesaplama)
    public static void AddTransaction(string tarih, string belgeNo, string aciklama, string plaka, decimal borc, decimal alacak)
    {
        using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
        {
            connection.Open();

            // Önceki borç durumunu al ve yeni borcu hesapla
            decimal previousBalance = GetLastBalance();
            decimal newBalance = previousBalance + borc - alacak;

            // Yeni işlem ekle
            string insertQuery = @"
            INSERT INTO Islemler (Tarih, BelgeNo, Aciklama, Plaka, Borc, Alacak, GuncelBorc)
            VALUES (@Tarih, @BelgeNo, @Aciklama, @Plaka, @Borc, @Alacak, @GuncelBorc);";

            var command = new SQLiteCommand(insertQuery, connection);
            command.Parameters.AddWithValue("@Tarih", tarih);
            command.Parameters.AddWithValue("@BelgeNo", belgeNo);
            command.Parameters.AddWithValue("@Aciklama", aciklama);
            command.Parameters.AddWithValue("@Plaka", plaka);
            command.Parameters.AddWithValue("@Borc", borc);
            command.Parameters.AddWithValue("@Alacak", alacak);
            command.Parameters.AddWithValue("@GuncelBorc", newBalance);

            command.ExecuteNonQuery();
            Console.WriteLine("İşlem başarıyla eklendi. Güncel Borç: " + newBalance);
        }
    }
    // İşlemleri Listeleme
    public static DataTable GetAllTransactions()
    {
        using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
        {
            connection.Open();

            // Id sütununu da sorguya ekledik
            string selectQuery = "SELECT Id, Tarih, BelgeNo, Aciklama, Plaka, Borc, Alacak, GuncelBorc FROM Islemler;";
            var command = new SQLiteCommand(selectQuery, connection);
            var adapter = new SQLiteDataAdapter(command);
            var dataTable = new DataTable();
            adapter.Fill(dataTable);

            return dataTable;
        }
    }


    // Yeni aya borç devri (Devir işlemi)
    public static void AddDevirTransaction(string plaka, string tarih)
    {
        using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
        {
            connection.Open();

            // Önceki borç durumunu al
            decimal previousBalance = GetLastBalance();

            // Devir işlemi ekle
            string insertQuery = @"
            INSERT INTO Islemler (Tarih, BelgeNo, Aciklama, Plaka, Borc, Alacak, GuncelBorc)
            VALUES (@Tarih, @BelgeNo, @Aciklama, @Plaka, @Borc, @Alacak, @GuncelBorc);";

            var command = new SQLiteCommand(insertQuery, connection);
            command.Parameters.AddWithValue("@Tarih", tarih);
            command.Parameters.AddWithValue("@BelgeNo", "DEVR-001");
            command.Parameters.AddWithValue("@Aciklama", "Önceki aydan devir");
            command.Parameters.AddWithValue("@Plaka", plaka);
            command.Parameters.AddWithValue("@Borc", previousBalance);
            command.Parameters.AddWithValue("@Alacak", 0); // Devirde alacak yok
            command.Parameters.AddWithValue("@GuncelBorc", previousBalance);

            command.ExecuteNonQuery();
            Console.WriteLine("Devir işlemi başarıyla eklendi. Devredilen Borç: " + previousBalance);
        }
    }

    public static bool DeleteTransaction(int id)
    {
        using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
        {
            connection.Open();

            string deleteQuery = "DELETE FROM Islemler WHERE Id = @Id;";
            var command = new SQLiteCommand(deleteQuery, connection);
            command.Parameters.AddWithValue("@Id", id);

            int rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected > 0)
            {
                Console.WriteLine("Kayıt başarıyla silindi.");
                return true; // Silme işlemi başarılı
            }
            else
            {
                Console.WriteLine("Silinecek kayıt bulunamadı.");
                return false; // Silinecek kayıt bulunamadı
            }
        }
    }

    // Aylık toplamları hesaplayan metot
    public static DataTable GetMonthlySummary()
    {
        using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
        {
            connection.Open();

            // Aylık toplamları hesaplayan SQL sorgusu
            string query = @"
            SELECT 
                strftime('%m.%Y', Tarih) AS [Dönem],
                SUM(Borc) AS [Borç],
                SUM(Alacak) AS [Alacak],
                SUM(Borc) - SUM(Alacak) AS [Fark]
            FROM Islemler
            GROUP BY strftime('%m.%Y', Tarih)
            ORDER BY strftime('%Y-%m', Tarih);";

            var command = new SQLiteCommand(query, connection);
            var adapter = new SQLiteDataAdapter(command);
            var dataTable = new DataTable();
            adapter.Fill(dataTable);

            return dataTable;
        }
    }

   





}
