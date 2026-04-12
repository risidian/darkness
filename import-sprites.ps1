# import-sprites.ps1
$lpcPath = "C:\Users\Mayce\Documents\GitHub\Universal-LPC-Spritesheet-Character-Generator\spritesheets"
$destPath = "C:\Users\Mayce\Documents\GitHub\darkness\Darkness.Godot\assets\sprites\full"

# Ensure dest paths exist
New-Item -ItemType Directory -Force -Path "$destPath\weapons\blunt\mace"
New-Item -ItemType Directory -Force -Path "$destPath\weapons\blunt\waraxe"
New-Item -ItemType Directory -Force -Path "$destPath\torso\jacket\tabard"

# Mace
Copy-Item -Path "$lpcPath\weapon\blunt\mace\*" -Destination "$destPath\weapons\blunt\mace" -Recurse -Force
# Waraxe
Copy-Item -Path "$lpcPath\weapon\blunt\waraxe\*" -Destination "$destPath\weapons\blunt\waraxe" -Recurse -Force
# Tabard (for male mage robes)
Copy-Item -Path "$lpcPath\torso\jacket\tabard\*" -Destination "$destPath\torso\jacket\tabard" -Recurse -Force
