using Books.API.Entities;
using Books.API.ExternalModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Books.API.Services
{
    public interface IBookRepository
    {
        IEnumerable<Book> GetBooks();

        Task<IEnumerable<Book>> GetBooksAsync();

        Task<Book> GetBookAsync(Guid id);

        Task<IEnumerable<Entities.Book>> GetBooksAsync(IEnumerable<Guid> bookIds);

        Task<BookCover> GetBookCoverAsync(string coverId);

        Task<IEnumerable<BookCover>> GetBookCoversAsync(Guid bookId);

        void AddBook(Entities.Book bookToAdd);

        Task<bool> SaveChangesAsync();
    }
}
