using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers
{
    [ApiController]
    [Authorize(Policy = "Active")]
    public class ActiveApiBaseController : ApiBaseController
    {
    }
}
