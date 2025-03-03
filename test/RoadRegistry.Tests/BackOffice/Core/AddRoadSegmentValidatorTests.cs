namespace RoadRegistry.Tests.BackOffice.Core;

using AutoFixture;
using FluentValidation;
using FluentValidation.TestHelper;
using NetTopologySuite.Geometries;
using RoadRegistry.BackOffice;
using RoadRegistry.BackOffice.Core;
using RoadRegistry.BackOffice.Messages;
using Xunit;

public class AddRoadSegmentValidatorTests
{
    public AddRoadSegmentValidatorTests()
    {
        Fixture = new Fixture();
        Fixture.CustomizeRoadNodeId();
        Fixture.CustomizeRoadSegmentId();
        Fixture.CustomizeRoadSegmentCategory();
        Fixture.CustomizeRoadSegmentMorphology();
        Fixture.CustomizeRoadSegmentStatus();
        Fixture.CustomizeRoadSegmentAccessRestriction();
        Fixture.CustomizeRoadSegmentGeometryDrawMethod();
        Fixture.CustomizeRoadSegmentLaneCount();
        Fixture.CustomizeRoadSegmentLaneDirection();
        Fixture.CustomizeRoadSegmentNumberedRoadDirection();
        Fixture.CustomizeRoadSegmentNumberedRoadOrdinal();
        Fixture.CustomizeRoadSegmentSurfaceType();
        Fixture.CustomizeRoadSegmentWidth();
        Fixture.CustomizeEuropeanRoadNumber();
        Fixture.CustomizeNationalRoadNumber();
        Fixture.CustomizeNumberedRoadNumber();
        Fixture.CustomizeOrganizationId();
        Fixture.CustomizeOrganizationName();

        Fixture.Customize<RoadSegmentEuropeanRoadAttributes>(composer =>
            composer.Do(instance =>
            {
                instance.AttributeId = Fixture.Create<AttributeId>();
                instance.Number = Fixture.Create<EuropeanRoadNumber>();
            }).OmitAutoProperties());
        Fixture.Customize<RoadSegmentNationalRoadAttributes>(composer =>
            composer.Do(instance =>
            {
                instance.AttributeId = Fixture.Create<AttributeId>();
                instance.Number = Fixture.Create<NationalRoadNumber>();
            }).OmitAutoProperties());
        Fixture.Customize<RoadSegmentNumberedRoadAttributes>(composer =>
            composer.Do(instance =>
            {
                instance.AttributeId = Fixture.Create<AttributeId>();
                instance.Number = Fixture.Create<NumberedRoadNumber>();
                instance.Direction = Fixture.Create<RoadSegmentNumberedRoadDirection>();
                instance.Ordinal = Fixture.Create<RoadSegmentNumberedRoadOrdinal>();
            }).OmitAutoProperties());
        Fixture.Customize<RequestedRoadSegmentLaneAttribute>(composer =>
            composer.Do(instance =>
            {
                var positionGenerator = new Generator<RoadSegmentPosition>(Fixture);
                instance.AttributeId = Fixture.Create<AttributeId>();
                instance.FromPosition = positionGenerator.First(candidate => candidate >= 0.0m);
                instance.ToPosition = positionGenerator.First(candidate => candidate > instance.FromPosition);
                instance.Count = Fixture.Create<RoadSegmentLaneCount>();
                instance.Direction = Fixture.Create<RoadSegmentLaneDirection>();
            }).OmitAutoProperties());
        Fixture.Customize<RequestedRoadSegmentWidthAttribute>(composer =>
            composer.Do(instance =>
            {
                var positionGenerator = new Generator<RoadSegmentPosition>(Fixture);
                instance.AttributeId = Fixture.Create<AttributeId>();
                instance.FromPosition = positionGenerator.First(candidate => candidate >= 0.0m);
                instance.ToPosition = positionGenerator.First(candidate => candidate > instance.FromPosition);
                instance.Width = Fixture.Create<RoadSegmentWidth>();
            }).OmitAutoProperties());
        Fixture.Customize<RequestedRoadSegmentSurfaceAttribute>(composer =>
            composer.Do(instance =>
            {
                var positionGenerator = new Generator<RoadSegmentPosition>(Fixture);
                instance.AttributeId = Fixture.Create<AttributeId>();
                instance.FromPosition = positionGenerator.First(candidate => candidate >= 0.0m);
                instance.ToPosition = positionGenerator.First(candidate => candidate > instance.FromPosition);
                instance.Type = Fixture.Create<RoadSegmentSurfaceType>();
            }).OmitAutoProperties());
        Validator = new AddRoadSegmentValidator();
    }

