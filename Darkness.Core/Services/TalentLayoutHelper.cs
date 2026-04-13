using Darkness.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Darkness.Core.Services
{
    public static class TalentLayoutHelper
    {
        public static void CalculateLayout(List<TalentNode> nodes)
        {
            if (nodes == null || nodes.Count == 0) return;

            // 1. Assign Rows by depth
            AssignRows(nodes);

            // 2. Assign Columns (0, 1, 2)
            AssignColumns(nodes);

            // 3. Resolve collisions
            ResolveCollisions(nodes);
        }

        private static void AssignRows(List<TalentNode> nodes)
        {
            // Simple BFS or DFS to find longest path to each node
            var nodeDict = nodes.ToDictionary(n => n.Id);
            var memo = new Dictionary<string, int>();

            foreach (var node in nodes)
            {
                node.Row = GetDepth(node, nodeDict, memo);
            }
        }

        private static int GetDepth(TalentNode node, Dictionary<string, TalentNode> nodeDict, Dictionary<string, int> memo)
        {
            if (memo.ContainsKey(node.Id)) return memo[node.Id];

            if (node.PrerequisiteNodeIds.Count == 0)
            {
                memo[node.Id] = 0;
                return 0;
            }

            int maxPrereqDepth = 0;
            foreach (var prereqId in node.PrerequisiteNodeIds)
            {
                if (nodeDict.TryGetValue(prereqId, out var prereqNode))
                {
                    maxPrereqDepth = Math.Max(maxPrereqDepth, GetDepth(prereqNode, nodeDict, memo));
                }
            }

            int depth = maxPrereqDepth + 1;
            memo[node.Id] = depth;
            return depth;
        }

        private static void AssignColumns(List<TalentNode> nodes)
        {
            // Root nodes should be centered in Column 1
            var roots = nodes.Where(n => n.PrerequisiteNodeIds.Count == 0).ToList();
            foreach (var root in roots)
            {
                root.Column = 1;
            }

            // Assign children
            var rows = nodes.GroupBy(n => n.Row).OrderBy(g => g.Key);
            foreach (var rowGroup in rows)
            {
                if (rowGroup.Key == 0) continue;

                foreach (var node in rowGroup)
                {
                    // If multiple prerequisites, center it in Column 1 (Convergence)
                    if (node.PrerequisiteNodeIds.Count > 1)
                    {
                        node.Column = 1;
                        continue;
                    }

                    // Otherwise, inherit or branch from parent
                    var parentId = node.PrerequisiteNodeIds.FirstOrDefault();
                    if (parentId != null)
                    {
                        var parent = nodes.FirstOrDefault(n => n.Id == parentId);
                        if (parent != null)
                        {
                            var siblings = nodes.Where(n => n.PrerequisiteNodeIds.Contains(parentId)).ToList();
                            if (siblings.Count == 1)
                            {
                                node.Column = parent.Column;
                            }
                            else
                            {
                                // Branching logic
                                int index = siblings.IndexOf(node);
                                if (siblings.Count == 2)
                                {
                                    node.Column = (index == 0) ? 0 : 2;
                                }
                                else if (siblings.Count == 3)
                                {
                                    node.Column = index; // 0, 1, 2
                                }
                                else
                                {
                                    // More than 3? Just distribute as best as we can in 0, 1, 2
                                    node.Column = index % 3;
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void ResolveCollisions(List<TalentNode> nodes)
        {
            // Simple collision resolution within same row
            var rows = nodes.GroupBy(n => n.Row);
            foreach (var rowGroup in rows)
            {
                var rowNodes = rowGroup.ToList();
                var occupiedColumns = new HashSet<int>();
                foreach (var node in rowNodes.OrderBy(n => GetPreferenceScore(n)))
                {
                    if (occupiedColumns.Contains(node.Column))
                    {
                        // Shift to nearest available column in 0, 1, 2
                        int[] preferences = { 1, 0, 2 };
                        foreach (int col in preferences)
                        {
                            if (!occupiedColumns.Contains(col))
                            {
                                node.Column = col;
                                break;
                            }
                        }
                        // If all 0, 1, 2 are occupied? For now we just keep it, but in a 3-col grid this shouldn't happen if we limit nodes per row.
                    }
                    occupiedColumns.Add(node.Column);
                }
            }
        }

        private static int GetPreferenceScore(TalentNode node)
        {
            // Converged nodes and roots have higher priority for their preferred column (1)
            if (node.PrerequisiteNodeIds.Count != 1) return 0;
            return 1;
        }
    }
}
