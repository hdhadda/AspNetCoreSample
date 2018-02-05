
namespace TestWebApi.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using TestWebApi.Interfaces;
    using TestWebApi.Models;

    // [Produces("application/json")] to force for all return types.
    // https://docs.microsoft.com/en-us/aspnet/core/mvc/models/formatting
    [Route("api/[controller]")]
    public class ToDoItemsController : Controller
    {
        private readonly IToDoItemsList _toDoRepository;

        public ToDoItemsController(IToDoItemsList toDoRepository)
        {
            _toDoRepository = toDoRepository;
        }

        [HttpGet]
        public IActionResult List()
        {
            return Ok(_toDoRepository.All);
        }

        // GET api/authors/about
        [HttpGet("About")]
        public ContentResult About()
        {
            return Content("An API listing authors of docs.asp.net.");
        }

        // JsonResult: return JSON-formatted data
        // ContentResult: return plain-text-formatted string data
        // IActionResult
        [HttpGet("{featureName}/{id}")]
        public JsonResult GetItem2(string id)
        {
            return Json(_toDoRepository.All);
        }

        [HttpGet("{featureName}/{ids}/{id}")]
        public IActionResult GetItem3(string id)
        {
            return Json(_toDoRepository.All);
        }

        [HttpGet("{id}")]
        public IActionResult GetItem(string id)
        {
            return Ok(_toDoRepository.Find(id));
        }

        [HttpPost]
        public IActionResult Create([FromBody] ToDoItem item)
        {
            try
            {
                if (item == null || !ModelState.IsValid)
                {
                    return BadRequest(ErrorCode.TodoItemNameAndNotesRequired.ToString());
                }
                bool itemExists = _toDoRepository.DoesItemExist(item.ID);
                if (itemExists)
                {
                    return StatusCode(StatusCodes.Status409Conflict, ErrorCode.TodoItemIDInUse.ToString());
                }
                _toDoRepository.Insert(item);
            }
            catch (Exception)
            {
                return BadRequest(ErrorCode.CouldNotCreateItem.ToString());
            }
            return Ok(item);
        }

        [HttpPut]
        public IActionResult Edit([FromBody] ToDoItem item)
        {
            try
            {
                if (item == null || !ModelState.IsValid)
                {
                    return BadRequest(ErrorCode.TodoItemNameAndNotesRequired.ToString());
                }
                var existingItem = _toDoRepository.Find(item.ID);
                if (existingItem == null)
                {
                    return NotFound(ErrorCode.RecordNotFound.ToString());
                }
                _toDoRepository.Update(item);
            }
            catch (Exception)
            {
                return BadRequest(ErrorCode.CouldNotUpdateItem.ToString());
            }
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(string id)
        {
            try
            {
                var item = _toDoRepository.Find(id);
                if (item == null)
                {
                    return NotFound(ErrorCode.RecordNotFound.ToString());
                }
                _toDoRepository.Delete(id);
            }
            catch (Exception)
            {
                return BadRequest(ErrorCode.CouldNotDeleteItem.ToString());
            }
            return NoContent();
        }
    }
}

// Controller Base respose types:

//NoContent
//NotFound
//BadRequest
//Ok
//StatusCode