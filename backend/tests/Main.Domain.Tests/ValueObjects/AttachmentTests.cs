using FluentAssertions;

using Main.Domain.ValueObjects;

using SharedKernel;

namespace Main.Domain.Tests.ValueObjects;

public sealed class AttachmentTests
{
    private const string ValidFileKey = "attachments/user123/file.jpg";
    private const string ValidContentType = "image/jpeg";
    private const long ValidFileSizeInBytes = 1024;

    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        Outcome<Attachment> outcome = Attachment.Create(ValidFileKey, ValidContentType, ValidFileSizeInBytes);

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value.FileKey.Should().Be(ValidFileKey);
        outcome.Value.ContentType.Should().Be(ValidContentType);
        outcome.Value.FileSizeInBytes.Should().Be(ValidFileSizeInBytes);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithEmptyOrWhitespaceFileKey_ShouldReturnFailure(string? fileKey)
    {
        Outcome<Attachment> outcome = Attachment.Create(fileKey!, ValidContentType, ValidFileSizeInBytes);

        outcome.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/gif")]
    [InlineData("image/webp")]
    public void Create_WithAllowedContentType_ShouldReturnSuccess(string contentType)
    {
        Outcome<Attachment> outcome = Attachment.Create(ValidFileKey, contentType, ValidFileSizeInBytes);

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value.ContentType.Should().Be(contentType);
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("text/plain")]
    [InlineData("video/mp4")]
    [InlineData("image/svg+xml")]
    [InlineData("")]
    public void Create_WithUnsupportedContentType_ShouldReturnFailure(string contentType)
    {
        Outcome<Attachment> outcome = Attachment.Create(ValidFileKey, contentType, ValidFileSizeInBytes);

        outcome.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_WithInvalidFileSize_ShouldReturnFailure(long fileSizeInBytes)
    {
        Outcome<Attachment> outcome = Attachment.Create(ValidFileKey, ValidContentType, fileSizeInBytes);

        outcome.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_WithFileSizeExceedingMax_ShouldReturnFailure()
    {
        long maxSize = 10 * 1024 * 1024;

        Outcome<Attachment> outcome = Attachment.Create(ValidFileKey, ValidContentType, maxSize + 1);

        outcome.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_WithExactMaxFileSize_ShouldReturnSuccess()
    {
        long maxSize = 10 * 1024 * 1024;

        Outcome<Attachment> outcome = Attachment.Create(ValidFileKey, ValidContentType, maxSize);

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value.FileSizeInBytes.Should().Be(maxSize);
    }

    [Fact]
    public void Create_WithMinimumFileSize_ShouldReturnSuccess()
    {
        Outcome<Attachment> outcome = Attachment.Create(ValidFileKey, ValidContentType, 1);

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value.FileSizeInBytes.Should().Be(1);
    }

    [Fact]
    public void Create_WithCaseInsensitiveContentType_ShouldReturnSuccess()
    {
        Outcome<Attachment> outcome = Attachment.Create(ValidFileKey, "IMAGE/JPEG", ValidFileSizeInBytes);

        outcome.IsSuccess.Should().BeTrue();
    }
}
