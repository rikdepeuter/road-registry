namespace RoadRegistry.Framework.Projections
{
    using Newtonsoft.Json;

    public static class Formatters {
        public static string NamedJsonMessage<T>(T message)
        {
            return $"{message.GetType().Name} - {JsonConvert.SerializeObject(message, Formatting.Indented)}";
        }
    }
}