using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eCommerce.Backoffice.Shared.Model.Products;
using eCommerce.Backoffice.Shared.Services.Interfaces;
using eCommerce.Storefront.Model.Products;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Backoffice.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    [IgnoreAntiforgeryToken]
    public class SizesController : ControllerBase
    {
        private readonly IEntityService<ProductSize, long> _sizeService;

        public SizesController(IEntityService<ProductSize, long> sizeService)
        {
            _sizeService = sizeService;
        }

        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public IEnumerable<ProductSizeDto> GetSizes()
        {
            return _sizeService.Get().Select(p => new ProductSizeDto
            {
                Id = p.Id,
                Name = p.Name
            });
        }

        [HttpGet("{id}")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<ActionResult<ProductSizeDto>> GetSize(int id)
        {
            var productSize = await _sizeService.GetAsync(id);

            if (productSize == null)
            { 
                return NotFound();
            }

            return new ProductSizeDto { Id = productSize.Id, Name = productSize.Name };
        }

        [HttpPost]
        public async Task<ActionResult<ProductSizeDto>> CreateSize(ProductSizeDto size)
        {
            try
            {
                var productSize = await _sizeService.CreateAsync(new ProductSize { Id = size.Id, Name = size.Name });

                size.Id = productSize.Id;
            }
            catch (DbUpdateException ex)
            {
                if (ex?.InnerException?.Message != null)
                {
                    return BadRequest(ex?.InnerException?.Message);
                }
                else
                {
                    return BadRequest(ex?.Message);
                }
            }            

            return CreatedAtAction(nameof(GetSize), new { id = size.Id }, size);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSize(int id, ProductSizeDto size)
        {
            if (id != size.Id)
            {
                return BadRequest();
            }

            try
            {
                await _sizeService.ModifyAsync(new ProductSize { Id = size.Id, Name = size.Name });
            }
            catch (DbUpdateConcurrencyException)
            {
                return NotFound();
            }
            catch (DbUpdateException ex)
            {
                if (ex?.InnerException?.Message != null)
                {
                    return BadRequest(ex?.InnerException?.Message);
                }
                else
                {
                    return BadRequest(ex?.Message);
                }
            }   

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSize(int id)
        {
            try
            {
                await _sizeService.DeleteAsync(id);
            }
            catch (DbUpdateException ex)
            {
                if (ex?.InnerException?.Message != null)
                {
                    return BadRequest(ex?.InnerException?.Message);
                }
                else
                {
                    return BadRequest(ex?.Message);
                }
            }   

            return NoContent();
        }
    }
}