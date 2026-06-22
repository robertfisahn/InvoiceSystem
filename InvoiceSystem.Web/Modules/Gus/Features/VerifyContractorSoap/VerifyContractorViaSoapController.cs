using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceSystem.Web.Modules.Gus.Features.VerifyContractorSoap;

[ApiController]
[Route("api/contractors")]
public sealed class VerifyContractorViaSoapController(IMediator mediator) : ControllerBase
{
    [HttpPost("{id:int}/verify-soap")]
    public async Task<IActionResult> Verify(int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new VerifyContractorViaSoapCommand(id), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new
        {
            name = result.Name,
            regon = result.Regon,
            address = result.Address,
            statusVat = result.StatusVat,
            checkedAt = result.CheckedAt.ToString("yyyy-MM-dd HH:mm:ss")
        });
    }
}
