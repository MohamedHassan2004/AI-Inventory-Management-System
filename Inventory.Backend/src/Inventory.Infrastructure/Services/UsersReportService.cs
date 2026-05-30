using System;
using System.Collections.Generic;
using System.Text;

using Inventory.Application.DTOs.Reports.Users;
using Inventory.Application.Interfaces.Services;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Services;

public class UsersReportService : IUsersReportService
{
    private readonly ApplicationDbContext _dbContext;

    public UsersReportService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<CashierSalesDto>> GetCashierSalesAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Cashier)
            .Where(o =>
                o.Status == OrderStatus.Completed &&
                o.OrderDate >= startDate &&
                o.OrderDate <= endDate)
            .GroupBy(o => new
            {
                o.CashierId,
                o.Cashier.FullName
            })
            .Select(g => new CashierSalesDto
            {
                CashierId = g.Key.CashierId,

                CashierName = g.Key.FullName,

                TotalOrders = g.Count(),

                TotalRevenue = g.Sum(x => x.FinalTotal)
            })
            .OrderByDescending(x => x.TotalRevenue)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CashierDto>> GetCashiersAsync(CancellationToken cancellationToken)
    {
       var cashiers = await (
                from user in _dbContext.Users
                join userRole in _dbContext.UserRoles
                    on user.Id equals userRole.UserId
                join role in _dbContext.Roles
                    on userRole.RoleId equals role.Id
                where role.Name == UserRole.Cashier.ToString()
                select new CashierDto
                {
                    Id = user.Id,
                    Name = user.FullName
                })
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        return cashiers;
    }

    public async Task<IEnumerable<UserStatusBreakdownDto>> GetUserStatusBreakdownAsync(
    CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .GroupBy(u => u.AccountStatus)
            .Select(g => new UserStatusBreakdownDto
            {
                Status = g.Key.ToString(),

                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);
    }
}