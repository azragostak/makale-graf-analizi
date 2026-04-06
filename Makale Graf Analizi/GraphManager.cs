using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;

namespace Makale_Graf_Analizi
{
    public class GraphManager
    {
        public Dictionary<string, Makale> Makaleler { get; set; } = new Dictionary<string, Makale>();

        // --- MANUEL JSON YÜKLEME ---
        public void DosyaYukle(string dosyaYolu)
        {
            Makaleler.Clear();
            if (!File.Exists(dosyaYolu)) return;

            string jsonContent = File.ReadAllText(dosyaYolu).Trim();

            // Köşeli parantez temizliği
            if (jsonContent.StartsWith("[")) jsonContent = jsonContent.Substring(1);
            if (jsonContent.EndsWith("]")) jsonContent = jsonContent.Substring(0, jsonContent.Length - 1);

            
            string[] objects = jsonContent.Split(new string[] { "}," }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string objRaw in objects)
            {
                
                string cleanedObj = objRaw.Trim();
                if (!cleanedObj.EndsWith("}")) cleanedObj += "}";
                if (!cleanedObj.StartsWith("{")) cleanedObj = "{" + cleanedObj;

                string id = ExtractStringValue(cleanedObj, "\"id\":");
                string title = ExtractStringValue(cleanedObj, "\"title\":");
                if (string.IsNullOrEmpty(title)) title = ExtractStringValue(cleanedObj, "\"display_name\":");

                int year = ExtractIntValue(cleanedObj, "\"publication_year\":");
                if (year == 0) year = ExtractIntValue(cleanedObj, "\"year\":");

                List<string> refs = ExtractListValue(cleanedObj, "\"referenced_works\":");

             
                List<string> authors = ExtractAuthors(cleanedObj);

                Makale yeniMakale = new Makale
                {
                    Id = id,
                    Title = title,
                    Year = year,
                    ReferencedWorks = refs,
                    Authors = authors,
                    Location = new PointF(0, 0),
                    Velocity = new PointF(0, 0)
                };

                if (!string.IsNullOrEmpty(id) && !Makaleler.ContainsKey(id))
                {
                    Makaleler.Add(id, yeniMakale);
                }
            }

            ReferansSayilariniHesapla();
        }

        // --- GELİŞMİŞ YAZAR AYIKLAMA ---
        private List<string> ExtractAuthors(string source)
        {
            List<string> authors = new List<string>();

            // "authors" array'ini bul
            int authorsStart = source.IndexOf("\"authors\"");
            if (authorsStart == -1) return authors;

            // Array'in başlangıcını bul
            int arrayStart = source.IndexOf("[", authorsStart);
            if (arrayStart == -1) return authors;

            // Array'in sonunu bul
            int arrayEnd = source.IndexOf("]", arrayStart);
            if (arrayEnd == -1) return authors;

            // Array içeriğini al
            string authorsContent = source.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);

            // Yazarları ayır (tırnak içindeki değerleri al)
            int pos = 0;
            while (pos < authorsContent.Length)
            {
                int startQuote = authorsContent.IndexOf("\"", pos);
                if (startQuote == -1) break;

                int endQuote = authorsContent.IndexOf("\"", startQuote + 1);
                if (endQuote == -1) break;

                string authorName = authorsContent.Substring(startQuote + 1, endQuote - startQuote - 1);

                // Escape karakterlerini temizle
                authorName = authorName.Replace("\\\"", "\"");

                if (!string.IsNullOrWhiteSpace(authorName) && !authors.Contains(authorName))
                {
                    authors.Add(authorName);
                }

                pos = endQuote + 1;
            }

            return authors;
        }

        private void ReferansSayilariniHesapla()
        {
            foreach (var m in Makaleler.Values) m.CitationCount = 0;
            foreach (var makale in Makaleler.Values)
            {
                foreach (var refId in makale.ReferencedWorks)
                {
                    if (Makaleler.ContainsKey(refId))
                    {
                        Makaleler[refId].CitationCount++;
                    }
                }
            }
        }

        // --- DİĞER YARDIMCI METOTLAR ---
        private string ExtractStringValue(string source, string key)
        {
            int keyIndex = source.IndexOf(key);
            if (keyIndex == -1) return "";
            int start = source.IndexOf("\"", keyIndex + key.Length) + 1;
            int end = source.IndexOf("\"", start);
            if (start <= 0 || end == -1) return "";
            return source.Substring(start, end - start);
        }

        private int ExtractIntValue(string source, string key)
        {
            int keyIndex = source.IndexOf(key);
            if (keyIndex == -1) return 0;
            int start = keyIndex + key.Length;
            while (start < source.Length && !char.IsDigit(source[start])) start++;
            int end = start;
            while (end < source.Length && char.IsDigit(source[end])) end++;
            if (start >= source.Length || start == end) return 0;
            int.TryParse(source.Substring(start, end - start), out int res);
            return res;
        }

        private List<string> ExtractListValue(string source, string key)
        {
            List<string> list = new List<string>();
            int keyIndex = source.IndexOf(key);
            if (keyIndex == -1) return list;
            int start = source.IndexOf("[", keyIndex);
            int end = source.IndexOf("]", start);
            if (start == -1 || end == -1) return list;
            string content = source.Substring(start + 1, end - start - 1);
            foreach (var part in content.Split(','))
            {
                string clean = part.Trim().Trim('\"');
                if (!string.IsNullOrEmpty(clean)) list.Add(clean);
            }
            return list;
        }

