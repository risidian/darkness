using System.Collections.Generic;
using Darkness.Core.Models;

namespace Darkness.Core.Interfaces
{
    public interface ISheetDefinitionCatalog
    {
        List<SheetDefinition> GetSheetDefinitions(CharacterAppearance appearance);
        CharacterAppearance GetDefaultAppearanceForClass(string className);
        List<string> GetOptionNames(string category, string gender);
        SheetDefinition? GetSheetDefinitionByName(string slot, string displayName);
    }
}
