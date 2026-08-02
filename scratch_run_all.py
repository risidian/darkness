import os
import subprocess

commands = [
    'python scripts/generate_spritesheet.py --prompt "seagull, white bird, coastal, pixel art RPG enemy" --output "Darkness.Godot/assets/sprites/seagull.png"',
    'python scripts/generate_spritesheet.py --prompt "pirate, cutlass, bandana, ragged clothes, pixel art RPG enemy" --output "Darkness.Godot/assets/sprites/pirate.png"',
    'python scripts/generate_spritesheet.py --prompt "gray wolf, snarling, forest predator, pixel art RPG enemy" --output "Darkness.Godot/assets/sprites/wolf.png"',
    'python scripts/generate_spritesheet.py --prompt "bandit, hooded, leather armor, sword, pixel art RPG enemy" --output "Darkness.Godot/assets/sprites/bandit.png"',
    'python scripts/generate_spritesheet.py --prompt "skeleton warrior, undead, rusty sword and shield, pixel art RPG enemy" --output "Darkness.Godot/assets/sprites/skeleton.png"',
    'python scripts/generate_spritesheet.py --prompt "ghostly wraith, translucent, dark robes, floating, pixel art RPG enemy" --output "Darkness.Godot/assets/sprites/wraith.png"',
    'python scripts/generate_spritesheet.py --prompt "dark knight, heavy plate armor, greatsword, pixel art RPG enemy" --output "Darkness.Godot/assets/sprites/knight.png"',
    'python scripts/generate_spritesheet.py --prompt "imperial soldier, chainmail, spear, military, pixel art RPG enemy" --output "Darkness.Godot/assets/sprites/soldier.png"',
    'python scripts/generate_spritesheet.py --prompt "crossbowman, light armor, crossbow, mercenary, pixel art RPG enemy" --output "Darkness.Godot/assets/sprites/crossbowman.png"',
    'python scripts/generate_spritesheet.py --prompt "veteran mercenary, battle-scarred, heavy armor, axe, pixel art RPG enemy" --output "Darkness.Godot/assets/sprites/mercenary.png"',
    'python scripts/generate_spritesheet.py --prompt "wild hound, feral dog, snarling, pixel art RPG enemy" --output "Darkness.Godot/assets/sprites/hound.png"'
]

for cmd in commands:
    print(f"Running: {cmd}")
    subprocess.run(cmd, shell=True, check=True)
