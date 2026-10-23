# 🌾 Agro Farm

> **Game farming 2D dengan karakter vector, dunia berbasis tile, kamera scrolling, dan mode bangun.**  
> Dibangun di atas **WinForms + GDI+** murni — tanpa engine eksternal, tanpa library tambahan.

---

## ✨ Fitur Utama

| Fitur | Detail |
|---|---|
| 🧑 **Karakter Vector** | Digambar sepenuhnya dengan GDI+ — animasi jalan, sprint, 4 arah hadap |
| 🕹️ **Input WASD + Gamepad** | Keyboard WASD/arrow, Shift untuk sprint, analog stick siap dipakai |
| 📷 **Kamera Smooth** | Follow player dengan lerp, clamp batas dunia, zoom dengan scroll |
| 🗺️ **Map dari File** | Load peta dari file `.map` teks biasa, mudah diedit manual |
| ⚒️ **Build Mode** | Toggle dengan `B` — taruh/hapus tile dengan mouse, palette 8 jenis |
| 🌱 **Farm System** | Cangkul, siram, tanam, panen dengan minigame timing |
| 🌤️ **Day/Night Cycle** | Langit, matahari, bulan bergerak. Hujan acak tiap hari |
| 🎨 **Procedural Art** | Semua visual (karakter, tile, pohon, bunga, dll) digambar kode |

---

## 🚀 Quick Start

**Prasyarat:** .NET 10 SDK · Windows

```bash
git clone https://github.com/excelyno/percobaanGameGrafkomPBO.git
cd percobaanGameGrafkomPBO/percobaan2
dotnet run
```

---

## 🎮 Kontrol

### Gerakan
| Tombol | Aksi |
|---|---|
| `W` `A` `S` `D` atau `↑ ↓ ← →` | Gerak player |
| `Shift` + WASD | Sprint (lebih cepat) |

### Build Mode
| Tombol / Aksi | Fungsi |
|---|---|
| `B` | Toggle build mode on/off |
| **Klik Kiri** | Taruh tile yang dipilih |
| **Klik Kanan** | Hapus tile → Grass |
| **Scroll Wheel** | Ganti tile di palette |

### Farm
| Tombol / Aksi | Fungsi |
|---|---|
| `1` | 🪓 Cangkul — Grass → Tilled |
| `2` | 💧 Penyiram — Tilled → Watered |
| `3` | 🌾 Tanam Gandum (5g, 3 hari) |
| `4` | 🌽 Tanam Jagung (8g, 4 hari) |
| `5` | 🍅 Tanam Tomat (6g, 3 hari) |
| `6` | ⚔️ Sabit — panen saat matang |
| **Klik tile** | Gunakan alat aktif |
| `SPACE` | Stop kursor minigame panen |

### Lain-lain
| Tombol | Fungsi |
|---|---|
| `Scroll Wheel` | Zoom kamera (0.5× – 2.5×) |
| `R` | Toggle hujan |
| `N` | Skip ke hari berikutnya |

---

## 🗂️ Struktur Proyek

```
percobaan2/
│
├── Core/                     ← Fondasi engine
│   ├── Interfaces.cs         │  IRenderable, IGameContext
│   ├── GameObject.cs         │  Base class — posisi X, Y ter-enkapsulasi
│   ├── Gfx.cs                │  Helper grafika: Lerp, RoundRect, Glow
│   ├── InputState.cs         │  Snapshot input WASD + gamepad per frame
│   └── Camera.cs             │  Kamera 2D: smooth follow, zoom, screen↔world
│
├── Entities/                 ← Objek dalam dunia game
│   ├── Player.cs             │  Karakter vector — fisika, animasi, 4 arah
│   ├── Crop.cs               │  abstract Crop + Wheat, Corn, Tomato
│   ├── Tile.cs               │  Petak lahan + enum SoilState
│   └── DayNightCycle.cs      │  Siklus waktu: Daylight, ArcT, NewDay event
│
├── Tools/                    ← Alat pemain (Strategy pattern)
│   ├── Tool.cs               │  abstract Tool
│   ├── HoeTool.cs
│   ├── WateringCan.cs
│   ├── SeedBag.cs            │  Factory Function untuk jenis tanaman apapun
│   └── Sickle.cs
│
├── Systems/                  ← Sub-sistem yang update tiap frame
│   ├── Rain.cs               │  Partikel hujan (220 tetesan, screen-space)
│   └── HarvestMinigame.cs    │  Timing minigame: PERFECT / Bagus / Meleset
│
├── World/                    ← Dunia dan editor
│   ├── TileMap.cs            │  Load .map, render dengan frustum culling
│   ├── BuildMode.cs          │  Editor tile: palette, klik, highlight
│   └── default.map           │  File peta default (teks, bisa diedit)
│
├── UI/
│   └── GameForm.cs           ← Game loop, input routing, render layer-by-layer
│
└── Program.cs                ← Entry point (6 baris)
```

