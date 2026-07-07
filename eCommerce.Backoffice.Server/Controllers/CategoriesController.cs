using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eCommerce.Backoffice.Shared.Model.Products;
using eCommerce.Backoffice.Shared.Services.Interfaces;
using eCommerce.Storefront.Model.Products;
using eCommerce.Storefront.Services.Cache;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eCommerce.Backoffice.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    [IgnoreAntiforgeryToken]
    public class CategoriesController : ControllerBase
    {
        private readonly IEntityService<Category, long> _categoryService;
        private readonly ICacheStorage _cacheStorage;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(IEntityService<Category, long> categoryService,
            ICacheStorage cacheStorage,
            ILogger<CategoriesController> logger)
        {
            _categoryService = categoryService;
            _cacheStorage = cacheStorage;
            _logger = logger;
        }

        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public IEnumerable<CategoryDto> GetCategories()
        {
            return _categoryService.Get().Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name
            });
        }

        [HttpGet("{id}")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<ActionResult<CategoryDto>> GetCategory(int id)
        {
            var category = await _categoryService.GetAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name
            };
        }

        [HttpPost]
        public async Task<ActionResult<CategoryDto>> CreateCategory(CategoryDto category)
        {
            try
            {
                var c = await _categoryService.CreateAsync(new Category
                {
                    Id = category.Id,
                    Name = category.Name
                });

                category.Id = c.Id;

                _cacheStorage.Remove(CacheKeys.AllCategories.ToString());
            }
            catch (DbUpdateException ex)
            {
                return HandleDbUpdateException(ex);
            }

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, CategoryDto category)
        {
            if (id != category.Id)
            {
                return BadRequest();
            }

            try
            {
                await _categoryService.ModifyAsync(new Category
                {
                    Id = category.Id,
                    Name = category.Name
                });
                _cacheStorage.Remove(CacheKeys.AllCategories.ToString());
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
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                await _categoryService.DeleteAsync(id);
                _cacheStorage.Remove(CacheKeys.AllCategories.ToString());
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