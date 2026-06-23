namespace ViewGrid.Core;

/// <summary>
/// ViewGridControl içindeki görsel sunum tipleri. Eski ListView uyumlu modlar korunur;
/// yeni modlar aynı render altyapısı üzerinden daha doğru isimlendirilmiş kart/list senaryoları sunar.
/// </summary>
public enum ViewGridMode
{
    /// <summary>Poster tarzı büyük görsel kart görünümü. Kullanıcı dostu isimdir; içeride büyük ikon/poster kart altyapısını kullanır.</summary>
    Poster,

    /// <summary>Çok büyük ikon/poster kartları. Eski kodlarla uyumluluk için korunur; yeni kullanımda Poster tercih edilebilir.</summary>
    ExtraLargeIcons,

    /// <summary>Büyük ikon kartları.</summary>
    LargeIcons,

    /// <summary>Orta ikon / kompakt kartlar.</summary>
    MediumIcons,

    /// <summary>Başlıksız sade liste.</summary>
    List,

    /// <summary>Klasik çok kolonlu kart/grid görünümü.</summary>
    Tile,

    /// <summary>4-5 satırlık, geniş ve okunaklı kart/ticket görünümü.</summary>
    LargeCard,

    /// <summary>Kolon başlıklı klasik tablo/detay liste görünümü.</summary>
    Details,

    /// <summary>Excel benzeri en yoğun satır görünümü.</summary>
    DenseList,

    /// <summary>Ticket, mesaj ve iş takipleri için durum rengi/badge mantığına uygun dashboard kart.</summary>
    DashboardCard,

    /// <summary>Her kayıt için tek satır geniş kart; mesaj/ticket akışında okunabilir liste.</summary>
    RowCard,

    /// <summary>Albüm kapağı, film afişi, öğrenci fotoğrafı veya makine görseli için kompakt medya kutusu.</summary>
    MediaTile,

    /// <summary>Windows Explorer / fotoğraf galerisi tarzı görsel katalog.</summary>
    Gallery,

    /// <summary>Film/kapak şeridi gibi yatay ağırlıklı görsel katalog görünümü.</summary>
    FilmStrip,

    /// <summary>Tüm görünür kolonları etiket/değer olarak alt alta gösteren tam genişlik detay kartı.</summary>
    DetailCard,

    /// <summary>Hat, makine ve operatör seçimleri için ikon ağırlıklı grid görünümü.</summary>
    IconGrid,

    /// <summary>Status kolonlarına göre gruplanmış liste/kart kullanımını temsil eden görünüm.</summary>
    GroupedList,

    /// <summary>Gruplar altında kartların kümelendiği katalog/hat/makine görünümü.</summary>
    GroupCard,

    /// <summary>Visual Studio PropertyGrid benzeri etiket/değer detay kartı.</summary>
    PropertyCard,

    /// <summary>Power BI tarzı büyük KPI/metrik kartları.</summary>
    KpiDashboard,

    /// <summary>Durum, kapasite veya risk değerlerini ısı haritası kartlarıyla gösterir.</summary>
    HeatMap,

    /// <summary>Satır içinde mini trend/sparkline bilgisi göstermeye uygun kompakt kart.</summary>
    MiniChart,

    /// <summary>Outlook benzeri başlık + önizleme satırı görünümü.</summary>
    RowPreview,

    /// <summary>Ticket durum kolonları için Kanban benzeri kart görünümü.</summary>
    Kanban,

    /// <summary>Zaman bazlı olay/mesaj akışı için dikey geniş kart görünümü.</summary>
    Timeline,

    /// <summary>Üst liste + alt/yan detay paneli senaryoları için detay odaklı görünüm.</summary>
    MasterDetail
}
