using LuisCorreiaOsteopata.WEB.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace LuisCorreiaOsteopata.WEB.Controllers.API;

[Route("api/[controller]")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly DataContext _context;

    public AccountController(DataContext context)
    {
        _context = context;
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics(string staffId = null)
    {
        var now = DateTime.Now;
        var year = now.Year;
        var culture = new System.Globalization.CultureInfo("pt-PT");

        var appointmentsQuery = _context.Appointments.AsQueryable();
        if (!string.IsNullOrEmpty(staffId))
            appointmentsQuery = appointmentsQuery.Where(a => a.Staff.User.Id == staffId);

        var ordersQuery = _context.Orders.AsQueryable();
        if (!string.IsNullOrEmpty(staffId))
            ordersQuery = ordersQuery.Where(o => o.User.Id == staffId);

        // Base month list (1–12)
        var months = Enumerable.Range(1, 12)
            .Select(m => new
            {
                MonthNumber = m,
                Month = culture.DateTimeFormat.GetAbbreviatedMonthName(m)
            })
            .ToList();

        // --- Appointments by Month ---
        var appointmentsByMonthRaw = await appointmentsQuery
            .Where(a => a.AppointmentDate.Year == year)
            .GroupBy(a => a.AppointmentDate.Month)
            .Select(g => new { MonthNumber = g.Key, Count = g.Count() })
            .ToListAsync();

        var appointmentsByMonth = months
            .GroupJoin(appointmentsByMonthRaw,
                m => m.MonthNumber,
                a => a.MonthNumber,
                (m, aGroup) => new
                {
                    m.MonthNumber,
                    m.Month,
                    Count = aGroup.FirstOrDefault()?.Count ?? 0
                })
            .OrderBy(x => x.MonthNumber)
            .ToList();

        // --- Orders by Month ---
        var ordersByMonthRaw = await ordersQuery
            .Where(o => o.OrderDate.Year == year)
            .GroupBy(o => o.OrderDate.Month)
            .Select(g => new { MonthNumber = g.Key, Count = g.Count() })
            .ToListAsync();

        var ordersByMonth = months
            .GroupJoin(ordersByMonthRaw,
                m => m.MonthNumber,
                o => o.MonthNumber,
                (m, oGroup) => new
                {
                    m.MonthNumber,
                    m.Month,
                    Count = oGroup.FirstOrDefault()?.Count ?? 0
                })
            .OrderBy(x => x.MonthNumber)
            .ToList();

        // --- Revenue by Month ---
        var revenueByMonthRaw = await ordersQuery
            .Where(o => o.OrderDate.Year == year && o.IsPaid)
            .GroupBy(o => o.OrderDate.Month)
            .Select(g => new { MonthNumber = g.Key, Revenue = g.Sum(x => x.OrderTotal) })
            .ToListAsync();

        var revenueByMonth = months
            .GroupJoin(revenueByMonthRaw,
                m => m.MonthNumber,
                r => r.MonthNumber,
                (m, rGroup) => new
                {
                    m.MonthNumber,
                    m.Month,
                    Revenue = rGroup.FirstOrDefault()?.Revenue ?? 0
                })
            .OrderBy(x => x.MonthNumber)
            .ToList();

        // --- Summary metrics ---
        var totalAppointments = await appointmentsQuery.CountAsync();
        var upcomingAppointments = await appointmentsQuery.CountAsync(a => a.AppointmentDate >= now);
        var totalOrders = await ordersQuery.CountAsync();
        var totalRevenue = await ordersQuery.Where(o => o.IsPaid).SumAsync(o => (decimal?)o.OrderTotal) ?? 0;

        var response = new
        {
            Summary = new
            {
                TotalAppointments = totalAppointments,
                UpcomingAppointments = upcomingAppointments,
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue
            },
            Charts = new
            {
                AppointmentsByMonth = appointmentsByMonth,
                OrdersByMonth = ordersByMonth,
                RevenueByMonth = revenueByMonth
            }
        };

        return Ok(response);
    }
}


