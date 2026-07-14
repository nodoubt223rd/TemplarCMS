using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using TemplarCMS.Api.Content;
using TemplarCMS.Application.Content;
using TemplarCMS.Domain.Content;
using Xunit;

namespace TemplarCMS.Api.Tests.Content;

public sealed class ContentLookupEndpointsTests
{
    [Fact]
    public async Task GetByIdAsync_ShouldReturnOk_WhenItemExists()
    {
        var itemId = new ContentItemId(Guid.NewGuid());
        var resolvedItem =
            CreateResolvedItem(
                itemId: itemId,
                parentId: new ContentItemId(Guid.NewGuid()));
        var service =
            new FakeContentItemService(
                resolvedItem);

        var result =
            await ContentLookupEndpoints.GetByIdAsync(
                itemId.Value,
                "EN",
                1,
                service,
                TestContext.Current.CancellationToken);

        var ok = Assert.IsType<Ok<ContentItemResponse>>(result.Result);
        Assert.NotNull(ok.Value);
        Assert.NotNull(service.LastContext);

        Assert.Equal(itemId.Value.ToString(), ok.Value.Id);
        Assert.Equal("/home/articles/hello-world", ok.Value.Path);
        Assert.Equal(
            $"/api/v1/content/{itemId.Value}?lang=en&version=1",
            ok.Value.Links.Self.Href);
        Assert.Equal(
            $"/api/v1/content/{itemId.Value}/dependencies?lang=en&version=1",
            ok.Value.Links.Dependencies.Href);
        Assert.Equal(
            $"/api/v1/content/{itemId.Value}/rename",
            ok.Value.Links.Rename.Href);
        Assert.Equal(
            $"/api/v1/content/{itemId.Value}/move",
            ok.Value.Links.Move.Href);
        Assert.Equal(
            $"/api/v1/content/{itemId.Value}/branch?lang=en&version=1",
            ok.Value.Links.Branch.Href);
        Assert.Equal(itemId, service.LastRequestedItemId);
        Assert.Equal(new ContentLanguage("en"), service.LastContext.Language);
        Assert.Equal(ContentVersion.First, service.LastContext.Version);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnProblem_WhenItemIsMissing()
    {
        var id = Guid.NewGuid();

        var result =
            await ContentLookupEndpoints.GetByIdAsync(
                id,
                "en",
                1,
                new FakeContentItemService(null),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnProblem_WhenVersionIsInvalid()
    {
        var result =
            await ContentLookupEndpoints.GetByIdAsync(
                Guid.NewGuid(),
                "en",
                -1,
                new FakeContentItemService(null),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task GetByPathAsync_ShouldReturnOk_WhenItemExists()
    {
        var itemId = new ContentItemId(Guid.NewGuid());
        var resolvedItem =
            CreateResolvedItem(
                itemId: itemId,
                parentId: new ContentItemId(Guid.NewGuid()));
        var service =
            new FakeContentItemService(
                resolvedItem);

        var result =
            await ContentLookupEndpoints.GetByPathAsync(
                "HOME/ARTICLES/HELLO-WORLD",
                "EN",
                1,
                service,
                TestContext.Current.CancellationToken);

        var ok = Assert.IsType<Ok<ContentItemResponse>>(result.Result);
        Assert.NotNull(service.LastContext);
        Assert.NotNull(ok.Value);

        Assert.Equal("/home/articles/hello-world", ok.Value.Path);
        Assert.Equal("en", ok.Value.Language);
        Assert.Equal(1, ok.Value.Version);
        Assert.Equal("Hello World", ok.Value.Fields["title"]);
        Assert.Equal(
            "/api/v1/content/by-path/home/articles/hello-world?lang=en&version=1",
            ok.Value.Links.Self.Href);
        Assert.Equal(
            $"/api/v1/content/{itemId.Value}/dependencies?lang=en&version=1",
            ok.Value.Links.Dependencies.Href);
        Assert.Equal(
            new ContentPath("/home/articles/hello-world"),
            service.LastRequestedPath);
        Assert.Equal(new ContentLanguage("en"), service.LastContext.Language);
        Assert.Equal(ContentVersion.First, service.LastContext.Version);
    }

    [Fact]
    public async Task GetByPathAsync_ShouldReturnProblem_WhenPathIsMissing()
    {
        var result =
            await ContentLookupEndpoints.GetByPathAsync(
                null,
                "en",
                1,
                new FakeContentItemService(null),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task GetByPathAsync_ShouldReturnProblem_WhenItemIsMissing()
    {
        var result =
            await ContentLookupEndpoints.GetByPathAsync(
                "home/missing",
                "en",
                1,
                new FakeContentItemService(null),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    [Fact]
    public async Task GetByPathAsync_ShouldReturnProblem_WhenVersionIsInvalid()
    {
        var result =
            await ContentLookupEndpoints.GetByPathAsync(
                "home/articles/hello-world",
                "en",
                -1,
                new FakeContentItemService(null),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task GetChildrenAsync_ShouldReturnOk_WhenParentHasChildren()
    {
        var parentId = new ContentItemId(Guid.NewGuid());
        var parent =
            CreateResolvedItem(
                itemId: parentId,
                parentId: null,
                path: "/home");
        var childA =
            CreateResolvedItem(
                itemId: new ContentItemId(Guid.NewGuid()),
                parentId: parentId,
                path: "/home/child-a",
                name: "Child A",
                key: "child-a",
                title: "A");
        var childB =
            CreateResolvedItem(
                itemId: new ContentItemId(Guid.NewGuid()),
                parentId: parentId,
                path: "/home/child-b",
                name: "Child B",
                key: "child-b",
                title: "B");
        var service =
            new FakeContentItemService(
                parent,
                [childA, childB]);

        var result =
            await ContentLookupEndpoints.GetChildrenAsync(
                parentId.Value,
                "EN",
                1,
                service,
                TestContext.Current.CancellationToken);

        var ok = Assert.IsType<Ok<ContentItemCollectionResponse>>(result.Result);
        Assert.NotNull(ok.Value);
        Assert.NotNull(service.LastContext);
        Assert.NotNull(ok.Value.Links.Parent);

        Assert.Equal(
            $"/api/v1/content/{parentId.Value}/children?lang=en&version=1",
            ok.Value.Links.Self.Href);
        Assert.Equal(
            $"/api/v1/content/{parentId.Value}?lang=en&version=1",
            ok.Value.Links.Parent.Href);
        Assert.Equal(2, ok.Value.Embedded.Items.Count);
        Assert.Equal(
            new[] { "/home/child-a", "/home/child-b" },
            ok.Value.Embedded.Items.Select(item => item.Path).ToArray());
        Assert.Equal(
            new[] { "A", "B" },
            ok.Value.Embedded.Items.Select(item => item.Fields["title"]).ToArray());
        Assert.Equal(parentId, service.LastRequestedChildParentId);
        Assert.Equal(new ContentLanguage("en"), service.LastContext.Language);
        Assert.Equal(ContentVersion.First, service.LastContext.Version);
    }

    [Fact]
    public async Task GetChildrenAsync_ShouldReturnOk_WhenParentHasNoChildren()
    {
        var parentId = new ContentItemId(Guid.NewGuid());
        var parent =
            CreateResolvedItem(
                itemId: parentId,
                parentId: null,
                path: "/home");
        var service =
            new FakeContentItemService(
                parent,
                []);

        var result =
            await ContentLookupEndpoints.GetChildrenAsync(
                parentId.Value,
                "en",
                1,
                service,
                TestContext.Current.CancellationToken);

        var ok = Assert.IsType<Ok<ContentItemCollectionResponse>>(result.Result);
        Assert.NotNull(ok.Value);

        Assert.Empty(ok.Value.Embedded.Items);
    }

    [Fact]
    public async Task GetChildrenAsync_ShouldReturnProblem_WhenParentIsMissing()
    {
        var result =
            await ContentLookupEndpoints.GetChildrenAsync(
                Guid.NewGuid(),
                "en",
                1,
                new FakeContentItemService(null, []),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    [Fact]
    public async Task GetChildrenAsync_ShouldReturnProblem_WhenVersionIsInvalid()
    {
        var result =
            await ContentLookupEndpoints.GetChildrenAsync(
                Guid.NewGuid(),
                "en",
                -1,
                new FakeContentItemService(null, []),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task GetRootChildrenAsync_ShouldReturnOk_WhenRootItemsExist()
    {
        var rootA =
            CreateResolvedItem(
                itemId: new ContentItemId(Guid.NewGuid()),
                parentId: null,
                path: "/home",
                name: "Home",
                key: "home",
                title: "Home");
        var rootB =
            CreateResolvedItem(
                itemId: new ContentItemId(Guid.NewGuid()),
                parentId: null,
                path: "/articles",
                name: "Articles",
                key: "articles",
                title: "Articles");
        var service =
            new FakeContentItemService(
                null,
                [rootA, rootB]);

        var result =
            await ContentLookupEndpoints.GetRootChildrenAsync(
                "EN",
                1,
                service,
                TestContext.Current.CancellationToken);

        var ok = Assert.IsType<Ok<ContentItemCollectionResponse>>(result.Result);
        Assert.NotNull(ok.Value);
        Assert.NotNull(service.LastContext);

        Assert.Equal(
            "/api/v1/content/root/children?lang=en&version=1",
            ok.Value.Links.Self.Href);
        Assert.Null(ok.Value.Links.Parent);
        Assert.Equal(2, ok.Value.Embedded.Items.Count);
        Assert.Equal(
            new[] { "/home", "/articles" },
            ok.Value.Embedded.Items.Select(item => item.Path).ToArray());
        Assert.Null(service.LastRequestedChildParentId);
        Assert.Equal(new ContentLanguage("en"), service.LastContext.Language);
        Assert.Equal(ContentVersion.First, service.LastContext.Version);
    }

    [Fact]
    public async Task GetRootChildrenAsync_ShouldReturnOk_WhenNoRootItemsExist()
    {
        var service =
            new FakeContentItemService(
                null,
                []);

        var result =
            await ContentLookupEndpoints.GetRootChildrenAsync(
                "en",
                1,
                service,
                TestContext.Current.CancellationToken);

        var ok = Assert.IsType<Ok<ContentItemCollectionResponse>>(result.Result);
        Assert.NotNull(ok.Value);

        Assert.Empty(ok.Value.Embedded.Items);
        Assert.Null(ok.Value.Links.Parent);
    }

    [Fact]
    public async Task GetRootChildrenAsync_ShouldReturnProblem_WhenVersionIsInvalid()
    {
        var result =
            await ContentLookupEndpoints.GetRootChildrenAsync(
                "en",
                -1,
                new FakeContentItemService(null, []),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task GetBranchAsync_ShouldReturnParentAndDirectChildren()
    {
        var parentId = new ContentItemId(Guid.NewGuid());
        var parent =
            CreateResolvedItem(
                itemId: parentId,
                parentId: null,
                path: "/home");
        var childA =
            CreateResolvedItem(
                itemId: new ContentItemId(Guid.NewGuid()),
                parentId: parentId,
                path: "/home/child-a",
                name: "Child A",
                key: "child-a",
                title: "A");
        var childB =
            CreateResolvedItem(
                itemId: new ContentItemId(Guid.NewGuid()),
                parentId: parentId,
                path: "/home/child-b",
                name: "Child B",
                key: "child-b",
                title: "B");
        var service =
            new FakeContentItemService(
                parent,
                [childA, childB]);

        var result =
            await ContentLookupEndpoints.GetBranchAsync(
                parentId.Value,
                "EN",
                1,
                service,
                TestContext.Current.CancellationToken);

        var ok = Assert.IsType<Ok<ContentBranchResponse>>(result.Result);
        Assert.NotNull(ok.Value);
        Assert.NotNull(ok.Value.Item);
        Assert.NotNull(ok.Value.Links.Item);

        Assert.Equal(parentId.Value.ToString(), ok.Value.Item.Id);
        Assert.Equal("/home", ok.Value.Item.Path);
        Assert.Equal(
            $"/api/v1/content/{parentId.Value}/branch?lang=en&version=1",
            ok.Value.Links.Self.Href);
        Assert.Equal(
            $"/api/v1/content/{parentId.Value}?lang=en&version=1",
            ok.Value.Links.Item.Href);
        Assert.Equal(
            new[] { "/home/child-a", "/home/child-b" },
            ok.Value.Embedded.Children.Select(child => child.Path).ToArray());
    }

    [Fact]
    public async Task GetBranchAsync_ShouldReturnProblem_WhenParentIsMissing()
    {
        var result =
            await ContentLookupEndpoints.GetBranchAsync(
                Guid.NewGuid(),
                "en",
                1,
                new FakeContentItemService(null, []),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    [Fact]
    public async Task GetRootBranchAsync_ShouldReturnRootChildren()
    {
        var rootA =
            CreateResolvedItem(
                itemId: new ContentItemId(Guid.NewGuid()),
                parentId: null,
                path: "/home",
                name: "Home",
                key: "home",
                title: "Home");
        var rootB =
            CreateResolvedItem(
                itemId: new ContentItemId(Guid.NewGuid()),
                parentId: null,
                path: "/articles",
                name: "Articles",
                key: "articles",
                title: "Articles");
        var service =
            new FakeContentItemService(
                null,
                [rootA, rootB]);

        var result =
            await ContentLookupEndpoints.GetRootBranchAsync(
                "EN",
                1,
                service,
                TestContext.Current.CancellationToken);

        var ok = Assert.IsType<Ok<ContentBranchResponse>>(result.Result);
        Assert.NotNull(ok.Value);

        Assert.Null(ok.Value.Item);
        Assert.Null(ok.Value.Links.Item);
        Assert.Equal(
            "/api/v1/content/root/branch?lang=en&version=1",
            ok.Value.Links.Self.Href);
        Assert.Equal(
            new[] { "/home", "/articles" },
            ok.Value.Embedded.Children.Select(child => child.Path).ToArray());
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnCreated_WhenRequestIsValid()
    {
        var templateId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var parentContentItemId = new ContentItemId(parentId);
        var parentItem =
            CreateResolvedItem(
                itemId: parentContentItemId,
                parentId: null,
                path: "/home",
                name: "Home",
                key: "home",
                title: "Home");
        var service =
            new FakeContentItemService(
                parentItem,
                []);
        service.SetItem(parentItem);
        service.SetChildren(parentContentItemId, Array.Empty<ResolvedContentItem>());
        service.OnSaveItemAsync = item =>
        {
            var createdItem =
                CreateResolvedItem(
                    itemId: item.Id,
                    parentId: item.ParentId,
                    path: "/home-page",
                    name: item.Name,
                    key: item.Key.ToString(),
                    title: item.Name,
                    templateId: item.TemplateId);

            service.StoredItem = createdItem;
            service.SetItem(createdItem);
            service.SetChildren(parentContentItemId, [createdItem]);

            return Task.CompletedTask;
        };

        var result =
            await ContentLookupEndpoints.CreateAsync(
                new CreateContentItemRequest
                {
                    Name = " Home Page ",
                    Key = "Home Page",
                    TemplateId = templateId,
                    ParentId = parentId
                },
                service,
                TestContext.Current.CancellationToken);

        var created = Assert.IsType<Created<ContentMutationResponse>>(result.Result);
        Assert.NotNull(created.Value);
        Assert.NotNull(service.LastSavedItem);

        Assert.Equal("Home Page", service.LastSavedItem.Name);
        Assert.Equal("home-page", service.LastSavedItem.Key.ToString());
        Assert.Equal(new TemplateId(templateId), service.LastSavedItem.TemplateId);
        Assert.Equal(new ContentItemId(parentId), service.LastSavedItem.ParentId);
        Assert.Equal("/home-page", created.Value.Item.Path);
        Assert.Equal(
            $"/api/v1/content/{service.LastSavedItem.Id.Value}?lang=en&version=1",
            created.Location);
        Assert.Equal(
            $"/api/v1/content/{service.LastSavedItem.Id.Value}?lang=en&version=1",
            created.Value.Item.Links.Self.Href);
        var affectedBranch = Assert.Single(created.Value.AffectedBranches);
        Assert.Equal("created-under", affectedBranch.Scope);
        Assert.NotNull(affectedBranch.Branch.Item);
        Assert.Equal("/home", affectedBranch.Branch.Item.Path);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnConflict_WhenSiblingKeyAlreadyExists()
    {
        var service =
            new FakeContentItemService(
                null,
                []);
        service.OnSaveItemAsync = _ => throw new InvalidOperationException(
            "Content item key 'home-page' already exists under parent '<root>'.");

        var result =
            await ContentLookupEndpoints.CreateAsync(
                new CreateContentItemRequest
                {
                    Name = "Home Page",
                    Key = "home-page",
                    TemplateId = Guid.NewGuid()
                },
                service,
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnProblem_WhenRequestIsMissing()
    {
        var result =
            await ContentLookupEndpoints.CreateAsync(
                null,
                new FakeContentItemService(null, []),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnOk_WhenRequestIsValid()
    {
        var itemId = new ContentItemId(Guid.NewGuid());
        var existingItem =
            CreateResolvedItem(
                itemId: itemId,
                parentId: null,
                path: "/home",
                name: "Home",
                key: "home");
        var service =
            new FakeContentItemService(
                existingItem,
                []);
        service.StoredItem = existingItem;
        service.OnSaveItemAsync = item =>
        {
            service.StoredItem =
                CreateResolvedItem(
                    itemId: item.Id,
                    parentId: item.ParentId,
                    path: "/home",
                    name: item.Name,
                    key: item.Key.ToString(),
                    title: item.Name,
                    templateId: item.TemplateId);

            return Task.CompletedTask;
        };

        var result =
            await ContentLookupEndpoints.UpdateAsync(
                itemId.Value,
                new UpdateContentItemRequest
                {
                    Name = "Home Updated"
                },
                "EN",
                1,
                service,
                TestContext.Current.CancellationToken);

        var ok = Assert.IsType<Ok<ContentItemResponse>>(result.Result);
        Assert.NotNull(ok.Value);
        Assert.NotNull(service.LastSavedItem);

        Assert.Equal(itemId, service.LastSavedItem.Id);
        Assert.Equal("Home Updated", service.LastSavedItem.Name);
        Assert.Equal(new ContentItemKey("home"), service.LastSavedItem.Key);
        Assert.Equal(existingItem.Item.TemplateId, service.LastSavedItem.TemplateId);
        Assert.Equal("/home", ok.Value.Path);
        Assert.Equal("Home Updated", ok.Value.Name);
        Assert.Equal(
            $"/api/v1/content/{itemId.Value}?lang=en&version=1",
            ok.Value.Links.Self.Href);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnProblem_WhenRequestIsMissing()
    {
        var result =
            await ContentLookupEndpoints.UpdateAsync(
                Guid.NewGuid(),
                null,
                "en",
                1,
                new FakeContentItemService(null, []),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnProblem_WhenItemIsMissing()
    {
        var result =
            await ContentLookupEndpoints.UpdateAsync(
                Guid.NewGuid(),
                new UpdateContentItemRequest
                {
                    Name = "Home Updated"
                },
                "en",
                1,
                new FakeContentItemService(null, []),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnProblem_WhenRequestIsInvalid()
    {
        var itemId = new ContentItemId(Guid.NewGuid());
        var existingItem =
            CreateResolvedItem(
                itemId: itemId,
                parentId: null,
                path: "/home",
                name: "Home",
                key: "home");
        var service =
            new FakeContentItemService(
                existingItem,
                []);

        var result =
            await ContentLookupEndpoints.UpdateAsync(
                itemId.Value,
                new UpdateContentItemRequest
                {
                    Name = " "
                },
                "en",
                1,
                service,
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task RenameAsync_ShouldReturnOk_WhenRequestIsValid()
    {
        var itemId = new ContentItemId(Guid.NewGuid());
        var existingItem =
            CreateResolvedItem(
                itemId: itemId,
                parentId: null,
                path: "/home",
                name: "Home",
                key: "home");
        var service =
            new FakeContentItemService(
                existingItem,
                []);
        service.StoredItem = existingItem;
        service.SetItem(existingItem);
        service.SetChildren(null, [existingItem]);
        service.OnRenameItemAsync = (savedItemId, name, key) =>
        {
            var renamedItem =
                CreateResolvedItem(
                    itemId: savedItemId,
                    parentId: null,
                    path: "/landing-page",
                    name: name,
                    key: key.ToString(),
                    title: name,
                    templateId: existingItem.Item.TemplateId);

            service.StoredItem = renamedItem;
            service.SetItem(renamedItem);
            service.SetChildren(null, [renamedItem]);

            return Task.CompletedTask;
        };

        var result =
            await ContentLookupEndpoints.RenameAsync(
                itemId.Value,
                new RenameContentItemRequest
                {
                    Name = "Landing Page",
                    Key = "landing page"
                },
                "EN",
                1,
                service,
                TestContext.Current.CancellationToken);

        var ok = Assert.IsType<Ok<ContentMutationResponse>>(result.Result);
        Assert.NotNull(ok.Value);

        Assert.Equal(itemId, service.LastRenamedItemId);
        Assert.Equal("Landing Page", service.LastRenameName);
        Assert.Equal(new ContentItemKey("landing-page"), service.LastRenameKey);
        Assert.Equal("/landing-page", ok.Value.Item.Path);
        Assert.Equal("Landing Page", ok.Value.Item.Name);
        var affectedBranch = Assert.Single(ok.Value.AffectedBranches);
        Assert.Equal("renamed-under", affectedBranch.Scope);
    }

    [Fact]
    public async Task RenameAsync_ShouldReturnConflict_WhenSiblingKeyAlreadyExists()
    {
        var itemId = new ContentItemId(Guid.NewGuid());
        var service = new FakeContentItemService(null, []);
        service.OnRenameItemAsync = (_, _, _) =>
            throw new InvalidOperationException(
                "Content item key 'landing-page' already exists under parent '<root>'.");

        var result =
            await ContentLookupEndpoints.RenameAsync(
                itemId.Value,
                new RenameContentItemRequest
                {
                    Name = "Landing Page",
                    Key = "landing-page"
                },
                "en",
                1,
                service,
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
    }

    [Fact]
    public async Task MoveAsync_ShouldReturnOk_WhenRequestIsValid()
    {
        var itemId = new ContentItemId(Guid.NewGuid());
        var oldParentId = new ContentItemId(Guid.NewGuid());
        var newParentId = new ContentItemId(Guid.NewGuid());
        var oldParent =
            CreateResolvedItem(
                itemId: oldParentId,
                parentId: null,
                path: "/home",
                name: "Home",
                key: "home",
                title: "Home");
        var newParent =
            CreateResolvedItem(
                itemId: newParentId,
                parentId: null,
                path: "/site",
                name: "Site",
                key: "site",
                title: "Site");
        var existingItem =
            CreateResolvedItem(
                itemId: itemId,
                parentId: oldParentId,
                path: "/home/page",
                name: "Page",
                key: "page");
        var service =
            new FakeContentItemService(
                existingItem,
                []);
        service.StoredItem = existingItem;
        service.SetItem(existingItem);
        service.SetItem(oldParent);
        service.SetItem(newParent);
        service.SetChildren(oldParentId, [existingItem]);
        service.SetChildren(newParentId, Array.Empty<ResolvedContentItem>());
        service.OnMoveItemAsync = (savedItemId, parentId) =>
        {
            var movedItem =
                CreateResolvedItem(
                    itemId: savedItemId,
                    parentId: parentId,
                    path: "/site/page",
                    name: existingItem.Item.Name,
                    key: existingItem.Item.Key.ToString(),
                    title: existingItem.Item.Name,
                    templateId: existingItem.Item.TemplateId);

            service.StoredItem = movedItem;
            service.SetItem(movedItem);
            service.SetChildren(oldParentId, Array.Empty<ResolvedContentItem>());

            if (parentId != null)
            {
                service.SetChildren(parentId, [movedItem]);
            }

            return Task.CompletedTask;
        };

        var result =
            await ContentLookupEndpoints.MoveAsync(
                itemId.Value,
                new MoveContentItemRequest
                {
                    ParentId = newParentId.Value
                },
                "EN",
                1,
                service,
                TestContext.Current.CancellationToken);

        var ok = Assert.IsType<Ok<ContentMutationResponse>>(result.Result);
        Assert.NotNull(ok.Value);

        Assert.Equal(itemId, service.LastMovedItemId);
        Assert.Equal(newParentId, service.LastMoveParentId);
        Assert.Equal("/site/page", ok.Value.Item.Path);
        Assert.Equal(2, ok.Value.AffectedBranches.Count);
        Assert.Contains(ok.Value.AffectedBranches, branch => branch.Scope == "moved-from");
        Assert.Contains(ok.Value.AffectedBranches, branch => branch.Scope == "moved-to");
    }

    [Fact]
    public async Task MoveAsync_ShouldReturnProblem_WhenMoveCreatesCycle()
    {
        var itemId = new ContentItemId(Guid.NewGuid());
        var existingItem =
            CreateResolvedItem(
                itemId: itemId,
                parentId: new ContentItemId(Guid.NewGuid()),
                path: "/home/page",
                name: "Page",
                key: "page");
        var service = new FakeContentItemService(existingItem, []);
        service.OnMoveItemAsync = (_, _) =>
            throw new InvalidOperationException(
                $"Content item '{itemId}' cannot be moved beneath itself or one of its descendants.");

        var result =
            await ContentLookupEndpoints.MoveAsync(
                itemId.Value,
                new MoveContentItemRequest
                {
                    ParentId = Guid.NewGuid()
                },
                "en",
                1,
                service,
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task MoveAsync_ShouldReturnConflict_WhenSiblingKeyAlreadyExistsUnderNewParent()
    {
        var existingItem =
            CreateResolvedItem(
                itemId: new ContentItemId(Guid.NewGuid()),
                parentId: new ContentItemId(Guid.NewGuid()),
                path: "/home/page",
                name: "Page",
                key: "page");
        var service = new FakeContentItemService(existingItem, []);
        service.OnMoveItemAsync = (_, _) =>
            throw new InvalidOperationException(
                "Content item key 'home' already exists under parent 'abc'.");

        var result =
            await ContentLookupEndpoints.MoveAsync(
                existingItem.Item.Id.Value,
                new MoveContentItemRequest
                {
                    ParentId = Guid.NewGuid()
                },
                "en",
                1,
                service,
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnProblem_WhenRequestIsInvalid()
    {
        var result =
            await ContentLookupEndpoints.CreateAsync(
                new CreateContentItemRequest
                {
                    Name = "Home Page",
                    Key = " ",
                    TemplateId = Guid.NewGuid()
                },
                new FakeContentItemService(null, []),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task SetValuesAsync_ShouldReturnOk_WhenRequestIsValid()
    {
        var itemId = new ContentItemId(Guid.NewGuid());
        var initialItem =
            CreateResolvedItem(
                itemId: itemId,
                parentId: null,
                path: "/home",
                name: "Home",
                key: "home",
                title: "Before");
        var service =
            new FakeContentItemService(
                initialItem,
                []);
        service.OnSaveFieldValuesByKeyAsync = (savedItemId, context, values) =>
        {
            service.StoredItem =
                CreateResolvedItem(
                    itemId: savedItemId,
                    parentId: null,
                    path: "/home",
                    name: "Home",
                    key: "home",
                    title: values["title"] ?? string.Empty);

            return Task.CompletedTask;
        };

        var result =
            await ContentLookupEndpoints.SetValuesAsync(
                itemId.Value,
                new SetContentFieldValuesRequest
                {
                    Language = "fr-ca",
                    Version = 3,
                    Values = new Dictionary<string, string?>
                    {
                        ["title"] = "Bonjour"
                    }
                },
                service,
                TestContext.Current.CancellationToken);

        var ok = Assert.IsType<Ok<ContentItemResponse>>(result.Result);
        Assert.NotNull(ok.Value);
        Assert.NotNull(service.LastSavedValuesContext);
        Assert.NotNull(service.LastSavedValues);

        Assert.Equal(itemId, service.LastSavedValuesItemId);
        Assert.Equal(new ContentLanguage("fr-ca"), service.LastSavedValuesContext.Language);
        Assert.Equal(new ContentVersion(3), service.LastSavedValuesContext.Version);
        Assert.Equal("Bonjour", service.LastSavedValues["title"]);
        Assert.Equal("Bonjour", ok.Value.Fields["title"]);
        Assert.Equal("fr-ca", ok.Value.Language);
        Assert.Equal(3, ok.Value.Version);
        Assert.Equal(
            $"/api/v1/content/{itemId.Value}/dependencies?lang=fr-ca&version=3",
            ok.Value.Links.Dependencies.Href);
    }

    [Fact]
    public async Task SetValuesAsync_ShouldReturnProblem_WhenRequestIsMissing()
    {
        var result =
            await ContentLookupEndpoints.SetValuesAsync(
                Guid.NewGuid(),
                null,
                new FakeContentItemService(null, []),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task SetValuesAsync_ShouldReturnProblem_WhenItemIsMissing()
    {
        var service =
            new FakeContentItemService(null, []);
        service.OnSaveFieldValuesByKeyAsync = (_, _, _) =>
            throw new InvalidOperationException("Content item 'missing' was not found.");

        var result =
            await ContentLookupEndpoints.SetValuesAsync(
                Guid.NewGuid(),
                new SetContentFieldValuesRequest
                {
                    Language = "en",
                    Version = 1,
                    Values = new Dictionary<string, string?>
                    {
                        ["title"] = "Home"
                    }
                },
                service,
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    [Fact]
    public async Task SetValuesAsync_ShouldReturnProblem_WhenRequestIsInvalid()
    {
        var result =
            await ContentLookupEndpoints.SetValuesAsync(
                Guid.NewGuid(),
                new SetContentFieldValuesRequest
                {
                    Language = " ",
                    Version = 1,
                    Values = new Dictionary<string, string?>
                    {
                        ["title"] = "Home"
                    }
                },
                new FakeContentItemService(null, []),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnNoContent_WhenItemExists()
    {
        var itemId = new ContentItemId(Guid.NewGuid());
        var item =
            CreateResolvedItem(
                itemId: itemId,
                parentId: null,
                path: "/home",
                name: "Home",
                key: "home");
        var service =
            new FakeContentItemService(
                item,
                []);

        var result =
            await ContentLookupEndpoints.DeleteAsync(
                itemId.Value,
                service,
                TestContext.Current.CancellationToken);

        Assert.IsType<NoContent>(result.Result);
        Assert.Equal(itemId, service.LastDeletedItemId);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnProblem_WhenItemIsMissing()
    {
        var result =
            await ContentLookupEndpoints.DeleteAsync(
                Guid.NewGuid(),
                new FakeContentItemService(null, []),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnProblem_WhenItemHasChildren()
    {
        var itemId = new ContentItemId(Guid.NewGuid());
        var item =
            CreateResolvedItem(
                itemId: itemId,
                parentId: null,
                path: "/home",
                name: "Home",
                key: "home");
        var service =
            new FakeContentItemService(
                item,
                []);
        service.OnDeleteItemAsync = _ => throw new InvalidOperationException(
            $"Content item '{itemId}' cannot be deleted because it has direct child items.");

        var result =
            await ContentLookupEndpoints.DeleteAsync(
                itemId.Value,
                service,
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnProblem_WhenIdIsInvalid()
    {
        var result =
            await ContentLookupEndpoints.DeleteAsync(
                Guid.Empty,
                new FakeContentItemService(null, []),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    [Fact]
    public async Task GetDependenciesAsync_ShouldReturnOk_WhenItemHasNoChildren()
    {
        var itemId = new ContentItemId(Guid.NewGuid());
        var item =
            CreateResolvedItem(
                itemId: itemId,
                parentId: null,
                path: "/home",
                name: "Home",
                key: "home");
        var service =
            new FakeContentItemService(
                item,
                []);

        var result =
            await ContentLookupEndpoints.GetDependenciesAsync(
                itemId.Value,
                "EN",
                1,
                service,
                TestContext.Current.CancellationToken);

        var ok = Assert.IsType<Ok<ContentItemDependencyResponse>>(result.Result);
        Assert.NotNull(ok.Value);

        Assert.Equal(itemId.Value.ToString(), ok.Value.Id);
        Assert.Equal("/home", ok.Value.Path);
        Assert.True(ok.Value.CanDelete);
        Assert.Equal(0, ok.Value.Summary.DirectChildCount);
        Assert.Empty(ok.Value.Embedded.Children);
        Assert.Equal(
            $"/api/v1/content/{itemId.Value}/dependencies?lang=en&version=1",
            ok.Value.Links.Self.Href);
        Assert.Equal(
            $"/api/v1/content/{itemId.Value}?lang=en&version=1",
            ok.Value.Links.ContentItem.Href);
    }

    [Fact]
    public async Task GetDependenciesAsync_ShouldReturnChildren_WhenDirectChildrenExist()
    {
        var itemId = new ContentItemId(Guid.NewGuid());
        var item =
            CreateResolvedItem(
                itemId: itemId,
                parentId: null,
                path: "/home",
                name: "Home",
                key: "home");
        var childA =
            CreateResolvedItem(
                itemId: new ContentItemId(Guid.NewGuid()),
                parentId: itemId,
                path: "/home/a-child",
                name: "A Child",
                key: "a-child",
                title: "A");
        var childB =
            CreateResolvedItem(
                itemId: new ContentItemId(Guid.NewGuid()),
                parentId: itemId,
                path: "/home/b-child",
                name: "B Child",
                key: "b-child",
                title: "B");
        var service =
            new FakeContentItemService(
                item,
                [childB, childA]);

        var result =
            await ContentLookupEndpoints.GetDependenciesAsync(
                itemId.Value,
                "en",
                1,
                service,
                TestContext.Current.CancellationToken);

        var ok = Assert.IsType<Ok<ContentItemDependencyResponse>>(result.Result);
        Assert.NotNull(ok.Value);

        Assert.False(ok.Value.CanDelete);
        Assert.Equal(2, ok.Value.Summary.DirectChildCount);
        Assert.Equal(
            new[]
            {
                "/home/a-child",
                "/home/b-child"
            },
            ok.Value.Embedded.Children.Select(child => child.Path).ToArray());
        Assert.Equal(
            $"/api/v1/content/{childA.Item.Id.Value}?lang=en&version=1",
            ok.Value.Embedded.Children.First().Links.Self.Href);
    }

    [Fact]
    public async Task GetDependenciesAsync_ShouldReturnProblem_WhenItemMissing()
    {
        var result =
            await ContentLookupEndpoints.GetDependenciesAsync(
                Guid.NewGuid(),
                "en",
                1,
                new FakeContentItemService(null, []),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    [Fact]
    public async Task GetDependenciesAsync_ShouldReturnProblem_WhenVersionIsInvalid()
    {
        var result =
            await ContentLookupEndpoints.GetDependenciesAsync(
                Guid.NewGuid(),
                "en",
                -1,
                new FakeContentItemService(null, []),
                TestContext.Current.CancellationToken);

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);

        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
    }

    private sealed class FakeContentItemService : IContentItemService
    {
        private readonly ResolvedContentItem? _item;
        private readonly IReadOnlyCollection<ResolvedContentItem> _children;
        private readonly Dictionary<ContentItemId, ResolvedContentItem> _itemsById;
        private readonly Dictionary<string, IReadOnlyCollection<ResolvedContentItem>> _childrenByParent;

        public FakeContentItemService(
            ResolvedContentItem? item,
            IReadOnlyCollection<ResolvedContentItem>? children = null)
        {
            _item = item;
            _children = children ?? [];
            _itemsById = new Dictionary<ContentItemId, ResolvedContentItem>();
            _childrenByParent = new Dictionary<string, IReadOnlyCollection<ResolvedContentItem>>(StringComparer.Ordinal);

            if (item != null)
            {
                _itemsById[item.Item.Id] = item;
            }

            foreach (var child in _children)
            {
                _itemsById[child.Item.Id] = child;
            }

            if (_children.Count > 0)
            {
                _childrenByParent[GetParentKey(_children.First().Item.ParentId)] = _children;
            }
        }

        public Func<ContentItemDefinition, Task>? OnSaveItemAsync { get; set; }

        public Func<ContentItemId, string, ContentItemKey, Task>? OnRenameItemAsync { get; set; }

        public Func<ContentItemId, ContentItemId?, Task>? OnMoveItemAsync { get; set; }

        public Func<ContentItemId, FieldValueResolutionContext, IReadOnlyDictionary<string, string?>, Task>? OnSaveFieldValuesByKeyAsync { get; set; }

        public Func<ContentItemId, Task>? OnDeleteItemAsync { get; set; }

        public ContentItemId? LastRequestedItemId { get; private set; }

        public ContentItemId? LastRequestedChildParentId { get; private set; }

        public ContentItemDefinition? LastSavedItem { get; private set; }

        public ContentItemId? LastRenamedItemId { get; private set; }

        public string? LastRenameName { get; private set; }

        public ContentItemKey? LastRenameKey { get; private set; }

        public ContentItemId? LastMovedItemId { get; private set; }

        public ContentItemId? LastMoveParentId { get; private set; }

        public ContentItemId? LastSavedValuesItemId { get; private set; }

        public ContentItemId? LastDeletedItemId { get; private set; }

        public FieldValueResolutionContext? LastSavedValuesContext { get; private set; }

        public IReadOnlyDictionary<string, string?>? LastSavedValues { get; private set; }

        public ContentPath? LastRequestedPath { get; private set; }

        public FieldValueResolutionContext? LastContext { get; private set; }

        public ResolvedContentItem? StoredItem { get; set; }

        public void SetItem(ResolvedContentItem item)
        {
            _itemsById[item.Item.Id] = item;
        }

        public void SetChildren(
            ContentItemId? parentId,
            IReadOnlyCollection<ResolvedContentItem> children)
        {
            _childrenByParent[GetParentKey(parentId)] = children;

            foreach (var child in children)
            {
                _itemsById[child.Item.Id] = child;
            }
        }

        public Task<ResolvedContentItem?> GetItemAsync(
            ContentItemId itemId,
            FieldValueResolutionContext context,
            CancellationToken cancellationToken = default)
        {
            LastRequestedItemId = itemId;
            LastContext = context;

            if (StoredItem != null && StoredItem.Item.Id == itemId)
            {
                return Task.FromResult<ResolvedContentItem?>(StoredItem);
            }

            if (_itemsById.TryGetValue(itemId, out var item))
            {
                return Task.FromResult<ResolvedContentItem?>(item);
            }

            return Task.FromResult(_item);
        }

        public Task<ResolvedContentItem?> GetItemAsync(
            ContentPath path,
            FieldValueResolutionContext context,
            CancellationToken cancellationToken = default)
        {
            LastRequestedPath = path;
            LastContext = context;
            return Task.FromResult(_item);
        }

        public Task<IReadOnlyCollection<ResolvedContentItem>> GetChildItemsAsync(
            ContentItemId? parentId,
            FieldValueResolutionContext context,
            CancellationToken cancellationToken = default)
        {
            LastRequestedChildParentId = parentId;
            LastContext = context;

            if (_childrenByParent.TryGetValue(GetParentKey(parentId), out var children))
            {
                return Task.FromResult(children);
            }

            return Task.FromResult(_children);
        }

        public Task SaveItemAsync(
            ContentItemDefinition item,
            CancellationToken cancellationToken = default)
        {
            LastSavedItem = item;

            return OnSaveItemAsync == null
                ? Task.CompletedTask
                : OnSaveItemAsync(item);
        }

        public Task RenameItemAsync(
            ContentItemId itemId,
            string name,
            ContentItemKey key,
            CancellationToken cancellationToken = default)
        {
            LastRenamedItemId = itemId;
            LastRenameName = name;
            LastRenameKey = key;

            return OnRenameItemAsync == null
                ? Task.CompletedTask
                : OnRenameItemAsync(itemId, name, key);
        }

        public Task MoveItemAsync(
            ContentItemId itemId,
            ContentItemId? parentId,
            CancellationToken cancellationToken = default)
        {
            LastMovedItemId = itemId;
            LastMoveParentId = parentId;

            return OnMoveItemAsync == null
                ? Task.CompletedTask
                : OnMoveItemAsync(itemId, parentId);
        }

        public Task SaveFieldValuesAsync(
            ContentItemId itemId,
            IReadOnlyCollection<ContentFieldValue> values,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task SaveFieldValuesAsync(
            ContentItemId itemId,
            FieldValueResolutionContext context,
            IReadOnlyDictionary<string, string?> values,
            CancellationToken cancellationToken = default)
        {
            LastSavedValuesItemId = itemId;
            LastSavedValuesContext = context;
            LastSavedValues = values;

            return OnSaveFieldValuesByKeyAsync == null
                ? Task.CompletedTask
                : OnSaveFieldValuesByKeyAsync(itemId, context, values);
        }

        public Task DeleteItemAsync(
            ContentItemId itemId,
            CancellationToken cancellationToken = default)
        {
            LastDeletedItemId = itemId;

            return OnDeleteItemAsync == null
                ? Task.CompletedTask
                : OnDeleteItemAsync(itemId);
        }

        private static string GetParentKey(ContentItemId? parentId)
        {
            return parentId?.ToString() ?? "<root>";
        }
    }

    private static ResolvedContentItem CreateResolvedItem(
        ContentItemId itemId,
        ContentItemId? parentId,
        string path = "/home/articles/hello-world",
        string name = "Hello World",
        string key = "hello-world",
        string title = "Hello World",
        TemplateId? templateId = null)
    {
        var resolvedTemplateId = templateId ?? new TemplateId(Guid.NewGuid());

        return new ResolvedContentItem(
            new ContentItemDefinition(
                itemId,
                name,
                new ContentItemKey(key),
                resolvedTemplateId,
                parentId),
            new ContentPath(path),
            new Dictionary<string, ContentFieldValue?>
            {
                ["title"] = new ContentFieldValue(
                    itemId,
                    new FieldId(Guid.NewGuid()),
                    "title",
                    new ContentLanguage("en"),
                    ContentVersion.First,
                    title)
            },
            new Dictionary<string, TypedFieldValue>
            {
                ["title"] = new StringTypedFieldValue(title)
            });
    }
}