---

## 🗺️ Format File Peta (`.map`)

Buat atau edit peta dengan teks editor biasa. Setiap karakter = satu tile (48×48 px):

```
############################
#..........................#
#..TTT.....FFF.............#
#.....,,,,,,,..XXXXXX......#
#.....H........XXXXXX......#
#.........~~~~.............#
############################
```

| Karakter | Tile | Solid? |
|:---:|---|:---:|
| `.` | Grass | ❌ |
| `~` | Water | ✅ |
| `,` | Path | ❌ |
| `#` | Stone / Dinding | ✅ |
| `T` | Pohon | ✅ |
| `F` | Bunga | ❌ |
| `X` | Farm zone (lahan tanam) | ❌ |
| `H` | Rumah | ✅ |
| ` ` | Void | ✅ |

> **Tip:** Taruh file `.map` kustom di folder `World/` lalu ganti nama-nya di `GameForm.LoadMap()`.

---

## 🧩 Arsitektur & Pola Desain

### Dependency Graph

```
Program.cs
    └── UI/GameForm                  implements IGameContext
            ├── Core/InputState      ← poll keyboard + gamepad tiap frame
            ├── Core/Camera          ← transformasi world-space → screen
            ├── World/TileMap        ← render peta dengan culling
            ├── World/BuildMode      ← edit tile saat runtime
            ├── Entities/Player      ← fisika + animasi vektor
            ├── Entities/Tile        ← lahan + Crop
            ├── Entities/DayNightCycle
            ├── Tools/*              ← pakai IGameContext + Tile
            └── Systems/*            ← Rain, HarvestMinigame
```

### Prinsip OOP

| Prinsip | Implementasi |
|---|---|
| **Encapsulation** | `GameObject.X/Y` hanya bisa diset via `SetPosition()` |
| **Abstraction** | `abstract Crop`, `abstract Tool` |
| **Inheritance** | `Wheat : Crop`, `Player : GameObject` |
| **Polymorphism** | `crop.Draw()` → `DrawPlant()` berbeda tiap tanaman |
| **Interface** | `IRenderable` (gambar), `IGameContext` (DI antara Tool ↔ Form) |
| **Strategy** | Tiap `Tool` punya `Use()` sendiri |
| **Factory** | `SeedBag(Func<Crop>)` — buat tanaman tanpa coupling |

---

## ➕ Cara Menambah Fitur

### Tanaman Baru

```csharp
// Entities/Crop.cs — tambahkan class baru:
public class Potato : Crop
{
    public override string Name       => "Kentang";
    public override int    GrowthDays => 5;
    public override int    SellPrice  => 30;

    protected override void DrawPlant(Graphics g, float time, float growth)
    {
        // gambar dengan GDI+: g.FillEllipse(...), g.DrawPath(...), dll.
    }
}
```

Lalu daftarkan di `UI/GameForm.cs → InitTools()`:
```csharp
new SeedBag("Kentang", 10, () => new Potato()),
```

---

### Alat Baru

```csharp
// Buat file Tools/Fertilizer.cs:
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

---

### Tile Jenis Baru

1. Tambahkan entry di `enum TileType` di `World/TileMap.cs`
2. Tambahkan metadata (solid, warna) di `MapCell._meta`
3. Tambahkan visual di `DrawTileDetail()`
4. Tambahkan ke palette `BuildMode._palette`

---

## 🎨 Render Layer

`GameForm.OnPaint()` menggambar layer secara berurutan:

```
1. DrawSky()           screen-space  — gradient langit + warna fajar/senja
2. DrawCelestial()     screen-space  — matahari / bulan + glow
3. DrawClouds()        screen-space  — awan bergerak kiri-kanan
── Camera.ApplyTransform() ──────────────────── mulai world-space ──
4. TileMap.Draw()      world-space   — tile dengan frustum culling
5. DrawFarmGrid()      world-space   — overlay lahan + tanaman
6. BuildMode.Draw()    world-space   — highlight tile saat build mode
7. Player.Draw()       world-space   — karakter vektor
── g.ResetTransform() ───────────────────────── kembali screen-space ──
8. Rain.Draw()         screen-space  — partikel hujan
9. Night overlay       screen-space  — alpha blend gelap malam
10. BuildMode.DrawHud() screen-space  — banner + palette build
11. DrawHud()          screen-space  — info panel + toolbar + toast
12. HarvestMinigame    screen-space  — overlay timing (jika aktif)
```

---

## 📦 Tech Stack

| | |
|---|---|
| **Framework** | .NET 10 · WinForms |
| **Rendering** | GDI+ (`System.Drawing`) |
| **Bahasa** | C# 13 |
| **Input** | WinForms keyboard events + `HashSet<Keys>` polling |
| **Target OS** | Windows |
| **Dependensi** | _Nol_ — tidak ada NuGet package |