using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

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

    public void ResetLayers()
    {
        EnsureLayers();
        foreach (var layer in _layers.Values)
        {
            layer.SpriteFrames = null;
            layer.Hide();
            layer.Frame = 0;
            layer.Stop();
        }
    }

    public async Task SetupCharacter(Character c, ISheetDefinitionCatalog catalog, IFileSystemService fileSystem,
        ISpriteCompositor? compositor = null)
    {
        EnsureLayers();
        GD.Print($"[LayeredSprite] SetupCharacter started for {c.Name}. Class: {c.Class}");

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
            Head = c.Head ?? "Human Male",
            ShieldType = c.ShieldType ?? "None",
            OffHandType = c.OffHandType ?? "None"
        };

        if (c.FullSpriteSheet != null && c.FullSpriteSheet.Length > 0)
        {
            GD.Print($"[LayeredSprite] Using FullSpriteSheet for {c.Name} ({c.FullSpriteSheet.Length} bytes)");
            await SetupFromBytes(c.FullSpriteSheet);
        }
        else if (compositor != null)
        {
            // Generate sprite sheet on the fly from appearance data
            GD.Print($"[LayeredSprite] Generating base sprite sheet for {c.Name}...");

            try
            {
                var definitions = catalog.GetSheetDefinitions(appearance);
                GD.Print($"[LayeredSprite] Compositing {definitions.Count} sheet definitions...");
                
                c.FullSpriteSheet = await compositor.CompositeFullSheet(definitions, appearance, fileSystem);
                GD.Print($"[LayeredSprite] Generated sheet for {c.Name}: {c.FullSpriteSheet?.Length ?? 0} bytes");
                
                if (c.FullSpriteSheet != null && c.FullSpriteSheet.Length > 0)
                {
                    await SetupFromBytes(c.FullSpriteSheet);
                }
                else
                {
                    GD.PrintErr($"[LayeredSprite] CompositeFullSheet returned empty/null buffer for {c.Name}");
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[LayeredSprite] Failed to generate sheet for {c.Name}: {ex.Message}");
                GD.PrintErr(ex.StackTrace);
            }
        }
        else
        {
            GD.PrintErr($"[LayeredSprite] No compositor provided for {c.Name} — cannot generate sprite sheet.");
        }
    }

    public async Task SetupFromBytes(byte[] data)
    {
        GD.Print($"[LayeredSprite] SetupFromBytes called with {data.Length} bytes.");
        ResetLayers();

        if (!_layers.TryGetValue("Body", out var bodySprite))
        {
            GD.PrintErr("[LayeredSprite] Body layer not found in dictionary!");
            return;
        }

        var frames = ImageUtils.CreateSpriteFrames(data, 64, 64);
        if (frames != null)
        {
            GD.Print($"[LayeredSprite] Created SpriteFrames from bytes. Animations: {string.Join(", ", frames.GetAnimationNames())}");
            bodySprite.SpriteFrames = frames;
            bodySprite.FlipH = _flipH;
            bodySprite.Show();
            bodySprite.Modulate = new Color(1, 1, 1, 1); // Reset transparency

            if (frames.HasAnimation("idle_down"))
            {
                bodySprite.Play("idle_down");
            }
            else if (frames.GetAnimationNames().Length > 0)
            {
                bodySprite.Play(frames.GetAnimationNames()[0]);
            }
        }
        else
        {
            GD.PrintErr("[LayeredSprite] Failed to create SpriteFrames from bytes.");
        }
    }

    public async Task SetupMonster(string monsterType, IFileSystemService fileSystem)
    {
        ResetLayers();
        GD.Print($"[LayeredSprite] SetupMonster started for: {monsterType}");

        if (!_layers.TryGetValue("Body", out var bodySprite))
        {
            GD.PrintErr("[LayeredSprite] Body layer not found in dictionary!");
            return;
        }

        var frames = new SpriteFrames();
        if (frames.HasAnimation("default")) frames.RemoveAnimation("default");

        string basePath = $"sprites/monsters/{monsterType.ToLower()}/";
        GD.Print($"[LayeredSprite] Base path: {basePath}");

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
        ResetLayers();

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

            int frameW = (frameH <= 48) ? 64 : frameH;

            GD.Print($"[LayeredSprite] Successfully read {data.Length} bytes for {animName}. Detected Frame Size: {frameW}x{frameH}");
            ImageUtils.AddAnimationFromBytes(frames, animName, data, frameW, frameH);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[LayeredSprite] Failed to load {animName} from {path}: {ex.Message}");
        }
    }

    private async Task<SpriteFrames?> LoadFrames(string path, IFileSystemService fileSystem)
    {
        var stream = await fileSystem.OpenAppPackageFileAsync(path);
        using var ms = new System.IO.MemoryStream();
        await stream.CopyToAsync(ms);
        return ImageUtils.CreateSpriteFrames(ms.ToArray(), 64, 64);
    }

    public void Play(string animation)
    {
        if (!GodotObject.IsInstanceValid(this) || !IsInsideTree()) return;
        foreach (var kvp in _layers)
        {
            var sprite = kvp.Value;
            if (sprite.Visible && sprite.SpriteFrames != null && sprite.SpriteFrames.HasAnimation(animation))
            {
                sprite.Play(animation);
                
                // Handle positioning for oversize frames
                var tex = sprite.SpriteFrames.GetFrameTexture(animation, 0) as AtlasTexture;
                if (tex != null)
                {
                    if (tex.Region.Size.X > 64)
                    {
                        sprite.Position = new Vector2(-64, -64);
                    }
                    else
                    {
                        sprite.Position = Vector2.Zero;
                    }
                }
            }
        }
    }

    public bool HasAnimation(string animation)
    {
        if (_layers.TryGetValue("Body", out var bodySprite))
        {
            return bodySprite.SpriteFrames != null && bodySprite.SpriteFrames.HasAnimation(animation);
        }
        return false;
    }
}
