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
    public class ColorsController : ControllerBase
    {
        private readonly IEntityService<ProductColor, long> _colorService;

        public ColorsController(IEntityService<ProductColor, long> colorService)
        {
            _colorService = colorService;
        }

        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public IEnumerable<ProductColorDto> GetColors()
        {
            return _colorService.Get().Select(p => new ProductColorDto
            {
                Id = p.Id,
                Name = p.Name
            });
        }

        [HttpGet("{id}")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<ActionResult<ProductColorDto>> GetColor(int id)
        {
            var productColor = await _colorService.GetAsync(id);

            if (productColor == null)
            { 
                return NotFound();
            }

            return new ProductColorDto 
            { 
                Id = productColor.Id, 
                Name = productColor.Name 
            };
        }

        [HttpPost]
        public async Task<ActionResult<ProductColorDto>> CreateColor(ProductColorDto color)
        {
            try
            {
                var productColor = await _colorService.CreateAsync(new ProductColor 
                { 
                    Id = color.Id, 
                    Name = color.Name 
                });

                color.Id = productColor.Id;
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

            return CreatedAtAction(nameof(GetColor), new { id = color.Id }, color);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateColor(int id, ProductColorDto color)
        {
            if (id != color.Id)
            {
                return BadRequest();
            }

            try
            {
                await _colorService.ModifyAsync(new ProductColor 
                { 
                    Id = color.Id, 
                    Name = color.Name 
                });
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
        public async Task<IActionResult> DeleteColor(int id)
        {
            try
            {
                await _colorService.DeleteAsync(id);
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