using System.Collections.Generic;
using Darkness.Core.Models;

namespace Darkness.Core.Logic
{
    public class StoryController
    {
        public int CurrentBeat { get; private set; } = 1;

        public void SetBeat(int beat)
        {
            CurrentBeat = beat;
        }

        public (List<Enemy> Enemies, int? SurvivalTurns, List<Character> AdditionalPartyMembers) GetEncounterForBeat(int beat)
        {
            var enemies = new List<Enemy>();
            int? survivalTurns = null;
            var additionalPartyMembers = new List<Character>();

            switch (beat)
            {
                case 3:
                    // Beat 3: First Combat (Darius vs 3 Undead Dogs)
                    enemies.Add(new Enemy { Name = "Undead Dog A", MaxHP = 40, CurrentHP = 40, Attack = 12, Defense = 5, Speed = 12 });
                    enemies.Add(new Enemy { Name = "Undead Dog B", MaxHP = 40, CurrentHP = 40, Attack = 12, Defense = 5, Speed = 12 });
                    enemies.Add(new Enemy { Name = "Undead Dog C", MaxHP = 40, CurrentHP = 40, Attack = 12, Defense = 5, Speed = 12 });
                    break;
                
                case 4:
                    // Beat 4: Dark Warrior (Invincible, vanishes after 5 rounds)
                    enemies.Add(new Enemy
                    {
                        Name = "Dark Warrior",
                        Level = 20,
                        MaxHP = 9999,
                        CurrentHP = 9999,
                        Attack = 50,
                        Defense = 100,
                        Speed = 30,
                        IsInvincible = true
                    });
                    survivalTurns = 5;
                    break;
                
                case 5:
                    // Beat 5: The Sorcerer
                    enemies.Add(new Enemy { Name = "Sorcerer", Level = 8, MaxHP = 120, CurrentHP = 120, Attack = 35, Defense = 8, Speed = 20 });
                    break;

                case 6:
                    // Beat 6: Journey Begins (Goblins)
                    enemies.Add(new Enemy { Name = "Goblin Scout", MaxHP = 60, CurrentHP = 60, Attack = 15, Defense = 6, Speed = 14 });
                    enemies.Add(new Enemy { Name = "Goblin Fighter", MaxHP = 80, CurrentHP = 80, Attack = 18, Defense = 8, Speed = 12 });
                    break;

                case 7:
                    // Beat 7: The Knight (Meeting and fighting Tywin)
                    enemies.Add(new Enemy { Name = "Tywin the Knight", Level = 12, MaxHP = 300, CurrentHP = 300, Attack = 40, Defense = 20, Speed = 18 });
                    break;

                case 8:
                    // Beat 8: Araknos Demon (First joint-character boss battle)
                    enemies.Add(new Enemy { Name = "Araknos Demon", Level = 15, MaxHP = 500, CurrentHP = 500, Attack = 60, Defense = 25, Speed = 22 });
                    additionalPartyMembers.Add(CreateTywin());
                    break;

                case 9:
                    // Beat 9: Final Boss Kyarias (Undead Army)
                    enemies.Add(new Enemy
                    {
                        Name = "Kyarias the Undead King",
                        Level = 25,
                        MaxHP = 2000,
                        CurrentHP = 2000,
                        Attack = 80,
                        Defense = 40,
                        Speed = 25
                    });
                    enemies.Add(new Enemy { Name = "Undead Soldier A", MaxHP = 200, CurrentHP = 200, Attack = 30, Defense = 15, Speed = 10 });
                    enemies.Add(new Enemy { Name = "Undead Soldier B", MaxHP = 200, CurrentHP = 200, Attack = 30, Defense = 15, Speed = 10 });
                    enemies.Add(new Enemy { Name = "Skeletal Archer", MaxHP = 150, CurrentHP = 150, Attack = 40, Defense = 10, Speed = 15 });
                    
                    additionalPartyMembers.Add(CreateTywin());
                    break;

                default:
                    // Generic encounter for other beats
                    enemies.Add(new Enemy { Name = "Shadow Minion", MaxHP = 50, CurrentHP = 50, Attack = 10, Defense = 5, Speed = 10 });
                    if (beat >= 8)
                    {
                        additionalPartyMembers.Add(CreateTywin());
                    }
                    break;
            }

            return (enemies, survivalTurns, additionalPartyMembers);
        }

        private Character CreateTywin()
        {
            return new Character
            {
                Name = "Tywin",
                Class = "Paladin",
                Strength = 18,
                Dexterity = 12,
                Constitution = 16,
                Intelligence = 10,
                Wisdom = 14,
                Charisma = 15,
                MaxHP = 250,
                CurrentHP = 250,
                Defense = 15,
                Speed = 12
            };
        }
    }
}
