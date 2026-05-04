using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Darkness.Core.Models;
using Xunit;

namespace Darkness.Tests.Audit;

[Trait("Category", "Audit")]
public class QuestJsonRoundTripTests
{
    [Fact]
    public void EveryQuestJson_DeserializesCleanly_AndEnumValuesAreRecognized()
    {
        var root = FindSolutionRoot();
        var fullPath = Path.Combine(root, "Darkness.Godot", "assets", "data", "quests");
        Assert.True(Directory.Exists(fullPath), $"Quest data directory missing: {fullPath}");

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));

        var declaredStepTypes = Enum.GetValues<QuestStepType>().ToHashSet();
        var declaredRewardTypes = Enum.GetValues<QuestRewardType>().ToHashSet();

        var failures = new System.Collections.Generic.List<string>();

        foreach (var file in Directory.GetFiles(fullPath, "*.json"))
        {
            if (file.EndsWith(".bak")) continue;

            try
            {
                var content = File.ReadAllText(file);
                var chain = JsonSerializer.Deserialize<QuestChain>(content, options);
                if (chain == null)
                {
                    failures.Add($"{Path.GetFileName(file)}: deserialized as null");
                    continue;
                }

                foreach (var step in chain.Steps)
                {
                    if (!declaredStepTypes.Contains(step.Type))
                    {
                        failures.Add($"{Path.GetFileName(file)}: step '{step.Id}' has unrecognized Type {(int)step.Type}");
                    }
                    foreach (var reward in step.Rewards)
                    {
                        if (!declaredRewardTypes.Contains(reward.Type))
                        {
                            failures.Add($"{Path.GetFileName(file)}: step '{step.Id}' reward has unrecognized Type {(int)reward.Type}");
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                failures.Add($"{Path.GetFileName(file)}: {ex.Message}");
            }
        }

        Assert.True(failures.Count == 0, "Quest JSON round-trip failures:\n  " + string.Join("\n  ", failures));
    }

    private static string FindSolutionRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current) && Directory.GetFiles(current, "Darkness.sln").Length == 0)
        {
            current = Path.GetDirectoryName(current);
        }
        return current ?? throw new Exception("Could not find Darkness.sln root");
    }
}
