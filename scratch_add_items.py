import json
import os

new_items = [
  {
    "Id": 65,
    "Name": "Black Leather Chest",
    "Description": "Dark leather armor.",
    "Type": "Armor",
    "Weight": 3,
    "Value": 200,
    "Quantity": 1,
    "EquipmentSlot": "Body",
    "DefenseBonus": 4,
    "ClassRequirement": None,
    "SpriteDefinitionName": "Leather (Black)"
  },
  {
    "Id": 66,
    "Name": "Crimson Robes",
    "Description": "Red mage robes.",
    "Type": "Armor",
    "Weight": 2,
    "Value": 250,
    "Quantity": 1,
    "EquipmentSlot": "Body",
    "DefenseBonus": 3,
    "IntelligenceBonus": 4,
    "RequiredLevel": 5,
    "ClassRequirement": "Mage",
    "SpriteDefinitionName": "Mage Robes (Red)"
  },
  {
    "Id": 67,
    "Name": "Silver Plate",
    "Description": "Shining silver plate armor.",
    "Type": "Armor",
    "Weight": 10,
    "Value": 900,
    "Quantity": 1,
    "EquipmentSlot": "Body",
    "DefenseBonus": 14,
    "RequiredLevel": 10,
    "RequiredStrength": 15,
    "ClassRequirement": None,
    "SpriteDefinitionName": "Plate (Silver)"
  },
  {
    "Id": 68,
    "Name": "Brass Plate",
    "Description": "Sturdy brass plate armor.",
    "Type": "Armor",
    "Weight": 9,
    "Value": 700,
    "Quantity": 1,
    "EquipmentSlot": "Body",
    "DefenseBonus": 11,
    "RequiredLevel": 8,
    "RequiredStrength": 14,
    "ClassRequirement": None,
    "SpriteDefinitionName": "Plate (Brass)"
  },
  {
    "Id": 69,
    "Name": "Leather Gloves",
    "Description": "Simple leather gloves.",
    "Type": "Armor",
    "Weight": 1,
    "Value": 50,
    "Quantity": 1,
    "EquipmentSlot": "Arms",
    "DefenseBonus": 1,
    "ClassRequirement": None,
    "SpriteDefinitionName": "Gloves"
  },
  {
    "Id": 70,
    "Name": "Simple Sandals",
    "Description": "Basic footwear.",
    "Type": "Armor",
    "Weight": 1,
    "Value": 20,
    "Quantity": 1,
    "EquipmentSlot": "Feet",
    "DefenseBonus": 0,
    "ClassRequirement": None,
    "SpriteDefinitionName": "Sandals"
  },
  {
    "Id": 71,
    "Name": "Cloth Slacks",
    "Description": "Simple cloth pants.",
    "Type": "Armor",
    "Weight": 1,
    "Value": 30,
    "Quantity": 1,
    "EquipmentSlot": "Legs",
    "DefenseBonus": 1,
    "ClassRequirement": None,
    "SpriteDefinitionName": "Slacks"
  },
  {
    "Id": 72,
    "Name": "Diamond Focus",
    "Description": "A diamond spell focus.",
    "Type": "Shield",
    "Weight": 1,
    "Value": 500,
    "Quantity": 1,
    "EquipmentSlot": "OffHand",
    "DefenseBonus": 2,
    "IntelligenceBonus": 5,
    "RequiredLevel": 10,
    "ClassRequirement": "Mage",
    "SpriteDefinitionName": "Diamond Focus (Off-Hand)"
  },
  {
    "Id": 73,
    "Name": "Gnarled Focus",
    "Description": "A twisted wooden spell focus.",
    "Type": "Shield",
    "Weight": 1,
    "Value": 150,
    "Quantity": 1,
    "EquipmentSlot": "OffHand",
    "DefenseBonus": 1,
    "IntelligenceBonus": 2,
    "RequiredLevel": 3,
    "ClassRequirement": "Mage",
    "SpriteDefinitionName": "Gnarled Focus (Off-Hand)"
  },
  {
    "Id": 74,
    "Name": "Looped Focus",
    "Description": "A looped wooden spell focus.",
    "Type": "Shield",
    "Weight": 1,
    "Value": 300,
    "Quantity": 1,
    "EquipmentSlot": "OffHand",
    "DefenseBonus": 1,
    "IntelligenceBonus": 3,
    "RequiredLevel": 6,
    "ClassRequirement": "Mage",
    "SpriteDefinitionName": "Looped Focus (Off-Hand)"
  },
  {
    "Id": 75,
    "Name": "Gnarled Staff",
    "Description": "A twisted magical staff.",
    "Type": "Weapon",
    "Weight": 3,
    "Value": 250,
    "Quantity": 1,
    "DamageDice": "1d6",
    "EquipmentSlot": "Weapon",
    "IntelligenceBonus": 5,
    "RequiredLevel": 5,
    "ClassRequirement": "Mage",
    "SpriteDefinitionName": "Gnarled Staff"
  },
  {
    "Id": 76,
    "Name": "Looped Staff",
    "Description": "A looped magical staff.",
    "Type": "Weapon",
    "Weight": 3,
    "Value": 450,
    "Quantity": 1,
    "DamageDice": "1d8",
    "EquipmentSlot": "Weapon",
    "IntelligenceBonus": 8,
    "RequiredLevel": 10,
    "ClassRequirement": "Mage",
    "SpriteDefinitionName": "Looped Staff"
  },
  {
    "Id": 77,
    "Name": "Boomerang",
    "Description": "A returning throwing weapon.",
    "Type": "Weapon",
    "Weight": 1,
    "Value": 100,
    "Quantity": 1,
    "DamageDice": "1d4",
    "EquipmentSlot": "Weapon",
    "DexterityBonus": 2,
    "ClassRequirement": "Rogue",
    "SpriteDefinitionName": "Boomerang"
  },
  {
    "Id": 78,
    "Name": "Great Bow",
    "Description": "A massive longbow.",
    "Type": "Weapon",
    "Weight": 4,
    "Value": 500,
    "Quantity": 1,
    "DamageDice": "1d10",
    "EquipmentSlot": "Weapon",
    "DexterityBonus": 6,
    "RequiredLevel": 10,
    "RequiredDexterity": 15,
    "ClassRequirement": "Rogue",
    "SpriteDefinitionName": "Great Bow"
  },
  {
    "Id": 79,
    "Name": "Recurve Bow",
    "Description": "A powerful composite bow.",
    "Type": "Weapon",
    "Weight": 3,
    "Value": 350,
    "Quantity": 1,
    "DamageDice": "1d8",
    "EquipmentSlot": "Weapon",
    "DexterityBonus": 4,
    "RequiredLevel": 6,
    "RequiredDexterity": 12,
    "ClassRequirement": "Rogue",
    "SpriteDefinitionName": "Recurve Bow"
  },
  {
    "Id": 80,
    "Name": "Crossbow",
    "Description": "A mechanical bow.",
    "Type": "Weapon",
    "Weight": 5,
    "Value": 400,
    "Quantity": 1,
    "DamageDice": "1d10",
    "EquipmentSlot": "Weapon",
    "AttackBonus": 3,
    "DexterityBonus": 2,
    "RequiredLevel": 8,
    "ClassRequirement": "Rogue",
    "SpriteDefinitionName": "Crossbow"
  },
  {
    "Id": 81,
    "Name": "Slingshot",
    "Description": "A simple slingshot.",
    "Type": "Weapon",
    "Weight": 1,
    "Value": 50,
    "Quantity": 1,
    "DamageDice": "1d4",
    "EquipmentSlot": "Weapon",
    "DexterityBonus": 1,
    "ClassRequirement": "Rogue",
    "SpriteDefinitionName": "Slingshot"
  },
  {
    "Id": 82,
    "Name": "Heavy Longsword",
    "Description": "A wide-bladed longsword.",
    "Type": "Weapon",
    "Weight": 5,
    "Value": 400,
    "Quantity": 1,
    "DamageDice": "1d10",
    "EquipmentSlot": "Weapon",
    "AttackBonus": 8,
    "RequiredLevel": 10,
    "RequiredStrength": 14,
    "ClassRequirement": "Knight",
    "SpriteDefinitionName": "Alt Longsword"
  },
  {
    "Id": 83,
    "Name": "Curved Saber",
    "Description": "A swift curved sword.",
    "Type": "Weapon",
    "Weight": 3,
    "Value": 300,
    "Quantity": 1,
    "DamageDice": "1d8",
    "EquipmentSlot": "Weapon",
    "AttackBonus": 5,
    "DexterityBonus": 2,
    "RequiredLevel": 7,
    "ClassRequirement": "Knight",
    "SpriteDefinitionName": "Saber"
  },
  {
    "Id": 84,
    "Name": "Desert Scimitar",
    "Description": "A deadly sweeping blade.",
    "Type": "Weapon",
    "Weight": 3,
    "Value": 350,
    "Quantity": 1,
    "DamageDice": "1d8",
    "EquipmentSlot": "Weapon",
    "AttackBonus": 6,
    "DexterityBonus": 3,
    "RequiredLevel": 8,
    "ClassRequirement": "Warrior",
    "SpriteDefinitionName": "Scimitar"
  }
]

