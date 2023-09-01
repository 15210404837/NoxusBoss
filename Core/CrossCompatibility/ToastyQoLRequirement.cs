using System;

namespace NoxusBoss.Core.CrossCompatibility
{
    public struct ToastyQoLRequirement
    {
        // Typically the name of a boss, but theoretically can be anything.
        public string RequirementName;

        public Func<bool> Requirement;

        public ToastyQoLRequirement(string requirementName, Func<bool> requirement)
        {
            RequirementName = requirementName;
            Requirement = requirement;
        }
    }
}