        public List<string> KCoreHesapla(int k)
        {
            List<string> aktifNodes = Makaleler.Keys.ToList();
            bool degisimOldu = true;
            while (degisimOldu)
            {
                degisimOldu = false;
                List<string> silinecekler = new List<string>();
                foreach (var id in aktifNodes)
                {
                    int derece = 0;
                    Makale buMakale = Makaleler[id];
                    foreach (var refId in buMakale.ReferencedWorks)
                        if (aktifNodes.Contains(refId)) derece++;
                    foreach (var digerId in aktifNodes)
                    {
                        if (digerId == id) continue;
                        if (Makaleler[digerId].ReferencedWorks.Contains(id)) derece++;
                    }
                    if (derece < k) silinecekler.Add(id);
                }
                if (silinecekler.Count > 0)
                {
                    foreach (var silId in silinecekler) aktifNodes.Remove(silId);
                    degisimOldu = true;
                }
            }
            foreach (var m in Makaleler.Values) m.IsInKCore = false;
            foreach (var id in aktifNodes) Makaleler[id].IsInKCore = true;
            return aktifNodes;
        }

        

        
        public Dictionary<string, double> CalculateBetweennessCentrality()
        {
            // 1. Kenarları Yönsüz Kabul Et (Undirected Conversion) 
            // Grafı yönsüz hale getirmek için: A -> B referansı varsa, hem A'nın komşusu B, hem B'nin komşusu A olur.
            Dictionary<string, HashSet<string>> adj = new Dictionary<string, HashSet<string>>();

            // Önce listeleri başlat
            foreach (var node in Makaleler.Keys) adj[node] = new HashSet<string>();

            // Bağlantıları doldur
            foreach (var m in Makaleler.Values)
            {
                foreach (var refId in m.ReferencedWorks)
                {
                    // Sadece veri setimizde var olan makaleleri dikkate alıyoruz
                    if (Makaleler.ContainsKey(refId))
                    {
                        // Yönsüz olduğu için çift taraflı ekle
                        adj[m.Id].Add(refId);
                        adj[refId].Add(m.Id);
                    }
                }
            }

            // 2. Brandes Algoritması (Betweenness Hesabı) [cite: 88, 89]
            Dictionary<string, double> betweenness = Makaleler.Keys.ToDictionary(k => k, v => 0.0);

            foreach (var s in Makaleler.Keys)
            {
                Stack<string> S = new Stack<string>();
                Dictionary<string, List<string>> P = Makaleler.Keys.ToDictionary(k => k, v => new List<string>());
                Dictionary<string, int> sigma = Makaleler.Keys.ToDictionary(k => k, v => 0);
                Dictionary<string, int> d = Makaleler.Keys.ToDictionary(k => k, v => -1);

                sigma[s] = 1;
                d[s] = 0;
                Queue<string> Q = new Queue<string>();
                Q.Enqueue(s);

                while (Q.Count > 0)
                {
                    var v = Q.Dequeue();
                    S.Push(v);
                    foreach (var w in adj[v])
                    {
                        // w ilk kez bulunduysa
                        if (d[w] < 0)
                        {
                            Q.Enqueue(w);
                            d[w] = d[v] + 1;
                        }
                        // w'ye en kısa yol v üzerinden ise
                        if (d[w] == d[v] + 1)
                        {
                            sigma[w] += sigma[v];
                            P[w].Add(v);
                        }
                    }
                }

                Dictionary<string, double> delta = Makaleler.Keys.ToDictionary(k => k, v => 0.0);
                while (S.Count > 0)
                {
                    var w = S.Pop();
                    foreach (var v in P[w])
                    {
                        // Brandes formülü
                        delta[v] += (double)sigma[v] / sigma[w] * (1.0 + delta[w]);
                    }
                    if (w != s)
                    {
                        betweenness[w] += delta[w];
                    }
                }
            }

            // Yönsüz grafta her yol iki yönden (s->t ve t->s) sayıldığı için sonucu 2'ye bölüyoruz.
            return betweenness.ToDictionary(k => k.Key, v => v.Value / 2.0);
        }

        
        public (int hIndex, double hMedian, List<Makale> hCore) HesaplaHMetrikleri(string makaleId)
        {
            foreach (var m in Makaleler.Values) m.IsHCore = false;
            List<Makale> atifYapanlar = Makaleler.Values.Where(m => m.ReferencedWorks.Contains(makaleId)).ToList();
            atifYapanlar.Sort((a, b) => b.CitationCount.CompareTo(a.CitationCount));
            int hIndex = 0;
            List<Makale> hCoreList = new List<Makale>();
            for (int i = 0; i < atifYapanlar.Count; i++)
            {
                if (atifYapanlar[i].CitationCount >= (i + 1))
                {
                    hIndex = i + 1;
                    atifYapanlar[i].IsHCore = true;
                    hCoreList.Add(atifYapanlar[i]);
                }
                else break;
            }
            double median = 0;
            if (hCoreList.Count > 0)
            {
                var skorlar = hCoreList.Select(m => m.CitationCount).OrderBy(x => x).ToList();
                int n = skorlar.Count;
                median = (n % 2 == 1) ? skorlar[n / 2] : (skorlar[(n / 2) - 1] + skorlar[n / 2]) / 2.0;
            }
            return (hIndex, median, hCoreList);
        }

        public List<Makale> GetIdSiraliMakaleler() => Makaleler.Values.OrderBy(m => m.Id).ToList();
        public int ToplamMakale => Makaleler.Count;
        public int ToplamReferans => Makaleler.Values.Sum(m => m.ReferencedWorks.Count);
        public Makale EnCokAtifAlan => Makaleler.Values.OrderByDescending(m => m.CitationCount).FirstOrDefault();
        public Makale EnCokAtifVeren => Makaleler.Values.OrderByDescending(m => m.ReferencedWorks.Count).FirstOrDefault();
    }
}
