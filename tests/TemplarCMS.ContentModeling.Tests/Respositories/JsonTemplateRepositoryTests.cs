using Microsoft.Extensions.Options;
using NSubstitute;
using System.Text.Json;
using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Repositories;
using TemplarCMS.ContentModeling.Serialization;
using TemplarCMS.Domain.Content;
using Xunit;

namespace TemplarCMS.ContentModeling.Tests.Repositories;

public sealed class JsonTemplateRepositoryTests
{
    private readonly IJsonTemplateMapper _mapper =
        Substitute.For<IJsonTemplateMapper>();

    [Fact]
    public void Ctor_ShouldThrow_WhenOptionsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new JsonTemplateRepository(
                null!,
                _mapper));
    }

    [Fact]
    public void Ctor_ShouldThrow_WhenMapperNull()
    {
        using var directory = new TemporaryDirectory();

        var options =
            Options.Create(new JsonTemplateRepositoryOptions
            {
                TemplatesPath = directory.Path
            });

        Assert.Throws<ArgumentNullException>(() =>
            new JsonTemplateRepository(
                options,
                null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Ctor_ShouldThrow_WhenTemplatesPathMissing(
        string? templatesPath)
    {
        var options =
            Options.Create(new JsonTemplateRepositoryOptions
            {
                TemplatesPath = templatesPath!
            });

        Assert.Throws<ArgumentException>(() =>
            new JsonTemplateRepository(
                options,
                _mapper));
    }

    [Fact]
    public async Task GetTemplatesAsync_ShouldThrow_WhenDirectoryMissing()
    {
        var missingPath =
            Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString("N"));

        var repository =
            CreateRepository(missingPath);

        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            repository.GetTemplatesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetTemplatesAsync_ShouldReturnMappedTemplates()
    {
        using var directory = new TemporaryDirectory();

        CreateTemplateFile(
            directory.Path,
            "article-page.json",
            "Article Page",
            "article-page");

        var expectedTemplate =
            new TemplateDefinition(
                new TemplateId(Guid.NewGuid()),
                "Article Page",
                new TemplateKey("article-page"));

        _mapper
            .Map(Arg.Any<JsonTemplateDefinition>())
            .Returns(expectedTemplate);

        var repository =
            CreateRepository(directory.Path);

        var result =
            await repository.GetTemplatesAsync(TestContext.Current.CancellationToken);

        var template =
            Assert.Single(result);

        Assert.Same(expectedTemplate, template);

        _mapper
            .Received(1)
            .Map(Arg.Is<JsonTemplateDefinition>(
                dto =>
                    dto.Name == "Article Page" &&
                    dto.Key == "article-page"));
    }

    [Fact]
    public async Task GetTemplatesAsync_ShouldLoadTemplatesInDeterministicOrder()
    {
        using var directory = new TemporaryDirectory();

        CreateTemplateFile(
            directory.Path,
            "z-page.json",
            "Z Page",
            "z-page");

        CreateTemplateFile(
            directory.Path,
            "a-page.json",
            "A Page",
            "a-page");

        CreateTemplateFile(
            directory.Path,
            "m-page.json",
            "M Page",
            "m-page");

        var mappedTemplates =
            new List<TemplateDefinition>();

        _mapper
            .Map(Arg.Any<JsonTemplateDefinition>())
            .Returns(callInfo =>
            {
                var dto =
                    callInfo.Arg<JsonTemplateDefinition>();

                var template =
                    new TemplateDefinition(
                        new TemplateId(dto.Id),
                        dto.Name!,
                        new TemplateKey(dto.Key!));

                mappedTemplates.Add(template);

                return template;
            });

        var repository =
            CreateRepository(directory.Path);

        var result =
            await repository.GetTemplatesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(
            new[]
            {
                "a-page",
                "m-page",
                "z-page"
            },
            mappedTemplates.Select(template => template.Key.Value));

        Assert.Equal(
            new[]
            {
                "a-page",
                "m-page",
                "z-page"
            },
            result.Select(template => template.Key.Value));
    }

    [Fact]
    public async Task GetTemplatesAsync_ShouldThrow_WhenJsonIsInvalid()
    {
        using var directory = new TemporaryDirectory();

        File.WriteAllText(
            Path.Combine(directory.Path, "invalid.json"),
            """
            {
              "id":
            """);

        var repository =
            CreateRepository(directory.Path);

        await Assert.ThrowsAsync<JsonException>(() =>
            repository.GetTemplatesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetTemplatesAsync_ShouldThrow_WhenTemplateCannotBeDeserialized()
    {
        using var directory = new TemporaryDirectory();

        File.WriteAllText(
            Path.Combine(directory.Path, "empty.json"),
            "null");

        var repository =
            CreateRepository(directory.Path);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.GetTemplatesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetTemplatesAsync_ShouldRespectCancellationToken()
    {
        using var directory = new TemporaryDirectory();

        CreateTemplateFile(
            directory.Path,
            "article-page.json",
            "Article Page",
            "article-page");

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        var repository =
            CreateRepository(directory.Path);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            repository.GetTemplatesAsync(
                cancellationTokenSource.Token));
    }

    [Fact]
    public async Task CreateTemplateAsync_ShouldPersistTemplateAsJson()
    {
        using var directory = new TemporaryDirectory();
        var repository =
            CreateRepository(directory.Path);
        var template =
            new TemplateDefinition(
                new TemplateId(Guid.NewGuid()),
                "Article Page",
                new TemplateKey("article-page"),
                sections:
                [
                    new TemplateSectionDefinition(
                        Guid.NewGuid(),
                        "Content",
                        "content",
                        100,
                        [
                            new FieldDefinition(
                                new FieldId(Guid.NewGuid()),
                                "Title",
                                "title",
                                FieldType.SingleLineText,
                                isUnversioned: true,
                                metadata: new Dictionary<string, string>
                                {
                                    ["maxLength"] = "100"
                                })
                        ])
                ]);

        await repository.CreateTemplateAsync(
            template,
            TestContext.Current.CancellationToken);

        var templatePath =
            Path.Combine(
                directory.Path,
                "article-page.json");

        Assert.True(File.Exists(templatePath));

        var json =
            await File.ReadAllTextAsync(
                templatePath,
                TestContext.Current.CancellationToken);
        var dto =
            JsonSerializer.Deserialize<JsonTemplateDefinition>(
                json);

        Assert.NotNull(dto);
        Assert.Equal(template.Id.Value, dto.Id);
        Assert.Equal("Article Page", dto.Name);
        Assert.Equal("article-page", dto.Key);

        var section = Assert.Single(dto.Sections);
        Assert.Equal("content", section.Key);

        var field = Assert.Single(section.Fields);
        Assert.Equal("title", field.Key);
        Assert.Equal("SingleLineText", field.FieldType);
        Assert.True(field.IsUnversioned);
        Assert.Equal("100", field.Metadata["maxLength"]);
    }

    [Fact]
    public async Task CreateTemplateAsync_ShouldThrow_WhenTemplateKeyAlreadyExists()
    {
        using var directory = new TemporaryDirectory();

        CreateTemplateFile(
            directory.Path,
            "article-page.json",
            "Article Page",
            "article-page");

        _mapper
            .Map(Arg.Any<JsonTemplateDefinition>())
            .Returns(
                new TemplateDefinition(
                    new TemplateId(Guid.NewGuid()),
                    "Article Page",
                    new TemplateKey("article-page")));

        var repository =
            CreateRepository(directory.Path);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.CreateTemplateAsync(
                new TemplateDefinition(
                    new TemplateId(Guid.NewGuid()),
                    "Another Article",
                    new TemplateKey("article-page")),
                TestContext.Current.CancellationToken));
    }

    private JsonTemplateRepository CreateRepository(
        string templatesPath)
    {
        var options =
            Options.Create(new JsonTemplateRepositoryOptions
            {
                TemplatesPath = templatesPath
            });

        return new JsonTemplateRepository(
            options,
            _mapper);
    }

    private static void CreateTemplateFile(
        string directoryPath,
        string fileName,
        string name,
        string key)
    {
        var json =
            $$"""
            {
              "id": "{{Guid.NewGuid()}}",
              "name": "{{name}}",
              "key": "{{key}}",
              "sections": []
            }
            """;

        File.WriteAllText(
            Path.Combine(directoryPath, fileName),
            json);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path =
                System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(
                    Path,
                    recursive: true);
            }
        }
    }
}
