import json
import os

# Tier mappings
def get_drops_for_level(level):
    if level <= 2:
        return [
            {"ItemName": "Small Health Potion", "Chance": 0.3},
            {"ItemName": "Leather Scraps", "Chance": 0.1},
            {"ItemName": "Simple Dagger", "Chance": 0.1}
        ]
    elif level <= 4:
        return [
            {"ItemName": "Small Health Potion", "Chance": 0.25},
            {"ItemName": "Iron Sword", "Chance": 0.08},
            {"ItemName": "Iron Ore", "Chance": 0.1}
        ]
    elif level <= 7:
        return [
            {"ItemName": "Small Mana Potion", "Chance": 0.2},
            {"ItemName": "Large Health Potion", "Chance": 0.2},
            {"ItemName": "Steel Chest", "Chance": 0.1},
            {"ItemName": "Steel Ingot", "Chance": 0.1}
        ]
    else: # 8+
        return [
            {"ItemName": "Large Mana Potion", "Chance": 0.25},
            {"ItemName": "Large Health Potion", "Chance": 0.25},
            {"ItemName": "Dark Ore", "Chance": 0.1},
            {"ItemName": "Dark Helm", "Chance": 0.08},
            {"ItemName": "Shadow Blade", "Chance": 0.05}
        ]

def process_encounters(file_path):
    with open(file_path, 'r') as f:
        data = json.load(f)
    
    for area in data:
        for encounter in area.get('Encounters', []):
            combat = encounter.get('Combat', {})
            for enemy in combat.get('Enemies', []):
                level = enemy.get('Level', 1)
                enemy['RandomDrops'] = get_drops_for_level(level)
                
    with open(file_path, 'w') as f:
        json.dump(data, f, indent=2)

def process_quest(file_path, fixed_drops, random_drops=None, is_army=False):
    if not os.path.exists(file_path):
        return
    with open(file_path, 'r') as f:
        data = json.load(f)
        
    for state in data.get('States', []):
        combat = state.get('CombatEncounter')
        if combat:
            for i, enemy in enumerate(combat.get('Enemies', [])):
                if is_army and i > 0: # First enemy is boss, rest is army
                    enemy['RandomDrops'] = random_drops or get_drops_for_level(enemy.get('Level', 5))
                else:
                    if fixed_drops:
                        enemy['FixedDrops'] = [{"ItemName": item} for item in fixed_drops]
                    if random_drops and not fixed_drops:
                        enemy['RandomDrops'] = random_drops
                        
    with open(file_path, 'w') as f:
        json.dump(data, f, indent=2)

# files
data_dir = "Darkness.Godot/assets/data"
process_encounters(f"{data_dir}/encounters.json")
process_encounters(f"{data_dir}/deathmatch-encounters.json")

# Quests
process_quest(f"{data_dir}/quests/beat_3.json", fixed_drops=None, random_drops=[{"ItemName": "Small Health Potion", "Chance": 0.3}, {"ItemName": "Leather Scraps", "Chance": 0.2}])
process_quest(f"{data_dir}/quests/beat_4.json", fixed_drops=["Assassin's Stiletto"])
process_quest(f"{data_dir}/quests/beat_5.json", fixed_drops=["Holy Avenger"])
process_quest(f"{data_dir}/quests/beat_7.json", fixed_drops=None, random_drops=[{"ItemName": "Small Health Potion", "Chance": 0.25}, {"ItemName": "Iron Ore", "Chance": 0.15}])
process_quest(f"{data_dir}/quests/beat_8.json", fixed_drops=["Steel Chest"])
process_quest(f"{data_dir}/quests/beat_9.json", fixed_drops=["Berserker's Cleaver"])
process_quest(f"{data_dir}/quests/beat_10.json", fixed_drops=["Staff of the Void"], is_army=True)

print("Done")
