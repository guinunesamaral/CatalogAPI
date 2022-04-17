using Catalog.Api.Entities;
using Catalog.Api.Dtos;

namespace Catalog.Api
{
  public static class Extensions
  {
    public static ItemDto AsDto(this Item item)
    {
      return new ItemDto(item.Id, item.Name, item.Description, item.Price, item.CreatedDate);
    }
  }
}