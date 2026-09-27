# LANtern v0.1.9-alpha

Bu alfa sürümü Windows 10 sanal monitör uyumluluğunu ve sürücü yükseltme akışını düzeltir.

## Windows 10 sanal monitör düzeltmesi

- Sanal monitör sürücüsü artık Windows 10 sürüm 2004, build 19041 ve üzerini hedefler.
- Windows 10 22H2 build 19045 üzerinde INF donanım eşleşmesini engelleyen Windows 11 alt sınırı kaldırıldı.
- `Root\LANternVirtualDisplay` ve `LANternVirtualDisplay` donanım kimlikleri korunmuştur.
- Sürücü sürümü uygulama sürümüyle birlikte `0.1.9.0` olarak güncellendi.

## Daha güvenli yükseltme

- Yükseltme sırasında sanal aygıt servisi durdurulur, eski LANtern sürücü paketleri Driver Store'dan kaldırılır ve yeni paket yeniden kurulur.
- Daha önce Code 28 durumunda kalan aygıtlar yeni uyumlu INF ile yeniden eşleştirilir.
- LANtern artık yalnız yazılım aygıtı oluşturulduğu için sanal monitörü bağlı saymaz; PnP aygıtı hatasız başlatıldığında bağlı gösterir.
- Bağlantı başarısız olursa Aygıt Yöneticisi problem kodu yönetim paneline bildirilir.

## Yükseltme notu

Mevcut kuruluma `LANtern-Setup-x64.exe` dosyasını yönetici olarak çalıştırarak doğrudan yükseltme yapılabilir. Kurulum eski geliştirme sürücüsünü kaldırıp yeni paketi kurar. Yeniden başlatma istenirse Windows'u yeniden başlatın ve sanal monitörü LANtern'dan tekrar bağlayın.

## Önemli

- Bu sürüm güvenilen özel ağlarda test amaçlıdır.
- Sanal monitör sürücüsü geliştirme sertifikasıyla imzalanmıştır; Windows veya SmartScreen uyarı gösterebilir.
- Kararlı genel sürüm için üretim sertifikalı sürücü gerekir.