    public Fixture Fixture { get; }

    public AddRoadSegmentValidator Validator { get; }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    public void TemporaryIdMustBeGreaterThan(int value)
    {
        Validator.ShouldHaveValidationErrorFor(c => c.TemporaryId, value);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    public void StartNodeIdMustBeGreaterThan(int value)
    {
        Validator.ShouldHaveValidationErrorFor(c => c.StartNodeId, value);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    public void EndNodeIdMustBeGreaterThan(int value)
    {
        Validator.ShouldHaveValidationErrorFor(c => c.EndNodeId, value);
    }

    [Fact]
    public void StartNodeMustNotBeEndNodeId()
    {
        var id = Fixture.Create<RoadNodeId>();
        var data = new RoadRegistry.BackOffice.Messages.AddRoadSegment
        {
            StartNodeId = id, EndNodeId = id
        };
        Validator.ShouldHaveValidationErrorFor(c => c.EndNodeId, data);
    }

    [Fact]
    public void GeometryMustNotBeNull()
    {
        Validator.ShouldHaveValidationErrorFor(c => c.Geometry, (RoadSegmentGeometry)null);
    }

    [Fact]
    public void GeometryHasExpectedValidator()
    {
        Validator.ShouldHaveChildValidator(c => c.Geometry, typeof(RoadSegmentGeometryValidator));
    }

    [Fact]
    public void GeometryDrawMethodMustBeWithinDomain()
    {
        Validator.ShouldHaveValidationErrorFor(c => c.GeometryDrawMethod, Fixture.Create<string>());
    }

    [Fact]
    public void MorphologyMustBeWithinDomain()
    {
        Validator.ShouldHaveValidationErrorFor(c => c.Morphology, Fixture.Create<string>());
    }

    [Fact]
    public void StatusMustBeWithinDomain()
    {
        Validator.ShouldHaveValidationErrorFor(c => c.Status, Fixture.Create<string>());
    }

    [Fact]
    public void CategoryMustBeWithinDomain()
    {
        Validator.ShouldHaveValidationErrorFor(c => c.Category, Fixture.Create<string>());
    }

    [Fact]
    public void AccessRestrictionMustBeWithinDomain()
    {
        Validator.ShouldHaveValidationErrorFor(c => c.AccessRestriction, Fixture.Create<string>());
    }

//        [Fact]
//        public void PartOfEuropeanRoadsMustNotBeNull()
//        {
//            Validator.ShouldHaveValidationErrorFor(c => c.PartOfEuropeanRoads, (RoadSegmentEuropeanRoadAttributes[])null);
//        }
//
//        [Fact]
//        public void PartOfEuropeanRoadMustNotBeNull()
//        {
//            var data = Fixture.CreateMany<RoadSegmentEuropeanRoadAttributes>().ToArray();
//            var index = new Random().Next(0, data.Length);
//            data[index] = null;
//
//            Validator.ShouldHaveValidationErrorFor(c => c.PartOfEuropeanRoads, data);
//        }
//
//        [Fact]
//        public void PartOfEuropeanRoadsHasExpectedValidator()
//        {
//            Validator.ShouldHaveChildValidator(c => c.PartOfEuropeanRoads, typeof(AddRoadSegmentToEuropeanRoadValidator));
//        }
//
//        [Fact]
//        public void PartOfNationalRoadsMustNotBeNull()
//        {
//            Validator.ShouldHaveValidationErrorFor(c => c.PartOfNationalRoads, (RoadSegmentNationalRoadAttributes[])null);
//        }
//
//        [Fact]
//        public void PartOfNationalRoadMustNotBeNull()
//        {
//            var data = Fixture.CreateMany<RoadSegmentNationalRoadAttributes>().ToArray();
//            var index = new Random().Next(0, data.Length);
//            data[index] = null;
//
//            Validator.ShouldHaveValidationErrorFor(c => c.PartOfNationalRoads, data);
//        }
//
//        [Fact]
//        public void PartOfNationalRoadsHasExpectedValidator()
//        {
//            Validator.ShouldHaveChildValidator(c => c.PartOfNationalRoads, typeof(AddRoadSegmentToNationalRoadValidator));
//        }
//
//        [Fact]
//        public void PartOfNumberedRoadsMustNotBeNull()
//        {
//            Validator.ShouldHaveValidationErrorFor(c => c.PartOfNumberedRoads, (RoadSegmentNumberedRoadAttributes[])null);
//        }
//
//        [Fact]
//        public void PartOfNumberedRoadMustNotBeNull()
//        {
//            var data = Fixture.CreateMany<RoadSegmentNumberedRoadAttributes>().ToArray();
//            var index = new Random().Next(0, data.Length);
//            data[index] = null;
//
//            Validator.ShouldHaveValidationErrorFor(c => c.PartOfNumberedRoads, data);
//        }
//
//        [Fact]
//        public void PartOfNumberedRoadsHasExpectedValidator()
//        {
//            Validator.ShouldHaveChildValidator(c => c.PartOfNumberedRoads, typeof(AddRoadSegmentToNumberedRoadValidator));
//        }

    [Fact]
    public void LanesMustNotBeNull()
    {
        Validator.ShouldHaveValidationErrorFor(c => c.Lanes, (RequestedRoadSegmentLaneAttribute[])null);
    }

    [Fact]
    public void LaneMustNotBeNull()
    {
        var data = Fixture.CreateMany<RequestedRoadSegmentLaneAttribute>().ToArray();
        var index = new Random().Next(0, data.Length);
        data[index] = null;

        Validator.ShouldHaveValidationErrorFor(c => c.Lanes, data);
    }

    [Fact]
    public void LanesHasExpectedValidator()
    {
        Validator.ShouldHaveChildValidator(c => c.Lanes, typeof(RequestedRoadSegmentLaneAttributeValidator));
    }

    [Fact]
    public void WidthsMustNotBeNull()
    {
        Validator.ShouldHaveValidationErrorFor(c => c.Widths, (RequestedRoadSegmentWidthAttribute[])null);
    }

    [Fact]
    public void WidthMustNotBeNull()
    {
        var data = Fixture.CreateMany<RequestedRoadSegmentWidthAttribute>().ToArray();
        var index = new Random().Next(0, data.Length);
        data[index] = null;

        Validator.ShouldHaveValidationErrorFor(c => c.Widths, data);
    }

    [Fact]
    public void WidthsHasExpectedValidator()
    {
        Validator.ShouldHaveChildValidator(c => c.Widths, typeof(RequestedRoadSegmentWidthAttributeValidator));
    }

    [Fact]
    public void SurfacesMustNotBeNull()
    {
        Validator.ShouldHaveValidationErrorFor(c => c.Surfaces, (RequestedRoadSegmentSurfaceAttribute[])null);
    }

    [Fact]
    public void SurfaceMustNotBeNull()
    {
        var data = Fixture.CreateMany<RequestedRoadSegmentSurfaceAttribute>().ToArray();
        var index = new Random().Next(0, data.Length);
        data[index] = null;

        Validator.ShouldHaveValidationErrorFor(c => c.Surfaces, data);
    }

    [Fact]
    public void SurfacesHasExpectedValidator()
    {
        Validator.ShouldHaveChildValidator(c => c.Surfaces, typeof(RequestedRoadSegmentSurfaceAttributeValidator));
    }

    [Fact]
    public void VerifyValid()
    {
        Fixture.CustomizePolylineM();

        var data = new RoadRegistry.BackOffice.Messages.AddRoadSegment
        {
            TemporaryId = Fixture.Create<RoadSegmentId>(),
            StartNodeId = Fixture.Create<RoadNodeId>(),
            EndNodeId = Fixture.Create<RoadNodeId>(),
            Geometry = GeometryTranslator.Translate(Fixture.Create<MultiLineString>()),
            MaintenanceAuthority = Fixture.Create<OrganizationId>(),
            GeometryDrawMethod = Fixture.Create<RoadSegmentGeometryDrawMethod>(),
            Morphology = Fixture.Create<RoadSegmentMorphology>(),
            Status = Fixture.Create<RoadSegmentStatus>(),
            Category = Fixture.Create<RoadSegmentCategory>(),
            AccessRestriction = Fixture.Create<RoadSegmentAccessRestriction>(),
            LeftSideStreetNameId = Fixture.Create<int?>(),
            RightSideStreetNameId = Fixture.Create<int?>(),
            Lanes = Fixture.CreateMany<RequestedRoadSegmentLaneAttribute>().ToArray(),
            Widths = Fixture.CreateMany<RequestedRoadSegmentWidthAttribute>().ToArray(),
            Surfaces = Fixture.CreateMany<RequestedRoadSegmentSurfaceAttribute>().ToArray()
        };

        Validator.ValidateAndThrow(data);
    }
}
