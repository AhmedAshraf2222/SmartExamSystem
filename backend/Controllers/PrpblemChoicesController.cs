using Graduation_proj.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GraduationProj.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProblemChoicesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProblemChoicesController> _logger;
        private readonly string _uploadsFolder;

        public ProblemChoicesController(ApplicationDbContext context, IWebHostEnvironment env, ILogger<ProblemChoicesController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _uploadsFolder = Path.Combine(env.WebRootPath, "Uploads");
            Directory.CreateDirectory(_uploadsFolder);
        }

        /// <summary>
        /// Retrieves a list of all problem choices.
        /// </summary>
        /// <returns>A list of problem choices with their details.</returns>
        [HttpGet]
        public async Task<IActionResult> GetProblemChoices()
        {
            try
            {
                var problemChoices = await _context.ProblemChoices
                    .Include(pc => pc.Problem)
                    .Select(pc => new ProblemChoiceDto
                    {
                        ChoiceId = pc.ChoiceId,
                        Choices = pc.Choices,
                        ChoiceImagePath = pc.ChoiceImagePath,
                        UnitOrder = pc.UnitOrder,
                        ProblemId = pc.ProblemId,
                        ProblemHeader = pc.Problem != null ? pc.Problem.ProblemHeader : null
                    })
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} problem choices.", problemChoices.Count);
                return Ok(problemChoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving problem choices.");
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving problem choices." });
            }
        }

        /// <summary>
        /// Retrieves problem choices for a specific problem by its ID.
        /// </summary>
        /// <param name="problemId">The ID of the problem to retrieve choices for.</param>
        /// <returns>A list of problem choices for the specified problem.</returns>
        [HttpGet("byProblem/{problemId}")]
        public async Task<IActionResult> GetProblemChoicesByProblem(int problemId)
        {
            try
            {
                var problemExists = await _context.Problems.AnyAsync(p => p.ProblemId == problemId);
                if (!problemExists)
                {
                    _logger.LogWarning("Problem with ID {ProblemId} not found.", problemId);
                    return NotFound(new { success = false, message = "Problem not found." });
                }

                var problemChoices = await _context.ProblemChoices
                    .Include(pc => pc.Problem)
                    .Where(pc => pc.ProblemId == problemId)
                    .Select(pc => new ProblemChoiceDto
                    {
                        ChoiceId = pc.ChoiceId,
                        Choices = pc.Choices,
                        ChoiceImagePath = pc.ChoiceImagePath,
                        UnitOrder = pc.UnitOrder,
                        ProblemId = pc.ProblemId,
                        ProblemHeader = pc.Problem != null ? pc.Problem.ProblemHeader : null
                    })
                    .OrderBy(pc => pc.UnitOrder)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} problem choices for problem ID {ProblemId}.", problemChoices.Count, problemId);
                return Ok(problemChoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving problem choices for problem ID {ProblemId}.", problemId);
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving problem choices." });
            }
        }

        /// <summary>
        /// Retrieves a specific problem choice by its ID.
        /// </summary>
        /// <param name="id">The ID of the problem choice to retrieve.</param>
        /// <returns>The problem choice details if found; otherwise, a 404 error.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProblemChoice(int id)
        {
            try
            {
                var problemChoice = await _context.ProblemChoices
                    .Include(pc => pc.Problem)
                    .FirstOrDefaultAsync(pc => pc.ChoiceId == id);

                if (problemChoice == null)
                {
                    _logger.LogWarning("Problem choice with ID {Id} not found.", id);
                    return NotFound(new { success = false, message = "Problem choice not found." });
                }

                var problemChoiceDto = new ProblemChoiceDto
                {
                    ChoiceId = problemChoice.ChoiceId,
                    Choices = problemChoice.Choices,
                    ChoiceImagePath = problemChoice.ChoiceImagePath,
                    UnitOrder = problemChoice.UnitOrder,
                    ProblemId = problemChoice.ProblemId,
                    ProblemHeader = problemChoice.Problem != null ? problemChoice.Problem.ProblemHeader : null
                };

                return Ok(problemChoiceDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving problem choice with ID {Id}.", id);
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving the problem choice." });
            }
        }

        /// <summary>
        /// Adds a new problem choice with an optional image.
        /// </summary>
        /// <param name="choiceDto">The problem choice data to add.</param>
        /// <returns>The created problem choice details with a 201 status code.</returns>
        [HttpPost]
        public async Task<IActionResult> AddProblemChoice([FromForm] CreateProblemChoiceDto choiceDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Invalid model state for AddProblemChoice: {Errors}", string.Join(", ", errors));
                return BadRequest(new { success = false, errors });
            }

            try
            {
                var problemExists = await _context.Problems.AnyAsync(p => p.ProblemId == choiceDto.ProblemId);
                if (!problemExists)
                {
                    _logger.LogWarning("Problem with ID {ProblemId} not found.", choiceDto.ProblemId);
                    return BadRequest(new { success = false, message = "Invalid Problem ID. Problem does not exist." });
                }

                string choiceImagePath = null;
                if (choiceDto.ChoiceImage != null)
                {
                    var fileExtension = Path.GetExtension(choiceDto.ChoiceImage.FileName).ToLower();
                    if (!new[] { ".jpg", ".jpeg", ".png" }.Contains(fileExtension))
                    {
                        _logger.LogWarning("Invalid image format for problem choice.");
                        return BadRequest(new { success = false, message = "Only JPG, JPEG, and PNG images are allowed." });
                    }

                    var fileName = $"{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine(_uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await choiceDto.ChoiceImage.CopyToAsync(stream);
                    }
                    choiceImagePath = $"/uploads/{fileName}";
                }

                var problemChoice = new ProblemChoice
                {
                    Choices = choiceDto.Choices,
                    ChoiceImagePath = choiceImagePath,
                    UnitOrder = choiceDto.UnitOrder,
                    ProblemId = choiceDto.ProblemId,
                    Problem = null
                };

                _context.ProblemChoices.Add(problemChoice);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Problem choice created with ID {ChoiceId}.", problemChoice.ChoiceId);
                return CreatedAtAction(nameof(GetProblemChoice), new { id = problemChoice.ChoiceId }, new
                {
                    success = true,
                    message = "Problem choice created successfully.",
                    choiceId = problemChoice.ChoiceId
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while adding problem choice.");
                return StatusCode(500, new { success = false, message = "An error occurred while adding the problem choice." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while adding problem choice.");
                return StatusCode(500, new { success = false, message = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Updates an existing problem choice with an optional image.
        /// </summary>
        /// <param name="id">The ID of the problem choice to update.</param>
        /// <param name="choiceDto">The updated problem choice data.</param>
        /// <returns>A success message if updated; otherwise, a 404 or 400 error.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> EditProblemChoice(int id, [FromForm] CreateProblemChoiceDto choiceDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Invalid model state for EditProblemChoice: {Errors}", string.Join(", ", errors));
                return BadRequest(new { success = false, errors });
            }

            try
            {
                var problemChoice = await _context.ProblemChoices.FirstOrDefaultAsync(pc => pc.ChoiceId == id);
                if (problemChoice == null)
                {
                    _logger.LogWarning("Problem choice with ID {Id} not found for update.", id);
                    return NotFound(new { success = false, message = "Problem choice not found." });
                }

                var problemExists = await _context.Problems.AnyAsync(p => p.ProblemId == choiceDto.ProblemId);
                if (!problemExists)
                {
                    _logger.LogWarning("Problem with ID {ProblemId} not found for problem choice update.", choiceDto.ProblemId);
                    return BadRequest(new { success = false, message = "Invalid Problem ID. Problem does not exist." });
                }

                string choiceImagePath = problemChoice.ChoiceImagePath;
                if (choiceDto.ChoiceImage != null)
                {
                    var fileExtension = Path.GetExtension(choiceDto.ChoiceImage.FileName).ToLower();
                    if (!new[] { ".jpg", ".jpeg", ".png" }.Contains(fileExtension))
                    {
                        _logger.LogWarning("Invalid image format for problem choice update.");
                        return BadRequest(new { success = false, message = "Only JPG, JPEG, and PNG images are allowed." });
                    }

                    var fileName = $"{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine(_uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await choiceDto.ChoiceImage.CopyToAsync(stream);
                    }
                    choiceImagePath = $"/uploads/{fileName}";

                    if (!string.IsNullOrEmpty(problemChoice.ChoiceImagePath))
                    {
                        var oldFilePath = Path.Combine(_uploadsFolder, problemChoice.ChoiceImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }
                }

                problemChoice.Choices = choiceDto.Choices;
                problemChoice.ChoiceImagePath = choiceImagePath;
                problemChoice.UnitOrder = choiceDto.UnitOrder;
                problemChoice.ProblemId = choiceDto.ProblemId;
                problemChoice.Problem = null;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Problem choice with ID {Id} updated.", id);
                return Ok(new { success = true, message = "Problem choice updated successfully." });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while updating problem choice with ID {Id}.", id);
                return StatusCode(500, new { success = false, message = "An error occurred while updating the problem choice." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating problem choice with ID {Id}.", id);
                return StatusCode(500, new { success = false, message = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Deletes a problem choice by its ID.
        /// </summary>
        /// <param name="id">The ID of the problem choice to delete.</param>
        /// <returns>A success message if deleted; otherwise, a 404 error.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProblemChoice(int id)
        {
            try
            {
                var problemChoice = await _context.ProblemChoices.FirstOrDefaultAsync(pc => pc.ChoiceId == id);
                if (problemChoice == null)
                {
                    _logger.LogWarning("Problem choice with ID {Id} not found for deletion.", id);
                    return NotFound(new { success = false, message = "Problem choice not found." });
                }

                if (!string.IsNullOrEmpty(problemChoice.ChoiceImagePath))
                {
                    var filePath = Path.Combine(_uploadsFolder, problemChoice.ChoiceImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.ProblemChoices.Remove(problemChoice);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Problem choice with ID {Id} deleted.", id);
                return Ok(new { success = true, message = "Problem choice deleted successfully." });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while deleting problem choice with ID {Id}.", id);
                return StatusCode(500, new { success = false, message = "An error occurred while deleting the problem choice." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting problem choice with ID {Id}.", id);
                return StatusCode(500, new { success = false, message = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Retrieves a list of all problems.
        /// </summary>
        /// <returns>A list of problems with their IDs and headers.</returns>
        [HttpGet("problems")]
        public async Task<IActionResult> GetProblems()
        {
            try
            {
                var problems = await _context.Problems
                    .Select(p => new
                    {
                        p.ProblemId,
                        p.ProblemHeader
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

    public class CreateProblemChoiceDto
    {
        [Required(ErrorMessage = "Choice text is required.")]
        [MaxLength(500, ErrorMessage = "Choice text cannot exceed 500 characters.")]
        public string Choices { get; set; }

        public IFormFile? ChoiceImage { get; set; }

        [Required(ErrorMessage = "Unit order is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Unit order must be a positive number.")]
        public int UnitOrder { get; set; }

        [Required(ErrorMessage = "Problem ID is required.")]
        public int ProblemId { get; set; }
    }
}