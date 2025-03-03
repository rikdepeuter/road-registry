﻿namespace RoadRegistry.Tests.BackOffice;

using Albedo;
using AutoFixture;
using AutoFixture.Idioms;
using Framework.Assertions;
using RoadRegistry.BackOffice;
using Xunit;

public class RoadSegmentWidthTests
{
    private readonly Fixture _fixture;

    public RoadSegmentWidthTests()
    {
        _fixture = new Fixture();
        _fixture.CustomizeRoadSegmentWidth();
    }

    [Fact]
    public void VerifyBehavior()
    {
        _fixture.Customizations.Add(
            new FiniteSequenceGenerator<int>(Enumerable.Range(0, RoadSegmentWidth.Maximum.ToInt32()).ToArray()));
        new CompositeIdiomaticAssertion(
            new ImplicitConversionOperatorAssertion<int>(_fixture),
            new ExplicitConversionMethodAssertion<int>(_fixture),
            new EquatableEqualsSelfAssertion(_fixture),
            new EquatableEqualsOtherAssertion(_fixture),
            new EqualityOperatorEqualsSelfAssertion(_fixture),
            new EqualityOperatorEqualsOtherAssertion(_fixture),
            new InequalityOperatorEqualsSelfAssertion(_fixture),
            new InequalityOperatorEqualsOtherAssertion(_fixture),
            new EqualsNewObjectAssertion(_fixture),
            new EqualsNullAssertion(_fixture),
            new EqualsSelfAssertion(_fixture),
            new EqualsOtherAssertion(_fixture),
            new EqualsSuccessiveAssertion(_fixture),
            new GetHashCodeSuccessiveAssertion(_fixture)
        ).Verify(typeof(RoadSegmentWidth));

        new GuardClauseAssertion(_fixture,
                new Int32RangeBehaviorExpectation(0, RoadSegmentWidth.Maximum.ToInt32(), -8, -9))
            .Verify(Constructors.Select(() => new RoadSegmentWidth(0)));
    }

    [Theory]
    [InlineData(int.MinValue, false)]
    [InlineData(-9, true)]
    [InlineData(-8, true)]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(45, true)]
    [InlineData(46, false)]
    [InlineData(int.MaxValue, false)]
    public void AcceptsReturnsExpectedResult(int value, bool expected)
    {
        var result = RoadSegmentWidth.Accepts(value);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToStringReturnsExpectedResult()
    {
        var value = _fixture.Create<int>() % RoadSegmentWidth.Maximum.ToInt32();
        var sut = new RoadSegmentWidth(value);

        Assert.Equal(value.ToString(), sut.ToString());
    }
}
