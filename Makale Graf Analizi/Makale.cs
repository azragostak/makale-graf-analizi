using System.Collections.Generic;
using System.Drawing; // Çizim koordinatları için gerekli

namespace Makale_Graf_Analizi
{
    public class Makale
    {
        // --- JSON Dosyasından Gelecek Veriler [cite: 24-43] ---
        public string Id { get; set; }

        public string Title { get; set; }

        public int Year { get; set; }


        public PointF Velocity { get; set; } = new PointF(0, 0);   // yerleşim için


        public List<string> Authors { get; set; } = new List<string>();

        // JSON'daki "referenced_works" alanı ile eşleşmesi için

        public List<string> ReferencedWorks { get; set; }

        // --- Hesaplayacağımız ve Çizim İçin Gereken Veriler ---

        // Makaleye kaç kişinin atıf yaptığı (Incoming edges) [cite: 49]
        public int CitationCount { get; set; } = 0;

        // Ekranda nerede çizileceği (X, Y koordinatı)
        public PointF Location { get; set; }

        // H-Core analizi yapıldığında bu makale o kümeye dahil mi?
        public bool IsHCore { get; set; } = false;
        public bool IsInKCore { get; internal set; }

        public Makale()
        {
            Authors = new List<string>();
            ReferencedWorks = new List<string>();
        }
    }
}