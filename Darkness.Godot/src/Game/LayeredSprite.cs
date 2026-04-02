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
    private ShaderMaterial _simpleMaterial;
    
    public override void _Ready()
    {
        var shader = GD.Load<Shader>("res://src/Shaders/simple_2d.gdshader");
        _simpleMaterial = new ShaderMaterial { Shader = shader };

        foreach (var child in GetChildren())
        {
            if (child is AnimatedSprite2D sprite)
            {
                _layers[sprite.Name] = sprite;
                // Force a simple material to avoid emulator uniform limits
                sprite.Material = _simpleMaterial;
            }
        }
    }

    public async Task SetupCharacter(Character c, ISpriteLayerCatalog catalog, IFileSystemService fileSystem)
    {
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
        
        // Clear old frames
        foreach (var layer in _layers.Values)
        {
            layer.SpriteFrames = null;
            layer.Hide();
        }

        foreach (var def in layerDefs)
        {
            // Map the Z-order or Path to a specific node
            var nodeName = GetNodeNameForPath(def.ResourcePath);
            if (_layers.TryGetValue(nodeName, out var sprite))
            {
                var frames = await LoadFrames(def.ResourcePath, fileSystem);
                sprite.SpriteFrames = frames;
                sprite.Show();
            }
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
        foreach (var sprite in _layers.Values)
        {
            if (sprite.Visible && sprite.SpriteFrames != null && sprite.SpriteFrames.HasAnimation(animation))
            {
                sprite.Play(animation);
            }
        }
    }
}
