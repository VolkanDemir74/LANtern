# LANtern v0.1.10-alpha

Bu hotfix Windows 10'da sanal monitör sürücüsünün Code 18 ile kurulamadığı durumu düzeltir.

## Windows 10 sanal monitör düzeltmesi

- Windows 10'da bulunmayan `WudfRd.inf` bağımlılığı kaldırıldı.
- UMDF reflector servisi Windows 10 için Microsoft'un önerdiği `AddService` yöntemiyle tanımlandı.
- Sanal monitör sürücüsü Windows 10 sürüm 2004, build 19041 ve üzerini hedefler.
- `Root\LANternVirtualDisplay` ve `LANternVirtualDisplay` donanım kimlikleri korunmuştur.
- Sürücü sürümü uygulama sürümüyle birlikte `0.1.10.0` olarak güncellendi.

## Daha güvenli yükseltme

- Yükseltme sırasında sanal aygıt servisi durdurulur, eski LANtern sürücü paketleri Driver Store'dan kaldırılır ve yeni paket yeniden kurulur.
- Daha önce Code 18 veya Code 28 durumunda kalan yazılım aygıtı yükseltme sırasında açıkça kaldırılır ve yeni INF ile yeniden oluşturulur.
- LANtern artık yalnız yazılım aygıtı oluşturulduğu için sanal monitörü bağlı saymaz; PnP aygıtı hatasız başlatıldığında bağlı gösterir.
- Bağlantı başarısız olursa Aygıt Yöneticisi problem kodu yönetim paneline bildirilir.

## Yükseltme notu

Mevcut kuruluma `LANtern-Setup-x64.exe` dosyasını yönetici olarak çalıştırarak doğrudan yükseltme yapılabilir. Kurulum eski geliştirme sürücüsünü kaldırıp yeni paketi kurar. Yeniden başlatma istenirse Windows'u yeniden başlatın ve sanal monitörü LANtern'dan tekrar bağlayın.

## Önemli

- Bu sürüm güvenilen özel ağlarda test amaçlıdır.
- Sanal monitör sürücüsü geliştirme sertifikasıyla imzalanmıştır; Windows veya SmartScreen uyarı gösterebilir.
- Kararlı genel sürüm için üretim sertifikalı sürücü gerekir.
