using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using System.Threading.Tasks;
using System;

namespace Darkness.Godot.Game;

public partial class LayeredSprite : Node2D
{
    private AnimatedSprite2D _bakedSprite = null!;
    private ShaderMaterial _simpleMaterial = null!;
    private bool _flipH = false;

    public bool FlipH
    {
        get => _flipH;
        set
        {
            _flipH = value;
            if (_bakedSprite != null)
            {
                _bakedSprite.FlipH = _flipH;
            }
        }
    }

    public override void _Ready()
    {
        if (!IsInsideTree()) return;
        EnsureSprite();
    }

    private void EnsureSprite()
    {
        if (_bakedSprite != null) return;

        _bakedSprite = GetNode<AnimatedSprite2D>("BakedSprite");
        var shader = GD.Load<Shader>("res://src/Shaders/simple_2d.gdshader");
        _simpleMaterial = new ShaderMaterial { Shader = shader };
        _bakedSprite.Material = _simpleMaterial;
    }

    public void ResetSprite()
    {
        EnsureSprite();
        _bakedSprite.SpriteFrames = null;
        _bakedSprite.Hide();
        _bakedSprite.Frame = 0;
        _bakedSprite.Stop();
    }

    public async Task SetupCharacter(Character c, ISheetDefinitionCatalog catalog, IFileSystemService fileSystem,
        ISpriteCompositor? compositor = null)
    {
        EnsureSprite();
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
        ResetSprite();

        if (_bakedSprite == null)
        {
            GD.PrintErr("[LayeredSprite] BakedSprite layer not found!");
            return;
        }

        var frames = ImageUtils.CreateSpriteFrames(data, 64, 64);
        if (frames != null)
        {
            GD.Print($"[LayeredSprite] Created SpriteFrames from bytes. Animations: {string.Join(", ", frames.GetAnimationNames())}");
            _bakedSprite.SpriteFrames = frames;
            _bakedSprite.FlipH = _flipH;
            _bakedSprite.Show();
            _bakedSprite.Modulate = new Color(1, 1, 1, 1);

            if (frames.HasAnimation("idle_down"))
            {
                _bakedSprite.Play("idle_down");
            }
            else if (frames.GetAnimationNames().Length > 0)
            {
                _bakedSprite.Play(frames.GetAnimationNames()[0]);
            }
        }
        else
        {
            GD.PrintErr("[LayeredSprite] Failed to create SpriteFrames from bytes.");
        }
    }

    public async Task SetupMonster(string monsterType, IFileSystemService fileSystem)
    {
        ResetSprite();
        GD.Print($"[LayeredSprite] SetupMonster started for: {monsterType}");

        if (_bakedSprite == null) return;

        var frames = new SpriteFrames();
        if (frames.HasAnimation("default")) frames.RemoveAnimation("default");

        string basePath = $"sprites/monsters/{monsterType.ToLower()}/";
        GD.Print($"[LayeredSprite] Base path: {basePath}");

        await LoadIfExists(frames, "idle", basePath + "idle.png", fileSystem);
        await LoadIfExists(frames, "walk", basePath + "walk.png", fileSystem);
        await LoadIfExists(frames, "run", basePath + "run.png", fileSystem);
        await LoadIfExists(frames, "jump", basePath + "jump.png", fileSystem);

        GD.Print($"[LayeredSprite] Loaded {frames.GetAnimationNames().Length} animations.");

        _bakedSprite.SpriteFrames = frames;
        _bakedSprite.FlipH = _flipH;
        _bakedSprite.Show();

        if (frames.HasAnimation("idle"))
        {
            _bakedSprite.Play("idle");
        }
    }

    public async Task SetupFullSheet(string path, IFileSystemService fileSystem)
    {
        ResetSprite();

        if (_bakedSprite == null) return;

        var frames = await LoadFrames(path, fileSystem);
        if (frames != null)
        {
            _bakedSprite.SpriteFrames = frames;
            _bakedSprite.FlipH = _flipH;
            _bakedSprite.Show();

            if (frames.HasAnimation("idle_down"))
            {
                _bakedSprite.Play("idle_down");
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
        
        if (_bakedSprite.Visible && _bakedSprite.SpriteFrames != null && _bakedSprite.SpriteFrames.HasAnimation(animation))
        {
            _bakedSprite.Play(animation);
            
            var tex = _bakedSprite.SpriteFrames.GetFrameTexture(animation, 0) as AtlasTexture;
            if (tex != null)
            {
                if (tex.Region.Size.X > 64)
                {
                    _bakedSprite.Position = new Vector2(-64, -64);
                }
                else
                {
                    _bakedSprite.Position = Vector2.Zero;
                }
            }
        }
    }

    public bool HasAnimation(string animation)
    {
        return _bakedSprite?.SpriteFrames != null && _bakedSprite.SpriteFrames.HasAnimation(animation);
    }
}
