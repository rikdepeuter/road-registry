namespace RoadRegistry.Tests.Framework.Assertions;

using System.Runtime.Serialization;

/// <summary>
///     Represents an error about an ill-behaved greater than operator.
/// </summary>
[Serializable]
public class GreaterThanOperatorException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="GreaterThanOperatorException" /> class.
    /// </summary>
    public GreaterThanOperatorException(Type type)
        : base($"The greater than operator on {type?.Name} is ill-behaved.")
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="GreaterThanOperatorException" /> class.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="message">
    ///     The error message that explains the reason for the exception.
    /// </param>
    public GreaterThanOperatorException(Type type, string message)
        : base(message)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="GreaterThanOperatorException" /> class.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="message">
    ///     The error message that explains the reason for the exception.
    /// </param>
    /// <param name="innerException">
    ///     The exception that is the cause of the current exception.
    /// </param>
    public GreaterThanOperatorException(Type type, string message, Exception innerException)
        : base(message, innerException)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="GreaterThanOperatorException" /> class.
    /// </summary>
    /// <param name="info">
    ///     The <see cref="System.Runtime.Serialization.SerializationInfo" /> that holds the
    ///     serialized object data about the exception being thrown.
    /// </param>
    /// <param name="context">
    ///     The <see cref="System.Runtime.Serialization.StreamingContext" /> that contains
    ///     contextual information about the source or destination.
    /// </param>
    protected GreaterThanOperatorException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        Type = (Type)info.GetValue("Type", typeof(Type));
    }

    public Type Type { get; }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        if (info == null)
            throw new ArgumentNullException(nameof(info));

        base.GetObjectData(info, context);
        info.AddValue("Type", Type);
    }
}
