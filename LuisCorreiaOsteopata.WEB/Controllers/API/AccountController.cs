using LuisCorreiaOsteopata.WEB.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        // Base queries
        var appointmentsQuery = _context.Appointments.AsQueryable();
        if (!string.IsNullOrEmpty(staffId))
            appointmentsQuery = appointmentsQuery.Where(a => a.Staff.User.Id == staffId);

        var ordersQuery = _context.Orders.AsQueryable();
        if (!string.IsNullOrEmpty(staffId))
            ordersQuery = ordersQuery.Where(o => o.User.Id == staffId);

        var now = DateTime.Now;

        // --- Appointments Metrics ---
        var totalAppointments = await appointmentsQuery.CountAsync();
        var upcomingAppointments = await appointmentsQuery.CountAsync(a => a.AppointmentDate >= now);

        var appointmentsPerWeek = await appointmentsQuery.CountAsync(a => EF.Functions.DateDiffWeek(a.AppointmentDate, now) == 0);
        var appointmentsPerMonth = await appointmentsQuery.CountAsync(a => a.AppointmentDate.Month == now.Month && a.AppointmentDate.Year == now.Year);
        var appointmentsPerYear = await appointmentsQuery.CountAsync(a => a.AppointmentDate.Year == now.Year);

        var appointmentsPerStaff = await _context.Appointments
            .GroupBy(a => a.StaffId)
            .Select(g => new { StaffId = g.Key, Count = g.Count() })
            .ToListAsync();

        // --- Orders Metrics ---
        var totalOrders = await ordersQuery.CountAsync();
        var ordersPerWeek = await ordersQuery.CountAsync(o => EF.Functions.DateDiffWeek(o.OrderDate, now) == 0);
        var ordersPerMonth = await ordersQuery.CountAsync(o => o.OrderDate.Month == now.Month && o.OrderDate.Year == now.Year);
        var ordersPerYear = await ordersQuery.CountAsync(o => o.OrderDate.Year == now.Year);

        var totalRevenue = await ordersQuery
            .Where(o => o.IsPaid)
            .SumAsync(o => (decimal?)o.OrderTotal) ?? 0;

        // --- Staff List ---
        var staffList = await _context.Staff
            .Select(s => new { s.Id, s.FullName })
            .ToListAsync();

        // --- Build response ---
        var response = new
        {
            TotalAppointments = totalAppointments,
            UpcomingAppointments = upcomingAppointments,
            AppointmentsPerWeek = appointmentsPerWeek,
            AppointmentsPerMonth = appointmentsPerMonth,
            AppointmentsPerYear = appointmentsPerYear,
            AppointmentsPerStaff = appointmentsPerStaff,

            TotalOrders = totalOrders,
            OrdersPerWeek = ordersPerWeek,
            OrdersPerMonth = ordersPerMonth,
            OrdersPerYear = ordersPerYear,
            TotalRevenue = totalRevenue,

            StaffList = staffList
        };

        return Ok(response);
    }
}


