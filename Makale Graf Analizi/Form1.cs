using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Linq;                     // OrderBy, ToList, All, etc.
using Timer = System.Windows.Forms.Timer;  // Timer adını netleştir

namespace Makale_Graf_Analizi
{
    public partial class Form1 : Form
    {
        // --- BU EKSİK SATIRI BURAYA EKLE ---
        HashSet<string> sonEklenenHCoreIds = new HashSet<string>();
        // -------------------------------------
        Makale odaklanilanMakale = null; // null ise her şeyi çiz, doluysa sadece onu ve komşularını çiz
        // --- Görünüm (kamera) ---
        float zoom = 1f;
        readonly float minZoom = 0.1f, maxZoom = 25f;   // çok daha fazla yakınlaştırma/uzaklaştırma
        int nodeRadius = 8;  // px – ArrangeOnCircle içinde dinamik hesaplayacağız

        PointF pan = new PointF(0, 0);   // ekran ofseti (world->screen)

        Point lastMouse;
        bool panning = false;

        ToolTip tt = new ToolTip();
        Makale hoverNode = null;

        Timer layoutTimer = new Timer();   // (artık Forms.Timer)
        bool layoutRunning = false;

        bool focusMode = false;              // ilk tıkla odak moduna geçiyoruz
        HashSet<string> expandedSeeds = new HashSet<string>(); // hangi düğümlerden genişlettik (opsiyonel)


        string sabitGenelBilgi = "";
        GraphManager manager = new GraphManager();
        Makale seciliMakale = null;
        Random rnd = new Random();

        public Form1()
        {
            InitializeComponent();
            layoutTimer.Interval = 16; // yaklaşık 60 FPS
            layoutTimer.Tick += (s, e) => ForceLayoutStep();

            //---BU İKİ SATIRI EKLE-- -
            // 1. Resize olayını kodla bağla
            this.Resize += new EventHandler(Form1_Resize);

            // 2. PictureBox her yöne uzasın
            pbGraf.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        }
        // Görünür (çizilecek) düğümler
        HashSet<string> visibleIds = new HashSet<string>();

        // İstatistikleri görünür grafa göre yaz
        // Bu metodu Form1.cs içinde bul ve tamamen bununla değiştir:
        private void UpdateStats()
        {
            // Eğer hiç görünür düğüm yoksa bilgi ver
            if (visibleIds.Count == 0)
            {
                lblBilgi.Text = "Grafik boş.";
                return;
            }

            // 1. Temel Sayımlar
            int nodeCount = visibleIds.Count;
            int totalGiven = 0;
            int totalReceived = 0;

            // Sadece görünür düğümleri alıp listeye çeviriyoruz
            var visibleNodes = manager.Makaleler.Values
                .Where(v => visibleIds.Contains(v.Id))
                .ToList();

            // 2. İstatistikleri Hesapla
            // Bu yöntemle "Count" hatası almazsın çünkü anonim tip (new { ... }) kullanıyoruz.
            var istatistikler = visibleNodes.Select(m => new
            {
                Makale = m,

                // Bu makaleye atıf yapanlar (visibleNodes içinde arıyoruz)
                // Yani: Ekranda görünenlerden kaçı buna ok çıkarıyor?
                GorunurAtif = visibleNodes.Count(x => x.ReferencedWorks.Contains(m.Id)),

                // Bu makalenin atıf yaptıkları (visibleIds içinde olanlar)
                // Yani: Bu makaleden ekranda görünenlere kaç ok çıkıyor?
                GorunurReferans = m.ReferencedWorks.Count(t => visibleIds.Contains(t))
            }).ToList();

            // Toplamları hesapla
            totalGiven = istatistikler.Sum(x => x.GorunurReferans);
            totalReceived = istatistikler.Sum(x => x.GorunurAtif);

            // Yönlü grafta toplam kenar sayısı = toplam verilen referans sayısıdır
            int edgeCount = totalGiven;

            // En iyileri bul (Null kontrolü ile)
            var enCokAtifAlanObj = istatistikler.OrderByDescending(x => x.GorunurAtif).FirstOrDefault();
            var enCokRefVerenObj = istatistikler.OrderByDescending(x => x.GorunurReferans).FirstOrDefault();

            // Değerleri al (Yoksa tire "-" koy)
            string enCokAtifId = enCokAtifAlanObj?.Makale.Id.Replace("https://openalex.org/", "") ?? "-";
            int enCokAtifSayi = enCokAtifAlanObj?.GorunurAtif ?? 0;

            string enCokRefId = enCokRefVerenObj?.Makale.Id.Replace("https://openalex.org/", "") ?? "-";
            int enCokRefSayi = enCokRefVerenObj?.GorunurReferans ?? 0;

            // 3. Ekrana Yaz (DOĞRUDAN LABEL İSMİYLE)
            string bilgi =
                $"--- İSTATİSTİKLER (Görünür) ---\n" +
                $"Düğüm Sayısı: {nodeCount}\n" +
                $"Siyah Kenar: {edgeCount}\n" +
                $"Toplam Verilen Ref.: {totalGiven}\n" +
                $"Toplam Alınan Ref.: {totalReceived}\n\n" +
                $"EN ÇOK REF. ALAN:\n{enCokAtifId}\n(Sayısı: {enCokAtifSayi})\n\n" +
                $"EN ÇOK REF. VEREN:\n{enCokRefId}\n(Sayısı: {enCokRefSayi})";


            // HATA BURADAYDI: Controls["lblBilgi"] yerine direkt bunu kullanıyoruz:
            lblBilgi.Text = bilgi;
        }

