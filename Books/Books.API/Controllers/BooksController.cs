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
    [Route("api/books")]
    public class BooksController : ControllerBase
    {
        private readonly IBookRepository booksRepository;

        public BooksController(IBookRepository booksRepository)
        {
            this.booksRepository = booksRepository ?? throw new ArgumentNullException(nameof(IBookRepository));
        }

        [HttpGet]
        [BooksResultFilter]
        public async Task<IActionResult> GetBooks()
        {
            var bookEntities = await booksRepository.GetBooksAsync();
            return Ok(bookEntities);
        }

        [HttpGet]
        [Route("{id}")]
        [BookResultFilter]
        public async Task<IActionResult> GetBook(Guid id)
        {
            var bookEntity = await booksRepository.GetBookAsync(id);
            if(bookEntity == null)
            {
                return NotFound();
            }

            return Ok(bookEntity);
        }
    }
}
