using SkiaSharp;
using System;
public class Program {
    public static void Main() {
        var path = \"Darkness.Godot/assets/sprites/full/weapons/sword/arming/attack_slash/fg/steel.png\";
        using var stream = System.IO.File.OpenRead(path);
        using var bitmap = SKBitmap.Decode(stream);
        Console.WriteLine($\"Dimensions: {bitmap.Width}x{bitmap.Height}\");
    }
}
