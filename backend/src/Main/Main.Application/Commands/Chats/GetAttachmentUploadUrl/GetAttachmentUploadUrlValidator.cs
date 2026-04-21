using FluentValidation;

using Main.Domain.ValueObjects;

namespace Main.Application.Commands.Chats.GetAttachmentUploadUrl;

internal sealed class GetAttachmentUploadUrlValidator : AbstractValidator<GetAttachmentUploadUrlCommand>
{
    public GetAttachmentUploadUrlValidator()
    {
        RuleFor(c => c.ContentType)
            .NotEmpty().WithMessage("Content Type is required")
            .Must(ct => Attachment.IsSupported(ct))
            .WithMessage($"Content Type must be one of: {string.Join(", ", Attachment.AllowedContentTypes)}");

        RuleFor(c => c.ContentLength)
            .GreaterThan(0).WithMessage("Content Length must be greater than 0")
            .LessThanOrEqualTo(Attachment.MaxFileSizeInBytes)
            .WithMessage($"Content Length must not exceed {Attachment.MaxFileSizeInBytes / (1024 * 1024)} MB");
    }
}