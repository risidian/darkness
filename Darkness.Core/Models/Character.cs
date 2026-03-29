using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace Darkness.Core.Models
{
    public partial class Character : ObservableObject
    {
        [property: PrimaryKey, AutoIncrement]
        [ObservableProperty]
        private int _id;
        
        [property: Indexed]
        [ObservableProperty]
        private int _userId;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _class = string.Empty;

        [ObservableProperty]
        private string _hairColor = string.Empty;

        [ObservableProperty]
        private string _hairStyle = string.Empty;

        [ObservableProperty]
        private string _skinColor = string.Empty;
        
        // Base Stats
        [ObservableProperty]
        private int _strength;

        [ObservableProperty]
        private int _dexterity;

        [ObservableProperty]
        private int _constitution;

        [ObservableProperty]
        private int _intelligence;

        [ObservableProperty]
        private int _wisdom;

        [ObservableProperty]
        private int _charisma;
        
        // Derived Stats
        [ObservableProperty]
        private int _currentHP;

        [ObservableProperty]
        private int _maxHP;

        [ObservableProperty]
        private int _stamina;

        [ObservableProperty]
        private int _mana;
        
        [ObservableProperty]
        private int _speed;

        [ObservableProperty]
        private int _accuracy;

        [ObservableProperty]
        private int _evasion;

        [ObservableProperty]
        private int _defense;

        [ObservableProperty]
        private int _magicDefense;

        [ObservableProperty]
        private int _level;

        [ObservableProperty]
        private int _experience;

        [ObservableProperty]
        private int _attributePoints = 5;

        [ObservableProperty]
        private byte[]? _thumbnail;
    }
}
