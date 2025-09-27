using Microsoft.AspNetCore.Mvc;
using image_processing.Data;
using image_processing.Models;

namespace image_processing.Controllers;

[ApiController]
[Route("api/task")]
public class TaskController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public TaskController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTaskStatus(Guid id)
    {
        var task = await _dbContext.Tasks.FindAsync(id);
        if (task == null)
            return NotFound("Task not found.");

        return Ok(new
        {
            Id = task.Id,
            Status = task.Status,
            FailureReason = task.FailureReason,
            OriginUrl = task.OriginUrl,
            OutputUrl = task.OutputUrl
        });
    }
}