﻿using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Application.UseCases;
using CareNirvana.Service.Domain.Model;
using Microsoft.AspNetCore.Mvc;


namespace CareNirvana.Service.API.Controllers
{
    [Route("api/[controller]")]
    public class CodesetsController : ControllerBase
    {
        private readonly ICodesetsRepository _service;

        public CodesetsController(ICodesetsRepository service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] codesets entity)
        {
            var result = await _service.InsertAsync(entity);
            return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] codesets entity)
        {
            if (entity.Id != id) return BadRequest();
            var result = await _service.UpdateAsync(entity);
            return Ok(result);
        }
    }
}
