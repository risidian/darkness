using Darkness.Core.Models;
using Xunit;

namespace Darkness.Tests.Models;

public class EncounterTableTests
{
    [Fact]
    public void EncounterTable_Initialization_SetsDefaultValues()
    {
        var table = new EncounterTable();
        Assert.Equal(5, table.EncounterChance);
        Assert.Equal(1000f, table.EncounterDistance);
    }
}
