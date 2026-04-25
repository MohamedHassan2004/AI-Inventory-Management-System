using Inventory.Domain.Entities;
using Inventory.Domain.Interfaces;
using Inventory.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Infrastructure.Repositories
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null,
            CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .Where(p => p.Name == name)
                .Where(p => !excludeId.HasValue || p.Id != excludeId.Value)
                .AnyAsync(cancellationToken);
        }

        public async Task<bool> ExistsBySkuAsync(string sku, int? excludeId = null,
            CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .Where(p => p.SKU == sku)
                .Where(p => !excludeId.HasValue || p.Id != excludeId.Value)
                .AnyAsync(cancellationToken);
        }

        public async Task<Product?> GetWithBatchesAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .Include(p => p.Batches)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Product>> GetWithBatchesListAsync(IEnumerable<int> ids,
            CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .Include(p => p.Batches)
                .Include(p => p.Category)
                .Where(p => ids.Contains(p.Id))
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Product>> GetAllWithBatchesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .Include(p => p.Batches)
                .Include(p => p.Category)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Product>> GetLowStockProductsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .Include(p => p.Batches)
                .Include(p => p.Category)
                .Where(p => p.Batches.Where(b => b.RemainingQuantity > 0).Sum(b => b.RemainingQuantity) <=
                            p.ReorderPoint)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Product>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            var trimmed = searchTerm.Trim();
            if (trimmed.Length < 2) return [];

            var candidates = await _context.Products
                .Include(p => p.Batches)
                .Where(p =>
                    p.SKU == trimmed ||
                    EF.Functions.Like(p.Name, $"{trimmed}%") ||
                    EF.Functions.Like(p.SKU, $"{trimmed}%") ||
                    EF.Functions.Like(p.Name, $"%{trimmed}%") ||
                    EF.Functions.Like(p.SKU, $"%{trimmed}%"))
                .Take(50)
                .ToListAsync(cancellationToken);

            if (candidates.Count > 0)
                return RankResults(candidates, trimmed);

            return await FuzzySearchAsync(trimmed, cancellationToken);
        }

        private async Task<IEnumerable<Product>> FuzzySearchAsync(string term,
            CancellationToken cancellationToken)
        {
            var allProducts = await _context.Products
                .Include(p => p.Batches)
                .ToListAsync(cancellationToken);

            return allProducts
                .Where(p => IsMatch(p.Name, term) || IsMatch(p.SKU, term))
                .ToList();
        }

        private static bool IsMatch(string source, string term)
        {
            if (string.IsNullOrWhiteSpace(source)) return false;

            var sourceWords = source.ToLower().Split(' ');
            var termLower = term.ToLower();

            return sourceWords.Any(word =>
                LevenshteinDistance(word, termLower) <= GetThreshold(termLower));
        }

        private static int GetThreshold(string term) => term.Length switch
        {
            <= 3 => 0, 
            <= 5 => 1, 
            <= 8 => 2, 
            _ => 3
        };

        private static int LevenshteinDistance(string a, string b)
        {
            var m = a.Length;
            var n = b.Length;
            var dp = new int[m + 1, n + 1];

            for (var i = 0; i <= m; i++) dp[i, 0] = i;
            for (var j = 0; j <= n; j++) dp[0, j] = j;

            for (var i = 1; i <= m; i++)
                for (var j = 1; j <= n; j++)
                {
                    dp[i, j] = a[i - 1] == b[j - 1]
                        ? dp[i - 1, j - 1]
                        : 1 + Math.Min(dp[i - 1, j - 1],
                              Math.Min(dp[i - 1, j],
                                       dp[i, j - 1]));
                }

            return dp[m, n];
        }
        private static IEnumerable<Product> RankResults(List<Product> products, string term)
        {
            var termLower = term.ToLower();
            return products
                .OrderByDescending(p => p.SKU.Equals(term, StringComparison.OrdinalIgnoreCase))     
                .ThenByDescending(p => p.Name.StartsWith(term, StringComparison.OrdinalIgnoreCase)) 
                .ThenByDescending(p => p.Name.Contains(term, StringComparison.OrdinalIgnoreCase))   
                .Take(10);                                                                          
        }
    }
}
