using Graduation_proj.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Graduation_proj.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GroupsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GroupsController> _logger;

        public GroupsController(ApplicationDbContext context, ILogger<GroupsController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieves a list of all groups.
        /// </summary>
        /// <returns>A list of groups with their details.</returns>
        [HttpGet]
        public async Task<IActionResult> GetGroups()
        {
            try
            {
                var groups = await _context.Groups
                    .Include(g => g.Topic)
                    .OrderBy(g => g.Topic != null ? g.Topic.TopicName : string.Empty)
                    .Select(g => new GroupDto
                    {
                        GroupId = g.GroupId,
                        GroupName = g.GroupName,
                        MainDegree = g.MainDegree,
                        TotalProblems = g.TotalProblems,
                        TopicId = g.TopicId,
                        TopicName = g.Topic != null ? g.Topic.TopicName : null,
                        CommonQuestionHeader = g.CommonQuestionHeader,
                        HasCommonHeader = g.HasCommonHeader
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

        /// <summary>
        /// Retrieves a specific group by its ID.
        /// </summary>
        /// <param name="id">The ID of the group to retrieve.</param>
        /// <returns>The group details if found; otherwise, a 404 error.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetGroup(int id)
        {
            try
            {
                var group = await _context.Groups
                    .Include(g => g.Topic)
                    .FirstOrDefaultAsync(g => g.GroupId == id);

                if (group == null)
                {
                    _logger.LogWarning("Group with ID {Id} not found.", id);
                    return NotFound(new { success = false, message = "Group not found." });
                }

                var groupDto = new GroupDto
                {
                    GroupId = group.GroupId,
                    GroupName = group.GroupName,
                    MainDegree = group.MainDegree,
                    TotalProblems = group.TotalProblems,
                    TopicId = group.TopicId,
                    TopicName = group.Topic != null ? group.Topic.TopicName : null,
                    CommonQuestionHeader = group.CommonQuestionHeader,
                    HasCommonHeader = group.HasCommonHeader
                };

                return Ok(groupDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving group with ID {Id}.", id);
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving the group." });
            }
        }

        /// <summary>
        /// Adds a new group.
        /// </summary>
        /// <param name="groupDto">The group data to add.</param>
        /// <returns>The created group details with a 201 status code.</returns>
        [HttpPost]
        public async Task<IActionResult> AddGroup([FromBody] CreateGroupDto groupDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Invalid model state for AddGroup: {Errors}", string.Join(", ", errors));
                return BadRequest(new { success = false, errors });
            }

            try
            {
                var topicExists = await _context.Topics.AnyAsync(t => t.TopicId == groupDto.TopicId);
                if (!topicExists)
                {
                    _logger.LogWarning("Topic with ID {TopicId} not found.", groupDto.TopicId);
                    return BadRequest(new { success = false, message = "Invalid Topic ID. Topic does not exist." });
                }

                var group = new Group
                {
                    GroupName = groupDto.GroupName,
                    MainDegree = groupDto.MainDegree,
                    TotalProblems = groupDto.TotalProblems,
                    TopicId = groupDto.TopicId,
                    Topic = null,
                    CommonQuestionHeader = groupDto.CommonQuestionHeader,
                    HasCommonHeader = groupDto.HasCommonHeader
                };

                _context.Groups.Add(group);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Group {GroupName} created with ID {GroupId}.", group.GroupName, group.GroupId);
                return CreatedAtAction(nameof(GetGroup), new { id = group.GroupId }, new
                {
                    success = true,
                    message = "Group created successfully.",
                    groupId = group.GroupId
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while adding group {GroupName}.", groupDto.GroupName);
                return StatusCode(500, new { success = false, message = "An error occurred while adding the group." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while adding group {GroupName}.", groupDto.GroupName);
                return StatusCode(500, new { success = false, message = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Updates an existing group.
        /// </summary>
        /// <param name="id">The ID of the group to update.</param>
        /// <param name="groupDto">The updated group data.</param>
        /// <returns>A success message if updated; otherwise, a 404 or 400 error.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> EditGroup(int id, [FromBody] CreateGroupDto groupDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Invalid model state for EditGroup: {Errors}", string.Join(", ", errors));
                return BadRequest(new { success = false, errors });
            }

            try
            {
                var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupId == id);
                if (group == null)
                {
                    _logger.LogWarning("Group with ID {Id} not found for update.", id);
                    return NotFound(new { success = false, message = "Group not found." });
                }

                var topicExists = await _context.Topics.AnyAsync(t => t.TopicId == groupDto.TopicId);
                if (!topicExists)
                {
                    _logger.LogWarning("Topic with ID {TopicId} not found for group update.", groupDto.TopicId);
                    return BadRequest(new { success = false, message = "Invalid Topic ID. Topic does not exist." });
                }

                group.GroupName = groupDto.GroupName;
                group.MainDegree = groupDto.MainDegree;
                group.TotalProblems = groupDto.TotalProblems;
                group.TopicId = groupDto.TopicId;
                group.Topic = null;
                group.CommonQuestionHeader = groupDto.CommonQuestionHeader;
                group.HasCommonHeader = groupDto.HasCommonHeader;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Group {GroupName} with ID {Id} updated.", group.GroupName, id);
                return Ok(new { success = true, message = "Group updated successfully." });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while updating group with ID {Id}.", id);
                return StatusCode(500, new { success = false, message = "An error occurred while updating the group." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating group with ID {Id}.", id);
                return StatusCode(500, new { success = false, message = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Deletes a group by its ID.
        /// </summary>
        /// <param name="id">The ID of the group to delete.</param>
        /// <returns>A success message if deleted; otherwise, a 404 error.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGroup(int id)
        {
            try
            {
                var group = await _context.Groups.FindAsync(id);
                if (group == null)
                {
                    _logger.LogWarning("Group with ID {Id} not found for deletion.", id);
                    return NotFound(new { success = false, message = "Group not found." });
                }

                _context.Groups.Remove(group);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Group with ID {Id} deleted.", id);
                return Ok(new { success = true, message = "Group deleted successfully." });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while deleting group with ID {Id}.", id);
                return StatusCode(500, new { success = false, message = "An error occurred while deleting the group." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting group with ID {Id}.", id);
                return StatusCode(500, new { success = false, message = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Retrieves a list of all topics.
        /// </summary>
        /// <returns>A list of topics with their IDs and names.</returns>
        [HttpGet("topics")]
        public async Task<IActionResult> GetTopics()
        {
            try
            {
                var topics = await _context.Topics
                    .Select(t => new
                    {
                        t.TopicId,
                        t.TopicName
                    })
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} topics.", topics.Count);
                return Ok(topics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving topics.");
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving topics." });
            }
        }
    }

    public class GroupDto
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public int MainDegree { get; set; }
        public int TotalProblems { get; set; }
        public int TopicId { get; set; }
        public string TopicName { get; set; }
        public string CommonQuestionHeader { get; set; }
        public bool HasCommonHeader { get; set; }
    }

    public class CreateGroupDto
    {
        [Required(ErrorMessage = "Group name is required.")]
        [MaxLength(40, ErrorMessage = "Group name cannot exceed 40 characters.")]
        public string GroupName { get; set; }

        [Required(ErrorMessage = "Main degree is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Main degree must be a positive number.")]
        public int MainDegree { get; set; }

        [Required(ErrorMessage = "Total problems is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Total problems must be a positive number.")]
        public int TotalProblems { get; set; }

        [Required(ErrorMessage = "Topic ID is required.")]
        public int TopicId { get; set; }

        [MaxLength(1000, ErrorMessage = "Common question header cannot exceed 1000 characters.")]
        public string CommonQuestionHeader { get; set; }

        public bool HasCommonHeader { get; set; }
    }
}