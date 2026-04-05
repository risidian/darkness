using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Darkness.Core.Services
{
    public class DeathmatchService : IDeathmatchService
    {
        public Task<List<DeathmatchEncounter>> GetEncountersAsync()
        {
            var encounters = new List<DeathmatchEncounter>
            {
                new DeathmatchEncounter
                {
                    Id = 1,
                    Name = "Skeleton Squad",
                    RequiredLevel = 1,
                    Enemies = new List<Enemy>
                    {
                        new Enemy
                        {
                            Name = "Skeleton", Level = 1, MaxHP = 20, CurrentHP = 20, Attack = 5, Defense = 2, Speed = 3
                        },
                        new Enemy
                        {
                            Name = "Skeleton Archer", Level = 1, MaxHP = 15, CurrentHP = 15, Attack = 6, Defense = 1,
                            Speed = 5
                        }
                    },
                    Rewards = new List<Item>
                    {
                        new Item { Name = "Bone Sword", Type = "Weapon", Value = 10, AttackBonus = 2 }
                    }
                },
                new DeathmatchEncounter
                {
                    Id = 2,
                    Name = "Orc Raiders",
                    RequiredLevel = 5,
                    Enemies = new List<Enemy>
                    {
                        new Enemy
                        {
                            Name = "Orc Grunt", Level = 5, MaxHP = 50, CurrentHP = 50, Attack = 10, Defense = 5,
                            Speed = 4
                        },
                        new Enemy
                        {
                            Name = "Orc Shaman", Level = 5, MaxHP = 40, CurrentHP = 40, Attack = 8, Defense = 3,
                            Speed = 4
                        }
                    },
                    Rewards = new List<Item>
                    {
                        new Item { Name = "Orcish Axe", Type = "Weapon", Value = 50, AttackBonus = 8 }
                    }
                }
            };

            return Task.FromResult(encounters);
        }
    }
}