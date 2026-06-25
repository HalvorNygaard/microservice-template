using TUnit.Core.Interfaces;

[assembly: ParallelLimiter<MicroserviceTemplate.Tests.ParallelLimit>]

namespace MicroserviceTemplate.Tests;

public sealed class ParallelLimit : IParallelLimit
{
    public int Limit => 2;
}
