using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Darkness.Godot.Game;

public partial class LayeredSprite : Node2D
{
    private Dictionary<string, AnimatedSprite2D> _layers = new();
    private ShaderMaterial _simpleMaterial = null!;
    private bool _flipH = false;

    public bool FlipH
    {
        get => _flipH;
        set
        {
            _flipH = value;
            foreach (var sprite in _layers.Values)
            {
                sprite.FlipH = _flipH;
            }
        }
    }
    
    public override void _Ready()
    {
        if (!IsInsideTree()) return;
        EnsureLayers();
    }

    private void EnsureLayers()
    {
        if (_layers.Count > 0) return;

        var shader = GD.Load<Shader>("res://src/Shaders/simple_2d.gdshader");
        _simpleMaterial = new ShaderMaterial { Shader = shader };

        foreach (var child in GetChildren())
        {
            if (child is AnimatedSprite2D sprite)
            {
                _layers[sprite.Name] = sprite;
                sprite.Material = _simpleMaterial;
            }
        }
    }

    public async Task SetupCharacter(Character c, ISpriteLayerCatalog catalog, IFileSystemService fileSystem)
    {
        // Removed IsInsideTree guard - setup should always run
        EnsureLayers();

        if (c.FullSpriteSheet != null && c.FullSpriteSheet.Length > 0)
        {
            await SetupFromBytes(c.FullSpriteSheet);
            return;
        }

        // Total clear of all possible parts
        foreach (var layer in _layers.Values)
        {
            layer.SpriteFrames = null;
            layer.Hide();
        }

        var appearance = new CharacterAppearance
        {
            SkinColor = c.SkinColor,
            Face = c.Face ?? "Default",
            Eyes = c.Eyes ?? "Default",
            HairStyle = c.HairStyle,
            HairColor = c.HairColor,
            ArmorType = c.ArmorType,
            WeaponType = c.WeaponType,
            Feet = c.Feet,
            Arms = c.Arms,
            Legs = c.Legs,
            Head = "Human Male"
        };

        var layerDefs = catalog.GetLayersForAppearance(appearance);
        
        foreach (var def in layerDefs)
        {
            // Map the Z-order or Path to a specific node
            var nodeName = GetNodeNameForPath(def.ResourcePath);
            if (_layers.TryGetValue(nodeName, out var sprite))
            {
                var frames = await LoadFrames(def.ResourcePath, fileSystem);
                sprite.SpriteFrames = frames;
                sprite.FlipH = _flipH;
                sprite.Show();
            }
        }
    }

    public async Task SetupFromBytes(byte[] data)
    {
        EnsureLayers();

        // Total clear of all possible parts
        foreach (var layer in _layers.Values)
        {
            layer.SpriteFrames = null;
            layer.Hide();
        }

        if (!_layers.TryGetValue("Body", out var bodySprite))
        {
            GD.PrintErr("[LayeredSprite] Body layer not found in dictionary!");
            return;
        }

        var frames = ImageUtils.CreateSpriteFrames(data, 64, 64);
        if (frames != null)
        {
            bodySprite.SpriteFrames = frames;
            bodySprite.FlipH = _flipH;
            bodySprite.Show();

            if (frames.HasAnimation("idle_down"))
            {
                bodySprite.Play("idle_down");
            }
        }
    }

    public async Task SetupMonster(string monsterType, IFileSystemService fileSystem)
    {
        // Removed IsInsideTree guard - setup should always run
        EnsureLayers();
        GD.Print($"[LayeredSprite] SetupMonster started for: {monsterType}");
        
        // Total clear of all possible parts (Body, Head, Armor, etc.)
        foreach (var layer in _layers.Values)
        {
            layer.SpriteFrames = null;
            layer.Hide();
        }

        if (!_layers.TryGetValue("Body", out var bodySprite)) 
        {
            GD.PrintErr("[LayeredSprite] Body layer not found in dictionary!");
            return;
        }

        var frames = new SpriteFrames();
        if (frames.HasAnimation("default")) frames.RemoveAnimation("default");

        string basePath = $"sprites/monsters/{monsterType.ToLower()}/";
        GD.Print($"[LayeredSprite] Base path: {basePath}");

        // Try to load common animations
        await LoadIfExists(frames, "idle", basePath + "idle.png", fileSystem);
        await LoadIfExists(frames, "walk", basePath + "walk.png", fileSystem);
        await LoadIfExists(frames, "run", basePath + "run.png", fileSystem);
        await LoadIfExists(frames, "jump", basePath + "jump.png", fileSystem);

        GD.Print($"[LayeredSprite] Loaded {frames.GetAnimationNames().Length} animations.");

        bodySprite.SpriteFrames = frames;
        bodySprite.FlipH = _flipH;
        bodySprite.Show();
        
        if (frames.HasAnimation("idle"))
        {
            bodySprite.Play("idle");
        }
    }

