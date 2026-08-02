$jsonPath = "C:\Users\mayce\source\repos\darkness\Darkness.Godot\assets\data\items.json"
$items = Get-Content -Raw $jsonPath | ConvertFrom-Json

$mapping = @{
    "Simple Dagger" = "Dagger (Steel)"
    "Iron Sword" = "Arming Sword (Steel)"
    "Apprentice Wand" = "Mage Wand"
    "Oak Staff" = "Simple Staff"
    "Hunting Bow" = "Short Bow"
    "Iron Axe" = "Waraxe"
    "Iron Mace" = "Mace"
    "Leather Knuckles" = $null
    "Poison Dagger" = "Dagger (Steel)"
    "Shadow Blade" = "Katana"
    "Assassin's Stiletto" = "Rapier"
    "Crystal Staff" = "Crystal Staff"
    "Arcane Scepter" = "Arcane Staff"
    "Staff of the Void" = "Diamond Staff"
    "Heavy Battleaxe" = "Waraxe"
    "Warhammer" = "Mace"
    "Berserker's Cleaver" = "Waraxe"
    "Bastard Sword" = "Longsword"
    "Holy Avenger" = "Glowsword (Blue)"
    "Blessed Mace" = "Mace"
    "Divine Warhammer" = "Mace"
    "Leather Chest" = "Leather"
    "Iron Chest" = "Plate (Iron)"
    "Steel Chest" = "Plate (Steel)"
    "Dark Chest" = "Plate (Gold)"
    "Apprentice Robes" = "Mage Robes (Blue)"
    "Archmage Robes" = "Mage Robes (Purple)"
    "Leather Legs" = "Leggings"
    "Iron Legs" = "Pantaloons"
    "Steel Legs" = "Formal"
    "Dark Legs" = "Cuffed"
    "Leather Boots" = "Boots (Basic)"
    "Iron Boots" = "Boots (Fold)"
    "Steel Boots" = "Boots (Rimmed)"
    "Dark Boots" = "Shoes"
    "Wooden Buckler" = "Spartan"
    "Iron Shield" = "Crusader"
    "Knight Shield" = "Crusader"
    "Steel Shield" = "Spartan"
    "Dark Tower Shield" = "Crusader"
    "Mage's Ward" = $null
}

foreach ($item in $items) {
    if ($item.EquipmentSlot -eq "Head") {
        $item | Add-Member -NotePropertyName "SpriteDefinitionName" -NotePropertyValue $null -Force
    }
    elseif ($mapping.Contains($item.Name)) {
        $item | Add-Member -NotePropertyName "SpriteDefinitionName" -NotePropertyValue $mapping[$item.Name] -Force
    }
}

$items | ConvertTo-Json -Depth 10 | Set-Content $jsonPath -Encoding utf8

$recipesPath = "C:\Users\mayce\source\repos\darkness\Darkness.Godot\assets\data\recipes.json"
if (Test-Path $recipesPath) {
    $recipes = Get-Content -Raw $recipesPath | ConvertFrom-Json
    foreach ($recipe in $recipes) {
        $itemName = $recipe.OutputItem.Name
        if ($null -ne $itemName) {
            if ($recipe.OutputItem.EquipmentSlot -eq "Head") {
                $recipe.OutputItem | Add-Member -NotePropertyName "SpriteDefinitionName" -NotePropertyValue $null -Force
            }
            elseif ($mapping.Contains($itemName)) {
                $recipe.OutputItem | Add-Member -NotePropertyName "SpriteDefinitionName" -NotePropertyValue $mapping[$itemName] -Force
            }
        }
    }
    $recipes | ConvertTo-Json -Depth 10 | Set-Content $recipesPath -Encoding utf8
}
