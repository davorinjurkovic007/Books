using AutoMapper;
using Books.API.Filters;
using Books.API.Models;
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
        private readonly IMapper mapper;

        public BooksController(IBookRepository booksRepository, IMapper mapper)
        {
            this.booksRepository = booksRepository ?? throw new ArgumentNullException(nameof(IBookRepository));
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpGet]
        [BooksResultFilter]
        public async Task<IActionResult> GetBooks()
        {
            var bookEntities = await booksRepository.GetBooksAsync();
            return Ok(bookEntities);
        }

        [HttpGet]
        [Route("{id}", Name = "GetBook")]
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

        [HttpPost]
        [BookResultFilter]
        public async Task<IActionResult> CreateBook(BookForCreation bookForCreation)
        {
            var bookEntity = mapper.Map<Entities.Book>(bookForCreation);

            booksRepository.AddBook(bookEntity);

            await booksRepository.SaveChangesAsync();

            // Fetch (refetch) the book from the data store, including the author
            await booksRepository.GetBookAsync(bookEntity.Id);

            return CreatedAtRoute("GetBook", new { id = bookEntity.Id }, bookEntity);
        }
    }
}
