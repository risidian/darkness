using System.Collections.Generic;
using System.Linq;
using SystemJson = System.Text.Json;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;

namespace Darkness.Core.Services;

public class WeaponSkillService : IWeaponSkillService
{
    private readonly LiteDatabase _db;
    private readonly IFileSystemService _fileSystem;

    public WeaponSkillService(LiteDatabase db, IFileSystemService fileSystem)
    {
        _db = db;
        _fileSystem = fileSystem;
    }

    // Seed canonical skills if the collection is empty (covers test environments
    // where SkillSeeder has not run against the in-memory database).
    // Called lazily from GetSkillsForWeapon only, so tests that pre-populate
    // specific skill IDs via GetEquippedSkills won't encounter duplicate-key conflicts.
    private void EnsureSkillsSeeded()
    {
        var col = _db.GetCollection<Skill>("skills");
        if (col.Count() > 0) return;

        string json;
        try
        {
            json = _fileSystem.ReadAllText("assets/data/skills.json");
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine($"[WeaponSkillService] ERROR: Failed to read skills.json for seeding — {ex.Message}");
            return;
        }

        List<Skill>? skills;
        try
        {
            skills = SystemJson.JsonSerializer.Deserialize<List<Skill>>(json, new SystemJson.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (SystemJson.JsonException ex)
        {
            System.Console.WriteLine($"[WeaponSkillService] ERROR: Failed to parse skills.json — {ex.Message}");
            return;
        }

        if (skills != null && skills.Count > 0)
        {
            foreach (var skill in skills)
                col.Insert(skill);
            col.EnsureIndex(s => s.Id);
            col.EnsureIndex(s => s.Name);
        }
    }

    public List<Skill> GetAvailableSkills(Character character)
    {
        EnsureSkillsSeeded();
        var skillCol = _db.GetCollection<Skill>("skills");
        var allSkills = skillCol.FindAll().ToList();

        // Skills matching weapon
        var weaponSkills = allSkills.Where(s =>
            (s.WeaponRequirement == "None" || s.WeaponRequirement == character.WeaponType)
        ).ToList();

        // Skills granted by talents (by TalentRequirement ID or by Name matching a Talent Effect)
        var talentTrees = _db.GetCollection<TalentTree>("talent_trees").FindAll().ToList();
        var unlockedSkillNames = talentTrees
            .SelectMany(t => t.Nodes)
            .Where(n => character.UnlockedTalentIds.Contains(n.Id))
            .Where(n => !string.IsNullOrEmpty(n.Effect.Skill))
            .Select(n => n.Effect.Skill)
            .ToList();

        var talentSkills = allSkills.Where(s =>
            (s.TalentRequirement != null && character.UnlockedTalentIds.Contains(s.TalentRequirement)) ||
            unlockedSkillNames.Contains(s.Name)
        ).ToList();

        return weaponSkills.Union(talentSkills).ToList();
    }

    public List<Skill> GetEquippedSkills(Character character)
    {
        // Prioritize ActiveSkillSlots
        if (character.ActiveSkillSlots != null && character.ActiveSkillSlots.Any(id => id > 0))
        {
            var skillCollection = _db.GetCollection<Skill>("skills");
            return character.ActiveSkillSlots
                .Where(id => id > 0)
                .Select(id => skillCollection.FindById(id))
                .Where(s => s != null)
                .Cast<Skill>()
                .ToList();
        }

        // Fallback to legacy weapon-based skills if no active slots are set
        return GetSkillsForWeapon(character.WeaponType, character.OffHandType, character.ShieldType, character.UnlockedTalentIds);
    }

    public List<Skill> GetSkillsForWeapon(string? weaponType, string? offHandType, string? shieldType, List<string>? unlockedTalentIds = null)
    {
        weaponType ??= "None";
        offHandType ??= "None";
        shieldType ??= "None";

        EnsureSkillsSeeded();
        var skillCol = _db.GetCollection<Skill>("skills");
        var skills = new List<Skill>();

        // Primary weapon skills (non-defensive, non-passive, non-off-hand, not unarmed/generic)
        var primarySkills = skillCol.FindAll()
            .Where(s => s.WeaponRequirement != "None" &&
                         s.WeaponRequirement != "Shield" &&
                         s.SkillType != "Defensive" &&
                         !s.IsPassive &&
                         !s.IsOffHand &&
                         weaponType.Contains(s.WeaponRequirement, System.StringComparison.OrdinalIgnoreCase))
            .ToList();
        skills.AddRange(primarySkills);

        // Off-hand skills (prefer IsOffHand=true variants)
        if (offHandType != "None" && offHandType != weaponType && skills.Count < 3)
        {
            var offHandSkills = skillCol.FindAll()
                .Where(s => s.WeaponRequirement != "None" &&
                             s.WeaponRequirement != "Shield" &&
                             s.SkillType != "Defensive" &&
                             !s.IsPassive &&
                             s.IsOffHand &&
                             offHandType.Contains(s.WeaponRequirement, System.StringComparison.OrdinalIgnoreCase))
                .Take(3 - skills.Count)
                .ToList();

            // If no off-hand-specific skills, fall back to generic weapon skills
            if (offHandSkills.Count == 0)
            {
                offHandSkills = skillCol.FindAll()
                    .Where(s => s.WeaponRequirement != "None" &&
                                 s.WeaponRequirement != "Shield" &&
                                 s.SkillType != "Defensive" &&
                                 !s.IsPassive &&
                                 !s.IsOffHand &&
                                 offHandType.Contains(s.WeaponRequirement, System.StringComparison.OrdinalIgnoreCase))
                    .Take(3 - skills.Count)
                    .ToList();
            }

            skills.AddRange(offHandSkills);
        }

        // Unarmed fallback — skills with WeaponRequirement "None" that are not Defensive
        if (skills.Count == 0)
        {
            skills.AddRange(skillCol.FindAll()
                .Where(s => s.WeaponRequirement == "None" && s.SkillType != "Defensive" && s.TalentRequirement == null)
                .ToList());
        }

        // Defensive skill — priority: shield > weapon-specific > generic (WeaponRequirement "None")
        Skill? defSkill = null;
        if (shieldType != "None")
        {
            defSkill = skillCol.FindAll()
                .FirstOrDefault(s => s.SkillType == "Defensive" && s.WeaponRequirement == "Shield");
        }
        if (defSkill == null)
        {
            defSkill = skillCol.FindAll()
                .FirstOrDefault(s => s.SkillType == "Defensive" &&
                                      s.WeaponRequirement != "Shield" &&
                                      s.WeaponRequirement != "None" &&
                                      weaponType.Contains(s.WeaponRequirement, System.StringComparison.OrdinalIgnoreCase));
        }
        if (defSkill == null)
        {
            defSkill = skillCol.FindAll()
                .FirstOrDefault(s => s.SkillType == "Defensive" && s.WeaponRequirement == "None");
        }
        if (defSkill != null) skills.Add(defSkill);

        // Talent-based skills
        if (unlockedTalentIds != null && unlockedTalentIds.Count > 0)
        {
            var talentTrees = _db.GetCollection<TalentTree>("talent_trees").FindAll().ToList();
            var unlockedSkillNames = talentTrees
                .SelectMany(t => t.Nodes)
                .Where(n => unlockedTalentIds.Contains(n.Id))
                .Where(n => !string.IsNullOrEmpty(n.Effect.Skill))
                .Select(n => n.Effect.Skill)
                .ToList();

            var talentSkills = skillCol.FindAll()
                .Where(s => (s.TalentRequirement != null && unlockedTalentIds.Contains(s.TalentRequirement)) ||
                             unlockedSkillNames.Contains(s.Name))
                .ToList();
            skills.AddRange(talentSkills);
        }

        return skills.DistinctBy(s => s.Id).ToList();
    }
}
