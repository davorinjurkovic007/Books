using AutoMapper;
using Books.API.Filters;
using Books.API.ModelBinders;
using Books.API.Models;
using Books.API.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Books.API.Controllers
{
    [Route("api/bookcollections")]
    [ApiController]
    [BooksResultFilter]
    public class BookCollectionsController : ControllerBase
    {
        private readonly IBookRepository booksRepository;
        private readonly IMapper mapper;

        public BookCollectionsController(IBookRepository booksRepository, IMapper mapper) 
        {
            this.booksRepository = booksRepository ?? throw new ArgumentNullException(nameof(booksRepository));
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        // api/bookcollections/(id1,id2,...)
        [HttpGet("{bookIds})", Name = "GetBookCollection")]
        public async Task<IActionResult> GetBookCollection(
            [ModelBinder(BinderType = typeof(ArrayModelBinder))] IEnumerable<Guid> bookIds)
        {
            var bookEntities = await booksRepository.GetBooksAsync(bookIds);

            if(bookIds.Count() != bookEntities.Count())
            {
                return NotFound();
            }

            return Ok(bookEntities);
        }

        [HttpPost]
        public async Task<IActionResult> CreateBookCollection(IEnumerable<BookForCreation> bookCollection)
        {
            // in real life scenario, check for validation

            var bookEntities = mapper.Map<IEnumerable<Entities.Book>>(bookCollection);

            foreach(var bookEntity in bookEntities)
            {
                booksRepository.AddBook(bookEntity);
            }

            await booksRepository.SaveChangesAsync();

            var booksToReturn = await booksRepository.GetBooksAsync(bookEntities.Select(b => b.Id).ToList());

            var bookIds = string.Join(",", booksToReturn.Select(a => a.Id));

            return CreatedAtRoute("GetBookCollection", new { bookIds }, booksToReturn);
        }
    }
}
