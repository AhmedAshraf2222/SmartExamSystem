using Graduation_proj.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Graduation_proj.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProblemsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProblemsController> _logger;
        private readonly string _uploadsFolder;

        public ProblemsController(ApplicationDbContext context, IWebHostEnvironment env, ILogger<ProblemsController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _uploadsFolder = Path.Combine(env.WebRootPath, "Uploads");
            Directory.CreateDirectory(_uploadsFolder);
        }

        /// <summary>
        /// Retrieves a list of all problems.
        /// </summary>
        /// <returns>A list of problems with their details.</returns>
        [HttpGet]
        public async Task<IActionResult> GetProblems()
        {
            try
            {
                var problems = await _context.Problems
                    .Include(p => p.Group)
                    .Select(p => new ProblemDto
                    {
                        ProblemName = p.ProblemName,
                        ProblemId = p.ProblemId,
                        ProblemHeader = p.ProblemHeader,
                        ProblemImagePath = p.ProblemImagePath,
                        RightAnswer = p.RightAnswer,
                        Shuffle = p.Shuffle,
                        GroupId = p.GroupId,
                        GroupName = p.Group != null ? p.Group.GroupName : null,
                        MainDegree = p.Group != null ? p.Group.MainDegree : null,
                        Choices = p.ProblemChoices.Select(pc => new ProblemChoiceDto
                        {
                            ChoiceId = pc.ChoiceId,
                            Choices = pc.Choices,
                            ChoiceImagePath = pc.ChoiceImagePath,
                            UnitOrder = pc.UnitOrder,
                            ProblemId = pc.ProblemId,
                            ProblemHeader = p.ProblemHeader
                        }).OrderBy(pc => pc.UnitOrder).ToList()
                    })
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} problems.", problems.Count);
                return Ok(problems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving problems.");
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving problems." });
            }
        }

        /// <summary>
        /// Retrieves a specific problem by its ID.
        /// </summary>
        /// <param name="id">The ID of the problem to retrieve.</param>
        /// <returns>The problem details if found; otherwise, a 404 error.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProblem(int id)
        {
            try
            {
                var problem = await _context.Problems
                    .Include(p => p.Group)
                    .Include(p => p.ProblemChoices)
                    .FirstOrDefaultAsync(p => p.ProblemId == id);

                if (problem == null)
                {
                    _logger.LogWarning("Problem with ID {Id} not found.", id);
                    return NotFound(new { success = false, message = "Problem not found." });
                }

                var problemDto = new ProblemDto
                {
                    ProblemName = problem.ProblemName,
                    ProblemId = problem.ProblemId,
                    ProblemHeader = problem.ProblemHeader,
                    ProblemImagePath = problem.ProblemImagePath,
                    RightAnswer = problem.RightAnswer,
                    Shuffle = problem.Shuffle,
                    GroupId = problem.GroupId,
                    GroupName = problem.Group != null ? problem.Group.GroupName : null,
                    MainDegree = problem.Group != null ? problem.Group.MainDegree : null,
                    Choices = problem.ProblemChoices.Select(pc => new ProblemChoiceDto
                    {
                        ChoiceId = pc.ChoiceId,
                        Choices = pc.Choices,
                        ChoiceImagePath = pc.ChoiceImagePath,
                        UnitOrder = pc.UnitOrder,
                        ProblemId = pc.ProblemId,
                        ProblemHeader = problem.ProblemHeader
                    }).OrderBy(pc => pc.UnitOrder).ToList()
                };

                return Ok(problemDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving problem with ID {Id}.", id);
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving the problem." });
            }
        }

        /// <summary>
        /// Retrieves all choices for a specific problem by its ID.
        /// </summary>
        /// <param name="problemId">The ID of the problem to retrieve choices for.</param>
        /// <returns>A list of choices for the specified problem.</returns>
        [HttpGet("ProblemChoices/{problemId}")]
        public async Task<IActionResult> GetProblemChoices(int problemId)
        {
            try
            {
                var problemExists = await _context.Problems.AnyAsync(p => p.ProblemId == problemId);
                if (!problemExists)
                {
                    _logger.LogWarning("Problem with ID {ProblemId} not found for retrieving choices.", problemId);
                    return NotFound(new { success = false, message = "Problem not found." });
                }

                var choices = await _context.ProblemChoices
                    .Where(pc => pc.ProblemId == problemId)
                    .Select(pc => new ProblemChoiceDto
                    {
                        ChoiceId = pc.ChoiceId,
                        Choices = pc.Choices,
                        ChoiceImagePath = pc.ChoiceImagePath,
                        UnitOrder = pc.UnitOrder,
                        ProblemId = pc.ProblemId,
                        ProblemHeader = pc.Problem.ProblemHeader
                    })
                    .OrderBy(pc => pc.UnitOrder)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} choices for problem with ID {ProblemId}.", choices.Count, problemId);
                return Ok(choices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving choices for problem with ID {ProblemId}.", problemId);
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving the choices." });
            }
        }

        /// <summary>
        /// Adds a new problem with an optional image.
        /// </summary>
        /// <param name="problemDto">The problem data to add.</param>
        /// <returns>The created problem details with a 201 status code.</returns>
        [HttpPost]
        public async Task<IActionResult> AddProblem([FromForm] CreateProblemDto problemDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Invalid model state for AddProblem: {Errors}", string.Join(", ", errors));
                return BadRequest(new { success = false, errors });
            }

            try
            {
                var groupExists = await _context.Groups.AnyAsync(g => g.GroupId == problemDto.GroupId);
                if (!groupExists)
                {
                    _logger.LogWarning("Group with ID {GroupId} not found.", problemDto.GroupId);
                    return BadRequest(new { success = false, message = "Invalid Group ID. Group does not exist." });
                }

                string problemImagePath = null;
                if (problemDto.ProblemImage != null)
                {
                    var fileExtension = Path.GetExtension(problemDto.ProblemImage.FileName).ToLower();
                    if (!new[] { ".jpg", ".jpeg", ".png" }.Contains(fileExtension))
                    {
                        _logger.LogWarning("Invalid image format for problem.");
                        return BadRequest(new { success = false, message = "Only JPG, JPEG, and PNG images are allowed." });
                    }

                    var fileName = $"{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine(_uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await problemDto.ProblemImage.CopyToAsync(stream);
                    }
                    problemImagePath = $"/Uploads/{fileName}";
                }

                var problem = new Problem
                {
                    ProblemName = problemDto.ProblemName,
                    ProblemHeader = problemDto.ProblemHeader,
                    ProblemImagePath = problemImagePath,
                    RightAnswer = problemDto.RightAnswer,
                    Shuffle = problemDto.Shuffle,
                    GroupId = problemDto.GroupId,
                    Group = null
                };

                _context.Problems.Add(problem);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Problem created with ID {ProblemId}.", problem.ProblemId);
                return CreatedAtAction(nameof(GetProblem), new { id = problem.ProblemId }, new
                {
                    success = true,
                    message = "Problem created successfully.",
                    problemId = problem.ProblemId
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while adding problem.");
                return StatusCode(500, new { success = false, message = "An error occurred while adding the problem." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while adding problem.");
                return StatusCode(500, new { success = false, message = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Updates an existing problem with an optional image.
        /// </summary>
        /// <param name="id">The ID of the problem to update.</param>
        /// <param name="problemDto">The updated problem data.</param>
        /// <returns>A success message if updated; otherwise, a 404 or 400 error.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> EditProblem(int id, [FromForm] CreateProblemDto problemDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Invalid model state for EditProblem: {Errors}", string.Join(", ", errors));
                return BadRequest(new { success = false, errors });
            }

            try
            {
                var problem = await _context.Problems.FirstOrDefaultAsync(p => p.ProblemId == id);
                if (problem == null)
                {
                    _logger.LogWarning("Problem with ID {Id} not found for update.", id);
                    return NotFound(new { success = false, message = "Problem not found." });
                }

                var groupExists = await _context.Groups.AnyAsync(g => g.GroupId == problemDto.GroupId);
                if (!groupExists)
                {
                    _logger.LogWarning("Group with ID {GroupId} not found for problem update.", problemDto.GroupId);
                    return BadRequest(new { success = false, message = "Invalid Group ID. Group does not exist." });
                }

                string problemImagePath = problem.ProblemImagePath;
                if (problemDto.ProblemImage != null)
                {
                    var fileExtension = Path.GetExtension(problemDto.ProblemImage.FileName).ToLower();
                    if (!new[] { ".jpg", ".jpeg", ".png" }.Contains(fileExtension))
                    {
                        _logger.LogWarning("Invalid image format for problem update.");
                        return BadRequest(new { success = false, message = "Only JPG, JPEG, and PNG images are allowed." });
                    }

                    var fileName = $"{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine(_uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await problemDto.ProblemImage.CopyToAsync(stream);
                    }
                    problemImagePath = $"/Uploads/{fileName}";

                    if (!string.IsNullOrEmpty(problem.ProblemImagePath))
                    {
                        var oldFilePath = Path.Combine(_uploadsFolder, problem.ProblemImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }
                }

                problem.ProblemName = problemDto.ProblemName;
                problem.ProblemHeader = problemDto.ProblemHeader;
                problem.ProblemImagePath = problemImagePath;
                problem.RightAnswer = problemDto.RightAnswer;
                problem.Shuffle = problemDto.Shuffle;
                problem.GroupId = problemDto.GroupId;
                problem.Group = null;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Problem with ID {Id} updated.", id);
                return Ok(new { success = true, message = "Problem updated successfully." });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while updating problem with ID {Id}.", id);
                return StatusCode(500, new { success = false, message = "An error occurred while updating the problem." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating problem with ID {Id}.", id);
                return StatusCode(500, new { success = false, message = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Deletes a problem by its ID.
        /// </summary>
        /// <param name="id">The ID of the problem to delete.</param>
        /// <returns>A success message if deleted; otherwise, a 404 error.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProblem(int id)
        {
            try
            {
                var problem = await _context.Problems.FirstOrDefaultAsync(p => p.ProblemId == id);
                if (problem == null)
                {
                    _logger.LogWarning("Problem with ID {Id} not found for deletion.", id);
                    return NotFound(new { success = false, message = "Problem not found." });
                }

                if (!string.IsNullOrEmpty(problem.ProblemImagePath))
                {
                    var filePath = Path.Combine(_uploadsFolder, problem.ProblemImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.Problems.Remove(problem);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Problem with ID {Id} deleted.", id);
                return Ok(new { success = true, message = "Problem deleted successfully." });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while deleting problem with ID {Id}.", id);
                return StatusCode(500, new { success = false, message = "An error occurred while deleting the problem." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting problem with ID {Id}.", id);
                return StatusCode(500, new { success = false, message = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Retrieves a list of all groups.
        /// </summary>
        /// <returns>A list of groups with their IDs and names.</returns>
        [HttpGet("groups")]
        public async Task<IActionResult> GetGroups()
        {
            try
            {
                var groups = await _context.Groups
                    .Select(g => new
                    {
                        g.GroupId,
                        g.GroupName
                    })
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} groups.", groups.Count);
                return Ok(groups);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving groups.");
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving groups." });
            }
        }
    }

    public class ProblemDto
    {
        public int ProblemId { get; set; }
        public string ProblemName { get; set; }
        public string ProblemHeader { get; set; }
        public string? ProblemImagePath { get; set; }
        public int RightAnswer { get; set; }
        public bool Shuffle { get; set; }
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public int? MainDegree { get; set; }
        public List<ProblemChoiceDto> Choices { get; set; } = new List<ProblemChoiceDto>();
    }

    public class ProblemChoiceDto
    {
        public int ChoiceId { get; set; }
        public string Choices { get; set; }
        public string ChoiceImagePath { get; set; }
        public int UnitOrder { get; set; }
        public int ProblemId { get; set; }
        public string ProblemHeader { get; set; }
    }

    public class CreateProblemDto
    {
        [Required(ErrorMessage = "Problem name is required.")]
        [MaxLength(40)]
        public string ProblemName { get; set; }
        [Required(ErrorMessage = "Problem header is required.")]
        [MaxLength(1000, ErrorMessage = "Problem header cannot exceed 1000 characters.")]
        public string ProblemHeader { get; set; }

        public IFormFile? ProblemImage { get; set; }

        [Required(ErrorMessage = "Right answer is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Right answer must be a positive number.")]
        public int RightAnswer { get; set; }

        public bool Shuffle { get; set; }

        [Required(ErrorMessage = "Group ID is required.")]
        public int GroupId { get; set; }
    }
}