using AutoMapper;
using CoreCodeCamp.Data;
using CoreCodeCamp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreCodeCamp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CampsController : ControllerBase
    {
        private readonly ICampRepository _repository;
        private readonly IMapper _mapper;
        private readonly LinkGenerator _linkGenerator;

        public CampsController(ICampRepository repository, IMapper mapper, LinkGenerator linkGenerator)
        {
            _mapper = mapper;
            _linkGenerator = linkGenerator;
            _repository = repository;
        }

        [HttpGet]
        public async Task<ActionResult<CampModel[]>> GetCamps(bool includeTalks = false)
        {
            try
            {
                var results = await _repository.GetAllCampsAsync(includeTalks);
                Console.WriteLine(results);
                return _mapper.Map<CampModel[]>(results);
            }
            catch (Exception e)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, $"Database Error {e.Message}");
            }
        }
        [HttpGet("{moniker}")]
        public async Task<ActionResult<CampModel>> Get(string moniker)
        {
            try
            {
                var result = await _repository.GetCampAsync(moniker);

                if (result == null) return NotFound();

                return _mapper.Map<CampModel>(result);
            }
            catch (Exception)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Database Error");
            }
        }
        [HttpGet("search")]
        public async Task<ActionResult<CampModel[]>> SearchByDate(DateTime theDate, bool includeTalks = false)
        {
            try
            {
                var results = await _repository.GetAllCampsByEventDate(theDate, includeTalks);
                if (!results.Any()) return NotFound();
                return _mapper.Map<CampModel[]>(results);
            }
            catch (Exception)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Database Error");
            }
        }
        public async Task<ActionResult<CampModel>> Post(/*[FromBody]*/CampModel model)
        {
            try
            {
                var existent = await _repository.GetCampAsync(model.Moniker);
                if (existent != null)
                {
                    return BadRequest("Moniker in use");
                }
                var location = _linkGenerator.GetPathByAction("Get", "Camps", new { moniker = model.Moniker });
                if (string.IsNullOrEmpty(location))
                {
                    return BadRequest("Could not use current moniker");
                }

                var camp = _mapper.Map<Camp>(model);
                _repository.Add(camp);
                if (await _repository.SaveChangesAsync())
                {
                    return Created(location, _mapper.Map<CampModel>(camp));
                }
                //Create a new Camp
            }
            catch (Exception)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Database Error");
            }
            return BadRequest();
        }
        [HttpPut("{moniker}")]
        public async Task<ActionResult<CampModel>> Put(string moniker, CampModel model)
        {
            try
            {
                var oldCamp = await _repository.GetCampAsync(model.Moniker);
                if (oldCamp == null) return NotFound($"Could not find a camp with Moniker of {model.Moniker}");
                _mapper.Map(model, oldCamp);

                if (await _repository.SaveChangesAsync()) return _mapper.Map<CampModel>(oldCamp);

            }
            catch (Exception)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Database Error");
            }
            return BadRequest();
        }
        [HttpDelete("{moniker}")]
        public async Task<IActionResult> Delete(string moniker)
        {
            try
            {
                var oldCamp = await _repository.GetCampAsync(moniker);
                if (oldCamp == null) return NotFound($"Could not find a camp with Moniker of {moniker}");
                _repository.Delete(oldCamp);
                if(await _repository.SaveChangesAsync())
                {
                    return Ok();
                }
            }
            catch (Exception)
            {
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Database Error");
            }
            return BadRequest("Failed to delete");
        }
    }
}
