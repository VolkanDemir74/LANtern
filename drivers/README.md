# LANtern Virtual Display

Bu klasor Microsoft'un IddCx 1.4 IndirectDisplay orneginden turetilen UMDF
sanal ekran surucusunu icerir.

- Windows'a tek bir sanal monitor ekler.
- Tek monitor modu 1920x1080 @ 60 Hz'dir.
- Aygit adi `LANtern Virtual Monitor` olarak gorunur.
- Surucu video kodlamaz; Host uygulamasi ekrani normal monitor gibi yakalar.

Derleme icin Visual Studio 2026, Windows SDK 10.0.28000 ve WDK 10.0.28000
gereklidir. Gelistirme surucusunun kurulumu test imzalama veya uygun bir surucu
sertifikasi gerektirir.
