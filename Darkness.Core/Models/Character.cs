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

        [ObservableProperty]
        private string _face = "Default";

        [ObservableProperty]
        private string _eyes = "Default";

        [ObservableProperty]
        private string _head = "Human Male";

        [ObservableProperty]
        private string _feet = "Boots (Basic)";

        [ObservableProperty]
        private string _arms = "None";

        [ObservableProperty]
        private string _legs = "Slacks";

        [ObservableProperty]
        private string _armorType = "Leather";

        [ObservableProperty]
        private string _weaponType = "Arming Sword (Steel)";
        
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

        public CharacterSnapshot ToSnapshot() => new CharacterSnapshot(
            Name, Class, CurrentHP, MaxHP, Level, Thumbnail, HairColor, HairStyle, SkinColor
        );
    }
}
