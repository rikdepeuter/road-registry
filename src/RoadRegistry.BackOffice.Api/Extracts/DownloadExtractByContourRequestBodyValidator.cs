namespace RoadRegistry.BackOffice.Api.Extracts
{
    using System;
    using FluentValidation;
    using Microsoft.Extensions.Logging;
    using NetTopologySuite.Geometries;
    using NetTopologySuite.IO;

    public class DownloadExtractByContourRequestBodyValidator : AbstractValidator<DownloadExtractByContourRequestBody>
    {
        private readonly WKTReader _reader;
        private readonly ILogger<DownloadExtractByContourRequestBodyValidator> _logger;

        private const double MaxArea = 100000;

        public DownloadExtractByContourRequestBodyValidator(WKTReader reader, ILogger<DownloadExtractByContourRequestBodyValidator> logger)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            RuleFor(c => c.Contour)
                .NotEmpty()
                    .WithMessage("'Contour' must not be empty, null or missing")
                .Must(BeMultiPolygonGeometryAsWellKnownText)
                    .WithMessage("'Contour' must be a valid multipolygon or polygon represented as well-known text")
                    .When(c => !string.IsNullOrEmpty(c.Contour), ApplyConditionTo.CurrentValidator)
                .Must(HaveContourNotTooLarge)
                    .WithMessage($"'Contour' must have a surface area of maximum {MaxArea / 1000} km²")
                    .When(c => !string.IsNullOrEmpty(c.Contour), ApplyConditionTo.CurrentValidator);

            RuleFor(c => c.Buffer)
                .InclusiveBetween(0, 100).WithMessage("'Buffer' must be a value between 0 and 100");

            RuleFor(c => c.Description)
                .NotNull().WithMessage("'Description' must not be null or missing")
                .MaximumLength(ExtractDescription.MaxLength).WithMessage($"'Description' must not be longer than {ExtractDescription.MaxLength} characters");
        }

        private Geometry ParseWellKnownText(string wkt)
        {
            try
            {
                var geometry = _reader.Read(wkt);
                return geometry;
            }
            catch (ParseException exception)
            {
                _logger.LogWarning(exception, "The download extract request body validation encountered a problem while trying to parse the contour as well-known text");
                return null;
            }
        }

        private bool BeMultiPolygonGeometryAsWellKnownText(string text)
        {
            var geometry = ParseWellKnownText(text);
            return geometry switch
            {
                MultiPolygon multiPolygon => multiPolygon.IsValid,
                Polygon polygon => polygon.IsValid,
                _ => false
            };
        }

        private bool HaveContourNotTooLarge(string text)
        {
            var geometry = ParseWellKnownText(text);
            return geometry switch
            {
                MultiPolygon multiPolygon => multiPolygon.Area <= MaxArea,
                Polygon polygon => polygon.Area <= MaxArea,
                _ => false
            };
        }
    }
}