        // World (graf koordinatı) -> Screen (piksel) dönüşümü
        private PointF ToScreen(PointF w) => new PointF(pan.X + w.X * zoom, pan.Y + w.Y * zoom);

        // Screen -> World
        private PointF ToWorld(PointF s) => new PointF((s.X - pan.X) / zoom, (s.Y - pan.Y) / zoom);

        private void ExpandFromNode(Makale node)
        {
            visibleIds.Add(node.Id);

            // out: node -> referenced
            foreach (var refId in node.ReferencedWorks)
                if (manager.Makaleler.ContainsKey(refId))
                    visibleIds.Add(refId);

            // in: citing -> node
            foreach (var m in manager.Makaleler.Values)
                if (m.ReferencedWorks.Contains(node.Id))
                    visibleIds.Add(m.Id);
        }


        // Dosya Yükleme Butonu
        private void btnYukle_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "JSON Files|*.json";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                manager.DosyaYukle(ofd.FileName);

                // Başlangıçta hepsini göster (veya ilk 10'u)
                visibleIds = new HashSet<string>(manager.Makaleler.Keys);
                // visibleIds = new HashSet<string>(manager.Makaleler.Keys.Take(10)); // Sadece 10 tane ile başla (İsteğe bağlı)

                // Odaklanma modunu sıfırla
                focusMode = false;
                seciliMakale = null;
                odaklanilanMakale = null;
                sonEklenenHCoreIds.Clear();

                ArrangeOnCircle();

                // ÖNCE ÇİZ, SONRA İSTATİSTİK YAZ
                CizimYap();
                UpdateStats(); // <-- Burası artık dolu visibleIds ile çalışacak

