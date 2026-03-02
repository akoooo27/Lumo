using FluentAssertions;

using Main.Domain.ValueObjects;

using SharedKernel;

namespace Main.Domain.Tests.ValueObjects;

public sealed class WorkflowRunIdTests
{
    private const string ValidWorkflowRunId = "wfr_01JGX123456789012345678901";
    private const string Prefix = "wfr_";
    private const int ExpectedLength = 30;

    [Fact]
    public void From_WithValidId_ShouldReturnSuccess()
    {
        Outcome<WorkflowRunId> outcome = WorkflowRunId.From(ValidWorkflowRunId);

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value.Value.Should().Be(ValidWorkflowRunId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void From_WithEmptyOrWhitespace_ShouldReturnFailure(string? value)
    {
        Outcome<WorkflowRunId> outcome = WorkflowRunId.From(value);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Title.Should().Be("WorkflowRunId.Required");
    }

    [Theory]
    [InlineData("invalid_id")]
    [InlineData("wfr_short")]
    [InlineData("wfr_01JGX12345678901234567890")] // too short
    [InlineData("wfr_01JGX1234567890123456789012")] // too long
    [InlineData("xxx_01JGX123456789012345678901")] // wrong prefix
    public void From_WithInvalidFormat_ShouldReturnFailure(string value)
    {
        Outcome<WorkflowRunId> outcome = WorkflowRunId.From(value);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Title.Should().Be("WorkflowRunId.InvalidFormat");
    }

    [Fact]
    public void UnsafeFrom_ShouldCreateWithoutValidation()
    {
        WorkflowRunId result = WorkflowRunId.UnsafeFrom("invalid");

        result.Value.Should().Be("invalid");
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        WorkflowRunId id = WorkflowRunId.UnsafeFrom(ValidWorkflowRunId);

        string result = id.ToString();

        result.Should().Be(ValidWorkflowRunId);
    }

    [Fact]
    public void IsEmpty_WhenEmpty_ShouldReturnTrue()
    {
        WorkflowRunId id = WorkflowRunId.UnsafeFrom(string.Empty);

        id.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void IsEmpty_WhenNotEmpty_ShouldReturnFalse()
    {
        WorkflowRunId id = WorkflowRunId.UnsafeFrom(ValidWorkflowRunId);

        id.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void PrefixValue_ShouldReturnPrefix()
    {
        WorkflowRunId.PrefixValue.Should().Be(Prefix);
    }

    [Fact]
    public void Length_ShouldReturnExpectedLength()
    {
        WorkflowRunId.Length.Should().Be(ExpectedLength);
    }

    [Fact]
    public void Equality_WithSameValue_ShouldBeEqual()
    {
        WorkflowRunId id1 = WorkflowRunId.UnsafeFrom(ValidWorkflowRunId);
        WorkflowRunId id2 = WorkflowRunId.UnsafeFrom(ValidWorkflowRunId);

        id1.Should().Be(id2);
    }

    [Fact]
    public void Equality_WithDifferentValue_ShouldNotBeEqual()
    {
        WorkflowRunId id1 = WorkflowRunId.UnsafeFrom("wfr_01JGX123456789012345678901");
        WorkflowRunId id2 = WorkflowRunId.UnsafeFrom("wfr_01JGX123456789012345678902");

        id1.Should().NotBe(id2);
    }

    [Fact]
    public void ValidId_ShouldHaveCorrectLength()
    {
        ValidWorkflowRunId.Length.Should().Be(ExpectedLength);
    }

    [Fact]
    public void ValidId_ShouldStartWithPrefix()
    {
        ValidWorkflowRunId.Should().StartWith(Prefix);
    }
}
