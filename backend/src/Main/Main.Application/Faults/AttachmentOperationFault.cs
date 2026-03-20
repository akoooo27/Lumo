using SharedKernel;

namespace Main.Application.Faults;

internal static class AttachmentOperationFault
{
    internal static readonly Fault NotFound = Fault.NotFound
    (
        title: "Attachment.NotFound",
        detail: "The specified attachment was not found."
    );
}