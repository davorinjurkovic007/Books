using Books.API.Contexts;
using Books.API.Entities;
using Books.API.ExternalModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Books.API.Services
{
    public class BooksRepository : IBookRepository, IDisposable
    {
        private BookContext context;
        private readonly IHttpClientFactory httpClientFactory;

        public BooksRepository(BookContext context, IHttpClientFactory httpClientFactory)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public async Task<Book> GetBookAsync(Guid id)
        {
            await context.Database.ExecuteSqlInterpolatedAsync($"WAITFOR DELAY '00:00:02';");
            return await context.Books.Include(b => b.Author).FirstOrDefaultAsync(b => b.Id == id);
        }

        public IEnumerable<Book> GetBooks()
        {
            context.Database.ExecuteSqlInterpolated($"WAITFOR DELAY '00:00:02';");
            return context.Books.Include(b => b.Author).ToList();
        }

        public async Task<IEnumerable<Book>> GetBooksAsync()
        {
            return await context.Books.Include(b => b.Author).ToListAsync();
        }

        public async Task<IEnumerable<Entities.Book>> GetBooksAsync(IEnumerable<Guid> bookIds)
        {
            return await context.Books.Where(b => bookIds.Contains(b.Id)).Include(b => b.Author).ToListAsync();
        }

        public async Task<BookCover> GetBookCoverAsync(string coverId)
        {
            var httpClient = httpClientFactory.CreateClient();
            // pass through a dummy name
            var reponse = await httpClient.GetAsync($"http://localhost:52644/api/bookcovers/{coverId}");
            if(reponse.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<BookCover>(await reponse.Content.ReadAsStringAsync(),
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    });
            }

            return null;
        }

        public async Task<IEnumerable<BookCover>> GetBookCoversAsync(Guid bookId)
        {
            var httpClient = httpClientFactory.CreateClient();
            var bookCovers = new List<BookCover>();

            // create a list of fake bookcovers
            var bookCoverUrls = new[]
            {
                $"https://localhost:44339/api/bookcovers/{bookId}-dummycover1",
                $"https://localhost:44339/api/bookcovers/{bookId}-dummycover2",
                $"https://localhost:44339/api/bookcovers/{bookId}-dummycover3",
                $"https://localhost:44339/api/bookcovers/{bookId}-dummycover4",
                $"https://localhost:44339/api/bookcovers/{bookId}-dummycover5"
            };

            foreach(var bookCoverUrl in bookCoverUrls)
            {
                var response = await httpClient.GetAsync(bookCoverUrl);

                if(response.IsSuccessStatusCode)
                {
                    bookCovers.Add(JsonSerializer.Deserialize<BookCover>(
                        await response.Content.ReadAsStringAsync(),
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                        }));
                }
            }

            return bookCovers;
        }

        public void AddBook(Book bookToAdd)
        {
            if(bookToAdd == null)
            {
                throw new ArgumentNullException(nameof(bookToAdd));
            }

            context.Add(bookToAdd);
        }

        public async Task<bool> SaveChangesAsync()
        {
            // return true is 1 or more entities were changed
            return (await context.SaveChangesAsync() > 0);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(disposing)
            {
                if(context != null)
                {
                    context.Dispose();
                    context = null;
                }
            }
        }
    }
}