    public async Task SetupFullSheet(string path, IFileSystemService fileSystem)
    {
        EnsureLayers();

        // Total clear of all possible parts
        foreach (var layer in _layers.Values)
        {
            layer.SpriteFrames = null;
            layer.Hide();
        }

        if (!_layers.TryGetValue("Body", out var bodySprite))
        {
            GD.PrintErr("[LayeredSprite] Body layer not found in dictionary!");
            return;
        }

        var frames = await LoadFrames(path, fileSystem);
        if (frames != null)
        {
            bodySprite.SpriteFrames = frames;
            bodySprite.FlipH = _flipH;
            bodySprite.Show();

            if (frames.HasAnimation("idle_down"))
            {
                bodySprite.Play("idle_down");
            }
        }
    }

    private async Task LoadIfExists(SpriteFrames frames, string animName, string path, IFileSystemService fileSystem)
    {
        try
        {
            GD.Print($"[LayeredSprite] Attempting to load: {path}");
            var stream = await fileSystem.OpenAppPackageFileAsync(path);
            using var ms = new System.IO.MemoryStream();
            await stream.CopyToAsync(ms);
            var data = ms.ToArray();
            
            var img = new Image();
            img.LoadPngFromBuffer(data);
            int frameH = img.GetHeight();
            
            // Heuristic for hound sprites: they are 64px wide even if only 32px tall
            int frameW = (frameH <= 48) ? 64 : frameH;
            
            GD.Print($"[LayeredSprite] Successfully read {data.Length} bytes for {animName}. Detected Frame Size: {frameW}x{frameH}");
            ImageUtils.AddAnimationFromBytes(frames, animName, data, frameW, frameH);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[LayeredSprite] Failed to load {animName} from {path}: {ex.Message}");
        }
    }

    private string GetNodeNameForPath(string path)
    {
        if (path.Contains("/body/")) return "Body";
        if (path.Contains("/head/")) return "Head";
        if (path.Contains("/face/")) return "Face";
        if (path.Contains("/eyes/")) return "Eyes";
        if (path.Contains("/hair/")) return "Hair";
        if (path.Contains("/armor/")) return "Armor";
        if (path.Contains("/feet/")) return "Feet";
        if (path.Contains("/legs/")) return "Legs";
        if (path.Contains("/arms/")) return "Arms";
        if (path.Contains("/weapons/")) return "Weapon";
        return "Body";
    }

    private async Task<SpriteFrames?> LoadFrames(string path, IFileSystemService fileSystem)
    {
        // We use the filesystem service to get the PNG bytes, then ImageUtils to slice them
        var stream = await fileSystem.OpenAppPackageFileAsync(path);
        using var ms = new System.IO.MemoryStream();
        await stream.CopyToAsync(ms);
        return ImageUtils.CreateSpriteFrames(ms.ToArray(), 64, 64);
    }

    public void Play(string animation)
    {
        if (!GodotObject.IsInstanceValid(this) || !IsInsideTree()) return;
        foreach (var sprite in _layers.Values)
        {
            if (sprite.Visible && sprite.SpriteFrames != null && sprite.SpriteFrames.HasAnimation(animation))
            {
                sprite.Play(animation);
            }
        }
    }

    public bool HasAnimation(string animation)
    {
        // We check the Body layer as the primary indicator for animation existence
        if (_layers.TryGetValue("Body", out var bodySprite))
        {
            return bodySprite.SpriteFrames != null && bodySprite.SpriteFrames.HasAnimation(animation);
        }
        return false;
    }
}
