# Trend Teknoloji - Destek Sistemi

Bu proje, müþteri destek taleplerini yönetmek için geliþtirilmiþ bir masaüstü uygulamasýdýr. Frontend React ve Electron ile, backend ise .NET Core API ile geliþtirilmiþtir.

## Özellikler

- Kullanýcý giriþi ve kayýt
- Müþteri ve yönetici rolleri
- Destek talepleri oluþturma ve yönetme
- Gerçek zamanlý mesajlaþma (SignalR)
- Masaüstü uygulamasý olarak çalýþma

## Kurulum

### Geliþtirme Ortamý

1. Backend için:
   ```
   cd source/repos/DestekAPI/DestekAPI
   dotnet restore
   dotnet build
   ```

2. Frontend için:
   ```
   cd destek-frontend
   npm install
   ```

### Geliþtirme Modunda Çalýþtýrma

1. Backend'i baþlatýn:
   ```
   cd source/repos/DestekAPI/DestekAPI
   dotnet run
   ```

2. Frontend'i Electron ile baþlatýn:
   ```
   cd destek-frontend
   npm run electron
   ```

### Uygulama Paketleme

Uygulamayý paketlemek için ana dizindeki `build-app.bat` dosyasýný çalýþtýrýn:

```
build-app.bat
```

Bu script:
1. Backend'i publish eder
2. Frontend'i build eder
3. Electron uygulamasýný paketler

Paketlenmiþ uygulama `destek-frontend/dist` klasöründe oluþturulacaktýr.

## Kullaným

Paketlenmiþ uygulamayý çalýþtýrdýðýnýzda:

1. Uygulama otomatik olarak backend'i baþlatýr
2. Giriþ ekraný görüntülenir
3. Kullanýcý adý ve þifre ile giriþ yapabilirsiniz
4. Rolünüze göre (müþteri veya yönetici) ilgili panel açýlýr

## Notlar

- Uygulama ilk açýldýðýnda backend'in baþlamasý birkaç saniye sürebilir
- Veritabaný baðlantýsý için SQL Server gereklidir
- Uygulama, backend ve frontend'i tek bir paket olarak daðýtýr, ayrýca bir kurulum gerekmez