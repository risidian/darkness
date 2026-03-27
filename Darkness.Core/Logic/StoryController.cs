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

                case 8:
                    // Beat 8: Tywin (Joint forces start here)
                    enemies.Add(new Enemy
                    {
                        Name = "Elite Guard A",
                        Level = 10,
                        MaxHP = 150,
                        CurrentHP = 150,
                        Attack = 20,
                        Defense = 10,
                        Speed = 15
                    });
                    enemies.Add(new Enemy
                    {
                        Name = "Elite Guard B",
                        Level = 10,
                        MaxHP = 150,
                        CurrentHP = 150,
                        Attack = 20,
                        Defense = 10,
                        Speed = 15
                    });
                    
                    additionalPartyMembers.Add(CreateTywin());
                    break;

                case 10:
                    // Beat 10: Final Boss Kyarias (Undead Army)
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
