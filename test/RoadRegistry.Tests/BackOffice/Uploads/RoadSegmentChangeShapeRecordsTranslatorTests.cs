namespace RoadRegistry.Tests.BackOffice.Uploads;

using System.IO.Compression;
using AutoFixture;
using Be.Vlaanderen.Basisregisters.Shaperon;
using Be.Vlaanderen.Basisregisters.Shaperon.Geometries;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Implementation;
using RoadRegistry.BackOffice.Uploads;
using Xunit;
using Point = NetTopologySuite.Geometries.Point;

public class RoadSegmentChangeShapeRecordsTranslatorTests : IDisposable
{
    private readonly ZipArchive _archive;
    private readonly ZipArchiveEntry _entry;
    private readonly IEnumerator<ShapeRecord> _enumerator;
    private readonly Fixture _fixture;
    private readonly MemoryStream _stream;
    private readonly RoadSegmentChangeShapeRecordsTranslator _sut;

    public RoadSegmentChangeShapeRecordsTranslatorTests()
    {
        _fixture = new Fixture();
        _fixture.CustomizeRoadNodeId();
        _fixture.CustomizeRoadSegmentId();
        _fixture.CustomizeRoadSegmentGeometryDrawMethod();
        _fixture.CustomizeOrganizationId();
        _fixture.CustomizeRoadSegmentMorphology();
        _fixture.CustomizeRoadSegmentStatus();
        _fixture.CustomizeRoadSegmentCategory();
        _fixture.CustomizeRoadSegmentAccessRestriction();

        _fixture.Customize<Point>(customization =>
            customization.FromFactory(generator =>
                new Point(
                    _fixture.Create<double>(),
                    _fixture.Create<double>()
                )
            ).OmitAutoProperties()
        );
        _fixture.Customize<LineString>(customization =>
            customization.FromFactory(generator =>
                new LineString(
                    new CoordinateArraySequence(
                        new[]
                        {
                            new Coordinate(0.0, 0.0),
                            new Coordinate(1.0, 1.0)
                        }),
                    GeometryConfiguration.GeometryFactory
                )
            ).OmitAutoProperties()
        );
        _fixture.Customize<MultiLineString>(customization =>
            customization.FromFactory(generator =>
                new MultiLineString(new[] { _fixture.Create<LineString>() })
            ).OmitAutoProperties()
        );
        _fixture.Customize<RecordNumber>(customizer =>
            customizer.FromFactory(random => new RecordNumber(random.Next(1, int.MaxValue))));
        _fixture.Customize<ShapeRecord>(customization =>
            customization.FromFactory(random =>
                new PolyLineMShapeContent(GeometryTranslator.FromGeometryMultiLineString(_fixture.Create<MultiLineString>())).RecordAs(_fixture.Create<RecordNumber>())
            ).OmitAutoProperties()
        );

        _sut = new RoadSegmentChangeShapeRecordsTranslator();
        _enumerator = new List<ShapeRecord>().GetEnumerator();
        _stream = new MemoryStream();
        _archive = new ZipArchive(_stream, ZipArchiveMode.Create);
        _entry = _archive.CreateEntry("wegsegment_all.shp");
    }

    public void Dispose()
    {
        _archive?.Dispose();
        _stream?.Dispose();
    }

    [Fact]
    public void IsZipArchiveShapeRecordsTranslator()
    {
        Assert.IsAssignableFrom<IZipArchiveShapeRecordsTranslator>(_sut);
    }

    [Fact]
    public void TranslateEntryCanNotBeNull()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.Translate(null, _enumerator, TranslatedChanges.Empty));
    }

    [Fact]
    public void TranslateRecordsCanNotBeNull()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.Translate(_entry, null, TranslatedChanges.Empty));
    }

    [Fact]
    public void TranslateChangesCanNotBeNull()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.Translate(_entry, _enumerator, null));
    }

    [Fact]
    public void TranslateWithoutRecordsReturnsExpectedResult()
    {
        var result = _sut.Translate(_entry, _enumerator, TranslatedChanges.Empty);

        Assert.Equal(
            TranslatedChanges.Empty,
            result);
    }

    [Fact]
    public void TranslateWithAddRecordsReturnsExpectedResult()
    {
        var segment = _fixture.Create<AddRoadSegment>();
        var record = _fixture.Create<ShapeRecord>().Content.RecordAs(segment.RecordNumber);
        var records = new List<ShapeRecord> { record };
        var enumerator = records.GetEnumerator();
        var changes = TranslatedChanges.Empty.AppendChange(segment);

        var result = _sut.Translate(_entry, enumerator, changes);

        var expected = TranslatedChanges.Empty.AppendChange(
            segment.WithGeometry(
                GeometryTranslator.ToGeometryMultiLineString(
                    ((PolyLineMShapeContent)record.Content).Shape)
            )
        );

        Assert.Equal(expected, result, new TranslatedChangeEqualityComparer());
    }

    [Fact]
    public void TranslateWithIdenticalRecordsReturnsExpectedResult()
    {
        var segment = _fixture.Create<ModifyRoadSegment>();
        var record = _fixture.Create<ShapeRecord>().Content.RecordAs(segment.RecordNumber);
        var records = new List<ShapeRecord> { record };
        var enumerator = records.GetEnumerator();
        var changes = TranslatedChanges.Empty.AppendProvisionalChange(segment);

        var result = _sut.Translate(_entry, enumerator, changes);

        var expected = segment.WithGeometry(
            GeometryTranslator.ToGeometryMultiLineString(
                ((PolyLineMShapeContent)record.Content).Shape)
        );
        var expectedResult = TranslatedChanges.Empty.AppendProvisionalChange(
            expected
        );

        Assert.Equal(expectedResult, result, new TranslatedChangeEqualityComparer());
        Assert.True(result.TryFindRoadSegmentProvisionalChange(segment.Id, out var actual));
        Assert.Equal(expected, actual, new TranslatedChangeEqualityComparer());
    }

    [Fact]
    public void TranslateWithModifyRecordsReturnsExpectedResult()
    {
        var segment = _fixture.Create<ModifyRoadSegment>();
        var record = _fixture.Create<ShapeRecord>().Content.RecordAs(segment.RecordNumber);
        var records = new List<ShapeRecord> { record };
        var enumerator = records.GetEnumerator();
        var changes = TranslatedChanges.Empty.AppendChange(segment);

        var result = _sut.Translate(_entry, enumerator, changes);

        var expected = TranslatedChanges.Empty.AppendChange(
            segment.WithGeometry(
                GeometryTranslator.ToGeometryMultiLineString(
                    ((PolyLineMShapeContent)record.Content).Shape)
            )
        );

        Assert.Equal(expected, result, new TranslatedChangeEqualityComparer());
    }

    [Fact]
    public void TranslateWithRemoveRecordsReturnsExpectedResult()
    {
        var segment = _fixture.Create<RemoveRoadSegment>();
        var record = _fixture.Create<ShapeRecord>().Content.RecordAs(segment.RecordNumber);
        var records = new List<ShapeRecord> { record };
        var enumerator = records.GetEnumerator();
        var changes = TranslatedChanges.Empty.AppendChange(segment);

        var result = _sut.Translate(_entry, enumerator, changes);

        var expected = TranslatedChanges.Empty.AppendChange(segment);

        Assert.Equal(expected, result, new TranslatedChangeEqualityComparer());
    }
}
