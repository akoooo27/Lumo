using FluentAssertions;

using Main.Domain.ValueObjects;

using SharedKernel;

namespace Main.Domain.Tests.ValueObjects;

public sealed class WorkflowIdTests
{
    private const string ValidWorkflowId = "wfl_01JGX123456789012345678901";
    private const string Prefix = "wfl_";
    private const int ExpectedLength = 30;

    [Fact]
    public void From_WithValidId_ShouldReturnSuccess()
    {
        Outcome<WorkflowId> outcome = WorkflowId.From(ValidWorkflowId);

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value.Value.Should().Be(ValidWorkflowId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void From_WithEmptyOrWhitespace_ShouldReturnFailure(string? value)
    {
        Outcome<WorkflowId> outcome = WorkflowId.From(value);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Title.Should().Be("WorkflowId.Required");
    }

    [Theory]
    [InlineData("invalid_id")]
    [InlineData("wfl_short")]
    [InlineData("wfl_01JGX12345678901234567890")] // too short
    [InlineData("wfl_01JGX1234567890123456789012")] // too long
    [InlineData("xxx_01JGX123456789012345678901")] // wrong prefix
    public void From_WithInvalidFormat_ShouldReturnFailure(string value)
    {
        Outcome<WorkflowId> outcome = WorkflowId.From(value);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Title.Should().Be("WorkflowId.InvalidFormat");
    }

    [Fact]
    public void UnsafeFrom_ShouldCreateWithoutValidation()
    {
        WorkflowId result = WorkflowId.UnsafeFrom("invalid");

        result.Value.Should().Be("invalid");
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        WorkflowId id = WorkflowId.UnsafeFrom(ValidWorkflowId);

        string result = id.ToString();

        result.Should().Be(ValidWorkflowId);
    }

    [Fact]
    public void IsEmpty_WhenEmpty_ShouldReturnTrue()
    {
        WorkflowId id = WorkflowId.UnsafeFrom(string.Empty);

        id.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void IsEmpty_WhenNotEmpty_ShouldReturnFalse()
    {
        WorkflowId id = WorkflowId.UnsafeFrom(ValidWorkflowId);

        id.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void PrefixValue_ShouldReturnPrefix()
    {
        WorkflowId.PrefixValue.Should().Be(Prefix);
    }

    [Fact]
    public void Length_ShouldReturnExpectedLength()
    {
        WorkflowId.Length.Should().Be(ExpectedLength);
    }

    [Fact]
    public void Equality_WithSameValue_ShouldBeEqual()
    {
        WorkflowId id1 = WorkflowId.UnsafeFrom(ValidWorkflowId);
        WorkflowId id2 = WorkflowId.UnsafeFrom(ValidWorkflowId);

        id1.Should().Be(id2);
    }

    [Fact]
    public void Equality_WithDifferentValue_ShouldNotBeEqual()
    {
        WorkflowId id1 = WorkflowId.UnsafeFrom("wfl_01JGX123456789012345678901");
        WorkflowId id2 = WorkflowId.UnsafeFrom("wfl_01JGX123456789012345678902");

        id1.Should().NotBe(id2);
    }

    [Fact]
    public void ValidId_ShouldHaveCorrectLength()
    {
        ValidWorkflowId.Length.Should().Be(ExpectedLength);
    }

    [Fact]
    public void ValidId_ShouldStartWithPrefix()
    {
        ValidWorkflowId.Should().StartWith(Prefix);
    }
}
