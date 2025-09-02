using Graduation_proj.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Graduation_proj.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TopicsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TopicsController> _logger;

        public TopicsController(ApplicationDbContext context, ILogger<TopicsController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieves a list of all topics.
        /// </summary>
        /// <returns>A list of topics with their IDs, names, and material IDs.</returns>
        [HttpGet]
        public async Task<IActionResult> GetTopics()
        {
            try
            {
                var topics = await _context.Topics
                    .Select(t => new TopicDto
                    {
                        TopicId = t.TopicId,
                        TopicName = t.TopicName,
                        MaterialId = t.MaterialId
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

        /// <summary>
        /// Retrieves a specific topic by its ID.
        /// </summary>
        /// <param name="id">The ID of the topic to retrieve.</param>
        /// <returns>The topic details if found; otherwise, a 404 error.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTopic(int id)
        {
            try
            {
                var topic = await _context.Topics
                    .FirstOrDefaultAsync(t => t.TopicId == id);

                if (topic == null)
                {
                    _logger.LogWarning("Topic with ID {Id} not found.", id);
                    return NotFound(new { success = false, message = "Topic not found." });
                }

                var topicDto = new TopicDto
                {
                    TopicId = topic.TopicId,
                    TopicName = topic.TopicName,
                    MaterialId = topic.MaterialId
                };

                return Ok(topicDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving topic with ID {Id}.", id);
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving the topic." });
            }
        }

        /// <summary>
        /// Adds a new topic.
        /// </summary>
        /// <param name="topicDto">The topic data to add.</param>
        /// <returns>The created topic details with a 201 status code.</returns>
        [HttpPost]
        public async Task<IActionResult> AddTopic([FromBody] CreateTopicDto topicDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Invalid model state for AddTopic: {Errors}", string.Join(", ", errors));
                return BadRequest(new { success = false, errors });
            }

            try
            {
                // التحقق من وجود MaterialId في جدول Materials
                var materialExists = await _context.Materials.AnyAsync(m => m.MaterialId == topicDto.MaterialId);
                if (!materialExists)
                {
                    _logger.LogWarning("Material with ID {MaterialId} not found.", topicDto.MaterialId);
                    return BadRequest(new { success = false, message = "Invalid Material ID. Material does not exist." });
                }

                var topic = new Topic
                {
                    TopicName = topicDto.TopicName,
                    MaterialId = topicDto.MaterialId,
                    Material = null // تجاهل الـ navigation property
                };

                _context.Topics.Add(topic);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Topic {TopicName} created with ID {TopicId}.", topic.TopicName, topic.TopicId);
                return CreatedAtAction(nameof(GetTopic), new { id = topic.TopicId }, new
                {
                    success = true,
                    message = "Topic created successfully.",
                    topicId = topic.TopicId
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while adding topic {TopicName}.", topicDto.TopicName);
                return StatusCode(500, new { success = false, message = "An error occurred while adding the topic." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while adding topic {TopicName}.", topicDto.TopicName);
                return StatusCode(500, new { success = false, message = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Updates an existing topic.
        /// </summary>
        /// <param name="id">The ID of the topic to update.</param>
        /// <param name="topicDto">The updated topic data.</param>
        /// <returns>A success message if updated; otherwise, a 404 or 400 error.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> EditTopic(int id, [FromBody] CreateTopicDto topicDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Invalid model state for EditTopic: {Errors}", string.Join(", ", errors));
                return BadRequest(new { success = false, errors });
            }

            try
            {
                var topic = await _context.Topics.FirstOrDefaultAsync(t => t.TopicId == id);
                if (topic == null)
                {
                    _logger.LogWarning("Topic with ID {Id} not found for update.", id);
                    return NotFound(new { success = false, message = "Topic not found." });
                }

                // التحقق من وجود MaterialId في جدول Materials
                var materialExists = await _context.Materials.AnyAsync(m => m.MaterialId == topicDto.MaterialId);
                if (!materialExists)
                {
                    _logger.LogWarning("Material with ID {MaterialId} not found for topic update.", topicDto.MaterialId);
                    return BadRequest(new { success = false, message = "Invalid Material ID. Material does not exist." });
                }

                topic.TopicName = topicDto.TopicName;
                topic.MaterialId = topicDto.MaterialId;
                topic.Material = null; // تجاهل الـ navigation property

                await _context.SaveChangesAsync();

                _logger.LogInformation("Topic {TopicName} with ID {Id} updated.", topic.TopicName, id);
                return Ok(new { success = true, message = "Topic updated successfully." });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while updating topic with ID {Id}.", id);
                return StatusCode(500, new { success = false, message = "An error occurred while updating the topic." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating topic with ID {Id}.", id);
                return StatusCode(500, new { success = false, message = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Deletes a topic by its ID.
        /// </summary>
        /// <param name="id">The ID of the topic to delete.</param>
        /// <returns>A success message if deleted; otherwise, a 404 error.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTopic(int id)
        {
            try
            {
                var topic = await _context.Topics.FindAsync(id);
                if (topic == null)
                {
                    _logger.LogWarning("Topic with ID {Id} not found for deletion.", id);
                    return NotFound(new { success = false, message = "Topic not found." });
                }

                _context.Topics.Remove(topic);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Topic with ID {Id} deleted.", id);
                return Ok(new { success = true, message = "Topic deleted successfully." });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while deleting topic with ID {Id}.", id);
                return StatusCode(500, new { success = false, message = "An error occurred while deleting the topic." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting topic with ID {Id}.", id);
                return StatusCode(500, new { success = false, message = "An unexpected error occurred." });
            }
        }

        /// <summary>
        /// Retrieves a list of all materials.
        /// </summary>
        /// <returns>A list of materials with their IDs and names.</returns>
        [HttpGet("materials")]
        public async Task<IActionResult> GetMaterials()
        {
            try
            {
                var materials = await _context.Materials
                    .Select(m => new
                    {
                        m.MaterialId,
                        m.MaterialName
                    })
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} materials.", materials.Count);
                return Ok(materials);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving materials.");
                return StatusCode(500, new { success = false, message = "An error occurred while retrieving materials." });
            }
        }
    }

    // DTO لعرض بيانات الـ Topic
    public class TopicDto
    {
        public int TopicId { get; set; }
        public string TopicName { get; set; }
        public int MaterialId { get; set; }
    }

    // DTO لإضافة أو تعديل الـ Topic
    public class CreateTopicDto
    {
        [Required(ErrorMessage = "Topic name is required.")]
        [MaxLength(40, ErrorMessage = "Topic name cannot exceed 40 characters.")]
        public string TopicName { get; set; }

        [Required(ErrorMessage = "Material ID is required.")]
        public int MaterialId { get; set; }
    }
}