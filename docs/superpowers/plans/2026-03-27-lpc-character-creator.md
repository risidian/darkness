# LPC Sprite-Based Character Creator Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the text-only character creation page with a visual character creator that composites LPC sprite sheet layers in real-time, showing a live preview of the character as the player customizes hair, skin, armor, and weapon.

**Architecture:** Individual LPC layer PNGs are bundled as MAUI raw resources. A `SpriteCompositor` service in `Darkness.Core` accepts appearance selections and composites them into a single sprite sheet image using SkiaSharp (platform-agnostic image manipulation). The `CharacterGenViewModel` calls the compositor whenever a picker changes, producing a preview image displayed in the MAUI page. The same compositor output feeds MonoGame's `Texture2D` for in-game rendering. A `SpriteSheetDefinition` model maps layer names to file paths and z-order.

**Tech Stack:** SkiaSharp (cross-platform image compositing), .NET 10 MAUI, CommunityToolkit.Mvvm, XUnit + Moq.

---

## File Map

| File | Action | Responsibility |
|------|--------|----------------|
| `Darkness.Core/Models/SpriteLayerDefinition.cs` | Create | Data model for a single sprite layer (path, zOrder) |
| `Darkness.Core/Models/CharacterAppearance.cs` | Create | Groups all appearance choices (skin, hair style, hair color, armor, weapon) |
| `Darkness.Core/Models/Character.cs` | Modify | Add `HairStyle` field |
| `Darkness.Core/Services/SpriteCompositor.cs` | Create | Composites layer PNGs into a single sprite sheet byte array using SkiaSharp |
| `Darkness.Core/Interfaces/ISpriteCompositor.cs` | Create | Interface for the compositor |
| `Darkness.Core/Services/SpriteLayerCatalog.cs` | Create | Maps appearance choices → ordered list of `SpriteLayerDefinition` (file paths + z-order) |
| `Darkness.Core/Interfaces/ISpriteLayerCatalog.cs` | Create | Interface for the catalog |
| `Darkness.Core/ViewModels/CharacterGenViewModel.cs` | Modify | Add hair style picker, preview image property, trigger recomposite on any picker change |
| `Darkness.MAUI/Pages/CharacterGenPage.xaml` | Modify | Add Image preview and hair style picker |
| `Darkness.MAUI/MauiProgram.cs` | Modify | Register new services |
| `Darkness.MAUI/Darkness.MAUI.csproj` | Modify | Add SkiaSharp package, recursive raw asset glob |
| `Darkness.Core/Darkness.Core.csproj` | Modify | Add SkiaSharp package |
| `Darkness.MAUI/Resources/Raw/sprites/body/` | Create | Body layer PNGs per skin color |
| `Darkness.MAUI/Resources/Raw/sprites/hair/` | Create | Hair layer PNGs per style per color |
| `Darkness.MAUI/Resources/Raw/sprites/armor/` | Create | Armor layer PNGs per class |
| `Darkness.MAUI/Resources/Raw/sprites/weapons/` | Create | Weapon layer PNGs per class |
| `Darkness.Tests/Services/SpriteLayerCatalogTests.cs` | Create | Tests for layer selection logic |
| `Darkness.Tests/Services/SpriteCompositorTests.cs` | Create | Tests for compositing logic |
| `Darkness.Tests/ViewModels/CharacterGenViewModelTests.cs` | Modify | Add tests for preview triggering and new hair style property |

---

### Task 1: Add SkiaSharp Dependencies & Fix Asset Glob

**Files:**
- Modify: `Darkness.Core/Darkness.Core.csproj`
- Modify: `Darkness.MAUI/Darkness.MAUI.csproj`

- [ ] **Step 1: Add SkiaSharp to Darkness.Core**

```bash
cd Darkness.Core && dotnet add package SkiaSharp --version 3.116.1
```

This adds SkiaSharp to Core so the compositor can use `SKBitmap`/`SKCanvas` without any MAUI dependency.

- [ ] **Step 2: Add SkiaSharp to Darkness.MAUI**

```bash
cd Darkness.MAUI && dotnet add package SkiaSharp --version 3.116.1
```

- [ ] **Step 3: Fix MauiAsset glob for nested sprite directories**

In `Darkness.MAUI/Darkness.MAUI.csproj`, replace the existing raw asset line:

```xml
<MauiAsset Include="Resources\Raw\*" />
```

with a recursive glob:

```xml
<MauiAsset Include="Resources\Raw\**" />
```

This ensures PNGs inside `Resources/Raw/sprites/body/`, `sprites/hair/`, etc. are bundled.

- [ ] **Step 4: Build to verify**

```bash
dotnet build Darkness.Core/Darkness.Core.csproj
dotnet build Darkness.MAUI/Darkness.MAUI.csproj -f net10.0-windows10.0.19041.0
```

Expected: 0 errors.

- [ ] **Step 5: Commit**

```bash
git add Darkness.Core/Darkness.Core.csproj Darkness.MAUI/Darkness.MAUI.csproj
git commit -m "chore: add SkiaSharp dependency and fix recursive raw asset glob"
```

---

### Task 2: Sprite Layer Data Models

**Files:**
- Create: `Darkness.Core/Models/SpriteLayerDefinition.cs`
- Create: `Darkness.Core/Models/CharacterAppearance.cs`
- Modify: `Darkness.Core/Models/Character.cs`

- [ ] **Step 1: Create SpriteLayerDefinition model**

Create `Darkness.Core/Models/SpriteLayerDefinition.cs`:

```csharp
namespace Darkness.Core.Models
{
    public class SpriteLayerDefinition
    {
        public string ResourcePath { get; set; } = string.Empty;
        public int ZOrder { get; set; }

        public SpriteLayerDefinition(string resourcePath, int zOrder)
        {
            ResourcePath = resourcePath;
            ZOrder = zOrder;
        }
    }
}
```

`ResourcePath` is relative to `Resources/Raw/`, e.g. `sprites/body/light.png`. `ZOrder` determines draw order (lower = behind).

- [ ] **Step 2: Create CharacterAppearance model**

Create `Darkness.Core/Models/CharacterAppearance.cs`:

