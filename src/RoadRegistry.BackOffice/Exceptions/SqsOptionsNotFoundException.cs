namespace RoadRegistry.BackOffice.Exceptions;

using System;

public class SqsOptionsNotFoundException : ApplicationException
{
    public SqsOptionsNotFoundException(string argumentName) : base("Could not resolve SQS options")
    {
        ArgumentName = argumentName;
    }

    public string ArgumentName { get; init; }
}
