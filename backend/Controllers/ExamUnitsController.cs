using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Graduation_proj.Models;
using System.ComponentModel.DataAnnotations;

namespace Graduation_proj.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExamUnitsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ExamUnitsController> _logger;

        public ExamUnitsController(ApplicationDbContext context, ILogger<ExamUnitsController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<IActionResult> GetExamUnits()
        {
            try
            {
                var examUnits = await _context.ExamUnits
                    .Include(e => e.Exam)
                    .Include(e => e.Group)
                    .Select(e => new ExamUnitDto
                    {
                        UnitOrder = e.UnitOrder,
                        ExamId = e.ExamId,
                        ExamName = e.Exam != null ? e.Exam.ExamName : "N/A",
                        GroupId = e.GroupId,
                        GroupName = e.Group != null ? e.Group.GroupName : "N/A",
                        MainDegree = e.MainDegree,
                        TotalProblems = e.TotalProblems,
                        Shuffle = e.Shuffle,
                        AllProblems = e.AllProblems
                    })
                    .AsNoTracking()
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} exam units.", examUnits.Count);
                return Ok(new { success = true, data = examUnits });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "retrieving exam units");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetExamUnit(int id)
        {
            try
            {
                var examUnit = await _context.ExamUnits
                    .Include(e => e.Exam)
                    .Include(e => e.Group)
                    .Where(e => e.UnitOrder == id)
                    .Select(e => new ExamUnitDto
                    {
                        UnitOrder = e.UnitOrder,
                        ExamId = e.ExamId,
                        ExamName = e.Exam != null ? e.Exam.ExamName : "N/A",
                        GroupId = e.GroupId,
                        GroupName = e.Group != null ? e.Group.GroupName : "N/A",
                        MainDegree = e.MainDegree,
                        TotalProblems = e.TotalProblems,
                        Shuffle = e.Shuffle,
                        AllProblems = e.AllProblems
                    })
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (examUnit == null)
                {
                    _logger.LogWarning("Exam unit with UnitOrder {Id} not found.", id);
                    return NotFound(new { success = false, message = "Exam unit not found." });
                }

                _logger.LogInformation("Retrieved exam unit with UnitOrder {Id}.", id);
                return Ok(new { success = true, data = examUnit });
            }
            catch (Exception ex)
            {
                return HandleException(ex, $"retrieving exam unit with UnitOrder {id}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddExamUnit([FromBody] CreateExamUnitDto examUnitDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, errors = GetModelStateErrors() });
            }

            try
            {
                var validationResult = await ValidateExamAndGroupAsync(examUnitDto.ExamId, examUnitDto.GroupId);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new { success = false, message = validationResult.Message });
                }

                if (examUnitDto.AllProblems < examUnitDto.TotalProblems)
                {
                    _logger.LogWarning("AllProblems {AllProblems} is less than TotalProblems {TotalProblems} for ExamId {ExamId}.", examUnitDto.AllProblems, examUnitDto.TotalProblems, examUnitDto.ExamId);
                    return BadRequest(new { success = false, message = "AllProblems cannot be less than TotalProblems." });
                }

                var examUnit = new ExamUnit
                {
                    ExamId = examUnitDto.ExamId,
                    GroupId = examUnitDto.GroupId,
                    MainDegree = examUnitDto.MainDegree,
                    TotalProblems = examUnitDto.TotalProblems,
                    Shuffle = examUnitDto.Shuffle,
                    AllProblems = examUnitDto.AllProblems
                };

                _context.ExamUnits.Add(examUnit);
                await _context.SaveChangesAsync();

                await _context.Entry(examUnit).Reference(e => e.Exam).LoadAsync();
                await _context.Entry(examUnit).Reference(e => e.Group).LoadAsync();

                var responseDto = new ExamUnitDto
                {
                    UnitOrder = examUnit.UnitOrder,
                    ExamName = examUnit.Exam?.ExamName ?? "N/A",
                    GroupId = examUnit.GroupId,
                    GroupName = examUnit.Group?.GroupName ?? "N/A",
                    MainDegree = examUnit.MainDegree,
                    TotalProblems = examUnit.TotalProblems,
                    Shuffle = examUnit.Shuffle,
                    AllProblems = examUnit.AllProblems
                };

                _logger.LogInformation("Created exam unit with UnitOrder {UnitOrder} for ExamId {ExamId}.", examUnit.UnitOrder, examUnit.ExamId);
                return CreatedAtAction(nameof(GetExamUnit), new { id = examUnit.UnitOrder }, new { success = true, message = "Exam unit created successfully.", data = responseDto });
            }
            catch (Exception ex)
            {
                return HandleException(ex, $"adding exam unit with ExamId {examUnitDto.ExamId}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditExamUnit(int id, [FromBody] CreateExamUnitDto examUnitDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, errors = GetModelStateErrors() });
            }

            try
            {
                var existingExamUnit = await _context.ExamUnits.FirstOrDefaultAsync(eu => eu.UnitOrder == id);
                if (existingExamUnit == null)
                {
                    _logger.LogWarning("Exam unit with UnitOrder {Id} not found.", id);
                    return NotFound(new { success = false, message = "Exam unit not found." });
                }

                var validationResult = await ValidateExamAndGroupAsync(examUnitDto.ExamId, examUnitDto.GroupId);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new { success = false, message = validationResult.Message });
                }

                if (examUnitDto.AllProblems < examUnitDto.TotalProblems)
                {
                    _logger.LogWarning("AllProblems {AllProblems} is less than TotalProblems {TotalProblems} for UnitOrder {Id}.", examUnitDto.AllProblems, examUnitDto.TotalProblems, id);
                    return BadRequest(new { success = false, message = "AllProblems cannot be less than TotalProblems." });
                }

                existingExamUnit.ExamId = examUnitDto.ExamId;
                existingExamUnit.GroupId = examUnitDto.GroupId;
                existingExamUnit.MainDegree = examUnitDto.MainDegree;
                existingExamUnit.TotalProblems = examUnitDto.TotalProblems;
                existingExamUnit.Shuffle = examUnitDto.Shuffle;
                existingExamUnit.AllProblems = examUnitDto.AllProblems;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated exam unit with UnitOrder {Id}.", id);
                return Ok(new { success = true, message = "Exam unit updated successfully." });
            }
            catch (Exception ex)
            {
                return HandleException(ex, $"updating exam unit with UnitOrder {id}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExamUnit(int id)
        {
            try
            {
                var examUnit = await _context.ExamUnits.FirstOrDefaultAsync(eu => eu.UnitOrder == id);
                if (examUnit == null)
                {
                    _logger.LogWarning("Exam unit with UnitOrder {Id} not found.", id);
                    return NotFound(new { success = false, message = "Exam unit not found." });
                }

                _context.ExamUnits.Remove(examUnit);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted exam unit with UnitOrder {Id}.", id);
                return Ok(new { success = true, message = "Exam unit deleted successfully." });
            }
            catch (Exception ex)
            {
                return HandleException(ex, $"deleting exam unit with UnitOrder {id}");
            }
        }

        private IActionResult HandleException(Exception ex, string operation)
        {
            string errorDetails = ex.InnerException != null ? $" InnerException: {ex.InnerException.Message}" : "";
            if (ex is DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while {Operation}.{Details} StackTrace: {StackTrace}", operation, errorDetails, dbEx.StackTrace);
                return StatusCode(500, new { success = false, message = $"Database error while {operation}.", error = dbEx.InnerException?.Message ?? dbEx.Message });
            }

            _logger.LogError(ex, "Unexpected error while {Operation}.{Details} StackTrace: {StackTrace}", operation, errorDetails, ex.StackTrace);
            return StatusCode(500, new { success = false, message = $"Unexpected error while {operation}.", error = ex.Message + errorDetails });
        }

        private async Task<(bool IsValid, string Message)> ValidateExamAndGroupAsync(int examId, int groupId)
        {
            var examExists = await _context.Exams.AnyAsync(e => e.ExamId == examId);
            if (!examExists)
            {
                _logger.LogWarning("Exam with ID {ExamId} not found.", examId);
                return (false, "Invalid Exam ID. Exam does not exist.");
            }

            var groupExists = await _context.Groups.AnyAsync(g => g.GroupId == groupId);
            if (!groupExists)
            {
                _logger.LogWarning("Group with ID {GroupId} not found.", groupId);
                return (false, "Invalid Group ID. Group does not exist.");
            }

            return (true, string.Empty);
        }

        private List<string> GetModelStateErrors()
        {
            return ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
        }
    }

    public class ExamUnitDto
    {
        public int UnitOrder { get; set; }
        public int ExamId { get; set; }
        public string ExamName { get; set; } = string.Empty;
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public int MainDegree { get; set; }
        public int TotalProblems { get; set; }
        public bool Shuffle { get; set; }
        public int AllProblems { get; set; }
    }

    public class CreateExamUnitDto
    {
        [Required(ErrorMessage = "Exam ID is required.")]
        public int ExamId { get; set; }

        [Required(ErrorMessage = "Group ID is required.")]
        public int GroupId { get; set; }

        [Required(ErrorMessage = "Main degree is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Main degree cannot be negative.")]
        public int MainDegree { get; set; }

        [Required(ErrorMessage = "Total problems is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Total problems cannot be negative.")]
        public int TotalProblems { get; set; }

        public bool Shuffle { get; set; }

        [Required(ErrorMessage = "All problems is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "All problems cannot be negative.")]
        public int AllProblems { get; set; }
    }
}