                pbGraf.Focus();
            }
        }

        /*private void KonumlariAta()
        {
            if (pbGraf.Width == 0 || pbGraf.Height == 0) return;
            foreach (var m in manager.Makaleler.Values)
            {
                m.Location = new PointF(rnd.Next(50, pbGraf.Width - 50), rnd.Next(50, pbGraf.Height - 50));
            }
        }*/

        private void CizimYap()
        {
            if (pbGraf.Width == 0 || pbGraf.Height == 0) return;

            Bitmap bmp = new Bitmap(pbGraf.Width, pbGraf.Height);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

                // --- KALEMLER ve FONTLAR ---
                Pen kalem = new Pen(Color.FromArgb(70, Color.DarkSlateGray), 2);
                Pen kirmiziKalem = new Pen(Color.Red, 2);

                // Fontlar (Fotodaki gibi)
                Font fontSayi = new Font("Arial", 9, FontStyle.Bold);
                Font fontYazi = new Font("Arial", 7, FontStyle.Regular);

                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                // --- RENKLER (Pastel Tonlar - Fotodaki Gibi) ---
                Brush fircaSecili = new SolidBrush(Color.FromArgb(255, 165, 0)); // Turuncu (Seçili)
                Brush fircaCore = new SolidBrush(Color.FromArgb(255, 200, 200));   // Açık Kırmızı/Pembe (H-Core)
                Brush fircaNormal = new SolidBrush(Color.FromArgb(220, 220, 220)); // Gri (Diğerleri)
                Brush fircaMavi = new SolidBrush(Color.DeepSkyBlue); // İstersen mavi de kullanabilirsin
                Brush fircaYeni = new SolidBrush(Color.FromArgb(180, 160, 255)); // morumsu (yeni eklenen)

                // Yarıçapı sabitle (Yazı sığsın diye)
                int r = Math.Max(nodeRadius, 20);

                // --- YEŞİL YÖNLÜ BAĞLANTILAR (OKLAR DÜZELTİLDİ) ---
                using (var yesil = new Pen(Color.FromArgb(140, 0, 160, 0), 2))
                {
                    var sirali = manager.Makaleler.Values
                        .Where(m => visibleIds.Contains(m.Id))
                        .OrderBy(m => m.Id, StringComparer.Ordinal)
                        .ToList();

                    for (int i = 0; i < sirali.Count - 1; i++)
                    {
                        var p1 = ToScreen(sirali[i].Location);
                        var p2 = ToScreen(sirali[i + 1].Location);

                        // DÜZELTME: Oku merkeze (p2) değil, dairenin kenarına çizdiriyoruz.
                        // Böylece ok ucu dairenin altında kalmıyor.
                        PointF p2Kenar = GetCircleEdge(p1, p2, r);

                        DrawDirectedEdge(g, yesil, p1, p2Kenar);
                    }
                }

                // --- SİYAH/KIRMIZI REFERANS KENARLARI ---
                foreach (var makale in manager.Makaleler.Values.Where(m => visibleIds.Contains(m.Id)))
                {
                    foreach (var refId in makale.ReferencedWorks)
                    {
                        if (!visibleIds.Contains(refId)) continue;
                        var hedef = manager.Makaleler[refId];
                        bool isCoreEdge = makale.IsHCore && hedef.IsHCore;



                        var p1 = ToScreen(makale.Location);
                        var p2 = ToScreen(hedef.Location);

                        // DÜZELTME: Siyah oklar da dairenin kenarında bitsin
                        PointF p2Kenar = GetCircleEdge(p1, p2, r);

                        DrawDirectedEdge(g, isCoreEdge ? kirmiziKalem : kalem, p1, p2Kenar);
                    }
                }

                // --- DÜĞÜMLER ---
                foreach (var makale in manager.Makaleler.Values.Where(m => visibleIds.Contains(m.Id)))
                {
                    var s = ToScreen(makale.Location);
                    float x = s.X - r;
                    float y = s.Y - r;

                    // Rengi Belirle
                    Brush firca = fircaNormal;


                    // Öncelik: seçili > yeni eklenen > h-core > normal
                    if (makale == seciliMakale)
                        firca = fircaSecili;
                    else if (sonEklenenHCoreIds.Contains(makale.Id))
                        firca = fircaYeni;
                    else if (makale.IsHCore)
                        firca = fircaMavi;


                    // Daireyi Çiz
                    g.FillEllipse(firca, x, y, 2 * r, 2 * r);
                    g.DrawEllipse(Pens.DimGray, x, y, 2 * r, 2 * r);

                    // Yazıları Yaz
                    string kisaId = makale.Id.Replace("https://openalex.org/", "");
                    if (kisaId.Length > 8) kisaId = kisaId.Substring(0, 7) + "..";

                    RectangleF rectUst = new RectangleF(x, y + (r * 0.2f), 2 * r, r);
                    RectangleF rectAlt = new RectangleF(x, y + (r * 0.9f), 2 * r, r);

                    g.DrawString(makale.CitationCount.ToString(), fontSayi, Brushes.Black, rectUst, sf);
                    g.DrawString(kisaId, fontYazi, Brushes.DarkSlateGray, rectAlt, sf);
                }
            }
            pbGraf.Image = bmp;
        }

        // --- YARDIMCI METOT: Dairenin Kenar Noktasını Bulur ---
        private PointF GetCircleEdge(PointF p1, PointF p2, float radius)
        {
            float dx = p2.X - p1.X;
            float dy = p2.Y - p1.Y;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);

            if (distance == 0) return p2;

            // Hedef noktadan (p2) geriye doğru radius kadar git
            float ratio = radius / distance;
            return new PointF(p2.X - dx * ratio, p2.Y - dy * ratio);
        }


        private void btnSifirla_Click(object sender, EventArgs e)
        {
            // 1. Seçili olan (Turuncu) makaleyi iptal et
            seciliMakale = null;

            // 2. Tüm makalelerin "Core" (Mavi) işaretini kaldır
            foreach (var makale in manager.Makaleler.Values)
            {
                makale.IsHCore = false;
            }

            // 3. Bilgi ekranını temizle veya başlangıç bilgisini yaz
            if (Controls.ContainsKey("lblBilgi"))
            {
                string baslangicBilgisi = $"Toplam Makale: {manager.ToplamMakale}\n" +
                                          $"Toplam Referans: {manager.ToplamReferans}\n\n" +
                                          "Grafik sıfırlandı.";
                Controls["lblBilgi"].Text = baslangicBilgisi;
            }
            // Seçili düğüm bilgisini temizle
            lblSeciliDugum.Text = "";

            // 4. K değerini temizle (İsteğe bağlı)
            if (Controls.ContainsKey("txtK"))
                Controls["txtK"].Text = "";

            // 5. Grafiği tertemiz, gri haliyle yeniden çiz
            CizimYap();
        }

        private void pbGraf_MouseClick(object sender, MouseEventArgs e)
        {
            Makale tiklanan = TiklananBul(e.Location);
            if (tiklanan == null) return;

            seciliMakale = tiklanan;

            // 1) İlk tık: grafı bu node'a özelleştir (reset)
            if (!focusMode)
            {
                focusMode = true;
                visibleIds.Clear();
            }

            // --- BU TIKTAN ÖNCEKİ GÖRÜNÜR SETİ SAKLA ---
            var before = new HashSet<string>(visibleIds);

            // 2) Tıklanan düğümü ekle
            visibleIds.Add(tiklanan.Id);

            // 3) SADECE H-CORE OLAN KOMŞULARI EKLE
            // Tıklananın referans verdiği H-Core'lar
            foreach (var refId in tiklanan.ReferencedWorks)
            {
                if (manager.Makaleler.ContainsKey(refId))
                {
                    var hedef = manager.Makaleler[refId];
                    if (hedef.IsHCore)
                    {
                        visibleIds.Add(refId);
                    }
                }
            }

            // Tıklanana atıf yapan H-Core'lar
            foreach (var m in manager.Makaleler.Values)
            {
                if (m.ReferencedWorks.Contains(tiklanan.Id) && m.IsHCore)
                {
                    visibleIds.Add(m.Id);
                }
            }

            // 4) H metriklerini hesapla ve h-core listesini ekle
            var sonuc = manager.HesaplaHMetrikleri(tiklanan.Id);

            // H-Core listesindeki düğümleri de ekle
            foreach (var m in sonuc.hCore)
                visibleIds.Add(m.Id);

            // --- YENİ EKLENENLERİ HESAPLA ---
            sonEklenenHCoreIds.Clear();
            foreach (var id in visibleIds)
            {
                if (!before.Contains(id))
                    sonEklenenHCoreIds.Add(id);
            }
            sonEklenenHCoreIds.Remove(tiklanan.Id);

            // Bilgi panelini güncelle
            string yazarGosterim;
            if (tiklanan.Authors != null && tiklanan.Authors.Count > 0)
            {
                if (tiklanan.Authors.Count <= 2)
                    yazarGosterim = string.Join(", ", tiklanan.Authors);
                else
                    yazarGosterim = string.Join(", ", tiklanan.Authors.Take(2)) + $" ... (+{tiklanan.Authors.Count - 2})";
            }
            else yazarGosterim = "(Yazar yok)";

            lblSeciliDugum.Text =
                $"=== SEÇİLEN DÜĞÜM ===\n" +
                $"ID: {tiklanan.Id.Replace("https://openalex.org/", "")}\n" +
                $"Yıl: {tiklanan.Year}\n" +
                $"Yazarlar: {yazarGosterim}\n" +
                $"Atıf: {tiklanan.CitationCount}\n" +
                $"H-Index: {sonuc.hIndex}\n" +
                $"H-Median: {sonuc.hMedian}";

            // 5) Yerleşim + çizim
            ArrangeOnCircleVisible();
            CizimYap();
            UpdateStats();
        }






        // Form1.cs içinde bu metodu bul ve güncelle:
        private Makale TiklananBul(Point screenP)
        {
            // Tıklama hassasiyetini çizilen dairenin boyutuna göre ayarla
            // En az 10 piksel olsun ki çok küçükken de tıklanabilsin
            float hitRadius = Math.Max(nodeRadius, 10);

            var worldP = ToWorld(screenP);

            foreach (var m in manager.Makaleler.Values)
            {
                if (!visibleIds.Contains(m.Id)) continue;

                float dx = worldP.X - m.Location.X;
                float dy = worldP.Y - m.Location.Y;

                // Pisagor: (dx^2 + dy^2) <= (r / zoom)^2
                // Zoom yapıldığında da doğru algılaması için formül:
                if ((dx * dx + dy * dy) <= Math.Pow(hitRadius / zoom, 2))
                    return m;
            }
            return null;
        }



        // --- Form1.cs İçine ---

        private void btnAnaliz_Click(object sender, EventArgs e)
        {
            // --- MEVCUT K-CORE KODLARI (Buraları elleme) ---
            // 1. Kutudaki sayıyı al
            if (!int.TryParse(txtK.Text, out int k))
            {
                MessageBox.Show("Lütfen geçerli bir sayı giriniz.");
                return;
            }

            // 2. Algoritmayı çalıştır (K-Core)
            List<string> kCoreIds = manager.KCoreHesapla(k);

            // 3. Görselleştirme Ayarlarını Yap (Mavi/Gri boyama)
            foreach (var m in manager.Makaleler.Values)
            {
                if (kCoreIds.Contains(m.Id))
                    m.IsHCore = true;
                else
                    m.IsHCore = false;
            }

            // Filtreleme (İsteğe bağlı, sadece K-Core kalsın diyorsan):
            visibleIds.Clear();
            foreach (var id in kCoreIds) visibleIds.Add(id);

            // 4. Grafiği Yenile
            CizimYap();
            UpdateStats();


            // 5. K-Core Bilgisini Yazdır
            string kCoreBilgi = $"--- K-CORE ANALİZİ (k={k}) ---\n" +
                                $"Kalan Düğüm Sayısı: {kCoreIds.Count}\n" +
                                "Grafikte MAVİ renkli düğümler K-Core kümesidir.\n\n";

            MessageBox.Show(kCoreBilgi, "K-Core");
            UpdateStats();



            // --- YENİ EKLEYECEĞİN BETWEENNESS CENTRALITY KISMI ---
            // Buraya yapıştır:

            var bcSonuclar = manager.CalculateBetweennessCentrality();

            // İlk 5'i al
            var enYuksekBes = bcSonuclar.OrderByDescending(x => x.Value).Take(5);

            string bcMesaj = "--- En Yüksek Betweenness Centrality ---\n";
            foreach (var item in enYuksekBes)
            {
                // ID'nin sadece son kısmını göstermek daha okunaklı olabilir
                string kisaId = item.Key.Replace("https://openalex.org/", "");
                bcMesaj += $"ID: {kisaId} - Skor: {item.Value:F2}\n";
            }

            // İstersen MessageBox ile göster:
            MessageBox.Show(bcMesaj, "Betweenness Centrality Sonuçları");

            // VEYA (Daha şık) lblBilgi'ye ekle:
            if (Controls.ContainsKey("lblBilgi"))
            {
                Controls["lblBilgi"].Text += "\n" + bcMesaj;
            }
        }
        private void ArrangeOnCircle()
        {
            if (manager.Makaleler.Count == 0) return;

            var nodes = manager.Makaleler.Values.OrderBy(m => m.Id, StringComparer.Ordinal).ToList();
            int N = nodes.Count;

            // Ekranın boyuna göre hedef çember büyüklüğü (ekranda rahat görünsün)
            float targetScreenR = 0.45f * Math.Min(pbGraf.Width, pbGraf.Height);

            // Komşular birbirine değsin istiyorsak:
            // chord length c = 2 * R * sin(pi/N) ≈ 2*nodeRadius  => nodeRadius ≈ R * sin(pi/N)
            // Buradan nodeRadius'i hedef ekran yarıçapına göre çıkarıp sınırlarız
            int computedRadius = (int)(targetScreenR * Math.Sin(Math.PI / N));
            nodeRadius = Math.Max(4, Math.Min(22, computedRadius));   // 4..22 px aralığı

            // Şimdi world uzayındaki çember yarıçapını buna göre ayarla (zoom=1 kabulüyle)
            float worldR = nodeRadius / (float)Math.Sin(Math.PI / N);

            double step = (2 * Math.PI) / N;
            for (int i = 0; i < N; i++)
            {
                double a = i * step;
                float x = (float)(worldR * Math.Cos(a));
                float y = (float)(worldR * Math.Sin(a));
                nodes[i].Location = new PointF(x, y);
                // varsa hız sıfırlama
                nodes[i].Velocity = new PointF(0, 0);
            }

            // ekran ortasına hizala ve başlangıç zoom'u 1
            pan = new PointF(pbGraf.Width / 2f, pbGraf.Height / 2f);
            zoom = 1f;
        }

        private void ArrangeOnCircleVisible()
        {
            if (visibleIds.Count == 0) return;

            var nodes = manager.Makaleler.Values
                .Where(m => visibleIds.Contains(m.Id))
                .OrderBy(m => m.Id, StringComparer.Ordinal)
                .ToList();

            int N = nodes.Count;
            if (N == 0) return;

            // Ekrana göre hedef yarıçap
            float targetScreenR = 0.45f * Math.Min(pbGraf.Width, pbGraf.Height);

            // --- ÖZEL DURUM: N == 1 ---
            if (N == 1)
            {
                nodeRadius = 20;
                nodes[0].Location = new PointF(0, 0);   // world merkez
                return;
            }

            // --- ÖZEL DURUM: N == 2 ---
            if (N == 2)
            {
                nodeRadius = 20;
                float d = 3 * nodeRadius; // aralarını aç
                nodes[0].Location = new PointF(-d, 0);
                nodes[1].Location = new PointF(+d, 0);
                return;
            }

            // --- NORMAL DURUM: N >= 3 ---
            float s = (float)Math.Sin(Math.PI / N);
            if (Math.Abs(s) < 0.0001f) s = 0.0001f; // ekstra güvenlik

            nodeRadius = Math.Max(4, Math.Min(22, (int)(targetScreenR * s)));
            float worldR = nodeRadius / s;

            double step = (2 * Math.PI) / N;
            for (int i = 0; i < N; i++)
            {
                double a = i * step;
                nodes[i].Location = new PointF(
                    (float)(worldR * Math.Cos(a)),
                    (float)(worldR * Math.Sin(a))
                );
            }
        }





        private void ForceLayoutStep()
        {
            if (!layoutRunning || manager.Makaleler.Count == 0) return;

            float area = pbGraf.Width * pbGraf.Height;
            float k = (float)Math.Sqrt(area / (manager.Makaleler.Count + 1));
            float temp = 10f;
            float repulsion = 0.5f;
            float attraction = 0.01f;

            var nodes = manager.Makaleler.Values.ToList();

            // itme
            for (int i = 0; i < nodes.Count; i++)
            {
                var vi = nodes[i];
                PointF disp = new PointF(0, 0);
                for (int j = 0; j < nodes.Count; j++)
                {
                    if (i == j) continue;
                    var vj = nodes[j];
                    float dx = vi.Location.X - vj.Location.X;
                    float dy = vi.Location.Y - vj.Location.Y;
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy) + 0.01f;
                    float force = repulsion * (k * k / dist);
                    disp.X += (dx / dist) * force;
                    disp.Y += (dy / dist) * force;
                }
                vi.Velocity = new PointF(vi.Velocity.X + disp.X, vi.Velocity.Y + disp.Y);
            }

            // çekme
            foreach (var a in manager.Makaleler.Values)
            {
                foreach (var refId in a.ReferencedWorks)
                {
                    if (!manager.Makaleler.ContainsKey(refId)) continue;
                    var b = manager.Makaleler[refId];
                    float dx = a.Location.X - b.Location.X;
                    float dy = a.Location.Y - b.Location.Y;
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy) + 0.01f;
                    float force = attraction * (dist * dist / k);
                    float fx = (dx / dist) * force;
                    float fy = (dy / dist) * force;

                    a.Velocity = new PointF(a.Velocity.X - fx, a.Velocity.Y - fy);
                    b.Velocity = new PointF(b.Velocity.X + fx, b.Velocity.Y + fy);
                }
            }

            // hız uygula + sürtünme
            foreach (var v in manager.Makaleler.Values)
            {
                float speed = (float)Math.Sqrt(v.Velocity.X * v.Velocity.X + v.Velocity.Y * v.Velocity.Y);
                if (speed > temp)
                    v.Velocity = new PointF(v.Velocity.X * temp / speed, v.Velocity.Y * temp / speed);

                v.Location = new PointF(
                    Math.Clamp(v.Location.X + v.Velocity.X, 30, pbGraf.Width - 30),
                    Math.Clamp(v.Location.Y + v.Velocity.Y, 30, pbGraf.Height - 30)
                );

                v.Velocity = new PointF(v.Velocity.X * 0.85f, v.Velocity.Y * 0.85f);
            }

            // hareket durduysa
            if (nodes.All(n => Math.Abs(n.Velocity.X) < 0.05 && Math.Abs(n.Velocity.Y) < 0.05))
                layoutTimer.Stop();

            CizimYap();
        }
        // Yardımcı: yönlü kenar çizimi (referans veren -> verilen)
        private void DrawDirectedEdge(Graphics g, Pen pen, PointF p1, PointF p2)
        {
            g.DrawLine(pen, p1, p2);

            // zoom çok küçükse ok çizme
            if (zoom < 0.5f) return;

            const float head = 8f;  // ok ucu boyu (px)
            var dx = p2.X - p1.X;
            var dy = p2.Y - p1.Y;
            var len = MathF.Sqrt(dx * dx + dy * dy);
            if (len < 0.001f) return;

            var ux = dx / len;
            var uy = dy / len;

            // iki kanat (ok ucu)
            var left = new PointF(p2.X - head * (ux + uy / 2f), p2.Y - head * (uy - ux / 2f));
            var right = new PointF(p2.X - head * (ux - uy / 2f), p2.Y - head * (uy + ux / 2f));

            g.DrawLine(pen, p2, left);
            g.DrawLine(pen, p2, right);
        }



        // Formun boyutu değiştiğinde bu çalışacak
        private void Form1_Resize(object sender, EventArgs e)
        {
            if (manager.Makaleler.Count == 0 || this.WindowState == FormWindowState.Minimized) return;

            // sadece ekran ortasını güncelle
            pan = new PointF(pbGraf.Width / 2f, pbGraf.Height / 2f);
            CizimYap();
        }

        private void pbGraf_MouseWheel(object sender, MouseEventArgs e)
        {
            // imleç altı sabit kalsın
            var worldBefore = ToWorld(e.Location);

            // Ctrl basılıysa daha agresif zoom
            float step = (ModifierKeys & Keys.Control) == Keys.Control ? 1.4f : 1.2f;
            float factor = (e.Delta > 0) ? step : 1f / step;

            float newZoom = Math.Max(minZoom, Math.Min(maxZoom, zoom * factor));
            zoom = newZoom;

            // zoom merkezini koru
            var screenAfter = ToScreen(worldBefore);
            pan.X += (e.Location.X - screenAfter.X);
            pan.Y += (e.Location.Y - screenAfter.Y);

            CizimYap();
        }


        // 1. MOUSE BASILINCA (TUTMA)
        private void pbGraf_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                panning = true;       // Sürükleme başladı
                lastMouse = e.Location; // Nereden tuttuk?
                pbGraf.Cursor = Cursors.Hand; // İmleç el işareti olsun
            }
        }

        // 2. MOUSE BIRAKILINCA (BIRAKMA)
        private void pbGraf_MouseUp(object sender, MouseEventArgs e)
        {
            panning = false;          // Sürükleme bitti
            pbGraf.Cursor = Cursors.Default; // İmleç normale dönsün
        }

        // 3. MOUSE HAREKET EDİNCE (HEM SÜRÜKLEME HEM BİLGİ GÖSTERME)
        private void pbGraf_MouseMove(object sender, MouseEventArgs e)
        {
            // A. Eğer mouse basılıysa SÜRÜKLE (Pan)
            if (panning)
            {
                // Ne kadar hareket ettik?
                float deltaX = e.X - lastMouse.X;
                float deltaY = e.Y - lastMouse.Y;

                pan.X += deltaX;
                pan.Y += deltaY;

                lastMouse = e.Location; // Yeni konumu hatırla

                tt.RemoveAll(); // Sürüklerken yazıları gizle (Kasma yapmasın)
                CizimYap();     // Ekranı kaymış haliyle çiz
                return;         // Buradan çık, aşağıya (bilgi göstermeye) gitme
            }


            // B. Eğer mouse basılı değilse BİLGİ GÖSTER (Hover/Tooltip)


            var m = TiklananBul(e.Location);
            if (m != hoverNode)
            {
                hoverNode = m;
                tt.RemoveAll(); // hide old

                if (m == null) // mouse moved off any node
                    return;

                string yazarGosterim = (m.Authors != null && m.Authors.Count > 0)
                       ? string.Join(", ", m.Authors)
                       : "Yazar Bulunamadı";

                string text = $"ID: {m.Id}\n" +
                              $"Başlık: {m.Title}\n" +
                              $"Yazarlar: {yazarGosterim}\n" +
                              $"Yıl: {m.Year}\n" +
                              $"Atıf Sayısı: {m.CitationCount}";

                tt.Show(text, pbGraf, e.X + 15, e.Y + 15, 3000);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            focusMode = false;
            expandedSeeds.Clear();

            odaklanilanMakale = null;
            seciliMakale = null;

            visibleIds = new HashSet<string>(manager.Makaleler.Keys);

            ArrangeOnCircle();
            CizimYap();
            UpdateStats();
        }

        private void lblBilgi_Click(object sender, EventArgs e)
        {

        }
    }
}