using System.Text.Json;
using EKR.API.DTOs;
using EKR.API.Models;
using EKR.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EKR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IImageStorage _imageStorage;

    public ProductsController(IProductService productService, IImageStorage imageStorage)
    {
        _productService = productService;
        _imageStorage = imageStorage;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> Search([FromQuery] string? q)
    {
        return Ok(await _productService.SearchAsync(q));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id)
    {
        var product = await _productService.GetByIdAsync(id);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<ProductDto>> Create([FromForm] string data, IFormFile? image)
    {
        try
        {
            var request = JsonSerializer.Deserialize<CreateProductRequest>(data, JsonOptions());
            if (request is null)
            {
                return BadRequest(new { message = "Некорректные данные товара." });
            }

            string? imageUrl = null;
            if (image is { Length: > 0 })
            {
                imageUrl = await _imageStorage.UploadAsync(image);
            }

            var product = await _productService.CreateAsync(request, imageUrl);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<ProductDto>> Update(Guid id, [FromForm] string data, IFormFile? image)
    {
        try
        {
            var request = JsonSerializer.Deserialize<UpdateProductRequest>(data, JsonOptions());
            if (request is null)
            {
                return BadRequest(new { message = "Некорректные данные товара." });
            }

            string? imageUrl = null;
            if (image is { Length: > 0 })
            {
                imageUrl = await _imageStorage.UploadAsync(image);
            }

            var product = await _productService.UpdateAsync(id, request, imageUrl);
            return product is null ? NotFound() : Ok(product);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _productService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    private static JsonSerializerOptions JsonOptions() => new()
    {
        PropertyNameCaseInsensitive = true
    };
}
