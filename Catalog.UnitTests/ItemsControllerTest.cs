using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using Moq;
using Catalog.Api.Repositories;
using Catalog.Api.Entities;
using Catalog.Api.Controllers;
using Catalog.Api.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using FluentAssertions;

namespace Catalog.UnitTests;

public class ItemControllerTest
{
  private readonly Mock<IItemsRepository> repositoryStub = new();
  private readonly Mock<ILogger<ItemsController>> loggerStub = new();
  private readonly Random rand = new();

  [Fact]
  public async Task GetItemAsync_WithUnexistingItem_ReturnsNotFound()
  {
    repositoryStub.Setup(repo => repo.GetItemAsync(It.IsAny<Guid>())).ReturnsAsync((Item)null);

    var controller = new ItemsController(repositoryStub.Object, loggerStub.Object);
    var result = await controller.GetItemAsync(Guid.NewGuid());

    Assert.IsType<NotFoundResult>(result.Result);
  }

  [Fact]
  public async Task GetItemAsync_WithExistingItem_ReturnsExpectedItem()
  {
    Item expectedItem = CreateRandomItem();

    repositoryStub.Setup(repo => repo.GetItemAsync(It.IsAny<Guid>())).ReturnsAsync(expectedItem);

    var controller = new ItemsController(repositoryStub.Object, loggerStub.Object);
    var result = await controller.GetItemAsync(Guid.NewGuid());

    result.Value.Should().BeEquivalentTo(expectedItem);
  }

  [Fact]
  public async Task GetItemsAsync_WithExistingItems_ReturnsAllItems()
  {
    var expectedItems = new[] { CreateRandomItem(), CreateRandomItem(), CreateRandomItem() };

    repositoryStub.Setup(repo => repo.GetItemsAsync()).ReturnsAsync(expectedItems);

    var controller = new ItemsController(repositoryStub.Object, loggerStub.Object);
    var actualItems = await controller.GetItemsAsync();

    actualItems.Should().BeEquivalentTo(expectedItems);
  }

  [Fact]
  public async Task GetItemsAsync_WithMatchingItems_ReturnsMatchingItems()
  {
    var allItems = new[]{
      new Item(){Name="Potion"},
      new Item(){Name="Antidote"},
      new Item(){Name="Hi-Potion"},
    };

    var nameToMatch = "Potion";

    repositoryStub.Setup(repo => repo.GetItemsAsync()).ReturnsAsync(allItems);

    var controller = new ItemsController(repositoryStub.Object, loggerStub.Object);

    IEnumerable<ItemDto> foundItems = await controller.GetItemsAsync(nameToMatch);

    foundItems.Should().OnlyContain(item => item.Name == allItems[0].Name || item.Name == allItems[2].Name);
  }

  [Fact]
  public async Task CreateItemAsync_WithItemToCreate_ReturnsCreatedItem()
  {
    var itemToCreate = new CreateItemDto(Name: Guid.NewGuid().ToString(), null, Price: rand.Next(1000));

    var controller = new ItemsController(repositoryStub.Object, loggerStub.Object);

    var result = await controller.CreateItemAsync(itemToCreate);

    var createdItem = (result.Result as CreatedAtActionResult).Value as ItemDto;
    itemToCreate.Should().BeEquivalentTo(
      createdItem,
      options => options.ComparingByMembers<ItemDto>().ExcludingMissingMembers()
    );
    createdItem.Id.Should().NotBeEmpty();
    createdItem.CreatedDate.Should().BeCloseTo(DateTimeOffset.UtcNow, new TimeSpan(10000000));
  }

  [Fact]
  public async Task UpdateItemAsync_WithExistingItem_ReturnsNoContent()
  {
    Item existingItem = CreateRandomItem();
    repositoryStub.Setup(repo => repo.GetItemAsync(It.IsAny<Guid>())).ReturnsAsync(existingItem);

    var itemId = existingItem.Id;
    var itemToUpdate = new UpdateItemDto(Name: Guid.NewGuid().ToString(), "", Price: existingItem.Price + 3);

    var controller = new ItemsController(repositoryStub.Object, loggerStub.Object);

    var result = await controller.UpdateItemAsync(itemId, itemToUpdate);

    result.Should().BeOfType<NoContentResult>();
  }

  [Fact]
  public async Task DeleteItemAsync_WithExistingItem_ReturnsNoContent()
  {
    Item existingItem = CreateRandomItem();
    repositoryStub.Setup(repo => repo.GetItemAsync(It.IsAny<Guid>())).ReturnsAsync(existingItem);

    var controller = new ItemsController(repositoryStub.Object, loggerStub.Object);

    var result = await controller.DeleteItemAsync(existingItem.Id);

    result.Should().BeOfType<NoContentResult>();
  }

  private Item CreateRandomItem()
  {
    return new()
    {
      Id = Guid.NewGuid(),
      Name = Guid.NewGuid().ToString(),
      Price = rand.Next(1000),
      CreatedDate = DateTimeOffset.UtcNow
    };
  }
}