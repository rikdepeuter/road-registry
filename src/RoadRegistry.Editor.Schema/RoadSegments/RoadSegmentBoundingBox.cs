namespace RoadRegistry.Editor.Schema.RoadSegments;

using System.Linq;
using Be.Vlaanderen.Basisregisters.Shaperon;
using Be.Vlaanderen.Basisregisters.Shaperon.Geometries;
using NetTopologySuite.Geometries;

public class RoadSegmentBoundingBox
{
    public double MinimumX { get; set; }
    public double MaximumX { get; set; }
    public double MinimumY { get; set; }
    public double MaximumY { get; set; }
    public double MinimumM { get; set; }
    public double MaximumM { get; set; }

    public static RoadSegmentBoundingBox From(PolyLineM shape)
    {
        return new RoadSegmentBoundingBox
        {
            MinimumX = GeometryTranslator.ToGeometryMultiLineString(shape).EnvelopeInternal.MinX,
            MinimumY = GeometryTranslator.ToGeometryMultiLineString(shape).EnvelopeInternal.MinY,
            MaximumX = GeometryTranslator.ToGeometryMultiLineString(shape).EnvelopeInternal.MaxX,
            MaximumY = GeometryTranslator.ToGeometryMultiLineString(shape).EnvelopeInternal.MaxY,
            MinimumM = GeometryTranslator.ToGeometryMultiLineString(shape).GetOrdinates(Ordinate.M).DefaultIfEmpty(double.NegativeInfinity).Min(),
            MaximumM = GeometryTranslator.ToGeometryMultiLineString(shape).GetOrdinates(Ordinate.M).DefaultIfEmpty(double.PositiveInfinity).Max()
        };
    }

    public BoundingBox3D ToBoundingBox3D()
    {
        return new BoundingBox3D(
            MinimumX,
            MinimumY,
            MaximumX,
            MaximumY,
            0.0,
            0.0,
            MinimumM,
            MaximumM
        );
    }
}
