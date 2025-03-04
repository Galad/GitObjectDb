using AutoFixture;
using FakeItEasy;
using GitObjectDb.Comparison;
using GitObjectDb.Injection;
using GitObjectDb.Internal;
using GitObjectDb.Internal.Commands;
using GitObjectDb.Model;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Data.Software;
using GitObjectDb.Tests.Assets.Tools;
using LibGit2Sharp;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Models.Software;
using NUnit.Framework;
using System.IO;
using System.Linq;

namespace GitObjectDb.Tests.Commands;

[Parallelizable(ParallelScope.Self | ParallelScope.Children)]
public class CommitCommandTests
{
    [Test]
    [InlineAutoDataCustomizations(
        new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks) },
        CommitCommandType.Normal)]
    [InlineAutoDataCustomizations(
        new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks) },
        CommitCommandType.FastImport)]
    public void AddNewNodeUsingNodeFolders(CommitCommandType commitType, IFixture fixture, Application application, UniqueId newTableId, string message, Signature signature)
    {
        // Arrange
        var comparer = fixture.Create<Comparer>();
        var gitUpdateCommand = fixture.Create<ServiceResolver<CommitCommandType, IGitUpdateCommand>>();
        var sut = fixture.Create<ServiceResolver<CommitCommandType, ICommitCommand>>();
        var connection = fixture.Create<IConnectionInternal>();

        // Act
        var composer = new TransformationComposer(connection, "main", commitType, gitUpdateCommand, sut);
        composer.CreateOrUpdate(new Table { Id = newTableId }, application);
        sut.Invoke(commitType).Commit(composer, new(message, signature, signature));

        // Assert
        var changes = comparer.Compare(connection,
                                       connection.Repository.Lookup<Commit>("main~1"),
                                       connection.Repository.Head.Tip,
                                       connection.Model.DefaultComparisonPolicy);
        Assert.That(changes, Has.Count.EqualTo(1));
        var expectedPath = $"{application.Path.FolderPath}/Pages/{newTableId}/{newTableId}.json";
        Assert.That(changes.Added.Single().New.Path.FilePath, Is.EqualTo(expectedPath));
    }

    [Test]
    [InlineAutoDataCustomizations(
        new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks) },
        CommitCommandType.Normal)]
    [InlineAutoDataCustomizations(
        new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks) },
        CommitCommandType.FastImport)]
    public void AddNewNodeWithoutNodeFolders(CommitCommandType commitType, IFixture fixture, Table table, UniqueId newFieldId, string message, Signature signature)
    {
        // Arrange
        var comparer = fixture.Create<Comparer>();
        var gitUpdateCommand = fixture.Create<ServiceResolver<CommitCommandType, IGitUpdateCommand>>();
        var sut = fixture.Create<ServiceResolver<CommitCommandType, ICommitCommand>>();
        var connection = fixture.Create<IConnectionInternal>();

        // Act
        var composer = new TransformationComposer(connection, "main", commitType, gitUpdateCommand, sut);
        composer.CreateOrUpdate(new Field { Id = newFieldId }, table);
        sut.Invoke(commitType).Commit(composer, new(message, signature, signature));

        // Assert
        var changes = comparer.Compare(connection,
                                       connection.Repository.Lookup<Commit>("main~1"),
                                       connection.Repository.Head.Tip,
                                       connection.Model.DefaultComparisonPolicy);
        Assert.That(changes, Has.Count.EqualTo(1));
        var expectedPath = $"{table.Path.FolderPath}/Fields/{newFieldId}.json";
        Assert.That(changes.Added.Single().New.Path.FilePath, Is.EqualTo(expectedPath));
    }

    [Test]
    [InlineAutoDataCustomizations(
        new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks) },
        CommitCommandType.Normal)]
    [InlineAutoDataCustomizations(
        new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks) },
        CommitCommandType.FastImport)]
    public void AddNewResource(CommitCommandType commitType, IFixture fixture, Table table, string fileContent, string message, Signature signature)
    {
        // Arrange
        var comparer = fixture.Create<Comparer>();
        var gitUpdateCommand = fixture.Create<ServiceResolver<CommitCommandType, IGitUpdateCommand>>();
        var sut = fixture.Create<ServiceResolver<CommitCommandType, ICommitCommand>>();
        var connection = fixture.Create<IConnectionInternal>();
        var resource = new Resource(table, "Some/Folder", "File.txt", new Resource.Data(fileContent));

        // Act
        var composer = new TransformationComposer(connection, "main", commitType, gitUpdateCommand, sut);
        composer.CreateOrUpdate(resource);
        sut.Invoke(commitType).Commit(composer, new(message, signature, signature));

        // Assert
        var changes = comparer.Compare(connection,
                                       connection.Repository.Lookup<Commit>("main~1"),
                                       connection.Repository.Head.Tip,
                                       connection.Model.DefaultComparisonPolicy);
        Assert.That(changes, Has.Count.EqualTo(1));
        var expectedPath = $"{table.Path.FolderPath}/{FileSystemStorage.ResourceFolder}/Some/Folder/File.txt";
        Assert.That(changes.Added.Single().New.Path.FilePath, Is.EqualTo(expectedPath));
        var loaded = (Resource)changes.Added.Single().New;
        Assert.That(loaded.Embedded.ReadAsString(), Is.EqualTo(fileContent));
    }

    [Test]
    [InlineAutoDataCustomizations(
        new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks) },
        CommitCommandType.Normal)]
    [InlineAutoDataCustomizations(
        new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks) },
        CommitCommandType.FastImport)]
    public void DeletingNodeRemovesNestedChildren(CommitCommandType commitType, IFixture fixture, Table table, string message, Signature signature)
    {
        // Arrange
        var comparer = fixture.Create<Comparer>();
        var gitUpdateCommand = fixture.Create<ServiceResolver<CommitCommandType, IGitUpdateCommand>>();
        var sut = fixture.Create<ServiceResolver<CommitCommandType, ICommitCommand>>();
        var connection = fixture.Create<IConnectionInternal>();

        // Act
        var composer = new TransformationComposer(connection, "main", commitType, gitUpdateCommand, sut);
        composer.Delete(table);
        sut.Invoke(commitType).Commit(composer, new(message, signature, signature));

        // Assert
        var changes = comparer.Compare(connection,
                                       connection.Repository.Lookup<Commit>("main~1"),
                                       connection.Repository.Head.Tip,
                                       connection.Model.DefaultComparisonPolicy);
        Assert.That(changes, Has.Count.GreaterThan(1));
    }

    [Test]
    [InlineAutoDataCustomizations(
        new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks) },
        CommitCommandType.Normal)]
    [InlineAutoDataCustomizations(
        new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks) },
        CommitCommandType.FastImport)]
    public void RenamingNonGitFoldersIsSupported(CommitCommandType commitType, IFixture fixture, Field field, string message, Signature signature)
    {
        // Arrange
        var comparer = fixture.Create<Comparer>();
        var gitUpdateCommand = fixture.Create<ServiceResolver<CommitCommandType, IGitUpdateCommand>>();
        var sut = fixture.Create<ServiceResolver<CommitCommandType, ICommitCommand>>();
        var connection = fixture.Create<IConnectionInternal>();

        // Act
        var composer = new TransformationComposer(connection, "main", commitType, gitUpdateCommand, sut);
        var newPath = new DataPath(field.Path.FolderPath,
                                   $"someName{Path.GetExtension(field.Path.FileName)}",
                                   field.Path.UseNodeFolders);
        composer.Rename(field, newPath);
        sut.Invoke(commitType).Commit(composer, new(message, signature, signature));

        // Assert
        var changes = comparer.Compare(connection,
                                       connection.Repository.Lookup<Commit>("main~1"),
                                       connection.Repository.Head.Tip,
                                       connection.Model.DefaultComparisonPolicy);
        Assert.That(changes, Has.Count.EqualTo(1));
    }

    [Test]
    [InlineAutoDataCustomizations(
        new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks) },
        CommitCommandType.Normal)]
    [InlineAutoDataCustomizations(
        new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks) },
        CommitCommandType.FastImport)]
    public void RenamingGitFoldersIsNotSupported(CommitCommandType commitType, IFixture fixture, Table table, string message, Signature signature)
    {
        // Arrange
        var comparer = fixture.Create<Comparer>();
        var gitUpdateCommand = fixture.Create<ServiceResolver<CommitCommandType, IGitUpdateCommand>>();
        var sut = fixture.Create<ServiceResolver<CommitCommandType, ICommitCommand>>();
        var connection = fixture.Create<IConnectionInternal>();

        // Act
        var composer = new TransformationComposer(connection, "main", commitType, gitUpdateCommand, sut);
        var newPath = new DataPath(table.Path.FolderPath,
                                   $"someName{Path.GetExtension(table.Path.FileName)}",
                                   table.Path.UseNodeFolders);
        Assert.Throws<GitObjectDbException>(() => composer.Rename(table, newPath));
    }

    [Test]
    [InlineAutoDataCustomizations(
        new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks) },
        CommitCommandType.Normal)]
    [InlineAutoDataCustomizations(
        new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks) },
        CommitCommandType.FastImport)]
    public void EditNestedProperty(CommitCommandType commitType, IFixture fixture, Field field, string message, Signature signature)
    {
        // Arrange
        var comparer = fixture.Create<Comparer>();
        var gitUpdateCommand = fixture.Create<ServiceResolver<CommitCommandType, IGitUpdateCommand>>();
        var sut = fixture.Create<ServiceResolver<CommitCommandType, ICommitCommand>>();
        var connection = fixture.Create<IConnectionInternal>();

        // Act
        var composer = new TransformationComposer(connection, "main", commitType, gitUpdateCommand, sut);
        composer.CreateOrUpdate(field with
        {
            SomeValue = new()
            {
                B = new()
                {
                    IsVisible = !field.SomeValue.B.IsVisible,
                },
            },
        });
        sut.Invoke(commitType).Commit(composer, new(message, signature, signature));

        // Act
        var changes = comparer.Compare(connection,
                                       connection.Repository.Lookup<Commit>("main~1"),
                                       connection.Repository.Head.Tip,
                                       connection.Model.DefaultComparisonPolicy);
        Assert.That(changes, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(changes.Modified.OfType<Change.NodeChange>().Single().Differences, Has.Count.EqualTo(1));
            Assert.That(changes.Added, Is.Empty);
            Assert.That(changes.Deleted, Is.Empty);
        });
    }

    private class InternalMocks : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Inject<IMemoryCache>(new MemoryCache(Options.Create(new MemoryCacheOptions())));

            var connection = A.Fake<IConnectionInternal>(x => x.Strict());
            A.CallTo(() => connection.Repository).Returns(fixture.Create<IRepository>());
            A.CallTo(() => connection.Model).Returns(fixture.Create<IDataModel>());
            A.CallTo(() => connection.Serializer).Returns(fixture.Create<INodeSerializer>());
            A.CallTo(() => connection.Cache).Returns(fixture.Create<IMemoryCache>());
            fixture.Inject(connection);

            var validation = A.Fake<ITreeValidation>(x => x.Strict());
            A.CallTo(validation).WithVoidReturnType().DoesNothing();
            fixture.Inject(validation);

            fixture.Inject(new CommitCommandUsingTree(() => validation));
        }
    }
}
