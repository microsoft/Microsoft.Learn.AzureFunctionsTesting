using System;

namespace Microsoft.Learn.AzureFunctionsTesting
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class AssemblyFixtureAttribute : Attribute
    {
        public Type[] FixtureTypes { get; }

        public AssemblyFixtureAttribute(params Type[] fixtureTypes)
        {
            FixtureTypes = fixtureTypes;
        }
    }
}
