using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TemplarCMS.Application.Content;
using TemplarCMS.Abstractions.Content;
using TemplarCMS.Api.Templates;
using TemplarCMS.ContentModeling.Abstractions;
using TemplarCMS.ContentModeling.Catalog;
using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Repositories;
using TemplarCMS.ContentModeling.Validation;
using TemplarCMS.Domain.Content;
using Xunit;

namespace TemplarCMS.Api.Tests.Templates;

public sealed class TemplateEndpointsTests
{
    [Fact]
    public async Task GetFieldTypesAsync_ShouldReturnCSharpDefinedFieldMetadata()
    {
        var result = await FieldTypeEndpoints.GetAllAsync();

        var ok = Assert.IsType<Ok<FieldTypeCollectionResponse>>(result);
        Assert.NotNull(ok.Value);

        var fieldTypes = ok.Value.Embedded.FieldTypes.ToArray();
        Assert.NotEmpty(fieldTypes);

        var generalLink = Assert.Single(fieldTypes, fieldType => fieldType.Value == "GeneralLink");
        Assert.Equal("General Link", generalLink.Label);
        Assert.Equal("general-link", generalLink.EditorKind);
        Assert.Equal("text", generalLink.InputType);
        Assert.Equal("/api/v1/field-types", ok.Value.Links.Self.Href);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnTemplatesInStableOrder()
    {
        var article =
            CreateTemplate(
                "Article Page",
                "article-page");
        var landing =
            CreateTemplate(
                "Landing Page",
                "landing-page");
        var catalog =
            new FakeContentModelCatalog(
                article,
                landing);

        var result =
            await TemplateEndpoints.GetAllAsync(
                catalog,
                TestContext.Current.CancellationToken);

        Assert.NotNull(result.Value);

        var templates = result.Value.Embedded.Templates.ToArray();
        Assert.Equal(2, templates.Length);
        Assert.Equal("article-page", templates[0].Key);
        Assert.Equal("landing-page", templates[1].Key);
        Assert.Equal("/api/v1/templates", result.Value.Links.Self.Href);
        Assert.Equal($"/api/v1/templates/{article.Id.Value}", templates[0].Links.Self.Href);
        Assert.Equal($"/api/v1/templates/{article.Id.Value}/fields", templates[0].Links.Fields.Href);
        Assert.Equal($"/api/v1/templates/{article.Id.Value}/dependencies", templates[0].Links.Dependencies.Href);
        Assert.Equal("/api/v1/content", templates[0].Links.CreateItem.Href);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnOk_WhenTemplateExists()
    {
        var field =
            new FieldDefinition(
                new FieldId(Guid.NewGuid()),
                "Title",
                "title",
                FieldType.SingleLineText,
                isUnversioned: true);
        var section =
            new TemplateSectionDefinition(
                Guid.NewGuid(),
                "Content",
                "content",
                100,
                [field]);
        var authoredTemplate =
            new TemplateDefinition(
                new TemplateId(Guid.NewGuid()),
                "Article Page",
                new TemplateKey("article-page"),
                null,
                [section]);
        var template =
            new EffectiveTemplateDefinition(
                authoredTemplate.Id,
                authoredTemplate.Name,
                authoredTemplate.Key,
                authoredTemplate.Sections.ToArray());
        var catalog =
            new FakeContentModelCatalog(
                [authoredTemplate],
                template);

        var result =
            await TemplateEndpoints.GetByIdAsync(
                template.Id.Value,
                catalog,
                TestContext.Current.CancellationToken);

        var ok = Assert.IsType<Ok<TemplateResponse>>(result.Result);
        var response = Assert.IsType<TemplateResponse>(ok.Value);

        Assert.Equal(template.Id.Value.ToString(), response.Id);
        Assert.Equal(template.Name, response.Name);
        Assert.Equal(template.Key.ToString(), response.Key);
        Assert.Single(response.Sections);
        Assert.Equal($"/api/v1/templates/{template.Id.Value}", response.Links.Self.Href);
        Assert.Equal($"/api/v1/templates/{template.Id.Value}/fields", response.Links.Fields.Href);
        Assert.Equal($"/api/v1/templates/{template.Id.Value}/dependencies", response.Links.Dependencies.Href);
        Assert.Equal("/api/v1/content", response.Links.CreateItem.Href);
        Assert.Null(response.BaseTemplate);
        Assert.Null(catalog.LastRequestedTemplateId);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnBaseTemplate_WhenTemplateInheritsFromAnotherTemplate()
    {
        var baseTemplate =
            CreateAuthoredTemplate(
                "Base Page",
                "base-page");
        var childTemplate =
            CreateAuthoredTemplate(
                "Article Page",
                "article-page",
                baseTemplate);
        var catalog =
            new FakeContentModelCatalog(
                [baseTemplate, childTemplate]);

        var result =
            await TemplateEndpoints.GetByIdAsync(
                childTemplate.Id.Value,
                catalog,
                TestContext.Current.CancellationToken);

        var ok = Assert.IsType<Ok<TemplateResponse>>(result.Result);
        var response = Assert.IsType<TemplateResponse>(ok.Value);
        var responseBaseTemplate = Assert.IsType<TemplateBaseTemplateResponse>(response.BaseTemplate);
        Assert.Equal(baseTemplate.Id.Value.ToString(), responseBaseTemplate.Id);
        Assert.Equal(baseTemplate.Name, responseBaseTemplate.Name);
        Assert.Equal(baseTemplate.Key.ToString(), responseBaseTemplate.Key);
        Assert.Equal($"/api/v1/templates/{baseTemplate.Id.Value}", responseBaseTemplate.Links.Self.Href);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnProblem_WhenTemplateMissing()
    {
        var result =
            await TemplateEndpoints.GetByIdAsync(
                Guid.NewGuid(),
                new FakeContentModelCatalog(),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnProblem_WhenIdIsInvalid()
    {
        var result =
            await TemplateEndpoints.GetByIdAsync(
                Guid.Empty,
                new FakeContentModelCatalog(),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnCreated_WhenRequestIsValid()
    {
        var catalog =
            new FakeContentModelCatalog();
        var repository =
            new FakeTemplateRepository();
        repository.OnCreateTemplateAsync = template =>
        {
            catalog.AddAuthoredTemplate(template);
            catalog.AddTemplate(
                new EffectiveTemplateDefinition(
                    template.Id,
                    template.Name,
                    template.Key,
                    template.Sections.ToArray()));

            return Task.CompletedTask;
        };

        var result =
            await TemplateEndpoints.CreateAsync(
                new CreateTemplateRequest
                {
                    Name = "Article Page",
                    Key = "article-page",
                    Sections =
                    [
                        new CreateTemplateSectionRequest
                        {
                            Name = "Content",
                            Key = "content",
                            SortOrder = 100,
                            Fields =
                            [
                                new CreateTemplateFieldRequest
                                {
                                    Name = "Title",
                                    Key = "title",
                                    Type = "singleLineText",
                                    IsUnversioned = true
                                }
                            ]
                        }
                    ]
                },
                repository,
                catalog,
                TestContext.Current.CancellationToken);

        var created = Assert.IsType<Created<TemplateResponse>>(result.Result);
        Assert.NotNull(created.Value);
        Assert.NotNull(repository.LastCreatedTemplate);
        Assert.Equal("Article Page", repository.LastCreatedTemplate.Name);
        Assert.Equal("article-page", repository.LastCreatedTemplate.Key.ToString());
        Assert.Equal($"/api/v1/templates/{repository.LastCreatedTemplate.Id.Value}", created.Location);
        Assert.Equal("article-page", created.Value.Key);
        Assert.Single(created.Value.Sections);
    }

    [Fact]
    public async Task CreateAsync_ShouldMapSupportedAliasFieldTypes()
    {
        var catalog =
            new FakeContentModelCatalog();
        var repository =
            new FakeTemplateRepository();
        repository.OnCreateTemplateAsync = template =>
        {
            catalog.AddAuthoredTemplate(template);
            catalog.AddTemplate(
                new EffectiveTemplateDefinition(
                    template.Id,
                    template.Name,
                    template.Key,
                    template.Sections.ToArray()));

            return Task.CompletedTask;
        };

        var result =
            await TemplateEndpoints.CreateAsync(
                new CreateTemplateRequest
                {
                    Name = "Search Page",
                    Key = "search-page",
                    Sections =
                    [
                        new CreateTemplateSectionRequest
                        {
                            Name = "Settings",
                            Key = "settings",
                            Fields =
                            [
                                new CreateTemplateFieldRequest
                                {
                                    Name = "Tags",
                                    Key = "tags",
                                    Type = "TreelistEx"
                                },
                                new CreateTemplateFieldRequest
                                {
                                    Name = "Source",
                                    Key = "source",
                                    Type = "DropTree"
                                }
                            ]
                        }
                    ]
                },
                repository,
                catalog,
                TestContext.Current.CancellationToken);

        var created = Assert.IsType<Created<TemplateResponse>>(result.Result);
        Assert.NotNull(created.Value);
        Assert.NotNull(repository.LastCreatedTemplate);

        var createdSection = Assert.Single(repository.LastCreatedTemplate.Sections);
        var createdFields = createdSection.Fields.ToArray();
        Assert.Equal(FieldType.Multilist, createdFields[0].FieldType);
        Assert.Equal(FieldType.Droplink, createdFields[1].FieldType);
    }

    [Fact]
    public async Task CreateAsync_ShouldSupportGeneralLinkFieldType()
    {
        var catalog =
            new FakeContentModelCatalog();
        var repository =
            new FakeTemplateRepository();
        repository.OnCreateTemplateAsync = template =>
        {
            catalog.AddAuthoredTemplate(template);
            catalog.AddTemplate(
                new EffectiveTemplateDefinition(
                    template.Id,
                    template.Name,
                    template.Key,
                    template.Sections.ToArray()));

            return Task.CompletedTask;
        };

        var result =
            await TemplateEndpoints.CreateAsync(
                new CreateTemplateRequest
                {
                    Name = "Article Page",
                    Key = "article-page",
                    Sections =
                    [
                        new CreateTemplateSectionRequest
                        {
                            Name = "Help",
                            Key = "help",
                            Fields =
                            [
                                new CreateTemplateFieldRequest
                                {
                                    Name = "Help Link",
                                    Key = "help-link",
                                    Type = "General Link"
                                }
                            ]
                        }
                    ]
                },
                repository,
                catalog,
                TestContext.Current.CancellationToken);

        var created = Assert.IsType<Created<TemplateResponse>>(result.Result);
        Assert.NotNull(created.Value);
        Assert.NotNull(repository.LastCreatedTemplate);

        var createdSection = Assert.Single(repository.LastCreatedTemplate.Sections);
        var createdField = Assert.Single(createdSection.Fields);
        Assert.Equal(FieldType.GeneralLink, createdField.FieldType);
    }

    [Fact]
    public async Task CreateAsync_ShouldAssignBaseTemplate_WhenBaseTemplateKeyIsProvided()
    {
        var baseTemplate =
            CreateAuthoredTemplate(
                "Base Page",
                "base-page");
        var catalog =
            new FakeContentModelCatalog(
                authoredTemplates: [baseTemplate]);
        var repository =
            new FakeTemplateRepository();
        repository.OnCreateTemplateAsync = template =>
        {
            catalog.AddAuthoredTemplate(template);
            catalog.AddTemplate(
                new EffectiveTemplateDefinition(
                    template.Id,
                    template.Name,
                    template.Key,
                    template.Sections.ToArray()));

            return Task.CompletedTask;
        };

        var result =
            await TemplateEndpoints.CreateAsync(
                new CreateTemplateRequest
                {
                    Name = "Article Page",
                    Key = "article-page",
                    BaseTemplateKeys = ["base-page"],
                    Sections = []
                },
                repository,
                catalog,
                TestContext.Current.CancellationToken);

        var created = Assert.IsType<Created<TemplateResponse>>(result.Result);
        Assert.NotNull(created.Value);
        Assert.NotNull(repository.LastCreatedTemplate);
        Assert.NotNull(repository.LastCreatedTemplate.BaseTemplate);
        Assert.Equal(new TemplateKey("base-page"), repository.LastCreatedTemplate.BaseTemplate.Key);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnProblem_WhenMultipleBaseTemplateKeysAreProvided()
    {
        var result =
            await TemplateEndpoints.CreateAsync(
                new CreateTemplateRequest
                {
                    Name = "Article Page",
                    Key = "article-page",
                    BaseTemplateKeys =
                    [
                        "base-page",
                        "metadata"
                    ],
                    Sections = []
                },
                new FakeTemplateRepository(),
                new FakeContentModelCatalog(),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnConflict_WhenTemplateKeyAlreadyExists()
    {
        var repository =
            new FakeTemplateRepository
            {
                OnCreateTemplateAsync = _ => throw new InvalidOperationException(
                    "Template key 'article-page' already exists.")
            };

        var result =
            await TemplateEndpoints.CreateAsync(
                new CreateTemplateRequest
                {
                    Name = "Article Page",
                    Key = "article-page",
                    Sections = []
                },
                repository,
                new FakeContentModelCatalog(),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnProblem_WhenBaseTemplateIsMissing()
    {
        var result =
            await TemplateEndpoints.CreateAsync(
                new CreateTemplateRequest
                {
                    Name = "Article Page",
                    Key = "article-page",
                    BaseTemplateKeys = ["missing-base"],
                    Sections = []
                },
                new FakeTemplateRepository(),
                new FakeContentModelCatalog(),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnProblemWithHint_WhenFieldTypeNeedsDedicatedSupport()
    {
        var result =
            await TemplateEndpoints.CreateAsync(
                new CreateTemplateRequest
                {
                    Name = "Article Page",
                    Key = "article-page",
                    Sections =
                    [
                        new CreateTemplateSectionRequest
                        {
                            Name = "Content",
                            Key = "content",
                            Fields =
                            [
                                new CreateTemplateFieldRequest
                                {
                                    Name = "Help Link",
                                    Key = "help-link",
                                    Type = "Version Link"
                                }
                            ]
                        }
                    ]
                },
                new FakeTemplateRepository(),
                new FakeContentModelCatalog(),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        var value = Assert.IsType<ProblemDetails>(problem.ProblemDetails);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.NotNull(value.Detail);
        Assert.Contains("version-aware link field type", value.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_ShouldRollback_WhenCatalogRefreshFails()
    {
        var catalog =
            new FakeContentModelCatalog
            {
                RefreshException = new ContentModelCatalogRefreshException(
                    [
                        new ValidationError(
                            "DuplicateFieldKeyInTemplate",
                            "Template 'article-page' contains multiple fields with key 'title'.",
                            "title")
                    ])
            };
        var repository =
            new FakeTemplateRepository();

        var result =
            await TemplateEndpoints.CreateAsync(
                new CreateTemplateRequest
                {
                    Name = "Article Page",
                    Key = "article-page",
                    Sections =
                    [
                        new CreateTemplateSectionRequest
                        {
                            Name = "Content",
                            Key = "content",
                            Fields =
                            [
                                new CreateTemplateFieldRequest
                                {
                                    Name = "Title",
                                    Key = "title",
                                    Type = "singleLineText"
                                }
                            ]
                        }
                    ]
                },
                repository,
                catalog,
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Equal(new TemplateKey("article-page"), repository.LastDeletedTemplateKey);
        Assert.Equal(2, catalog.RefreshCallCount);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnProblem_WhenRequestIsMissing()
    {
        var result =
            await TemplateEndpoints.CreateAsync(
                null,
                new FakeTemplateRepository(),
                new FakeContentModelCatalog(),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnProblem_WhenRequestIsInvalid()
    {
        var result =
            await TemplateEndpoints.CreateAsync(
                new CreateTemplateRequest
                {
                    Name = "Article Page",
                    Key = " ",
                    Sections = []
                },
                new FakeTemplateRepository(),
                new FakeContentModelCatalog(),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnNoContent_WhenTemplateExists()
    {
        var template =
            CreateAuthoredTemplate(
                "Article Page",
                "article-page");
        var repository =
            new FakeTemplateRepository();

        var result =
            await TemplateEndpoints.DeleteAsync(
                template.Id.Value,
                repository,
                new FakeContentModelCatalog(
                    authoredTemplates: [template]),
                new FakeContentRepository(),
                TestContext.Current.CancellationToken);

        Assert.IsType<NoContent>(result.Result);
        Assert.Equal(new TemplateKey("article-page"), repository.LastDeletedTemplateKey);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnProblem_WhenTemplateMissing()
    {
        var result =
            await TemplateEndpoints.DeleteAsync(
                Guid.NewGuid(),
                new FakeTemplateRepository(),
                new FakeContentModelCatalog(),
                new FakeContentRepository(),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnOk_WhenTemplateExists()
    {
        var existingTemplate =
            CreateAuthoredTemplate(
                "Article Page",
                "article-page");
        var catalog =
            new FakeContentModelCatalog(
                authoredTemplates: [existingTemplate]);
        var repository =
            new FakeTemplateRepository();
        repository.OnUpdateTemplateAsync = (_, template) =>
        {
            catalog.AddAuthoredTemplate(template);
            catalog.AddTemplate(
                new EffectiveTemplateDefinition(
                    template.Id,
                    template.Name,
                    template.Key,
                    template.Sections.ToArray()));

            return Task.CompletedTask;
        };

        var result =
            await TemplateEndpoints.UpdateAsync(
                existingTemplate.Id.Value,
                new CreateTemplateRequest
                {
                    Name = "Landing Page",
                    Key = "landing-page",
                    Sections = []
                },
                repository,
                catalog,
                TestContext.Current.CancellationToken);

        var ok = Assert.IsType<Ok<TemplateResponse>>(result.Result);
        Assert.NotNull(ok.Value);
        Assert.NotNull(repository.LastUpdatedTemplate);
        Assert.Equal(existingTemplate.Key, repository.LastUpdatedExistingKey);
        Assert.Equal(existingTemplate.Id, repository.LastUpdatedTemplate.Id);
        Assert.Equal("landing-page", repository.LastUpdatedTemplate.Key.ToString());
        Assert.Equal("landing-page", ok.Value.Key);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnProblem_WhenTemplateMissing()
    {
        var result =
            await TemplateEndpoints.UpdateAsync(
                Guid.NewGuid(),
                new CreateTemplateRequest
                {
                    Name = "Landing Page",
                    Key = "landing-page",
                    Sections = []
                },
                new FakeTemplateRepository(),
                new FakeContentModelCatalog(),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnConflict_WhenTemplateKeyAlreadyExists()
    {
        var existingTemplate =
            CreateAuthoredTemplate(
                "Article Page",
                "article-page");
        var repository =
            new FakeTemplateRepository
            {
                OnUpdateTemplateAsync = (_, _) => throw new InvalidOperationException(
                    "Template key 'landing-page' already exists.")
            };

        var result =
            await TemplateEndpoints.UpdateAsync(
                existingTemplate.Id.Value,
                new CreateTemplateRequest
                {
                    Name = "Landing Page",
                    Key = "landing-page",
                    Sections = []
                },
                repository,
                new FakeContentModelCatalog(
                    authoredTemplates: [existingTemplate]),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
    }

    [Fact]
    public async Task UpdateAsync_ShouldRollback_WhenCatalogRefreshFails()
    {
        var existingTemplate =
            CreateAuthoredTemplate(
                "Article Page",
                "article-page");
        var catalog =
            new FakeContentModelCatalog(
                authoredTemplates: [existingTemplate])
            {
                RefreshException = new ContentModelCatalogRefreshException(
                    [
                        new ValidationError(
                            "DuplicateFieldKeyInTemplate",
                            "Template 'landing-page' contains multiple fields with key 'title'.",
                            "title")
                    ])
            };
        var repository =
            new FakeTemplateRepository();

        var result =
            await TemplateEndpoints.UpdateAsync(
                existingTemplate.Id.Value,
                new CreateTemplateRequest
                {
                    Name = "Landing Page",
                    Key = "landing-page",
                    Sections = []
                },
                repository,
                catalog,
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.NotNull(repository.LastRollbackTemplate);
        Assert.Equal(existingTemplate.Key, repository.LastRollbackTemplate.Key);
        Assert.Equal(new TemplateKey("landing-page"), repository.LastRollbackExistingKey);
        Assert.Equal(2, catalog.RefreshCallCount);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnProblem_WhenTemplateIsBaseForAnotherTemplate()
    {
        var baseTemplate =
            CreateAuthoredTemplate(
                "Base Page",
                "base-page");
        var childTemplate =
            CreateAuthoredTemplate(
                "Article Page",
                "article-page",
                baseTemplate);
        var repository =
            new FakeTemplateRepository(
                authoredTemplates:
                [
                    baseTemplate,
                    childTemplate
                ]);

        var result =
            await TemplateEndpoints.DeleteAsync(
                baseTemplate.Id.Value,
                repository,
                new FakeContentModelCatalog(
                    authoredTemplates:
                    [
                        baseTemplate,
                        childTemplate
                    ]),
                new FakeContentRepository(),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Null(repository.LastDeletedTemplateKey);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnProblem_WhenContentItemsStillUseTemplate()
    {
        var template =
            CreateAuthoredTemplate(
                "Article Page",
                "article-page");
        var contentRepository =
            new FakeContentRepository(
                [
                    CreateContentItem(template.Id)
                ]);

        var result =
            await TemplateEndpoints.DeleteAsync(
                template.Id.Value,
                new FakeTemplateRepository(
                    authoredTemplates: [template]),
                new FakeContentModelCatalog(
                    authoredTemplates: [template]),
                contentRepository,
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRollback_WhenCatalogRefreshFails()
    {
        var template =
            CreateAuthoredTemplate(
                "Article Page",
                "article-page");
        var catalog =
            new FakeContentModelCatalog(
                authoredTemplates: [template])
            {
                RefreshException = new ContentModelCatalogRefreshException(
                    [
                        new ValidationError(
                            "TemplateStillReferenced",
                            "Template 'article-page' is still referenced.",
                            "article-page")
                    ])
            };
        var repository =
            new FakeTemplateRepository();

        var result =
            await TemplateEndpoints.DeleteAsync(
                template.Id.Value,
                repository,
                catalog,
                new FakeContentRepository(),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.NotNull(repository.LastRecreatedTemplate);
        Assert.Equal(template.Key, repository.LastRecreatedTemplate.Key);
        Assert.Equal(2, catalog.RefreshCallCount);
    }

    [Fact]
    public async Task GetFieldsByIdAsync_ShouldReturnOk_WhenTemplateExists()
    {
        var template =
            CreateTemplate();
        var catalog =
            new FakeContentModelCatalog(
                template);

        var result =
            await TemplateEndpoints.GetFieldsByIdAsync(
                template.Id.Value,
                catalog,
                TestContext.Current.CancellationToken);

        var ok = Assert.IsType<Ok<TemplateFieldCollectionResponse>>(result.Result);
        Assert.NotNull(ok.Value);

        var field = Assert.Single(ok.Value.Embedded.Fields);
        Assert.Equal("title", field.Key);
        Assert.Equal("SingleLineText", field.Type);
        Assert.Equal("content", field.SectionKey);
        Assert.Equal($"/api/v1/templates/{template.Id.Value}/fields", ok.Value.Links.Self.Href);
        Assert.Equal($"/api/v1/templates/{template.Id.Value}", ok.Value.Links.Template.Href);
        Assert.Equal($"/api/v1/templates/{template.Id.Value}/dependencies", ok.Value.Links.Dependencies.Href);
        Assert.Equal("/api/v1/content", ok.Value.Links.CreateItem.Href);
    }

    [Fact]
    public async Task GetDependenciesByIdAsync_ShouldReturnOk_WhenTemplateHasNoDependencies()
    {
        var template =
            CreateAuthoredTemplate(
                "Article Page",
                "article-page");
        var catalog =
            new FakeContentModelCatalog(
                authoredTemplates: [template]);

        var result =
            await TemplateEndpoints.GetDependenciesByIdAsync(
                template.Id.Value,
                new FakeTemplateRepository(
                    authoredTemplates: [template]),
                catalog,
                new FakeContentRepository(),
                new FakeContentPathResolver(),
                TestContext.Current.CancellationToken);

        var ok = Assert.IsType<Ok<TemplateDependencyResponse>>(result.Result);
        Assert.NotNull(ok.Value);

        Assert.Equal(template.Id.Value.ToString(), ok.Value.TemplateId);
        Assert.Equal("article-page", ok.Value.TemplateKey);
        Assert.True(ok.Value.CanDelete);
        Assert.Equal(0, ok.Value.Summary.DependentTemplateCount);
        Assert.Equal(0, ok.Value.Summary.DependentContentItemCount);
        Assert.Empty(ok.Value.Embedded.Templates);
        Assert.Empty(ok.Value.Embedded.ContentItems);
        Assert.Equal($"/api/v1/templates/{template.Id.Value}/dependencies", ok.Value.Links.Self.Href);
        Assert.Equal($"/api/v1/templates/{template.Id.Value}", ok.Value.Links.Template.Href);
    }

    [Fact]
    public async Task GetDependenciesByIdAsync_ShouldReturnTemplatesAndContentItems_WhenDependenciesExist()
    {
        var baseTemplate =
            CreateAuthoredTemplate(
                "Base Page",
                "base-page");
        var childTemplate =
            CreateAuthoredTemplate(
                "Article Page",
                "article-page",
                baseTemplate);
        var grandchildTemplate =
            CreateAuthoredTemplate(
                "News Article Page",
                "news-article-page",
                childTemplate);
        var homeItem =
            CreateContentItem(
                baseTemplate.Id,
                name: "Home",
                key: "home");
        var articleItem =
            CreateContentItem(
                baseTemplate.Id,
                name: "Article",
                key: "article",
                parentId: homeItem.Id);

        var result =
            await TemplateEndpoints.GetDependenciesByIdAsync(
                baseTemplate.Id.Value,
                new FakeTemplateRepository(
                    authoredTemplates:
                    [
                        baseTemplate,
                        childTemplate,
                        grandchildTemplate
                    ]),
                new FakeContentModelCatalog(
                    authoredTemplates:
                    [
                        baseTemplate,
                        childTemplate,
                        grandchildTemplate
                    ]),
                new FakeContentRepository(
                    [
                        articleItem,
                        homeItem
                    ]),
                new FakeContentPathResolver(),
                TestContext.Current.CancellationToken);

        var ok = Assert.IsType<Ok<TemplateDependencyResponse>>(result.Result);
        Assert.NotNull(ok.Value);

        Assert.False(ok.Value.CanDelete);
        Assert.Equal(2, ok.Value.Summary.DependentTemplateCount);
        Assert.Equal(2, ok.Value.Summary.DependentContentItemCount);
        Assert.Equal(
            new[]
            {
                "article-page",
                "news-article-page"
            },
            ok.Value.Embedded.Templates.Select(template => template.Key).ToArray());
        Assert.Equal(
            new[]
            {
                "/home",
                "/home/article"
            },
            ok.Value.Embedded.ContentItems.Select(item => item.Path).ToArray());
        Assert.Equal(
            $"/api/v1/templates/{childTemplate.Id.Value}",
            ok.Value.Embedded.Templates.First().Links.Self.Href);
        Assert.Equal(
            $"/api/v1/content/{homeItem.Id.Value}?lang=en&version=1",
            ok.Value.Embedded.ContentItems.First().Links.Self.Href);
    }

    [Fact]
    public async Task GetDependenciesByIdAsync_ShouldReturnProblem_WhenTemplateMissing()
    {
        var result =
            await TemplateEndpoints.GetDependenciesByIdAsync(
                Guid.NewGuid(),
                new FakeTemplateRepository(),
                new FakeContentModelCatalog(),
                new FakeContentRepository(),
                new FakeContentPathResolver(),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    [Fact]
    public async Task GetDependenciesByIdAsync_ShouldReturnProblem_WhenIdIsInvalid()
    {
        var result =
            await TemplateEndpoints.GetDependenciesByIdAsync(
                Guid.Empty,
                new FakeTemplateRepository(),
                new FakeContentModelCatalog(),
                new FakeContentRepository(),
                new FakeContentPathResolver(),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task GetFieldsByIdAsync_ShouldReturnProblem_WhenTemplateMissing()
    {
        var result =
            await TemplateEndpoints.GetFieldsByIdAsync(
                Guid.NewGuid(),
                new FakeContentModelCatalog(),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    [Fact]
    public async Task GetFieldsByIdAsync_ShouldReturnProblem_WhenIdIsInvalid()
    {
        var result =
            await TemplateEndpoints.GetFieldsByIdAsync(
                Guid.Empty,
                new FakeContentModelCatalog(),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    private static EffectiveTemplateDefinition CreateTemplate()
    {
        return CreateTemplate(
            "Article Page",
            "article-page");
    }

    private static EffectiveTemplateDefinition CreateTemplate(
        string name,
        string key)
    {
        var field =
            new FieldDefinition(
                new FieldId(Guid.NewGuid()),
                "Title",
                "title",
                FieldType.SingleLineText,
                isUnversioned: true);
        var section =
            new TemplateSectionDefinition(
                Guid.NewGuid(),
                "Content",
                "content",
                100,
                new[] { field });

        return new EffectiveTemplateDefinition(
            new TemplateId(Guid.NewGuid()),
            name,
            new TemplateKey(key),
            new[] { section });
    }

    private static TemplateDefinition CreateAuthoredTemplate(
        string name,
        string key,
        TemplateDefinition? baseTemplate = null)
    {
        return new TemplateDefinition(
            new TemplateId(Guid.NewGuid()),
            name,
            new TemplateKey(key),
            baseTemplate,
            []);
    }

    private static ContentItemDefinition CreateContentItem(
        TemplateId templateId,
        string name = "Home",
        string key = "home",
        ContentItemId? parentId = null)
    {
        return new ContentItemDefinition(
            new ContentItemId(Guid.NewGuid()),
            name,
            new ContentItemKey(key),
            templateId,
            parentId);
    }

    private sealed class FakeContentModelCatalog : IContentModelCatalog
    {
        private readonly Dictionary<TemplateId, TemplateDefinition> _authoredTemplates;
        private readonly Dictionary<TemplateId, EffectiveTemplateDefinition> _templates;

        public FakeContentModelCatalog(
            params EffectiveTemplateDefinition?[] templates)
            : this(
                Array.Empty<TemplateDefinition>(),
                templates)
        {
        }

        public FakeContentModelCatalog(
            IReadOnlyCollection<TemplateDefinition> authoredTemplates,
            params EffectiveTemplateDefinition?[] templates)
        {
            _authoredTemplates = (authoredTemplates ?? Array.Empty<TemplateDefinition>())
                .ToDictionary(template => template.Id);
            _templates = (templates ?? Array.Empty<EffectiveTemplateDefinition?>())
                .Where(template => template != null)
                .Cast<EffectiveTemplateDefinition>()
                .ToDictionary(template => template.Id);
        }

        public TemplateId? LastRequestedTemplateId { get; private set; }

        public int RefreshCallCount { get; private set; }

        public ContentModelCatalogRefreshException? RefreshException { get; init; }

        public Task<TemplateDefinition?> GetTemplateAsync(
            TemplateId id,
            CancellationToken cancellationToken = default)
        {
            _authoredTemplates.TryGetValue(id, out var template);
            return Task.FromResult(template);
        }

        public Task<TemplateDefinition?> GetTemplateAsync(
            TemplateKey key,
            CancellationToken cancellationToken = default)
        {
            var template =
                _authoredTemplates.Values.FirstOrDefault(
                    value => value.Key == key);

            return Task.FromResult(template);
        }

        public Task<EffectiveTemplateDefinition?> GetEffectiveTemplateAsync(
            TemplateId id,
            CancellationToken cancellationToken = default)
        {
            LastRequestedTemplateId = id;
            _templates.TryGetValue(id, out var template);
            return Task.FromResult(template);
        }

        public Task<EffectiveTemplateDefinition?> GetEffectiveTemplateAsync(
            TemplateKey key,
            CancellationToken cancellationToken = default)
        {
            var template =
                _templates.Values.FirstOrDefault(
                    value => value.Key == key);

            return Task.FromResult(template);
        }

        public Task<IReadOnlyCollection<EffectiveTemplateDefinition>> GetEffectiveTemplatesAsync(
            CancellationToken cancellationToken = default)
        {
            var templates =
                _templates.Values
                    .OrderBy(template => template.Key.ToString(), StringComparer.Ordinal)
                    .ToArray();

            return Task.FromResult<IReadOnlyCollection<EffectiveTemplateDefinition>>(templates);
        }

        public void AddTemplate(
            EffectiveTemplateDefinition template)
        {
            _templates[template.Id] = template;
        }

        public void AddAuthoredTemplate(
            TemplateDefinition template)
        {
            _authoredTemplates[template.Id] = template;
        }

        public Task InvalidateAsync(
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task RefreshAsync(
            CancellationToken cancellationToken = default)
        {
            RefreshCallCount++;

            if (RefreshException != null)
            {
                throw RefreshException;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeTemplateRepository : ITemplateRepository
    {
        private readonly IReadOnlyCollection<TemplateDefinition> _authoredTemplates;

        public FakeTemplateRepository(
            IReadOnlyCollection<TemplateDefinition>? authoredTemplates = null)
        {
            _authoredTemplates = authoredTemplates ?? [];
        }

        public Func<TemplateDefinition, Task>? OnCreateTemplateAsync { get; set; }

        public Func<TemplateKey, TemplateDefinition, Task>? OnUpdateTemplateAsync { get; set; }

        public TemplateDefinition? LastCreatedTemplate { get; private set; }

        public TemplateDefinition? LastRecreatedTemplate { get; private set; }

        public TemplateKey? LastUpdatedExistingKey { get; private set; }

        public TemplateDefinition? LastUpdatedTemplate { get; private set; }

        public TemplateKey? LastRollbackExistingKey { get; private set; }

        public TemplateDefinition? LastRollbackTemplate { get; private set; }

        public TemplateKey? LastDeletedTemplateKey { get; private set; }

        public Task<IReadOnlyCollection<TemplateDefinition>> GetTemplatesAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_authoredTemplates);
        }

        public Task CreateTemplateAsync(
            TemplateDefinition template,
            CancellationToken cancellationToken = default)
        {
            if (LastDeletedTemplateKey == null)
            {
                LastCreatedTemplate = template;
            }
            else
            {
                LastRecreatedTemplate = template;
            }

            return OnCreateTemplateAsync == null
                ? Task.CompletedTask
                : OnCreateTemplateAsync(template);
        }

        public Task UpdateTemplateAsync(
            TemplateKey existingKey,
            TemplateDefinition template,
            CancellationToken cancellationToken = default)
        {
            if (LastUpdatedExistingKey == null)
            {
                LastUpdatedExistingKey = existingKey;
                LastUpdatedTemplate = template;
            }
            else
            {
                LastRollbackExistingKey = existingKey;
                LastRollbackTemplate = template;
            }

            return OnUpdateTemplateAsync == null
                ? Task.CompletedTask
                : OnUpdateTemplateAsync(existingKey, template);
        }

        public Task DeleteTemplateAsync(
            TemplateKey key,
            CancellationToken cancellationToken = default)
        {
            LastDeletedTemplateKey = key;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeContentRepository : IContentRepository
    {
        private readonly IReadOnlyCollection<ContentItemDefinition> _items;

        public FakeContentRepository(
            IReadOnlyCollection<ContentItemDefinition>? items = null)
        {
            _items = items ?? [];
        }

        public Task<ContentItemDefinition?> GetItemAsync(
            ContentItemId itemId,
            CancellationToken cancellationToken = default)
        {
            var item =
                _items.FirstOrDefault(
                    candidate => candidate.Id == itemId);

            return Task.FromResult(item);
        }

        public Task<ContentItemDefinition?> GetItemAsync(
            ContentPath path,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyCollection<ContentItemDefinition>> GetChildItemsAsync(
            ContentItemId? parentId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyCollection<ContentItemDefinition>> GetItemsByTemplateAsync(
            TemplateId templateId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<ContentItemDefinition>>(
                _items.Where(item => item.TemplateId == templateId).ToArray());
        }

        public Task<IReadOnlyCollection<ContentFieldValue>> GetFieldValuesAsync(
            ContentItemId itemId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task SaveItemAsync(
            ContentItemDefinition item,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task SaveFieldValuesAsync(
            ContentItemId itemId,
            IReadOnlyCollection<ContentFieldValue> values,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task DeleteItemAsync(
            ContentItemId itemId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeContentPathResolver : IContentPathResolver
    {
        public Task<ContentPath> ResolveAsync(
            ContentItemDefinition item,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(item);

            return Task.FromResult(
                item.ParentId == null
                    ? ContentPath.FromRoot(item.Key)
                    : new ContentPath($"/home/{item.Key}"));
        }

        public Task<IReadOnlyDictionary<ContentItemId, ContentPath>> ResolveAsync(
            IReadOnlyCollection<ContentItemDefinition> items,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(items);

            IReadOnlyDictionary<ContentItemId, ContentPath> resolved =
                items.ToDictionary(
                    item => item.Id,
                    item => item.ParentId == null
                        ? ContentPath.FromRoot(item.Key)
                        : new ContentPath($"/home/{item.Key}"));

            return Task.FromResult(resolved);
        }
    }
}
