using System.Collections.Generic;
using TradeCopia.Domain.Model;
using TradeCopia.Domain.Topology;

namespace TradeCopia.Domain.Config
{
    public sealed class ConfigValidationResult
    {
        public ConfigValidationResult(bool isValid, IReadOnlyList<string> errors)
        {
            IsValid = isValid;
            Errors = errors;
        }

        public bool IsValid { get; }
        public IReadOnlyList<string> Errors { get; }
    }

    public static class ConfigValidator
    {
        public static ConfigValidationResult Validate(ActiveConfigSnapshot snapshot)
        {
            var errors = new List<string>();
            if (snapshot.Groups.Count == 0)
            {
                errors.Add("At least one copy group is required to activate.");
            }

            foreach (var group in snapshot.Groups)
            {
                if (group.CopyMode == CopyMode.ExecutionMirror)
                {
                    errors.Add("Group '" + group.Name + "' uses Execution Mirror, which is not enabled until Order Mirror is stable.");
                }
            }

            var topology = TopologyValidator.Validate(snapshot.Groups);
            if (!topology.IsValid)
            {
                errors.AddRange(topology.Errors);
            }

            return new ConfigValidationResult(errors.Count == 0, errors);
        }
    }
}
