using MediatR;

namespace InvoiceSystem.Web.Modules.Dashboard.Features.Dashboard;

public sealed record GetDashboardQuery : IRequest<DashboardViewModel>;
