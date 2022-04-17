using Microsoft.AspNetCore.Mvc;
using Catalog.Api.Repositories;
using Catalog.Api.Entities;
using Catalog.Api.Dtos;

namespace Catalog.Api.Controllers
{
  [ApiController]
  [Route("items")]
  public class ItemsController : ControllerBase
  {
    private readonly IItemsRepository _repository;
    private readonly ILogger<ItemsController> _logger;

    public ItemsController(IItemsRepository repository, ILogger<ItemsController> logger)
    {
      _repository = repository;
      _logger = logger;
    }

    [HttpGet]
    public async Task<IEnumerable<ItemDto>> GetItemsAsync(string? name = null)
    {
      var items = (await _repository.GetItemsAsync()).Select(item => item.AsDto());

      if (!string.IsNullOrEmpty(name))
      {
        items = items.Where(item => item.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
      }
      _logger.LogInformation($"{DateTime.UtcNow.ToString("hh:mm:ss")}: Retrieved {items.Count()} items");

      return items;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ItemDto>> GetItemAsync(Guid id)
    {
      var item = await _repository.GetItemAsync(id);
      if (item is null)
      {
        return NotFound();
      }
      return item.AsDto();
    }

    [HttpPost]
    public async Task<ActionResult<ItemDto>> CreateItemAsync(CreateItemDto itemDto)
    {
      Item item = new()
      {
        Id = Guid.NewGuid(),
        Name = itemDto.Name,
        Price = itemDto.Price,
        CreatedDate = DateTimeOffset.UtcNow
      };

      await _repository.CreateItemAsync(item);

      return CreatedAtAction(nameof(GetItemAsync), new { id = item.Id }, item.AsDto());
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateItemAsync(Guid id, UpdateItemDto itemDto)
    {
      var existingItem = await _repository.GetItemAsync(id);

      if (existingItem is null)
      {
        return NotFound();
      }

      Item updatedItem = existingItem with
      {
        Name = itemDto.Name,
        Price = itemDto.Price
      };

      await _repository.UpdateItemAsync(updatedItem);

      return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteItemAsync(Guid id)
    {
      var existingItem = await _repository.GetItemAsync(id);

      if (existingItem is null)
      {
        return NotFound();
      }

      await _repository.DeleteItemAsync(id);

      return NoContent();
    }
  }
}