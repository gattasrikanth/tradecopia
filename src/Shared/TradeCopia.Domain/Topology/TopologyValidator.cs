using System;
using System.Collections.Generic;
using TradeCopia.Domain.Model;

namespace TradeCopia.Domain.Topology
{
    public sealed class TopologyValidationResult
    {
        public TopologyValidationResult(bool isValid, IReadOnlyList<string> errors)
        {
            IsValid = isValid;
            Errors = errors ?? Array.Empty<string>();
        }

        public bool IsValid { get; }
        public IReadOnlyList<string> Errors { get; }
    }

    public static class TopologyValidator
    {
        public static TopologyValidationResult Validate(IReadOnlyList<CopyGroup> groups)
        {
            if (groups == null)
            {
                throw new ArgumentNullException(nameof(groups));
            }

            var errors = new List<string>();
            var adjacency = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            var leaders = new HashSet<string>(StringComparer.Ordinal);
            var followers = new HashSet<string>(StringComparer.Ordinal);

            foreach (var group in groups)
            {
                if (group.Followers.Length == 0)
                {
                    errors.Add("Group '" + group.Name + "' has no followers.");
                }

                var seenFollowers = new HashSet<string>(StringComparer.Ordinal);
                foreach (var follower in group.Followers)
                {
                    if (follower.Account == group.Leader)
                    {
                        errors.Add("Group '" + group.Name + "' has a self-edge.");
                    }

                    if (!seenFollowers.Add(follower.Account.Value))
                    {
                        errors.Add("Group '" + group.Name + "' lists follower '" + follower.Account.Value + "' more than once.");
                    }

                    AddEdge(adjacency, group.Leader.Value, follower.Account.Value);
                    leaders.Add(group.Leader.Value);
                    followers.Add(follower.Account.Value);
                }
            }

            foreach (var account in leaders)
            {
                if (followers.Contains(account))
                {
                    errors.Add("Account '" + account + "' is both a leader and a follower. V1 requires a strict star/forest topology.");
                }
            }

            var visiting = new HashSet<string>(StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            foreach (var node in adjacency.Keys)
            {
                if (HasCycle(node, adjacency, visiting, visited, new List<string>(), errors))
                {
                    break;
                }
            }

            return new TopologyValidationResult(errors.Count == 0, errors);
        }

        private static void AddEdge(Dictionary<string, HashSet<string>> adjacency, string from, string to)
        {
            HashSet<string> set;
            if (!adjacency.TryGetValue(from, out set))
            {
                set = new HashSet<string>(StringComparer.Ordinal);
                adjacency[from] = set;
            }

            set.Add(to);
            if (!adjacency.ContainsKey(to))
            {
                adjacency[to] = new HashSet<string>(StringComparer.Ordinal);
            }
        }

        private static bool HasCycle(
            string node,
            Dictionary<string, HashSet<string>> adjacency,
            HashSet<string> visiting,
            HashSet<string> visited,
            List<string> stack,
            List<string> errors)
        {
            if (visited.Contains(node))
            {
                return false;
            }

            if (!visiting.Add(node))
            {
                errors.Add("Cycle detected: " + string.Join(" -> ", stack) + " -> " + node);
                return true;
            }

            stack.Add(node);
            HashSet<string> next;
            if (adjacency.TryGetValue(node, out next))
            {
                foreach (var child in next)
                {
                    if (HasCycle(child, adjacency, visiting, visited, stack, errors))
                    {
                        return true;
                    }
                }
            }

            stack.RemoveAt(stack.Count - 1);
            visiting.Remove(node);
            visited.Add(node);
            return false;
        }
    }
}
