namespace RoadRegistry.BackOffice.Api.Tests.Extracts;

using BackOffice.Abstractions.Extracts;
using FluentAssertions;
using FluentValidation;
using Handlers.Extracts;
using Microsoft.Extensions.Logging.Abstractions;
using NetTopologySuite.IO;

public class DownloadExtractByContourRequestBodyValidatorTests
{
    private const string ValidContour = "MULTIPOLYGON (((30 20, 45 40, 10 40, 30 20)))";
    private const string ValidDescription = "description";
    private const int ValidBuffer = 50;

    private readonly DownloadExtractByContourRequestValidator _validator;

    public DownloadExtractByContourRequestBodyValidatorTests()
    {
        _validator = new DownloadExtractByContourRequestValidator(
            new WKTReader(),
            new NullLogger<DownloadExtractByContourRequestValidator>());
    }

    [Fact]
    public async Task Validate_will_not_allow_invalid_geometry()
    {
        var act = () => _validator.ValidateAndThrowAsync(new DownloadExtractByContourRequest("invalid", ValidBuffer, ValidDescription));
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_will_not_allow_empty_geometry(string givenContour)
    {
        var act = () => _validator.ValidateAndThrowAsync(new DownloadExtractByContourRequest(givenContour, ValidBuffer, ValidDescription));
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Validate_will_allow_valid_geometry()
    {
        var act = () => _validator.ValidateAndThrowAsync(new DownloadExtractByContourRequest(ValidContour, ValidBuffer, ValidDescription));

        await act.Should().NotThrowAsync<ValidationException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public async Task Validate_will_not_allow_invalid_buffer(int givenBuffer)
    {
        var act = () => _validator.ValidateAndThrowAsync(new DownloadExtractByContourRequest(ValidContour, givenBuffer, ValidDescription));
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(99)]
    [InlineData(100)]
    public async Task Validate_will_allow_valid_buffer(int givenBuffer)
    {
        var act = () => _validator.ValidateAndThrowAsync(new DownloadExtractByContourRequest(ValidContour, givenBuffer, ValidDescription));
        await act.Should().NotThrowAsync<ValidationException>();
    }

    [Theory]
    [MemberData(nameof(ValidDescriptionCases))]
    public async Task Validate_will_allow_valid_description(string givenDescription)
    {
        var act = () => _validator.ValidateAndThrowAsync(new DownloadExtractByContourRequest(ValidContour, ValidBuffer, givenDescription));

        await act.Should().NotThrowAsync<ValidationException>();
    }

    public static IEnumerable<object[]> ValidDescriptionCases()
    {
        yield return new object[] { string.Empty };
        yield return new object[] { "description" };
        yield return new object[] { new string(Enumerable.Repeat('a', ExtractDescription.MaxLength).ToArray()) };
    }

    [Theory]
    [MemberData(nameof(InvalidDescriptionCases))]
    public async Task Validate_will_not_allow_invalid_description(string givenDescription)
    {
        var act = () => _validator.ValidateAndThrowAsync(new DownloadExtractByContourRequest(ValidContour, ValidBuffer, givenDescription));

        await act.Should().ThrowAsync<ValidationException>();
    }

    public static IEnumerable<object[]> InvalidDescriptionCases()
    {
        yield return new object[] { null };
        yield return new object[] { new string(Enumerable.Repeat('a', ExtractDescription.MaxLength + 1).ToArray()) };
    }
}
