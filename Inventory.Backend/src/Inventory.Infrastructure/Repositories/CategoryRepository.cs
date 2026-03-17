using Inventory.Domain.Entities;
using Inventory.Domain.Interfaces;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Infrastructure.Repositories
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        private readonly ApplicationDbContext _context;

        public CategoryRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default)
        {
            return await _context.Categories
                .Where(c => c.Name == name)
                .Where(c => !excludeId.HasValue || c.Id != excludeId.Value)
                .AnyAsync(cancellationToken);
        }
    }
}
