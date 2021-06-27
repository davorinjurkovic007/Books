using Books.API.Filters;
using Books.API.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Books.API.Controllers
{
    [ApiController]
    [Route("api/synchroonousbooks")]
    public class SynchronousBooksController : ControllerBase
    {
        private IBookRepository bookRepository;

        public SynchronousBooksController(IBookRepository bookRepository)
        {
            this.bookRepository = bookRepository;
        }

        [HttpGet]
        [BooksResultFilter]
        public IActionResult GetBooks()
        {
            var bookEntities = bookRepository.GetBooks();
            return Ok(bookEntities);
        }
    }
}
