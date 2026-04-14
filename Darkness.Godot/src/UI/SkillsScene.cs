using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;

namespace Darkness.Godot.UI;

public partial class SkillsScene : Control
{
    private ISessionService _session = null!;
    private INavigationService _navigation = null!;
    private IWeaponSkillService _weaponSkillService = null!;
    private ICharacterService _characterService = null!;

    private VBoxContainer _availableList = null!;
    private VBoxContainer _equippedList = null!;

    public override void _Ready()
    {
        if (!IsInsideTree()) return;
        var global = GetNode<Global>("/root/Global");
        var sp = global.Services!;
        _session = sp.GetRequiredService<ISessionService>();
        _navigation = sp.GetRequiredService<INavigationService>();
        _weaponSkillService = sp.GetRequiredService<IWeaponSkillService>();
        _characterService = sp.GetRequiredService<ICharacterService>();

        _availableList = GetNode<VBoxContainer>("MarginContainer/VBoxContainer/HSplitContainer/AVAILABLE/AvailableList");
        _equippedList = GetNode<VBoxContainer>("MarginContainer/VBoxContainer/HSplitContainer/EQUIPPED/EquippedList");
        
        GetNode<Button>("MarginContainer/VBoxContainer/BackButton").Pressed += () => _navigation.GoBackAsync();

        LoadSkills();
    }

    private void LoadSkills()
    {
        foreach (Node child in _availableList.GetChildren()) child.QueueFree();
        foreach (Node child in _equippedList.GetChildren()) child.QueueFree();

        var character = _session.CurrentCharacter;
        if (character == null) return;

        var availableSkills = _weaponSkillService.GetAvailableSkills(character)
            .Where(s => !s.IsPassive).ToList();

        // Populate Available Skills
        foreach (var skill in availableSkills)
        {
            bool isEquipped = character.ActiveSkillSlots.Contains(skill.Id);
            if (isEquipped) continue;

            var btn = new Button
            {
                Text = $"{skill.Name} ({skill.SkillType})",
                CustomMinimumSize = new Vector2(0, 60),
                TooltipText = skill.Description
            };
            btn.AddThemeFontSizeOverride("font_size", 24);
            int sid = skill.Id;
            btn.Pressed += () => EquipSkill(sid);
            _availableList.AddChild(btn);
        }

        // Populate Equipped Slots (Always show 5 slots)
        for (int i = 0; i < 5; i++)
        {
            int skillId = character.ActiveSkillSlots[i];
            if (skillId > 0)
            {
                var skill = availableSkills.FirstOrDefault(s => s.Id == skillId);
                var btn = new Button
                {
                    Text = skill != null ? $"{skill.Name} (SLOT {i + 1})" : "UNKNOWN SKILL",
                    CustomMinimumSize = new Vector2(0, 60),
                    TooltipText = skill?.Description ?? ""
                };
                btn.AddThemeFontSizeOverride("font_size", 24);
                int sid = skillId;
                btn.Pressed += () => UnequipSkill(sid);
                _equippedList.AddChild(btn);
            }
            else
            {
                var btn = new Button
                {
                    Text = $"- EMPTY SLOT {i + 1} -",
                    CustomMinimumSize = new Vector2(0, 60),
                    Disabled = true
                };
                btn.AddThemeFontSizeOverride("font_size", 24);
                _equippedList.AddChild(btn);
            }
        }
    }

    private async void EquipSkill(int skillId)
    {
        var character = _session.CurrentCharacter;
        if (character == null) return;

        // Find first empty slot
        int emptySlotIndex = character.ActiveSkillSlots.IndexOf(0);
        if (emptySlotIndex != -1)
        {
            character.ActiveSkillSlots[emptySlotIndex] = skillId;
            await Task.Run(() => _characterService.SaveCharacter(character));
            LoadSkills();
        }
        else
        {
            // Optional: Handle full slots (e.g., prompt to swap)
            GD.Print("All slots full. Choose a slot to replace (logic TBD)");
        }
    }

    private async void UnequipSkill(int skillId)
    {
        var character = _session.CurrentCharacter;
        if (character == null) return;

        int index = character.ActiveSkillSlots.IndexOf(skillId);
        if (index != -1)
        {
            character.ActiveSkillSlots[index] = 0;
            await Task.Run(() => _characterService.SaveCharacter(character));
            LoadSkills();
        }
    }
}
