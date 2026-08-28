using System.Collections.Generic;
using System.Linq;
using eCommerce.Backoffice.Shared.Model.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using eCommerce.Storefront.Model.Products;
using eCommerce.Backoffice.Shared.Services.Interfaces;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace eCommerce.Backoffice.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    [IgnoreAntiforgeryToken]
    public class BrandsController(IEntityService<Brand, long> brandService, ILogger<BrandsController> logger) : ControllerBase
    {
        private readonly IEntityService<Brand, long> _brandService = brandService;
        private readonly ILogger<BrandsController> _logger = logger;

        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public IEnumerable<BrandDto> GetBrands()
        {
            return _brandService.Get().Select(b => new BrandDto
            {
                Id = b.Id,
                Name = b.Name
            });
        }

        [HttpGet("{id}")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<ActionResult<BrandDto>> GetBrand(int id)
        {
            var brand = await _brandService.GetAsync(id);

            if (brand == null)
            {
                return NotFound();
            }

            return new BrandDto
            {
                Id = brand.Id,
                Name = brand.Name
            };
        }

        [HttpPost]
        public async Task<ActionResult<BrandDto>> CreateBrand(BrandDto brand)
        {
            try
            {
                var b = await _brandService.CreateAsync(new Brand
                {
                    Id = brand.Id,
                    Name = brand.Name
                });

                brand.Id = b.Id;
            }
            catch (DbUpdateException ex)
            {
                return HandleDbUpdateException(ex);
            }

            return CreatedAtAction(nameof(GetBrand), new { id = brand.Id }, brand);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBrand(int id, BrandDto brand)
        {
            if (id != brand.Id)
            {
                return BadRequest();
            }

            try
            {
                await _brandService.ModifyAsync(new Brand
                {
                    Id = brand.Id,
                    Name = brand.Name
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                return NotFound();
            }
            catch (DbUpdateException ex)
            {
                return HandleDbUpdateException(ex);
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBrand(int id)
        {
            try
            {
                await _brandService.DeleteAsync(id);
            }
            catch (DbUpdateException ex)
            {
                return HandleDbUpdateException(ex);
            }

            return NoContent();
        }

        private BadRequestObjectResult HandleDbUpdateException(DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update failed.");

            return BadRequest("The operation could not be completed. Please check your input and try again.");
        }
    }
}