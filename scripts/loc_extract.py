import json
import glob
import os
import argparse

def extract():
    parser = argparse.ArgumentParser()
    parser.add_argument('--dry-run', action='store_true')
    args = parser.parse_args()

    files = glob.glob('Darkness.Godot/assets/data/quests/*.json')
    files.sort()

    catalog = {}

    for filepath in files:
        with open(filepath, 'r', encoding='utf-8') as f:
            data = json.load(f)

        chain_id = data.get('Id')
        if not chain_id:
            continue

        if 'Title' in data and data['Title']:
            catalog[f"{chain_id}.title"] = data['Title']

        if 'Description' in data and data['Description']:
            catalog[f"{chain_id}.description"] = data['Description']

        for step in data.get('Steps', []):
            step_id = step.get('Id')
            if not step_id:
                continue

            # Dialogue
            if 'Dialogue' in step and step['Dialogue']:
                dialogue = step['Dialogue']
                if 'Speaker' in dialogue and dialogue['Speaker']:
                    catalog[f"{chain_id}.{step_id}.speaker"] = dialogue['Speaker']
                if 'Lines' in dialogue and isinstance(dialogue['Lines'], list):
                    for i, line in enumerate(dialogue['Lines']):
                        catalog[f"{chain_id}.{step_id}.line_{i}"] = line

            # Branch Options
            if 'Branch' in step and step['Branch']:
                branch = step['Branch']
                if 'Options' in branch and isinstance(branch['Options'], list):
                    for i, opt in enumerate(branch['Options']):
                        if 'Text' in opt and opt['Text']:
                            catalog[f"{chain_id}.{step_id}.option_{i}.text"] = opt['Text']

            # Combat Enemies & Allies
            if 'Combat' in step and step['Combat']:
                combat = step['Combat']
                if 'Enemies' in combat and isinstance(combat['Enemies'], list):
                    for i, enemy in enumerate(combat['Enemies']):
                        if 'Name' in enemy and enemy['Name']:
                            catalog[f"{chain_id}.{step_id}.enemy_{i}.name"] = enemy['Name']
                if 'Allies' in combat and isinstance(combat['Allies'], list):
                    for i, ally in enumerate(combat['Allies']):
                        if 'Name' in ally and ally['Name']:
                            catalog[f"{chain_id}.{step_id}.ally_{i}.name"] = ally['Name']
            
            # Visuals Npc
            if 'Visuals' in step and step['Visuals']:
                visuals = step['Visuals']
                if 'Npc' in visuals and visuals['Npc']:
                    npc = visuals['Npc']
                    if 'Name' in npc and npc['Name']:
                        catalog[f"{chain_id}.{step_id}.npc.name"] = npc['Name']

    if args.dry_run:
        print("DRY RUN - Found keys:")
        for k, v in catalog.items():
            print(f"  {k}: {v}")
    else:
        out_dir = 'Darkness.Godot/assets/data/i18n/en'
        os.makedirs(out_dir, exist_ok=True)
        out_path = os.path.join(out_dir, 'quests.json')
        with open(out_path, 'w', encoding='utf-8') as f:
            json.dump(catalog, f, indent=2, ensure_ascii=False)
        print(f"Wrote {len(catalog)} strings to {out_path}")

if __name__ == '__main__':
    extract()
