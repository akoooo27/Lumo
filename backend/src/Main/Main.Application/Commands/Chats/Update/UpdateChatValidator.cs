using FluentValidation;

using Main.Domain.Constants;

namespace Main.Application.Commands.Chats.Update;

internal sealed class UpdateChatValidator : AbstractValidator<UpdateChatCommand>
{
    public UpdateChatValidator()
    {
        RuleFor(ucc => ucc.ChatId)
            .NotEmpty().WithMessage("Chat ID is required");

        RuleFor(ucc => ucc)
            .Must(ucc => ucc.NewTitle is not null || ucc.IsArchived is not null || ucc.IsPinned is not null || ucc.HasFolderId)
            .WithMessage("At least one field must be provided to update chat");

        RuleFor(ucc => ucc)
            .Must(ucc => !(ucc.NewTitle is not null && ucc.IsArchived is not null))
            .WithMessage("Cannot update title and archive status in the same request");

        When(ucc => ucc.NewTitle is not null, () =>
        {
            RuleFor(ucc => ucc.NewTitle)
                .NotEmpty().WithMessage("Title cannot be empty")
                .MaximumLength(ChatConstants.MaxTitleLength)
                .WithMessage($"Title must not exceed {ChatConstants.MaxTitleLength} characters");
        });

        RuleFor(ucc => ucc)
            .Must(ucc => !(ucc.IsPinned is true && ucc.IsArchived is true))
            .WithMessage("Cannot pin and archive a chat in the same request");
    }
}