```csharp
namespace Darkness.Core.Models
{
    public class CharacterAppearance
    {
        public string SkinColor { get; set; } = "Light";
        public string HairStyle { get; set; } = "Long";
        public string HairColor { get; set; } = "Black";
        public string ArmorType { get; set; } = "Leather";
        public string WeaponType { get; set; } = "Longsword";
    }
}
```

- [ ] **Step 3: Add HairStyle field to Character model**

In `Darkness.Core/Models/Character.cs`, add after the `HairColor` property:

```csharp
public string HairStyle { get; set; } = string.Empty;
```

- [ ] **Step 4: Build to verify**

```bash
dotnet build Darkness.Core/Darkness.Core.csproj
```

Expected: 0 errors.

- [ ] **Step 5: Commit**

```bash
git add Darkness.Core/Models/SpriteLayerDefinition.cs Darkness.Core/Models/CharacterAppearance.cs Darkness.Core/Models/Character.cs
git commit -m "feat: add sprite layer data models and HairStyle field"
```

---

### Task 3: Sprite Layer Catalog (Maps Appearance → Layer Files)

**Files:**
- Create: `Darkness.Core/Interfaces/ISpriteLayerCatalog.cs`
- Create: `Darkness.Core/Services/SpriteLayerCatalog.cs`
- Create: `Darkness.Tests/Services/SpriteLayerCatalogTests.cs`

- [ ] **Step 1: Create ISpriteLayerCatalog interface**

Create `Darkness.Core/Interfaces/ISpriteLayerCatalog.cs`:

```csharp
using Darkness.Core.Models;

namespace Darkness.Core.Interfaces
{
    public interface ISpriteLayerCatalog
    {
        List<SpriteLayerDefinition> GetLayersForAppearance(CharacterAppearance appearance);
        CharacterAppearance GetDefaultAppearanceForClass(string className);
        List<string> HairStyles { get; }
    }
}
```

- [ ] **Step 2: Write the failing tests**

Create `Darkness.Tests/Services/SpriteLayerCatalogTests.cs`:

```csharp
using Darkness.Core.Models;
using Darkness.Core.Services;

namespace Darkness.Tests.Services
{
    public class SpriteLayerCatalogTests
    {
        private readonly SpriteLayerCatalog _catalog = new();

        [Fact]
        public void GetLayersForAppearance_ReturnsBodyHairArmorWeapon()
        {
            var appearance = new CharacterAppearance
            {
                SkinColor = "Light",
                HairStyle = "Long",
                HairColor = "Black",
                ArmorType = "Plate",
                WeaponType = "Longsword"
            };

            var layers = _catalog.GetLayersForAppearance(appearance);

            Assert.True(layers.Count >= 4);
            Assert.Contains(layers, l => l.ResourcePath.Contains("body/"));
            Assert.Contains(layers, l => l.ResourcePath.Contains("hair/"));
            Assert.Contains(layers, l => l.ResourcePath.Contains("armor/"));
            Assert.Contains(layers, l => l.ResourcePath.Contains("weapons/"));
        }

        [Fact]
        public void GetLayersForAppearance_LayersAreSortedByZOrder()
        {
            var appearance = new CharacterAppearance();
            var layers = _catalog.GetLayersForAppearance(appearance);

            for (int i = 1; i < layers.Count; i++)
            {
                Assert.True(layers[i].ZOrder >= layers[i - 1].ZOrder,
                    $"Layer at index {i} (z={layers[i].ZOrder}) should be >= layer at index {i - 1} (z={layers[i - 1].ZOrder})");
            }
        }

        [Fact]
        public void GetLayersForAppearance_BodyLayerIsLowestZOrder()
        {
            var appearance = new CharacterAppearance { SkinColor = "Tan" };
            var layers = _catalog.GetLayersForAppearance(appearance);

            var bodyLayer = layers.First(l => l.ResourcePath.Contains("body/"));
            Assert.Equal(layers.Min(l => l.ZOrder), bodyLayer.ZOrder);
        }

        [Fact]
        public void GetLayersForAppearance_SkinColorMapsToCorrectFile()
        {
            var appearance = new CharacterAppearance { SkinColor = "Tan" };
            var layers = _catalog.GetLayersForAppearance(appearance);

            var bodyLayer = layers.First(l => l.ResourcePath.Contains("body/"));
            Assert.Contains("tan", bodyLayer.ResourcePath.ToLower());
        }

        [Fact]
        public void GetLayersForAppearance_HairMapsToStyleAndColor()
        {
            var appearance = new CharacterAppearance { HairStyle = "Short", HairColor = "Blonde" };
            var layers = _catalog.GetLayersForAppearance(appearance);

            var hairLayer = layers.First(l => l.ResourcePath.Contains("hair/"));
            Assert.Contains("short", hairLayer.ResourcePath.ToLower());
            Assert.Contains("blonde", hairLayer.ResourcePath.ToLower());
        }

        [Theory]
        [InlineData("Warrior", "Plate", "Longsword")]
        [InlineData("Mage", "Robe", "Staff")]
        [InlineData("Rogue", "Leather", "Daggers")]
        public void GetDefaultAppearanceForClass_ReturnsCorrectEquipment(string className, string expectedArmor, string expectedWeapon)
        {
            var appearance = _catalog.GetDefaultAppearanceForClass(className);

            Assert.Equal(expectedArmor, appearance.ArmorType);
            Assert.Equal(expectedWeapon, appearance.WeaponType);
        }

        [Fact]
        public void HairStyles_ContainsExpectedOptions()
        {
            Assert.Contains("Long", _catalog.HairStyles);
            Assert.Contains("Short", _catalog.HairStyles);
            Assert.Contains("Mohawk", _catalog.HairStyles);
            Assert.Contains("Messy", _catalog.HairStyles);
        }
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

```bash
dotnet test Darkness.Tests --filter "FullyQualifiedName~SpriteLayerCatalogTests" --verbosity normal
```

Expected: FAIL — `SpriteLayerCatalog` does not exist yet.

- [ ] **Step 4: Implement SpriteLayerCatalog**

Create `Darkness.Core/Services/SpriteLayerCatalog.cs`:

```csharp
using Darkness.Core.Interfaces;
using Darkness.Core.Models;

