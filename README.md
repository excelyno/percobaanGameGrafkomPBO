# 🌾 Agro Farm — Developer Guide

Game farming 2D berbasis **WinForms + GDI+** yang dibangun dengan arsitektur modular dan prinsip OOP.  
Cocok sebagai proyek latihan **Grafika Komputer** dan **Pemrograman Berorientasi Objek (PBO)**.

---

## 🚀 Quick Start

**Prasyarat:** .NET 10 SDK, Windows

```bash
git clone https://github.com/excelyno/percobaanGameGrafkomPBO.git
cd percobaanGameGrafkomPBO/percobaan2
dotnet run
```

### Kontrol Game

| Tombol / Aksi | Fungsi |
|---|---|
| `1` | Cangkul |
| `2` | Penyiram |
| `3` | Bibit Gandum (5g) |
| `4` | Bibit Jagung (8g) |
| `5` | Bibit Tomat (6g) |
| `6` | Sabit (panen) |
| `R` | Toggle hujan |
| `N` | Skip ke hari berikutnya |
| **Klik kiri** tile | Gunakan alat aktif |
| **SPACE** | Stop kursor minigame panen |

---

## 🗂️ Struktur Proyek

```
percobaan2/
│
├── Core/                        ← Fondasi: tipe & utility yang dipakai semua lapisan
│   ├── Interfaces.cs            │   IRenderable, IGameContext
│   ├── GameObject.cs            │   Base class (encapsulated position X, Y)
│   └── Gfx.cs                   │   Helper grafika: Lerp, RoundRect, Glow
│
├── Entities/                    ← Model data domain game
│   ├── Crop.cs                  │   abstract Crop + Wheat, Corn, Tomato
│   ├── Tile.cs                  │   Petak lahan + enum SoilState
│   └── DayNightCycle.cs         │   Siklus waktu, Daylight, ArcT
│
├── Tools/                       ← Alat pemain (Strategy pattern)
│   ├── Tool.cs                  │   abstract Tool
│   ├── HoeTool.cs               │   Cangkul
│   ├── WateringCan.cs           │   Penyiram
│   ├── SeedBag.cs               │   Bibit (Factory Function)
│   └── Sickle.cs                │   Sabit — memulai HarvestMinigame
│
├── Systems/                     ← Sub-sistem game yang berjalan tiap frame
│   ├── Rain.cs                  │   Sistem partikel hujan (220 tetesan)
│   └── HarvestMinigame.cs       │   Minigame timing tekan SPACE
│
├── UI/
│   └── GameForm.cs              ← Form utama: game loop, input, render layer-by-layer
│
└── Program.cs                   ← Entry point (STAThread, Application.Run)
```

---

## 🧩 Arsitektur & Konsep OOP

### Dependency Graph (dari atas ke bawah)

```
Program.cs
    └── UI/GameForm          implements IGameContext
            ├── Core/Gfx               (utility rendering)
            ├── Entities/Tile          ←── Entities/Crop (Wheat, Corn, Tomato)
            ├── Entities/DayNightCycle
            ├── Tools/* (HoeTool, WateringCan, SeedBag, Sickle)
            │       └── menggunakan IGameContext & Tile
            └── Systems/* (Rain, HarvestMinigame)
```

### Prinsip OOP yang Dipakai

| Prinsip | Implementasi |
|---|---|
| **Encapsulation** | `GameObject.X/Y` hanya bisa diset lewat `SetPosition()` |
| **Abstraction** | `abstract class Crop`, `abstract class Tool` |
| **Inheritance** | `Wheat : Crop`, `HoeTool : Tool`, dll |
| **Polymorphism** | `crop.Draw()` memanggil `DrawPlant()` yang berbeda tiap jenis |
| **Interface** | `IRenderable` (gambar), `IGameContext` (Dependency Inversion) |
| **Strategy** | Setiap `Tool` punya implementasi `Use()` sendiri |
| **Factory** | `SeedBag` menerima `Func<Crop>` untuk membuat tanaman apa pun |

---

## ➕ Cara Menambah Fitur Baru

### 1. Tambah Jenis Tanaman Baru

Buat class baru di `Entities/Crop.cs` (atau file baru di folder `Entities/`):

```csharp
public class Potato : Crop
{
    public override string Name       => "Kentang";
    public override int    GrowthDays => 5;
    public override int    SellPrice  => 30;

    protected override void DrawPlant(Graphics g, float time, float growth)
    {
        // Gambar tanaman di sini dengan GDI+
        // g.FillEllipse(...), g.DrawPath(...), dll
    }
}
```

Lalu daftarkan di `UI/GameForm.cs` → method `InitTools()`:

```csharp
new SeedBag("Kentang", 10, () => new Potato()),
```

---

### 2. Tambah Alat Baru

Buat file baru di `Tools/`, misalnya `Fertilizer.cs`:

```csharp
using FarmGame.Core;
using FarmGame.Entities;

namespace FarmGame.Tools;

public class Fertilizer : Tool
{
    public override string Label => "Pupuk";

    public override void Use(Tile tile, IGameContext ctx)
    {
        // logika pemupukan
        ctx.Toast("Tanaman dipupuk!");
    }
}
```

Daftarkan di `InitTools()` di `GameForm.cs`.

---

### 3. Tambah Sistem Baru (misal: sistem cuaca lain)

Buat file baru di `Systems/`, misalnya `Wind.cs`:

```csharp
namespace FarmGame.Systems;

public class Wind
{
    public float Strength { get; private set; }

    public void Update(float dt) { /* ... */ }
}
```

Instansiasi di `GameForm` dan panggil `Update()` / `Draw()` di game loop.

---

## 🎨 Sistem Rendering (GDI+)

Render dilakukan layer-by-layer di `GameForm.OnPaint()`:

```
1. DrawSky()          — gradient langit (lerp warna berdasarkan Daylight)
2. DrawCelestial()    — matahari / bulan dengan glow radial gradient
3. DrawClouds()       — awan bergerak (matrix transform: translate + scale)
4. DrawWindmill()     — kincir angin (rotasi baling-baling tiap frame)
5. DrawHills()        — bukit dengan Bézier spline
6. DrawFarm()         — grid tile + tanaman (painter's algorithm)
7. Rain.Draw()        — partikel hujan (alpha blending)
8. Night overlay      — gelap malam (semi-transparent fill)
9. DrawHud()          — info panel + toolbar + toast
10. HarvestMinigame   — overlay minigame jika aktif
```

**Utilitas grafika tersedia di `Core/Gfx.cs`:**
- `Gfx.Lerp(a, b, t)` — interpolasi warna
- `Gfx.RoundRect(rect, radius)` — path persegi sudut bulat
- `Gfx.Glow(g, cx, cy, r, core, edge)` — cahaya radial gradient

---

## 🔄 Game Loop

```
Timer (16ms ≈ 60 FPS)
    │
    └── GameTick()
            ├── Update(dt)
            │       ├── Gerakkan awan
            │       ├── [jika minigame aktif] → update minigame saja
            │       └── [normal] → clock, hujan, siram otomatis
            └── Invalidate() → OnPaint()
```

---

## 📦 Tech Stack

| Komponen | Detail |
|---|---|
| Framework | .NET 10, WinForms |
| Rendering | GDI+ (`System.Drawing`) |
| Bahasa | C# 13 |
| IDE | Visual Studio / VS Code |
| Target OS | Windows |