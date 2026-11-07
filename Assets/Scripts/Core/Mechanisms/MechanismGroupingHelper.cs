using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChasingLight
{
    /// <summary>
    /// Utility helper to group linked switches and bridges into matching color groups.
    /// Connected components are assigned deterministic group IDs (0 = Yellow, 1 = Blue).
    /// Unlinked or newly created mechanisms default safely to group 0.
    /// </summary>
    public static class MechanismGroupingHelper
    {
        public static readonly Color[] GroupColors = new[]
        {
            new Color(1f, 0.84f, 0.31f),
            new Color(0.31f, 0.76f, 0.97f)
        };

        public static readonly string[] GroupNames = new[]
        {
            "Yellow",
            "Blue"
        };

        public static Color GetGroupColor(int groupId)
        {
            if (GroupColors == null || GroupColors.Length == 0) return Color.yellow;
            int index = Mathf.Abs(groupId) % GroupColors.Length;
            return GroupColors[index];
        }

        public static string GetGroupName(int groupId)
        {
            if (GroupNames == null || GroupNames.Length == 0) return "Default";
            int index = Mathf.Abs(groupId) % GroupNames.Length;
            return GroupNames[index];
        }

        /// <summary>
        /// Calculate mechanism groups for runtime StageData.
        /// </summary>
        public static void CalculateGroups(
            IReadOnlyList<GateData> gates,
            IReadOnlyList<SwitchData> switches,
            out Dictionary<int, int> gateGroups,
            out Dictionary<int, int> switchGroups)
        {
            var gateItems = gates != null
                ? gates.Select(g => (g.Id, (IEnumerable<int>)(g.Condition?.SwitchIds ?? Array.Empty<int>())))
                : Enumerable.Empty<(int, IEnumerable<int>)>();

            var switchIds = switches != null
                ? switches.Select(s => s.Id)
                : Enumerable.Empty<int>();

            CalculateGroupsInternal(gateItems, switchIds, out gateGroups, out switchGroups);
        }

        /// <summary>
        /// Calculate mechanism groups for Level Editor.
        /// </summary>
        public static void CalculateGroups(
            IReadOnlyList<GateDefinition> gates,
            IReadOnlyList<SwitchDefinition> switches,
            out Dictionary<int, int> gateGroups,
            out Dictionary<int, int> switchGroups)
        {
            var gateItems = gates != null
                ? gates.Select(g => (g.id, (IEnumerable<int>)(g.switchCondition?.switchIds ?? Enumerable.Empty<int>())))
                : Enumerable.Empty<(int, IEnumerable<int>)>();

            var switchIds = switches != null
                ? switches.Select(s => s.id)
                : Enumerable.Empty<int>();

            CalculateGroupsInternal(gateItems, switchIds, out gateGroups, out switchGroups);
        }

        private static void CalculateGroupsInternal(
            IEnumerable<(int gateId, IEnumerable<int> switchIds)> gates,
            IEnumerable<int> allSwitchIds,
            out Dictionary<int, int> gateGroups,
            out Dictionary<int, int> switchGroups)
        {
            gateGroups = new Dictionary<int, int>();
            switchGroups = new Dictionary<int, int>();

            var gateList = gates.ToList();
            var switchSet = new HashSet<int>(allSwitchIds);

            // Bipartite graph adjacency
            // "G_{id}" for Gate vertices, "S_{id}" for Switch vertices
            var adj = new Dictionary<string, List<string>>();

            void AddEdge(string u, string v)
            {
                if (!adj.TryGetValue(u, out var listU)) { listU = new List<string>(); adj[u] = listU; }
                if (!adj.TryGetValue(v, out var listV)) { listV = new List<string>(); adj[v] = listV; }
                listU.Add(v);
                listV.Add(u);
            }

            foreach (var (gateId, sIds) in gateList)
            {
                string gNode = $"G_{gateId}";
                if (!adj.ContainsKey(gNode)) adj[gNode] = new List<string>();

                if (sIds != null)
                {
                    foreach (var sId in sIds)
                    {
                        if (switchSet.Contains(sId))
                        {
                            string sNode = $"S_{sId}";
                            AddEdge(gNode, sNode);
                        }
                    }
                }
            }

            foreach (var sId in switchSet)
            {
                string sNode = $"S_{sId}";
                if (!adj.ContainsKey(sNode)) adj[sNode] = new List<string>();
            }

            // Find connected components that have at least one link
            var visited = new HashSet<string>();
            int currentGroupId = 0;

            // Visit in order of gates, then switches
            foreach (var (gateId, _) in gateList)
            {
                string gNode = $"G_{gateId}";
                if (visited.Contains(gNode)) continue;

                if (adj[gNode].Count > 0)
                {
                    TraverseComponent(gNode, currentGroupId, adj, visited, gateGroups, switchGroups);
                    currentGroupId++;
                }
            }

            foreach (var sId in switchSet)
            {
                string sNode = $"S_{sId}";
                if (visited.Contains(sNode)) continue;

                if (adj[sNode].Count > 0)
                {
                    TraverseComponent(sNode, currentGroupId, adj, visited, gateGroups, switchGroups);
                    currentGroupId++;
                }
            }

            // Isolated or unlinked gates/switches default to Yellow
            foreach (var (gateId, _) in gateList)
            {
                if (!gateGroups.ContainsKey(gateId))
                {
                    gateGroups[gateId] = 0;
                }
            }

            foreach (var sId in switchSet)
            {
                if (!switchGroups.ContainsKey(sId))
                {
                    switchGroups[sId] = 0;
                }
            }
        }

        private static void TraverseComponent(
            string startNode,
            int groupId,
            Dictionary<string, List<string>> adj,
            HashSet<string> visited,
            Dictionary<int, int> gateGroups,
            Dictionary<int, int> switchGroups)
        {
            var queue = new Queue<string>();
            queue.Enqueue(startNode);
            visited.Add(startNode);

            while (queue.Count > 0)
            {
                string curr = queue.Dequeue();

                if (curr.StartsWith("G_") && int.TryParse(curr.Substring(2), out int gId))
                {
                    gateGroups[gId] = groupId;
                }
                else if (curr.StartsWith("S_") && int.TryParse(curr.Substring(2), out int sId))
                {
                    switchGroups[sId] = groupId;
                }

                if (adj.TryGetValue(curr, out var neighbors))
                {
                    foreach (var neighbor in neighbors)
                    {
                        if (!visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }
        }
    }
}
