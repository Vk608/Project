using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using FABBatchValidator.Models;
using FABBatchValidator.Storage;

namespace FABBatchValidator.Controllers
{
    /// <summary>
    /// API endpoint for retrieving validated bibliographic records.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RecordsController : ControllerBase
    {
        private readonly JsonResultRepository _resultRepository;

        public RecordsController(JsonResultRepository resultRepository)
        {
            _resultRepository = resultRepository;
        }

        /// <summary>
        /// GET /api/records
        /// Returns all validated records stored in JSON.
        /// Empty array if no records have been validated yet.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<ValidatedRecord>>> GetRecords()
        {
            try
            {
                var records = await _resultRepository.LoadAsync();
                return Ok(records);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Failed to load records",
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// GET /api/records/job/{jobId}
        /// Returns records for a specific job ID.
        /// </summary>
        [HttpGet("job/{jobId}")]
        public async Task<ActionResult<List<ValidatedRecord>>> GetRecordsByJobId(string jobId)
        {
            try
            {
                var records = await _resultRepository.LoadByJobIdAsync(jobId);
                if (records.Count == 0)
                {
                    return NotFound(new { error = $"No records found for job {jobId}" });
                }
                return Ok(records);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = $"Failed to load records for job {jobId}",
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// GET /api/records/{index}
        /// Returns a single validated record by index.
        /// </summary>
        [HttpGet("{index}")]
        public async Task<ActionResult<ValidatedRecord>> GetRecord(int index)
        {
            try
            {
                var records = await _resultRepository.LoadAsync();
                
                if (index < 0 || index >= records.Count)
                {
                    return NotFound(new { error = "Record not found" });
                }

                return Ok(records[index]);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Failed to load record",
                    message = ex.Message
                });
            }
        }
    }
}
