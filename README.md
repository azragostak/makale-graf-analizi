# 📚 Makale Graf Analiz Sistemi

Bu proje, akademik makaleler arasındaki atıf (citation) ve referans ilişkilerini görselleştirmek ve ağ teorisi yöntemleriyle analiz etmek için geliştirilmiş bir **Windows Forms** uygulamasıdır. **OpenAlex** formatındaki verileri kullanarak karmaşık akademik ağları anlaşılır hale getirir.

---

## 🚀 Öne Çıkan Özellikler

- **İnteraktif Grafik Görselleştirme**: GDI+ kütüphanesi kullanılarak yüksek performanslı ve akıcı bir görselleştirme sağlar.
- **Analiz Algoritmaları**:
  - **H-Core Analizi**: Seçilen bir makale için H-index ve H-median değerlerini hesaplar ve çekirdek (core) ağını görselleştirir.
  - **K-Core Ayrıştırması**: Ağdaki en yoğun bağlantılı alt kümeleri bulmak için derece tabanlı filtreleme yapar.
  - **Betweenness Centrality**: Ağdaki bilgi akışını kontrol eden "köprü" makaleleri tespit eder.
- **Dinamik Yerleşim (Layout)**:
  - **Dairesel Yerleşim (Circle Layout)**: Makaleleri düzenli bir çember üzerinde sıralar.
  - **Kuvvet Tabanlı Yerleşim (Force-Directed Layout)**: Fizik tabanlı bir modelle düğümlerin birbirini itmesi ve çekmesiyle doğal bir ağ yapısı oluşturur.
- **Gerçek Zamanlı İstatistikler**: Düğüm sayısı, kenar sayısı ve en çok atıf alan/veren makaleler gibi verileri anlık olarak takip edin.
- **Gelişmiş Navigasyon**: Odaklanma (Focus) modu ile sadece belirli bir makalenin ilişkilerine yoğunlaşın.

---

## 🛠️ Teknik Gereksinimler

- **Framework**: .NET 8.0-windows
- **Dil**: C#
- **IDE**: Visual Studio 2022
- **Bağımlılıklar**: 
  - `Newtonsoft.Json` (Veri ayrıştırma için)

---

## 🖱️ Kontroller ve Kısayollar

| Aksiyon | Kontrol |
| :--- | :--- |
| **Yakınlaştırma (Zoom)** | Fare Tekerleği |
| **Hızlı Yakınlaştırma** | `Ctrl` + Fare Tekerleği |
| **Ekranı Kaydırma (Pan)** | Sol Tık + Sürükle |
| **Düğüm Seçimi** | Sol Tık (Düğüm üzerine) |
| **Seçimi Sıfırla** | Grafiğin boş bir alanına tıkla veya "Sıfırla" butonunu kullan |

---

## 📥 Kurulum

1. Depoyu bilgisayarınıza klonlayın:
   ```bash
   git clone https://github.com/azragostak/makale-graf-analizi.git
   ```
2. Çözüm dosyasını (`Makale Graf Analizi.sln`) Visual Studio 2022 ile açın.
3. Gerekli NuGet paketlerini (Newtonsoft.Json) geri yükleyin.
4. Projeyi derleyin ve başlatın.

---

## 👥 Katkıda Bulunanlar (Contributors)

Bu proje aşağıdaki ekip tarafından geliştirilmiştir:

- **Meryem Azra Gostak** - [GitHub](https://github.com/azragostak)
- **Rana Hüseynova**

---

## 📄 Lisans

Bu proje eğitim amaçlı geliştirilmiştir.
