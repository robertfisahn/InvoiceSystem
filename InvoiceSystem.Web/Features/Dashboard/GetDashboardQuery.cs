using MediatR;

namespace InvoiceSystem.Web.Features.Dashboard;

public sealed record GetDashboardQuery : IRequest<DashboardViewModel>;
