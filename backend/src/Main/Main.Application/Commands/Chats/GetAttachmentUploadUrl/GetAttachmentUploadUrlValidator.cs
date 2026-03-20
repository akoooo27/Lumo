using FluentValidation;

using Main.Application.Abstractions.Storage;

namespace Main.Application.Commands.Chats.GetAttachmentUploadUrl;

internal sealed class GetAttachmentUploadUrlValidator : AbstractValidator<GetAttachmentUploadUrlCommand>
{
    public GetAttachmentUploadUrlValidator()
    {
        RuleFor(c => c.ContentType)
            .NotEmpty().WithMessage("Content Type is required")
            .Must(ct => AttachmentConstants.AllowedContentTypes.Contains(ct, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Content Type must be one of: {string.Join(", ", AttachmentConstants.AllowedContentTypes)}");

        RuleFor(c => c.ContentLength)
            .GreaterThan(0).WithMessage("Content Length must be greater than 0")
            .LessThanOrEqualTo(AttachmentConstants.MaxFileSizeInBytes)
            .WithMessage($"Content Length must not exceed {AttachmentConstants.MaxFileSizeInBytes / (1024 * 1024)} MB");
    }
}