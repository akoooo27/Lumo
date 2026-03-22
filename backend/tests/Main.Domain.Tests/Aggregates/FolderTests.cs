using FluentAssertions;

using Main.Domain.Aggregates;
using Main.Domain.Constants;
using Main.Domain.Faults;
using Main.Domain.ValueObjects;

using SharedKernel;

namespace Main.Domain.Tests.Aggregates;

public sealed class FolderTests
{
    private static readonly DateTimeOffset UtcNow = DateTimeOffset.UtcNow;
    private static readonly Guid ValidUserId = Guid.NewGuid();
    private static readonly FolderId ValidFolderId = FolderId.UnsafeFrom("fld_01JGX123456789012345678901");
    private const string ValidName = "My Folder";
    private const int ValidSortOrder = 0;

    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        Outcome<Folder> outcome = Folder.Create(ValidFolderId, ValidUserId, ValidName, ValidSortOrder, UtcNow);

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value.Id.Should().Be(ValidFolderId);
        outcome.Value.UserId.Should().Be(ValidUserId);
        outcome.Value.Name.Should().Be(ValidName);
        outcome.Value.NormalizedName.Should().Be(ValidName.Trim().ToUpperInvariant());
        outcome.Value.SortOrder.Should().Be(ValidSortOrder);
        outcome.Value.CreatedAt.Should().Be(UtcNow);
        outcome.Value.UpdatedAt.Should().Be(UtcNow);
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldReturnFailure()
    {
        Outcome<Folder> outcome = Folder.Create(ValidFolderId, Guid.Empty, ValidName, ValidSortOrder, UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(FolderFaults.UserIdRequired);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithEmptyOrWhitespaceName_ShouldReturnFailure(string? name)
    {
        Outcome<Folder> outcome = Folder.Create(ValidFolderId, ValidUserId, name!, ValidSortOrder, UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(FolderFaults.NameRequired);
    }

    [Fact]
    public void Create_WithNameTooLong_ShouldReturnFailure()
    {
        string longName = new('A', FolderConstants.MaxNameLength + 1);

        Outcome<Folder> outcome = Folder.Create(ValidFolderId, ValidUserId, longName, ValidSortOrder, UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(FolderFaults.NameTooLong);
    }

    [Fact]
    public void Create_WithMaxLengthName_ShouldReturnSuccess()
    {
        string maxName = new('A', FolderConstants.MaxNameLength);

        Outcome<Folder> outcome = Folder.Create(ValidFolderId, ValidUserId, maxName, ValidSortOrder, UtcNow);

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value.Name.Should().Be(maxName);
    }

    [Fact]
    public void Create_WithNegativeSortOrder_ShouldReturnFailure()
    {
        Outcome<Folder> outcome = Folder.Create(ValidFolderId, ValidUserId, ValidName, -1, UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(FolderFaults.InvalidSortOrder);
    }

    [Fact]
    public void Create_ShouldTrimName()
    {
        Outcome<Folder> outcome = Folder.Create(ValidFolderId, ValidUserId, "  My Folder  ", ValidSortOrder, UtcNow);

        outcome.IsSuccess.Should().BeTrue();
        outcome.Value.Name.Should().Be("My Folder");
        outcome.Value.NormalizedName.Should().Be("MY FOLDER");
    }

    [Fact]
    public void Rename_WithValidName_ShouldUpdateNameAndTimestamp()
    {
        Folder folder = Folder.Create(ValidFolderId, ValidUserId, ValidName, ValidSortOrder, UtcNow).Value;

        DateTimeOffset renameTime = UtcNow.AddMinutes(5);

        Outcome outcome = folder.Rename("New Name", renameTime);

        outcome.IsSuccess.Should().BeTrue();
        folder.Name.Should().Be("New Name");
        folder.NormalizedName.Should().Be("NEW NAME");
        folder.UpdatedAt.Should().Be(renameTime);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Rename_WithEmptyOrWhitespaceName_ShouldReturnFailure(string? name)
    {
        Folder folder = Folder.Create(ValidFolderId, ValidUserId, ValidName, ValidSortOrder, UtcNow).Value;

        Outcome outcome = folder.Rename(name!, UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(FolderFaults.NameRequired);
    }

    [Fact]
    public void Rename_WithNameTooLong_ShouldReturnFailure()
    {
        Folder folder = Folder.Create(ValidFolderId, ValidUserId, ValidName, ValidSortOrder, UtcNow).Value;
        string longName = new('A', FolderConstants.MaxNameLength + 1);

        Outcome outcome = folder.Rename(longName, UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(FolderFaults.NameTooLong);
    }

    [Fact]
    public void SetSortOrder_WithValidValue_ShouldUpdateSortOrderAndTimestamp()
    {
        Folder folder = Folder.Create(ValidFolderId, ValidUserId, ValidName, ValidSortOrder, UtcNow).Value;

        DateTimeOffset updateTime = UtcNow.AddMinutes(5);

        Outcome outcome = folder.SetSortOrder(5, updateTime);

        outcome.IsSuccess.Should().BeTrue();
        folder.SortOrder.Should().Be(5);
        folder.UpdatedAt.Should().Be(updateTime);
    }

    [Fact]
    public void SetSortOrder_WithNegativeValue_ShouldReturnFailure()
    {
        Folder folder = Folder.Create(ValidFolderId, ValidUserId, ValidName, ValidSortOrder, UtcNow).Value;

        Outcome outcome = folder.SetSortOrder(-1, UtcNow);

        outcome.IsFailure.Should().BeTrue();
        outcome.Fault.Should().Be(FolderFaults.InvalidSortOrder);
    }
}