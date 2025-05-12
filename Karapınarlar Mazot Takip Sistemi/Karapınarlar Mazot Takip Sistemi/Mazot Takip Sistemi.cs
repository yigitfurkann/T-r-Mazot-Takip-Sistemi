using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Threading.Tasks;
using System.Windows.Forms;
using Karapınarlar_Mazot_Takip_Sistemi;
using System.IO;
using System.Data.SQLite;

namespace Karapınarlar_Mazot_Takip_Sistemi
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Veritabanını başlat
            DatabaseHelper.InitializeDatabase();

            // NumericUpDown sınırlarını ayarla
            nudBorc.Maximum = 10000000000; // Maksimum 10 milyar
            nudBorc.Minimum = 0;          // Minimum 0
            nudAlacak.Maximum = 10000000000; // Maksimum 10 milyar
            nudAlacak.Minimum = 0;          // Minimum 0

            // DataGridView'e verileri yükle
            LoadTransactions();
            LoadMonthlySummary();// İkinci GridView'e Verileri ekle
                                 // Kısayolu otomatik oluştur
            

            // Mevcut PictureBox'ı kullanarak resmi yükleyin
            try
            {
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory; // Uygulamanın çalıştırıldığı dizin
                string imagePath = Path.Combine(appDirectory, "Scaniak.png"); // Resim dosyasının yolu

                if (File.Exists(imagePath)) // Dosyanın varlığını kontrol et
                {
                    pictureBox1.Dock = DockStyle.Fill; // PictureBox'ı formun tamamına yay
                    pictureBox1.Image = System.Drawing.Image.FromFile(imagePath); // Resmi yükle
                    pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage; // Resmi tam olarak sığdır
                }
                else
                {
                    MessageBox.Show("Arka plan görseli bulunamadı. Lütfen dosyanın mevcut olduğundan emin olun.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Arka plan resmi yüklenirken bir hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // PictureBox'ı arka plana gönder
            pictureBox1.SendToBack(); // PictureBox'ı en alt katmana gönder

            // Diğer kontrollerin ön planda kalmasını sağlar
            foreach (Control control in this.Controls)
            {
                if (control != pictureBox1)
                {
                    control.BringToFront(); // Diğer kontrolleri öne getir
                }
            }
        }

        private void btnAddTransaction_Click(object sender, EventArgs e)
        {
            try
            {
                // Kullanıcıdan alınan veriler
                string tarih = dateTimePicker1.Value.ToString("dd.MM.yyyy HH:mm"); // Seçilen tarih ve saat
                string belgeNo = txtBelgeNo.Text.Trim();
                string aciklama = txtAciklama.Text.Trim();
                string plaka = txtPlaka.Text.Trim();
                decimal borc = nudBorc.Value;       // NumericUpDown
                decimal alacak = nudAlacak.Value;   // NumericUpDown

                // Boş alan kontrolü
                if (string.IsNullOrWhiteSpace(belgeNo))
                {
                    MessageBox.Show("Belge No alanı boş olamaz!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(aciklama))
                {
                    MessageBox.Show("Açıklama alanı boş olamaz!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(plaka))
                {
                    MessageBox.Show("Plaka alanı boş olamaz!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (borc == 0 && alacak == 0)
                {
                    MessageBox.Show("Borç ve Alacak alanlarından en az biri sıfırdan büyük olmalıdır!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Veritabanına ekleme
                DatabaseHelper.AddTransaction(tarih, belgeNo, aciklama, plaka, borc, alacak);
                MessageBox.Show("İşlem başarıyla eklendi.");

                // Kutucukları sıfırla
                txtBelgeNo.Text = "";              // TextBox boşalt
                txtAciklama.Text = "";             // TextBox boşalt
                txtPlaka.Text = "";                // TextBox boşalt
                nudBorc.Value = 0;                 // NumericUpDown sıfırla
                nudAlacak.Value = 0;               // NumericUpDown sıfırla
                dateTimePicker1.Value = DateTime.Now; // Tarihi bugüne ayarla

                // DataGridView'i güncelle
                LoadTransactions();
                LoadMonthlySummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Bir hata oluştu: " + ex.Message);
            }
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            // Bu metod şu an boş, özel bir işlem eklenmediyse kullanılmayabilir
        }

        private void btnListele_Click(object sender, EventArgs e)
        {
            // DataGridView'e verileri yükle
            LoadTransactions();
            LoadMonthlySummary();
        }

        // Verileri DataGridView'e yükleyen metot
        // Verileri DataGridView'e yükleyen metot
        private void LoadTransactions()
        {
            try
            {
                // Veritabanından işlemleri al
                DataTable transactions = DatabaseHelper.GetAllTransactions();

                // DataGridView'e bağla
                dgvTransactions.DataSource = transactions;

                // Sütun başlıklarını Türkçeleştir
                dgvTransactions.Columns["Tarih"].HeaderText = "Tarih ve Saat";
                dgvTransactions.Columns["BelgeNo"].HeaderText = "Belge No";
                dgvTransactions.Columns["Aciklama"].HeaderText = "Açıklama";
                dgvTransactions.Columns["Plaka"].HeaderText = "Plaka";
                dgvTransactions.Columns["Borc"].HeaderText = "Borç";
                dgvTransactions.Columns["Alacak"].HeaderText = "Alacak";
                dgvTransactions.Columns["GuncelBorc"].HeaderText = "Güncel Borç";

                // Borç, Alacak ve Güncel Borç sütunlarına format uygulama
                dgvTransactions.Columns["Borc"].DefaultCellStyle.Format = "N";
                dgvTransactions.Columns["Alacak"].DefaultCellStyle.Format = "N";
                dgvTransactions.Columns["GuncelBorc"].DefaultCellStyle.Format = "N";

                // Id sütununu gizle
                dgvTransactions.Columns["Id"].Visible = false;

                // Sütun genişliklerini otomatik ayarla
                dgvTransactions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

                // Alternatif: Belirli bir sütun için genişlik ayarı yapmak isterseniz
                // dgvTransactions.Columns["BelgeNo"].Width = 100;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Veriler yüklenirken bir hata oluştu: " + ex.Message);
            }
        }



        private void txtBelgeNo_TextChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void txtAciklama_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e) // Silme butonu
        {
            try
            {
                // DataGridView'de seçili satır kontrolü
                if (dgvTransactions.SelectedRows.Count > 0)
                {
                    // Seçili satırdan Id değerini al
                    int selectedRowIndex = dgvTransactions.SelectedRows[0].Index;
                    int id = Convert.ToInt32(dgvTransactions.Rows[selectedRowIndex].Cells["Id"].Value);

                    // Kullanıcıya silmek istediğini onaylat
                    DialogResult dialogResult = MessageBox.Show(
                        "Bu kaydı silmek istediğinizden emin misiniz?",
                        "Kayıt Silme",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning
                    );

                    if (dialogResult == DialogResult.Yes)
                    {
                        // Veritabanından silme işlemi
                        DatabaseHelper.DeleteTransaction(id);

                        // Mesaj ve GridView'leri güncelle
                        MessageBox.Show("Kayıt başarıyla silindi.");
                        LoadTransactions(); // İlk GridView'i güncelle
                        LoadMonthlySummary(); // İkinci GridView'i güncelle
                    }
                    else
                    {
                        MessageBox.Show("Kayıt silme işlemi iptal edildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Lütfen silmek istediğiniz bir satırı seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void ExportToPdf(string filePath)
        {
            try
            {
                if (dgvTransactions.Rows.Count == 0)
                {
                    MessageBox.Show("Tabloda görüntülenecek veri bulunamadı.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Kullanıcıya Dönem Toplamları eklemek isteyip istemediğini sor
                DialogResult result = MessageBox.Show(
                    "Dönem Toplamlarını da PDF'e eklemek ister misiniz?",
                    "PDF Oluşturma Seçimi",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                // Dizini kontrol et ve oluştur
                string directoryPath = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // PDF belge oluştur
                Document pdfDoc = new Document(PageSize.A4.Rotate(), 10f, 10f, 10f, 10f); // Yatay PDF
                PdfWriter.GetInstance(pdfDoc, new FileStream(filePath, FileMode.Create));
                pdfDoc.Open();

                // BaseFont ve Font tanımları
                var fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
                var baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                var titleFont = new iTextSharp.text.Font(baseFont, 15, iTextSharp.text.Font.BOLD);
                var cellFont = new iTextSharp.text.Font(baseFont, 10); // Küçük font

                // Başlık ekleme
                Paragraph title = new Paragraph("Hareket Dökümü", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20f
                };
                pdfDoc.Add(title);

                // İlk GridView'in verilerini PDF'e ekle
                PdfPTable transactionsTable = CreatePdfTable(dgvTransactions, cellFont);
                pdfDoc.Add(transactionsTable);

                // Kullanıcı Evet dediyse Dönem Toplamları GridView'ini de PDF'e ekle
                if (result == DialogResult.Yes && dgvMonthlySummary.Rows.Count > 0)
                {
                    Paragraph summaryTitle = new Paragraph("Dönem Toplamları", titleFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 20f
                    };
                    pdfDoc.Add(summaryTitle);

                    PdfPTable summaryTable = CreatePdfTable(dgvMonthlySummary, cellFont);
                    pdfDoc.Add(summaryTable);
                }

                // PDF belgesini kapat
                pdfDoc.Close();

                MessageBox.Show("PDF başarıyla oluşturuldu!", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Bir hata oluştu: " + ex.Message + "\n" + ex.StackTrace, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private PdfPTable CreatePdfTable(DataGridView gridView, iTextSharp.text.Font cellFont)
        {
            // Sütun sayısını "Id" sütunu hariç al
            int visibleColumnCount = gridView.Columns.Cast<DataGridViewColumn>()
                                    .Count(column => column.Visible && column.Name != "Id");

            PdfPTable table = new PdfPTable(visibleColumnCount)
            {
                WidthPercentage = 100,
                SpacingBefore = 10f,
                SpacingAfter = 20f
            };

            // Sütun genişlik oranlarını belirle
            float[] columnWidths;
            if (gridView == dgvTransactions) // Birinci GridView
            {
                columnWidths = new float[] { 3f, 3f, 5f, 3f, 2f, 2f, 2f }; // Tarih, BelgeNo, Açıklama, Plaka, Borç, Alacak, Güncel Borç
            }
            else // İkinci GridView
            {
                columnWidths = new float[] { 4f, 2f, 2f, 2f }; // Dönem, Borç, Alacak, Fark
            }
            table.SetWidths(columnWidths);

            // Sütun başlıklarını ekle
            foreach (DataGridViewColumn column in gridView.Columns)
            {
                if (column.Visible && column.Name != "Id") // "Id" sütununu hariç tut
                {
                    PdfPCell headerCell = new PdfPCell(new Phrase(column.HeaderText, cellFont))
                    {
                        BackgroundColor = BaseColor.LIGHT_GRAY,
                        HorizontalAlignment = Element.ALIGN_CENTER
                    };
                    table.AddCell(headerCell);
                }
            }

            // Verileri tabloya ekle
            foreach (DataGridViewRow row in gridView.Rows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cell.OwningColumn.Visible && cell.OwningColumn.Name != "Id") // "Id" sütununu hariç tut
                    {
                        string cellValue = cell.Value?.ToString() ?? "";

                        // Sayısal verileri biçimlendirme
                        if (cell.OwningColumn.Name == "Borc" || cell.OwningColumn.Name == "Alacak" || cell.OwningColumn.Name == "GuncelBorc" || cell.OwningColumn.Name == "Fark")
                        {
                            if (decimal.TryParse(cellValue, out decimal value))
                            {
                                cellValue = value.ToString("N2"); // Virgül ve nokta ile formatlama
                            }
                            else
                            {
                                cellValue = "";
                            }
                        }

                        // Hizalamaya göre hücre ayarları
                        PdfPCell pdfCell = new PdfPCell(new Phrase(cellValue, cellFont))
                        {
                            HorizontalAlignment = GetCellAlignment(cell.OwningColumn.Name),
                            Padding = 5f // Hücre içi boşluk
                        };

                        table.AddCell(pdfCell);
                    }
                }
            }

            return table;
        }

        // Sütun adına göre hizalama belirleme
        private int GetCellAlignment(string columnName)
        {
            // Sayısal sütunlar sağa hizalanır
            if (columnName == "Borc" || columnName == "Alacak" || columnName == "GuncelBorc" || columnName == "Fark")
            {
                return Element.ALIGN_RIGHT;
            }
            // Diğer sütunlar sola hizalanır
            return Element.ALIGN_LEFT;
        }


       

        // BİR AYDAKİLERİ BİR SAYFAYA SIĞDIRMAYA ÇALIŞ 


        private void btnExportToPdf_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "PDF Dosyaları (*.pdf)|*.pdf",
                Title = "PDF Olarak Kaydet"
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                ExportToPdf(saveFileDialog.FileName);
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void dgvMonthlySummary_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }


        private void LoadMonthlySummary()
        {
            try
            {
                // Aylık toplamları saklamak için DataTable oluştur
                DataTable monthlySummary = new DataTable();
                monthlySummary.Columns.Add("Dönem", typeof(string)); // Dönem sütunu
                monthlySummary.Columns.Add("Borç", typeof(decimal)); // Borç sütunu
                monthlySummary.Columns.Add("Alacak", typeof(decimal)); // Alacak sütunu
                monthlySummary.Columns.Add("Fark", typeof(decimal)); // Fark sütunu

                // İlk GridView'deki verileri gruplandır
                var groupedData = dgvTransactions.Rows
                    .Cast<DataGridViewRow>()
                    .Where(row => row.Cells["Tarih"].Value != null) // Tarihi olan satırları al
                    .GroupBy(row => DateTime.Parse(row.Cells["Tarih"].Value.ToString()).ToString("MM.yyyy")) // Ay ve yıl formatında gruplandır
                    .OrderBy(g => g.Key); // Ay sırasına göre sırala

                int aySayac = 1; // Ay sıralaması için sayaç
                decimal toplamBorc = 0, toplamAlacak = 0, toplamFark = 0; // Toplamlar için değişkenler

                foreach (var group in groupedData)
                {
                    // Grup verilerini hesapla
                    decimal totalBorc = group.Sum(row => Convert.ToDecimal(row.Cells["Borc"].Value));
                    decimal totalAlacak = group.Sum(row => Convert.ToDecimal(row.Cells["Alacak"].Value));
                    decimal fark = totalBorc - totalAlacak;

                    // Toplamları artır
                    toplamBorc += totalBorc;
                    toplamAlacak += totalAlacak;
                    toplamFark += fark;

                    // "Dönem" sütununda 1. Ay, 2. Ay şeklinde gösterim
                    monthlySummary.Rows.Add($"{aySayac}. Ay", totalBorc, totalAlacak, fark);

                    aySayac++; // Sayaç artır
                }

                // Toplamları DataTable'a ekle (Son satır olarak)
                monthlySummary.Rows.Add("TOPLAM", toplamBorc, toplamAlacak, toplamFark);

                // İkinci GridView'e bağla
                dgvMonthlySummary.DataSource = monthlySummary;

                // Sütun genişliklerini otomatik ayarla
                dgvMonthlySummary.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                // Sütun başlıklarını Türkçeleştir
                dgvMonthlySummary.Columns["Dönem"].HeaderText = "Dönem Toplamları";
                dgvMonthlySummary.Columns["Borç"].HeaderText = "Borç";
                dgvMonthlySummary.Columns["Alacak"].HeaderText = "Alacak";
                dgvMonthlySummary.Columns["Fark"].HeaderText = "Fark";

                // Sayısal sütunlara format uygula
                dgvMonthlySummary.Columns["Borç"].DefaultCellStyle.Format = "N2";
                dgvMonthlySummary.Columns["Alacak"].DefaultCellStyle.Format = "N2";
                dgvMonthlySummary.Columns["Fark"].DefaultCellStyle.Format = "N2";

                // "TOPLAM" satırını görsel olarak farklılaştırmak (isteğe bağlı)
                foreach (DataGridViewRow row in dgvMonthlySummary.Rows)
                {
                    if (row.Cells[0].Value?.ToString() == "TOPLAM")
                    {
                        row.DefaultCellStyle.Font = new System.Drawing.Font(dgvMonthlySummary.Font, System.Drawing.FontStyle.Regular);
                        row.DefaultCellStyle.BackColor = System.Drawing.Color.LightGray;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Aylık özet yüklenirken bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }





        private void dgvTransactions_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void button1_Click_1(object sender, EventArgs e)// Tüm kaydı silmek 
        {
            try
            {
                // Kullanıcıdan onay al
                DialogResult result = MessageBox.Show(
                    "TÜM KAYITLAR SİLİNECEK ONAYLIYOR MUSUNUZ?",
                    "Onay",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                if (result == DialogResult.Yes)
                {
                    using (var connection = new SQLiteConnection($"Data Source={DatabaseHelper.GetDbPath()};Version=3;"))
                    {
                        connection.Open();

                        // Tüm kayıtları sil
                        string deleteQuery = "DELETE FROM Islemler;";
                        var command = new SQLiteCommand(deleteQuery, connection);

                        int rowsAffected = command.ExecuteNonQuery();
                        MessageBox.Show($"{rowsAffected} kayıt başarıyla silindi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    // Silme işleminden sonra DataGridView'i güncelle
                    LoadTransactions(); // Bu metodun Form1 içinde tanımlı olduğundan emin olun.
                    LoadMonthlySummary(); // İkinci GridView'i güncelle
                }
                else
                {
                    MessageBox.Show("Silme işlemi iptal edildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Bir hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


    }
}
