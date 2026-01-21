using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebServer.Data;

namespace WebServer.Controllers;

[Route("api/select-tables")]
[ApiController]
public class SelectTablesController : ControllerBase
{
    [HttpGet("{name}")]
    [Authorize(Policy = "Viewer")]
    public ActionResult<IEnumerable<SelectOption>> GetByName(string name)
    {
        var options = SelectTables.GetOptions(name);
        if (options.Count == 0)
        {
            return NotFound();
        }

        return Ok(options);
    }

    [HttpGet]
    [Authorize(Policy = "Viewer")]
    public ActionResult<IDictionary<string, IReadOnlyList<SelectOption>>> GetAll()
    {
        return Ok(SelectTables.GetAllTables());
    }
}