namespace Darkness.Core.Services
{
    public class SpriteLayerCatalog : ISpriteLayerCatalog
    {
        // Z-order constants (LPC convention: body lowest, weapon on top)
        private const int ZBody = 10;
        private const int ZArmor = 40;
        private const int ZHair = 80;
        private const int ZWeapon = 100;

        public List<string> HairStyles { get; } = new() { "Long", "Short", "Mohawk", "Messy" };

        public List<SpriteLayerDefinition> GetLayersForAppearance(CharacterAppearance appearance)
        {
            var layers = new List<SpriteLayerDefinition>
            {
                new($"sprites/body/{appearance.SkinColor.ToLower()}.png", ZBody),
                new($"sprites/hair/{appearance.HairStyle.ToLower()}_{appearance.HairColor.ToLower()}.png", ZHair),
                new($"sprites/armor/{appearance.ArmorType.ToLower()}.png", ZArmor),
                new($"sprites/weapons/{appearance.WeaponType.ToLower()}.png", ZWeapon),
            };

            layers.Sort((a, b) => a.ZOrder.CompareTo(b.ZOrder));
            return layers;
        }

        public CharacterAppearance GetDefaultAppearanceForClass(string className)
        {
            var appearance = new CharacterAppearance();

            switch (className)
            {
                case "Warrior":
                    appearance.ArmorType = "Plate";
                    appearance.WeaponType = "Longsword";
                    break;
                case "Mage":
                    appearance.ArmorType = "Robe";
                    appearance.WeaponType = "Staff";
                    break;
                case "Rogue":
                    appearance.ArmorType = "Leather";
                    appearance.WeaponType = "Daggers";
                    break;
                default:
                    appearance.ArmorType = "Leather";
                    appearance.WeaponType = "Longsword";
                    break;
            }

            return appearance;
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test Darkness.Tests --filter "FullyQualifiedName~SpriteLayerCatalogTests" --verbosity normal
```

Expected: All 7 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add Darkness.Core/Interfaces/ISpriteLayerCatalog.cs Darkness.Core/Services/SpriteLayerCatalog.cs Darkness.Tests/Services/SpriteLayerCatalogTests.cs
git commit -m "feat: sprite layer catalog maps appearance choices to layer files"
```

---

### Task 4: Sprite Compositor (Layer PNG Compositing via SkiaSharp)

**Files:**
- Create: `Darkness.Core/Interfaces/ISpriteCompositor.cs`
- Create: `Darkness.Core/Services/SpriteCompositor.cs`
- Create: `Darkness.Tests/Services/SpriteCompositorTests.cs`

The compositor takes ordered layer streams and composites them into a single PNG byte array. It also extracts a single frame for the preview.

- [ ] **Step 1: Create ISpriteCompositor interface**

Create `Darkness.Core/Interfaces/ISpriteCompositor.cs`:

```csharp
namespace Darkness.Core.Interfaces
{
    public interface ISpriteCompositor
    {
        byte[] CompositeLayers(IReadOnlyList<Stream> layerStreams, int sheetWidth, int sheetHeight);
        byte[] ExtractFrame(byte[] spriteSheetPng, int frameX, int frameY, int frameWidth, int frameHeight, int scale);
    }
}
```

`CompositeLayers` alpha-composites all layers onto a canvas of the given size.
`ExtractFrame` cuts out a single frame and scales it up for the MAUI preview.

- [ ] **Step 2: Write the failing tests**

Create `Darkness.Tests/Services/SpriteCompositorTests.cs`:

```csharp
using Darkness.Core.Services;
using SkiaSharp;

namespace Darkness.Tests.Services
{
    public class SpriteCompositorTests
    {
        private readonly SpriteCompositor _compositor = new();

        private Stream CreateTestLayerStream(int width, int height, SKColor color)
        {
            using var bitmap = new SKBitmap(width, height);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(color);
            var stream = new MemoryStream();
            bitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
            stream.Position = 0;
            return stream;
        }

        [Fact]
        public void CompositeLayers_SingleLayer_ReturnsSameDimensions()
        {
            using var layer = CreateTestLayerStream(832, 1344, SKColors.Red);
            var result = _compositor.CompositeLayers(new List<Stream> { layer }, 832, 1344);

            Assert.NotNull(result);
            Assert.True(result.Length > 0);

            using var resultBitmap = SKBitmap.Decode(result);
            Assert.Equal(832, resultBitmap.Width);
            Assert.Equal(1344, resultBitmap.Height);
        }

        [Fact]
        public void CompositeLayers_TwoLayers_TopLayerOverlaps()
        {
            // Bottom: solid red
            using var bottom = CreateTestLayerStream(64, 64, SKColors.Red);
            // Top: solid blue (fully opaque, should completely cover red)
            using var top = CreateTestLayerStream(64, 64, SKColors.Blue);

            var result = _compositor.CompositeLayers(new List<Stream> { bottom, top }, 64, 64);

            using var resultBitmap = SKBitmap.Decode(result);
            var pixel = resultBitmap.GetPixel(32, 32);
            Assert.Equal(SKColors.Blue, pixel);
        }

        [Fact]
        public void CompositeLayers_TransparentTopLayer_ShowsBottom()
        {
            using var bottom = CreateTestLayerStream(64, 64, SKColors.Red);
            using var top = CreateTestLayerStream(64, 64, SKColors.Transparent);

            var result = _compositor.CompositeLayers(new List<Stream> { bottom, top }, 64, 64);

            using var resultBitmap = SKBitmap.Decode(result);
            var pixel = resultBitmap.GetPixel(32, 32);
            Assert.Equal(SKColors.Red, pixel);
        }

        [Fact]
        public void CompositeLayers_EmptyList_ReturnsTransparentSheet()
        {
            var result = _compositor.CompositeLayers(new List<Stream>(), 64, 64);

            using var resultBitmap = SKBitmap.Decode(result);
            Assert.Equal(64, resultBitmap.Width);
            var pixel = resultBitmap.GetPixel(0, 0);
            Assert.Equal(0, pixel.Alpha);
        }

        [Fact]
        public void ExtractFrame_ReturnsScaledFrame()
        {
            // Create a 128x128 sheet (2x2 grid of 64x64 frames)
            using var bitmap = new SKBitmap(128, 128);
            using var canvas = new SKCanvas(bitmap);
            // Top-left frame = red, top-right = blue
            canvas.Clear(SKColors.Transparent);
            using var redPaint = new SKPaint { Color = SKColors.Red };
            canvas.DrawRect(0, 0, 64, 64, redPaint);
            using var bluePaint = new SKPaint { Color = SKColors.Blue };
            canvas.DrawRect(64, 0, 64, 64, bluePaint);

            var sheetPng = new MemoryStream();
            bitmap.Encode(sheetPng, SKEncodedImageFormat.Png, 100);
            var sheetBytes = sheetPng.ToArray();

            // Extract top-left frame at 4x scale
            var result = _compositor.ExtractFrame(sheetBytes, 0, 0, 64, 64, 4);

            using var frame = SKBitmap.Decode(result);
            Assert.Equal(256, frame.Width);  // 64 * 4
            Assert.Equal(256, frame.Height);
            Assert.Equal(SKColors.Red, frame.GetPixel(128, 128));
        }
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

```bash
dotnet test Darkness.Tests --filter "FullyQualifiedName~SpriteCompositorTests" --verbosity normal
```

Expected: FAIL — `SpriteCompositor` does not exist yet.

- [ ] **Step 4: Implement SpriteCompositor**

Create `Darkness.Core/Services/SpriteCompositor.cs`:

```csharp
using Darkness.Core.Interfaces;
using SkiaSharp;

namespace Darkness.Core.Services
{
    public class SpriteCompositor : ISpriteCompositor
    {
        public byte[] CompositeLayers(IReadOnlyList<Stream> layerStreams, int sheetWidth, int sheetHeight)
        {
            using var composite = new SKBitmap(sheetWidth, sheetHeight);
            using var canvas = new SKCanvas(composite);
            canvas.Clear(SKColors.Transparent);

            foreach (var stream in layerStreams)
            {
                using var layerBitmap = SKBitmap.Decode(stream);
                if (layerBitmap != null)
                {
                    canvas.DrawBitmap(layerBitmap, 0, 0);
                }
            }

            using var image = SKImage.FromBitmap(composite);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

        public byte[] ExtractFrame(byte[] spriteSheetPng, int frameX, int frameY, int frameWidth, int frameHeight, int scale)
        {
            using var sheet = SKBitmap.Decode(spriteSheetPng);
            var sourceRect = new SKRectI(frameX, frameY, frameX + frameWidth, frameY + frameHeight);

            int scaledWidth = frameWidth * scale;
            int scaledHeight = frameHeight * scale;

            using var frameBitmap = new SKBitmap(scaledWidth, scaledHeight);
            using var canvas = new SKCanvas(frameBitmap);
            canvas.Clear(SKColors.Transparent);

            var destRect = new SKRect(0, 0, scaledWidth, scaledHeight);
            using var paint = new SKPaint { FilterQuality = SKFilterQuality.None }; // Nearest-neighbor for pixel art
            canvas.DrawBitmap(sheet, sourceRect, destRect, paint);

            using var image = SKImage.FromBitmap(frameBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
    }
}
```

Key design decisions:
- `FilterQuality = SKFilterQuality.None` — nearest-neighbor scaling preserves pixel art crispness.
- Layers are drawn in order (caller sorts by z-order before passing streams).
- Returns `byte[]` PNG so the result is usable in both MAUI (`ImageSource.FromStream`) and MonoGame (`Texture2D.FromStream`).

- [ ] **Step 5: Add SkiaSharp to test project**

```bash
cd Darkness.Tests && dotnet add package SkiaSharp --version 3.116.1
```

- [ ] **Step 6: Run tests to verify they pass**

```bash
dotnet test Darkness.Tests --filter "FullyQualifiedName~SpriteCompositorTests" --verbosity normal
```

Expected: All 5 tests PASS.

- [ ] **Step 7: Commit**

```bash
git add Darkness.Core/Interfaces/ISpriteCompositor.cs Darkness.Core/Services/SpriteCompositor.cs Darkness.Tests/Services/SpriteCompositorTests.cs Darkness.Tests/Darkness.Tests.csproj
git commit -m "feat: sprite compositor composites layer PNGs via SkiaSharp"
```

---

### Task 5: Bundle Placeholder LPC Layer PNGs

**Files:**
- Create: `Darkness.MAUI/Resources/Raw/sprites/body/light.png`
- Create: `Darkness.MAUI/Resources/Raw/sprites/body/tan.png`
- Create: `Darkness.MAUI/Resources/Raw/sprites/body/dark.png`
- Create: `Darkness.MAUI/Resources/Raw/sprites/hair/long_black.png` (and all style/color combos)
- Create: `Darkness.MAUI/Resources/Raw/sprites/armor/plate.png`
- Create: `Darkness.MAUI/Resources/Raw/sprites/armor/robe.png`
- Create: `Darkness.MAUI/Resources/Raw/sprites/armor/leather.png`
- Create: `Darkness.MAUI/Resources/Raw/sprites/weapons/longsword.png`
- Create: `Darkness.MAUI/Resources/Raw/sprites/weapons/staff.png`
- Create: `Darkness.MAUI/Resources/Raw/sprites/weapons/daggers.png`

This task requires downloading individual LPC layer PNGs from the Universal LPC Spritesheet Character Generator and organizing them into the expected directory structure. Each PNG must be a full **832×1344** sprite sheet for that single layer (body only, hair only, etc.) so they composite correctly.

- [ ] **Step 1: Create the sprite directory structure**

```bash
mkdir -p Darkness.MAUI/Resources/Raw/sprites/body
mkdir -p Darkness.MAUI/Resources/Raw/sprites/hair
mkdir -p Darkness.MAUI/Resources/Raw/sprites/armor
mkdir -p Darkness.MAUI/Resources/Raw/sprites/weapons
```

- [ ] **Step 2: Generate and save body layer PNGs**

Using the [Universal LPC Spritesheet Character Generator](https://sanderfrenken.github.io/Universal-LPC-Spritesheet-Character-Generator), generate **body-only** sheets (no hair, no equipment) for each skin tone. Save as:

| Skin | File |
|------|------|
| Light | `Darkness.MAUI/Resources/Raw/sprites/body/light.png` |
| Tan | `Darkness.MAUI/Resources/Raw/sprites/body/tan.png` |
| Dark | `Darkness.MAUI/Resources/Raw/sprites/body/dark.png` |

Each file must be an 832×1344 transparent PNG with only the base body.

If the LPC generator files are not immediately available, create **solid-color placeholder PNGs** (832×1344) so the pipeline can be tested end-to-end. Use a script like:

```csharp
// Placeholder generator (run once, then delete)
using SkiaSharp;
var colors = new Dictionary<string, SKColor>
{
    ["light"] = new SKColor(255, 224, 189),
    ["tan"] = new SKColor(210, 170, 120),
    ["dark"] = new SKColor(140, 90, 60),
};
foreach (var (name, color) in colors)
{
    using var bmp = new SKBitmap(832, 1344);
    using var canvas = new SKCanvas(bmp);
    canvas.Clear(SKColors.Transparent);
    // Draw a 32x48 figure in frame (0, 10*64) = south-facing idle
    using var paint = new SKPaint { Color = color };
    canvas.DrawRect(16, 10 * 64 + 8, 32, 48, paint);
    using var img = SKImage.FromBitmap(bmp);
    using var data = img.Encode(SKEncodedImageFormat.Png, 100);
    File.WriteAllBytes($"Darkness.MAUI/Resources/Raw/sprites/body/{name}.png", data.ToArray());
}
```

- [ ] **Step 3: Generate and save hair layer PNGs**

For each combination of style × color, save a hair-only layer PNG:

| Naming pattern | Example |
|----------------|---------|
| `{style}_{color}.png` | `long_black.png`, `short_blonde.png`, `mohawk_red.png`, `messy_white.png` |

Full list (4 styles × 5 colors = 20 files):
- `long_black.png`, `long_blonde.png`, `long_brown.png`, `long_red.png`, `long_white.png`
- `short_black.png`, `short_blonde.png`, `short_brown.png`, `short_red.png`, `short_white.png`
- `mohawk_black.png`, `mohawk_blonde.png`, `mohawk_brown.png`, `mohawk_red.png`, `mohawk_white.png`
- `messy_black.png`, `messy_blonde.png`, `messy_brown.png`, `messy_red.png`, `messy_white.png`

All in `Darkness.MAUI/Resources/Raw/sprites/hair/`. Each 832×1344, transparent except the hair pixels.

For placeholders, draw small colored rectangles at the top of the idle frame.

- [ ] **Step 4: Generate and save armor layer PNGs**

| Armor | File |
|-------|------|
| Plate | `Darkness.MAUI/Resources/Raw/sprites/armor/plate.png` |
| Robe | `Darkness.MAUI/Resources/Raw/sprites/armor/robe.png` |
| Leather | `Darkness.MAUI/Resources/Raw/sprites/armor/leather.png` |

Each 832×1344, transparent except the armor pixels overlaying the body.

- [ ] **Step 5: Generate and save weapon layer PNGs**

| Weapon | File |
|--------|------|
| Longsword | `Darkness.MAUI/Resources/Raw/sprites/weapons/longsword.png` |
| Staff | `Darkness.MAUI/Resources/Raw/sprites/weapons/staff.png` |
| Daggers | `Darkness.MAUI/Resources/Raw/sprites/weapons/daggers.png` |

Each 832×1344, transparent except the weapon pixels.

- [ ] **Step 6: Verify assets are bundled**

```bash
dotnet build Darkness.MAUI/Darkness.MAUI.csproj -f net10.0-windows10.0.19041.0
```

Expected: 0 errors, PNGs included as MauiAsset.

- [ ] **Step 7: Commit**

```bash
git add Darkness.MAUI/Resources/Raw/sprites/
git commit -m "feat: add LPC sprite layer PNGs for character creator"
```

---

### Task 6: Update CharacterGenViewModel with Live Preview

**Files:**
- Modify: `Darkness.Core/ViewModels/CharacterGenViewModel.cs`
- Modify: `Darkness.Tests/ViewModels/CharacterGenViewModelTests.cs`

- [ ] **Step 1: Write failing tests for the new preview behavior**

Add to `Darkness.Tests/ViewModels/CharacterGenViewModelTests.cs` — new tests at the end of the class:

```csharp
[Fact]
public void HairStyles_ContainsExpectedOptions()
{
    Assert.Contains("Long", _viewModel.HairStyles);
    Assert.Contains("Short", _viewModel.HairStyles);
    Assert.Contains("Mohawk", _viewModel.HairStyles);
    Assert.Contains("Messy", _viewModel.HairStyles);
}

[Fact]
public void DefaultValues_HairStyleIsLong()
{
    Assert.Equal("Long", _viewModel.SelectedHairStyle);
}

[Fact]
public async Task CreateCharacterAsync_SavesHairStyle()
{
    var user = new User { Id = 1 };
    _sessionServiceMock.Setup(x => x.CurrentUser).Returns(user);
    Character? savedCharacter = null;
    _characterServiceMock.Setup(x => x.SaveCharacterAsync(It.IsAny<Character>()))
        .Callback<Character>(c => savedCharacter = c)
        .ReturnsAsync(true);

    _viewModel.CharacterName = "Test";
    _viewModel.SelectedHairStyle = "Mohawk";

    await _viewModel.CreateCharacterCommand.ExecuteAsync(null);

    Assert.NotNull(savedCharacter);
    Assert.Equal("Mohawk", savedCharacter!.HairStyle);
}

[Fact]
public void ChangingClass_UpdatesArmorAndWeapon()
{
    _viewModel.SelectedClass = "Mage";
    Assert.Equal("Robe", _viewModel.SelectedArmor);
    Assert.Equal("Staff", _viewModel.SelectedWeapon);

    _viewModel.SelectedClass = "Rogue";
    Assert.Equal("Leather", _viewModel.SelectedArmor);
    Assert.Equal("Daggers", _viewModel.SelectedWeapon);
}
```

Also add these fields to the test class constructor — the ViewModel now takes additional dependencies. Update the constructor:

```csharp
private readonly Mock<ISpriteLayerCatalog> _catalogMock;
private readonly Mock<ISpriteCompositor> _compositorMock;
private readonly Mock<IFileSystemService> _fileSystemMock;

public CharacterGenViewModelTests()
{
    _characterServiceMock = new Mock<ICharacterService>();
    _sessionServiceMock = new Mock<ISessionService>();
    _navigationServiceMock = new Mock<INavigationService>();
    _dialogServiceMock = new Mock<IDialogService>();
    _catalogMock = new Mock<ISpriteLayerCatalog>();
    _compositorMock = new Mock<ISpriteCompositor>();
    _fileSystemMock = new Mock<IFileSystemService>();

    _catalogMock.Setup(x => x.HairStyles).Returns(new List<string> { "Long", "Short", "Mohawk", "Messy" });
    _catalogMock.Setup(x => x.GetDefaultAppearanceForClass(It.IsAny<string>()))
        .Returns<string>(cls =>
        {
            return cls switch
            {
                "Mage" => new CharacterAppearance { ArmorType = "Robe", WeaponType = "Staff" },
                "Rogue" => new CharacterAppearance { ArmorType = "Leather", WeaponType = "Daggers" },
                _ => new CharacterAppearance { ArmorType = "Plate", WeaponType = "Longsword" },
            };
        });
    _catalogMock.Setup(x => x.GetLayersForAppearance(It.IsAny<CharacterAppearance>()))
        .Returns(new List<SpriteLayerDefinition>());

    _viewModel = new CharacterGenViewModel(
        _characterServiceMock.Object,
        _sessionServiceMock.Object,
        _navigationServiceMock.Object,
        _dialogServiceMock.Object,
        _catalogMock.Object,
        _compositorMock.Object,
        _fileSystemMock.Object);
}
```

Ensure these using statements are at the top of the test file (add any that are missing):

```csharp
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Darkness.Core.Services;
using Darkness.Core.ViewModels;
using Moq;
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test Darkness.Tests --filter "FullyQualifiedName~CharacterGenViewModelTests" --verbosity normal
```

Expected: FAIL — constructor signature mismatch, missing properties.

- [ ] **Step 3: Update CharacterGenViewModel**

Replace the full contents of `Darkness.Core/ViewModels/CharacterGenViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;

namespace Darkness.Core.ViewModels
{
    public partial class CharacterGenViewModel : ViewModelBase
    {
        private readonly ICharacterService _characterService;
        private readonly ISessionService _sessionService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private readonly ISpriteLayerCatalog _catalog;
        private readonly ISpriteCompositor _compositor;
        private readonly IFileSystemService _fileSystem;

        [ObservableProperty]
        private string _characterName = string.Empty;

        [ObservableProperty]
        private string _selectedClass = "Warrior";

        [ObservableProperty]
        private string _selectedHairColor = "Black";

        [ObservableProperty]
        private string _selectedSkinColor = "Light";

        [ObservableProperty]
        private string _selectedHairStyle = "Long";

        [ObservableProperty]
        private string _selectedArmor = "Plate";

        [ObservableProperty]
        private string _selectedWeapon = "Longsword";

        [ObservableProperty]
        private byte[]? _previewImageBytes;

        public List<string> Classes { get; } = new() { "Warrior", "Mage", "Rogue" };
        public List<string> HairColors { get; } = new() { "Black", "Blonde", "Brown", "Red", "White" };
        public List<string> SkinColors { get; } = new() { "Light", "Tan", "Dark" };
        public List<string> HairStyles => _catalog.HairStyles;

        public CharacterGenViewModel(
            ICharacterService characterService,
            ISessionService sessionService,
            INavigationService navigationService,
            IDialogService dialogService,
            ISpriteLayerCatalog catalog,
            ISpriteCompositor compositor,
            IFileSystemService fileSystem)
        {
            _characterService = characterService;
            _sessionService = sessionService;
            _navigationService = navigationService;
            _dialogService = dialogService;
            _catalog = catalog;
            _compositor = compositor;
            _fileSystem = fileSystem;
        }

        partial void OnSelectedClassChanged(string value)
        {
            var defaults = _catalog.GetDefaultAppearanceForClass(value);
            SelectedArmor = defaults.ArmorType;
            SelectedWeapon = defaults.WeaponType;
            // Armor/weapon changes will trigger their own OnChanged → UpdatePreview
        }

        partial void OnSelectedHairColorChanged(string value) => UpdatePreviewAsync().FireAndForget();
        partial void OnSelectedSkinColorChanged(string value) => UpdatePreviewAsync().FireAndForget();
        partial void OnSelectedHairStyleChanged(string value) => UpdatePreviewAsync().FireAndForget();
        partial void OnSelectedArmorChanged(string value) => UpdatePreviewAsync().FireAndForget();
        partial void OnSelectedWeaponChanged(string value) => UpdatePreviewAsync().FireAndForget();

        public async Task UpdatePreviewAsync()
        {
            try
            {
                var appearance = new CharacterAppearance
                {
                    SkinColor = SelectedSkinColor,
                    HairStyle = SelectedHairStyle,
                    HairColor = SelectedHairColor,
                    ArmorType = SelectedArmor,
                    WeaponType = SelectedWeapon,
                };

                var layerDefs = _catalog.GetLayersForAppearance(appearance);
                var streams = new List<Stream>();

                foreach (var layer in layerDefs)
                {
                    try
                    {
                        var stream = await _fileSystem.OpenAppPackageFileAsync(layer.ResourcePath);
                        streams.Add(stream);
                    }
                    catch
                    {
                        // Skip missing layer files gracefully
                    }
                }

                if (streams.Count > 0)
                {
                    var sheetBytes = _compositor.CompositeLayers(streams, 832, 1344);
                    // Extract south-facing idle frame: row 10 (walk south), col 0
                    // Row 10 = walk rows start at row 8, south is row 10 (8 + 2)
                    PreviewImageBytes = _compositor.ExtractFrame(sheetBytes, 0, 10 * 64, 64, 64, 4);
                }

                foreach (var s in streams) s.Dispose();
            }
            catch
            {
                // Preview failure is non-critical
            }
        }

        [RelayCommand]
        public async Task CreateCharacterAsync()
        {
            if (string.IsNullOrWhiteSpace(CharacterName))
            {
                await _dialogService.DisplayAlertAsync("Error", "Please enter a name.", "OK");
                return;
            }

            if (_sessionService.CurrentUser == null)
            {
                await _dialogService.DisplayAlertAsync("Error", "No user session found. Please login again.", "OK");
                await _navigationService.NavigateToAsync("///LoadUserPage");
                return;
            }

            var character = new Character
            {
                UserId = _sessionService.CurrentUser.Id,
                Name = CharacterName,
                Class = SelectedClass,
                HairColor = SelectedHairColor,
                HairStyle = SelectedHairStyle,
                SkinColor = SelectedSkinColor,
                Level = 1,
                Experience = 0
            };

            SetBaseStats(character, SelectedClass);

            bool success = await _characterService.SaveCharacterAsync(character);

            if (success)
            {
                await _dialogService.DisplayAlertAsync("Success", "Character Created!", "OK");
                await _navigationService.NavigateToAsync("///MainPage");
            }
            else
            {
                await _dialogService.DisplayAlertAsync("Error", "Failed to save character.", "OK");
            }
        }

        private void SetBaseStats(Character character, string className)
        {
            switch (className)
            {
                case "Warrior":
                    character.STR = 15; character.DEX = 10; character.CON = 15;
                    character.INT = 5; character.WIS = 8; character.CHA = 10;
                    break;
                case "Mage":
                    character.STR = 5; character.DEX = 10; character.CON = 8;
                    character.INT = 18; character.WIS = 15; character.CHA = 10;
                    break;
                case "Rogue":
                    character.STR = 10; character.DEX = 18; character.CON = 10;
                    character.INT = 10; character.WIS = 8; character.CHA = 12;
                    break;
                default:
                    character.STR = 10; character.DEX = 10; character.CON = 10;
                    character.INT = 10; character.WIS = 10; character.CHA = 10;
                    break;
            }

            character.MaxHP = character.CON * 10;
            character.CurrentHP = character.MaxHP;
            character.Mana = character.WIS * 5;
            character.Stamina = character.CON * 5;
            character.Speed = character.DEX;
            character.Accuracy = 80 + character.DEX / 2;
            character.Evasion = character.DEX / 2;
            character.Defense = character.CON / 2;
            character.MagicDefense = character.WIS / 2;
        }
    }

    internal static class TaskExtensions
    {
        public static async void FireAndForget(this Task task)
        {
            try { await task; } catch { }
        }
    }
}
```

Key design:
- `OnSelectedClassChanged` auto-updates armor and weapon to class defaults via the catalog.
- Every appearance property change triggers `UpdatePreviewAsync` (fire-and-forget from the sync partial method).
- `PreviewImageBytes` is a `byte[]` that the XAML page converts to an `ImageSource` via a value converter.
- The south-facing idle frame is at row 10 (walk animation group starts at row 8; south direction is the 3rd row = index 10), column 0.

- [ ] **Step 4: Update existing tests for new constructor signature**

The existing tests that create `CharacterGenViewModel` with 4 parameters need updating to pass the 3 new mocks. Apply the constructor changes from Step 1 to the test class. All existing tests should continue to pass with the expanded constructor.

- [ ] **Step 5: Run all CharacterGenViewModel tests**

```bash
dotnet test Darkness.Tests --filter "FullyQualifiedName~CharacterGenViewModelTests" --verbosity normal
```

Expected: All tests PASS (old + new).

- [ ] **Step 6: Commit**

```bash
git add Darkness.Core/ViewModels/CharacterGenViewModel.cs Darkness.Tests/ViewModels/CharacterGenViewModelTests.cs
git commit -m "feat: add live sprite preview and hair style picker to character gen"
```

---

### Task 7: Update CharacterGenPage XAML with Preview and Hair Style Picker

**Files:**
- Modify: `Darkness.MAUI/Pages/CharacterGenPage.xaml`
- Create: `Darkness.MAUI/Converters/ByteArrayToImageSourceConverter.cs`

- [ ] **Step 1: Create the value converter**

Create `Darkness.MAUI/Converters/ByteArrayToImageSourceConverter.cs`:

```csharp
using System.Globalization;

namespace Darkness.MAUI.Converters
{
    public class ByteArrayToImageSourceConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is byte[] bytes && bytes.Length > 0)
            {
                return ImageSource.FromStream(() => new MemoryStream(bytes));
            }
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
```

- [ ] **Step 2: Update CharacterGenPage.xaml**

Replace the full contents of `Darkness.MAUI/Pages/CharacterGenPage.xaml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vm="using:Darkness.Core.ViewModels"
             xmlns:conv="using:Darkness.MAUI.Converters"
             x:Class="Darkness.MAUI.Pages.CharacterGenPage"
             x:DataType="vm:CharacterGenViewModel"
             Title="Character Creation"
             BackgroundColor="#121212">

    <ContentPage.Resources>
        <conv:ByteArrayToImageSourceConverter x:Key="BytesToImage" />
    </ContentPage.Resources>

    <ScrollView>
        <VerticalStackLayout Spacing="20" Padding="30" VerticalOptions="Center">
            <Label Text="Generate Your Hero"
                   FontSize="32"
                   TextColor="White"
                   HorizontalOptions="Center"
                   FontAttributes="Bold" />

            <!-- Character Preview -->
            <Border StrokeThickness="2"
                    Stroke="#333"
                    BackgroundColor="#1A1A1A"
                    HorizontalOptions="Center"
                    HeightRequest="280"
                    WidthRequest="280"
                    Padding="10">
                <Image Source="{Binding PreviewImageBytes, Converter={StaticResource BytesToImage}}"
                       WidthRequest="256"
                       HeightRequest="256"
                       Aspect="AspectFit"
                       HorizontalOptions="Center"
                       VerticalOptions="Center" />
            </Border>

            <Entry Text="{Binding CharacterName}"
                   Placeholder="Character Name"
                   PlaceholderColor="Gray"
                   TextColor="White"
                   BackgroundColor="#1E1E1E" />

            <Label Text="Class" TextColor="White" />
            <Picker SelectedItem="{Binding SelectedClass}"
                    ItemsSource="{Binding Classes}"
                    Title="Select Class"
                    TextColor="White"
                    BackgroundColor="#1E1E1E" />

            <Label Text="Hair Style" TextColor="White" />
            <Picker SelectedItem="{Binding SelectedHairStyle}"
                    ItemsSource="{Binding HairStyles}"
                    Title="Select Hair Style"
                    TextColor="White"
                    BackgroundColor="#1E1E1E" />

            <Label Text="Hair Color" TextColor="White" />
            <Picker SelectedItem="{Binding SelectedHairColor}"
                    ItemsSource="{Binding HairColors}"
                    Title="Select Hair Color"
                    TextColor="White"
                    BackgroundColor="#1E1E1E" />

            <Label Text="Skin Color" TextColor="White" />
            <Picker SelectedItem="{Binding SelectedSkinColor}"
                    ItemsSource="{Binding SkinColors}"
                    Title="Select Skin Color"
                    TextColor="White"
                    BackgroundColor="#1E1E1E" />

            <Button Text="Create Character"
                    Command="{Binding CreateCharacterCommand}"
                    BackgroundColor="#B71C1C"
                    TextColor="White"
                    CornerRadius="10"
                    HeightRequest="50" />
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

Key changes:
- Added `ByteArrayToImageSourceConverter` resource.
- Added character preview `Image` in a `Border` frame at the top.
- Added Hair Style picker bound to `SelectedHairStyle` / `HairStyles`.
- Added `x:DataType` and `xmlns:vm` / `xmlns:conv` for compiled bindings.

- [ ] **Step 3: Build to verify**

```bash
dotnet build Darkness.MAUI/Darkness.MAUI.csproj -f net10.0-windows10.0.19041.0
```

Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add Darkness.MAUI/Pages/CharacterGenPage.xaml Darkness.MAUI/Converters/ByteArrayToImageSourceConverter.cs
git commit -m "feat: add sprite preview and hair style picker to character gen UI"
```

---

### Task 8: Register New Services in DI

**Files:**
- Modify: `Darkness.MAUI/MauiProgram.cs`

- [ ] **Step 1: Update MauiProgram.cs**

In `Darkness.MAUI/MauiProgram.cs`, add the service registrations. Add these lines after the existing `AddSingleton<IDialogService, MauiDialogService>()` line:

```csharp
builder.Services.AddSingleton<ISpriteLayerCatalog, SpriteLayerCatalog>();
builder.Services.AddSingleton<ISpriteCompositor, SpriteCompositor>();
```

Add the required using statements at the top of the file:

```csharp
using Darkness.Core.Interfaces;
using Darkness.Core.Services;
```

Wait — `Darkness.Core.Interfaces` and `Darkness.Core.Services` are already used implicitly through other registrations. But the specific new types need the using. Check that the existing usings cover them:

The file already has `using Darkness.Core.Interfaces;` and `using Darkness.Core.Services;` — these cover `ISpriteLayerCatalog`, `ISpriteCompositor`, `SpriteLayerCatalog`, and `SpriteCompositor`. No new usings needed.

- [ ] **Step 2: Build the full solution**

```bash
dotnet build Darkness.MAUI/Darkness.MAUI.csproj -f net10.0-windows10.0.19041.0
```

Expected: 0 errors.

- [ ] **Step 3: Run all tests**

```bash
dotnet test Darkness.Tests --verbosity normal
```

Expected: All tests PASS.

- [ ] **Step 4: Commit**

```bash
git add Darkness.MAUI/MauiProgram.cs
git commit -m "feat: register sprite compositor and catalog in DI container"
```

---

### Task 9: Trigger Initial Preview on Page Load

**Files:**
- Modify: `Darkness.MAUI/Pages/CharacterGenPage.xaml.cs`

The preview needs an initial render when the page appears, since the property-changed handlers only fire on user interaction.

- [ ] **Step 1: Update CharacterGenPage code-behind**

Replace the full contents of `Darkness.MAUI/Pages/CharacterGenPage.xaml.cs`:

```csharp
using Darkness.Core.ViewModels;

namespace Darkness.MAUI.Pages
{
    public partial class CharacterGenPage : ContentPage
    {
        private readonly CharacterGenViewModel _viewModel;

        public CharacterGenPage(CharacterGenViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.UpdatePreviewAsync();
        }
    }
}
```

- [ ] **Step 2: Build to verify**

```bash
dotnet build Darkness.MAUI/Darkness.MAUI.csproj -f net10.0-windows10.0.19041.0
```

Expected: 0 errors.

- [ ] **Step 3: Commit**

```bash
git add Darkness.MAUI/Pages/CharacterGenPage.xaml.cs
git commit -m "feat: trigger initial sprite preview on character gen page load"
```

---

### Task 10: End-to-End Verification

- [ ] **Step 1: Run the full test suite**

```bash
dotnet test Darkness.Tests --verbosity normal
```

Expected: All tests PASS (existing + new tests from Tasks 3, 4, 6).

- [ ] **Step 2: Build the full solution**

```bash
dotnet build Darkness.sln
```

Expected: 0 errors.

- [ ] **Step 3: Run the app and verify the character creator**

```bash
dotnet build Darkness.MAUI -f net10.0-windows10.0.19041.0
```

Launch the app. Navigate to character creation. Verify:
- The preview image area is visible (even if placeholder PNGs show colored rectangles).
- Changing any picker (class, hair style, hair color, skin color) updates the preview.
- Selecting a class auto-updates the armor/weapon display.
- Creating a character still saves correctly with the new `HairStyle` field.

- [ ] **Step 4: Final commit**

```bash
git add -A
git commit -m "feat: complete Tier A LPC sprite character creator with live preview"
```