with open('C:/Users/mayce/source/repos/darkness/Darkness.Godot/assets/data/items.json', 'r', encoding='utf-8') as f:
    data = json.load(f)

data.extend(new_items)

with open('C:/Users/mayce/source/repos/darkness/Darkness.Godot/assets/data/items.json', 'w', encoding='utf-8', newline='\n') as f:
    json.dump(data, f, indent=2)

with open('C:/Users/mayce/source/repos/darkness/Darkness.Godot/assets/data/recipes.json', 'r', encoding='utf-8') as f:
    recipes = json.load(f)
    
new_recipes = [
  {
    "Id": 24,
    "Name": "Slingshot",
    "Result": {
      "Name": "Slingshot",
      "Description": "A simple slingshot.",
      "Type": "Weapon",
      "DexterityBonus": 1,
      "Value": 50,
      "Weight": 1,
      "DamageDice": "1d4",
      "EquipmentSlot": "Weapon"
    },
    "Materials": {
      "Leather Scraps": 1,
      "Iron Scrap": 1
    }
  },
  {
    "Id": 25,
    "Name": "Leather Gloves",
    "Result": {
      "Name": "Leather Gloves",
      "Description": "Simple leather gloves.",
      "Type": "Armor",
      "DefenseBonus": 1,
      "Value": 50,
      "Weight": 1,
      "EquipmentSlot": "Arms"
    },
    "Materials": {
      "Leather Scraps": 2
    }
  },
  {
    "Id": 26,
    "Name": "Gnarled Staff",
    "Result": {
      "Name": "Gnarled Staff",
      "Description": "A twisted magical staff.",
      "Type": "Weapon",
      "IntelligenceBonus": 5,
      "Value": 250,
      "Weight": 3,
      "DamageDice": "1d6",
      "EquipmentSlot": "Weapon"
    },
    "Materials": {
      "Magic Essence": 3,
      "Iron Scrap": 2
    }
  }
]
recipes.extend(new_recipes)

with open('C:/Users/mayce/source/repos/darkness/Darkness.Godot/assets/data/recipes.json', 'w', encoding='utf-8', newline='\n') as f:
    json.dump(recipes, f, indent=2)

os.makedirs('C:/Users/mayce/source/repos/darkness/.superpowers/sdd', exist_ok=True)
with open('C:/Users/mayce/source/repos/darkness/.superpowers/sdd/orphaned-sheetdefs-report.md', 'w', encoding='utf-8', newline='\n') as f:
    f.write("# Orphaned SheetDefinitions Report\n\nResolved orphaned items by generating new items and appending them to `items.json` and generating 3 new recipes in `recipes.json`.\n")

