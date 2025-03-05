using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using OfficeOpenXml;
using PortalWebServer.Data;
using PortalWebServer.Entity;
using System;
using System.Text.Json;

namespace PortalWebServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ExcelUploadController : ControllerBase
{
    // Dependency for interacting with Redis via IDistributedCache
    private readonly IDistributedCache _cache;
    private readonly ProductDbContext _context;

    public ExcelUploadController(IDistributedCache distributedCache,ProductDbContext context)
    {
        _cache = distributedCache;
        _context = context;
    }

    private async Task RemoveCacheAsync()
    {
        var cacheKey = "GET_ALL_PRODUCTS";
        await _cache.RemoveAsync(cacheKey);
    }

        [HttpPost()]
    public async Task<IActionResult> Post(Product productvm)
    {
        if (productvm == null) return BadRequest();
        else
        {
            await _context.AddAsync(productvm);
            await _context.SaveChangesAsync();
            return Ok(productvm);
        }
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Product productvm)
    {
        var existing_data = await _context.Products.FirstOrDefaultAsync(x => x.Id == id);
        if (existing_data == null) return NotFound();
        else
        {
            existing_data.Name = productvm.Name;
            existing_data.Price = productvm.Price;
            existing_data.Stock = productvm.Stock;
            _context.Update(existing_data);
            await _context.SaveChangesAsync();
            return Ok(existing_data);
        }
    }

    [HttpGet("getalldata")]
    public async Task<IActionResult> GetAllData()
    {
        IList<Product> products = new List<Product>();
        var cacheKey = "GET_ALL_PRODUCTS";
        try
        {
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                products = JsonSerializer.Deserialize<List<Product>>(cachedData) ?? new List<Product>();
            }
            else
            {
                products = _context.Products.ToList();
                if (products != null) //ambil dari cache datanya
                {
                    // Cache the data
                    var serializedData = JsonSerializer.Serialize(products);
                    var cacheOptions = new DistributedCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromMinutes(5));
                    await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions);
                }
            }
            await RemoveCacheAsync();
            return Ok(products);
        }
        catch (Exception ex)
        {

            return BadRequest();
        }
    }

    [HttpGet("download")]
    public async Task<IActionResult> DownloadExcel()
    {
        try
        {
            var products = await _context.Products.ToListAsync();

            if (products == null || !products.Any())
            {
                return BadRequest("Tidak ada data untuk diekspor.");
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Set license

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Products");

            // Header
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Name";
            worksheet.Cells[1, 3].Value = "Price";
            worksheet.Cells[1, 4].Value = "Stock";

            // Data
            int row = 2;
            foreach (var product in products)
            {
                worksheet.Cells[row, 1].Value = product.Id;
                worksheet.Cells[row, 2].Value = product.Name;
                worksheet.Cells[row, 3].Value = product.Price;
                worksheet.Cells[row, 4].Value = product.Stock;
                row++;
            }

            // AutoFit kolom
            worksheet.Cells.AutoFitColumns();

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Products.xlsx");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Terjadi kesalahan: {ex.Message}");
        }
    }


    [HttpPost("upload")]
    public async Task<IActionResult> UploadExcel(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File tidak boleh kosong.");

        try
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Tambahkan baris ini

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            using var package = new ExcelPackage(stream);
            ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

            int rowCount = worksheet.Dimension.Rows;
            var products = new List<Product>();

            for (int row = 2; row <= rowCount; row++) // Mulai dari baris ke-2 (lewati header)
            {
                var product = new Product
                {
                    Name = worksheet.Cells[row, 1].Value?.ToString() ?? "",
                    Price = decimal.TryParse(worksheet.Cells[row, 2].Value?.ToString(), out var price) ? price : 0,
                    Stock = int.TryParse(worksheet.Cells[row, 3].Value?.ToString(), out var stock) ? stock : 0
                };

                products.Add(product);
            }


            foreach (var item in products)
            {
                var existing_item = await _context.Products.FirstOrDefaultAsync(x => x.Name == item.Name);
                if (existing_item != null)
                {
                    _context.Products.Update(existing_item);
                }
                else
                {
                    await _context.Products.AddAsync(item);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Data berhasil diunggah dan disimpan." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Terjadi kesalahan: {ex.Message}");
        }
    }
}
