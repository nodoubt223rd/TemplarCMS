using Microsoft.Extensions.Options;
using NSubstitute;
using System.Text.Json;
using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Repositories;
using TemplarCMS.ContentModeling.Serialization;
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
            repository.GetTemplatesAsync());
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
                Guid.NewGuid(),
                "Article Page",
                "article-page");

        _mapper
            .Map(Arg.Any<JsonTemplateDefinition>())
            .Returns(expectedTemplate);

        var repository =
            CreateRepository(directory.Path);

        var result =
            await repository.GetTemplatesAsync();

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
                        dto.Id,
                        dto.Name!,
                        dto.Key!);

                mappedTemplates.Add(template);

                return template;
            });

        var repository =
            CreateRepository(directory.Path);

        var result =
            await repository.GetTemplatesAsync();

        Assert.Equal(
            new[]
            {
                "a-page",
                "m-page",
                "z-page"
            },
            mappedTemplates.Select(template => template.Key));

        Assert.Equal(
            new[]
            {
                "a-page",
                "m-page",
                "z-page"
            },
            result.Select(template => template.Key));
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
            repository.GetTemplatesAsync());
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
            repository.GetTemplatesAsync());
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
