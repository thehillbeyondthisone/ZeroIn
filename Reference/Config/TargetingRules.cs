using System.Collections.Generic;

namespace AutomatonRoamba
{
    public class TargetingRules
    {
        public List<string> IgnoredNames;
        public List<string> PriorityNames;

        public TargetingRules()
        {
            IgnoredNames = new List<string>();
            PriorityNames = new List<string>();
        }
    }
}
