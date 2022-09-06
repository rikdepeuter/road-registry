namespace RoadRegistry.Syndication.ProjectionHost
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Xml;
    using Be.Vlaanderen.Basisregisters.ProjectionHandling.SqlStreamStore;
    using Be.Vlaanderen.Basisregisters.ProjectionHandling.Syndication;
    using Mapping;
    using Microsoft.SyndicationFeed;
    using Projections.Syndication;

    public class AtomEnvelopeFactory
    {
        private readonly EventSerializerMapping _eventSerializers;
        private readonly DataContractSerializer _dataContractSerializer;

        public AtomEnvelopeFactory(
            EventSerializerMapping eventSerializerMapping,
            DataContractSerializer dataContractSerializer)
        {
            _eventSerializers = eventSerializerMapping;
            _dataContractSerializer = dataContractSerializer;
        }

        public object CreateEnvelope<T>(IAtomEntry message)
        {
            using (var contentXmlReader =
                XmlReader.Create(
                    new StringReader(message.Description),
                    new XmlReaderSettings { Async = true }))
            {
                var atomEntry = new AtomEntry(message, _dataContractSerializer.ReadObject(contentXmlReader));
                var content = (SyndicationContent<T>)atomEntry.Content;
                
                using (var eventXmlReader = XmlReader.Create(new StringReader(content.Event.OuterXml)))
                {
                    var serializer = FindEventSerializer(content);
                    if (serializer == null)
                        return null;

                    var @event = serializer.ReadObject(eventXmlReader);

                    var metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        [Envelope.PositionMetadataKey] = Convert.ToInt64(atomEntry.FeedEntry.Id),
                        [Envelope.EventNameMetadataKey] = atomEntry.FeedEntry.Title,
                        [Envelope.CreatedUtcMetadataKey] = atomEntry.FeedEntry.Published
                    };

                    var envelope =
                        new Envelope(
                                @event,
                                metadata)
                            .ToGenericEnvelope();

                    return envelope;
                }
            }
        }

        //TODO-rik obsolete
        private DataContractSerializer FindEventSerializer(AtomEntry atomEntry)
        {
            var splitTitle = atomEntry.FeedEntry.Title.Split('-');
            if (!splitTitle.Any())
                throw new FormatException($"Could not find event name in atom entry with id {atomEntry.FeedEntry.Id}. Title was '{atomEntry.FeedEntry.Title}'.");

            var eventName = splitTitle[0];

            return _eventSerializers.HasSerializerFor(eventName) ? _eventSerializers.GetSerializerFor(eventName) : null;
        }

        private DataContractSerializer FindEventSerializer<T>(SyndicationContent<T> content)
        {
            var eventName = content.Event.LocalName;

            return _eventSerializers.HasSerializerFor(eventName) ? _eventSerializers.GetSerializerFor(eventName) : null;
        }
    }
}
