using Invoyz.InvoiceService.Contracts.RestAPI.OutboundContracts;
using Microsoft.AspNetCore.Mvc;

namespace Invoyz.InvoiceService.Controllers
{
    public class DocumentsController() : BaseController(null)
    {
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CustomerContract))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get(
            [FromRoute] Guid id,
            CancellationToken cancellationToken)
        {
            //This is a very simple way to return the file. 
            //If this is a production ready solution, I will not return byte[], but use instead a CDN and in this endpoint return the CDN url for the blob.

            var basePath = Path.Combine(Environment.CurrentDirectory, $"Invoices");
            var filePath = Path.Combine(basePath, $"{id}.pdf");

            var fileBa = System.IO.File.ReadAllBytes(filePath);

            if (fileBa == null)
                return NoContent();

            return File(fileBa,"application/pdf", $"{id}.pdf");
        }

    }
}
