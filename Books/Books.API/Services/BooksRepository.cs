using Books.API.Contexts;
using Books.API.Entities;
using Books.API.ExternalModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Books.API.Services
{
    public class BooksRepository : IBookRepository, IDisposable
    {
        private BookContext context;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ILogger<BooksRepository> logger;
        private CancellationTokenSource cancellationTokenSource;

        public BooksRepository(BookContext context, IHttpClientFactory httpClientFactory, ILogger<BooksRepository> logger)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        private Task<int> GetBookPages()
        {
            return Task.Run(() =>
            {
                var pageCalculator = new Books.Legacy.ComplicatedPageCalculator();

                logger.LogInformation($"ThreadId when calculating the amount of pages: " +
                    $"{System.Threading.Thread.CurrentThread.ManagedThreadId}");

                return pageCalculator.CalculateBookPages();
            });
        }

        public async Task<IEnumerable<Book>> GetBooksAsync()
        {
            //var pageCalculator = new Books.Legacy.ComplicatedPageCalculator();
            //var amountOfPages = pageCalculator.CalculateBookPages();

            logger.LogInformation($"ThreadId when entering GetBookAsync: " +
                    $"{System.Threading.Thread.CurrentThread.ManagedThreadId}");

            // Awoid this call on the server. It is not optimized and will block. Used on WPF, XAML or client applications. 
            var bookPages = await GetBookPages();

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
            var reponse = await httpClient.GetAsync($"http://localhost:5000/api/bookcovers/{coverId}");
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

        private async Task<BookCover> DownloadBookCoverAsync(
            HttpClient httpClient, string bookCoverUrl, CancellationToken cancellationToken)
        {
            var response = await httpClient.GetAsync(bookCoverUrl, cancellationToken);

            if(response.IsSuccessStatusCode)
            {
                var bookCover = JsonSerializer.Deserialize<BookCover>(
                    await response.Content.ReadAsStringAsync(),
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                return bookCover;
            }

            cancellationTokenSource.Cancel();
            return null;
        }

        public async Task<IEnumerable<BookCover>> GetBookCoversAsync(Guid bookId)
        {
            var httpClient = httpClientFactory.CreateClient();
            var bookCovers = new List<BookCover>();
            cancellationTokenSource = new CancellationTokenSource();

            // create a list of fake bookcovers
            var bookCoverUrls = new[]
            {
                $"https://localhost:5001/api/bookcovers/{bookId}-dummycover1",
                //$"https://localhost:5001/api/bookcovers/{bookId}-dummycover2?returnFault=true",
                $"https://localhost:5001/api/bookcovers/{bookId}-dummycover2",
                $"https://localhost:5001/api/bookcovers/{bookId}-dummycover3",
                $"https://localhost:5001/api/bookcovers/{bookId}-dummycover4",
                $"https://localhost:5001/api/bookcovers/{bookId}-dummycover5"
            };

            // create the tasks
            var downloadBookCoverTasksQuery = from bookCoverUrl in bookCoverUrls select DownloadBookCoverAsync(httpClient, bookCoverUrl,
                cancellationTokenSource.Token);

            // start the tasks 
            var downloadBookCoverTasks = downloadBookCoverTasksQuery.ToList();

            try
            {
                return await Task.WhenAll(downloadBookCoverTasks);
            }
            catch(OperationCanceledException operationCancelException)
            {
                logger.LogInformation($"{operationCancelException.Message}");
                foreach(var task in downloadBookCoverTasks)
                {
                    logger.LogInformation($"Task {task.Id} has status {task.Status}");
                }

                return new List<BookCover>();
            }
            catch(Exception exception)
            {
                logger.LogError($"{exception.Message}");
                throw;
            }

            //foreach(var bookCoverUrl in bookCoverUrls)
            //{
            //    var response = await httpClient.GetAsync(bookCoverUrl);

            //    if(response.IsSuccessStatusCode)
            //    {
            //        bookCovers.Add(JsonSerializer.Deserialize<BookCover>(
            //            await response.Content.ReadAsStringAsync(),
            //            new JsonSerializerOptions
            //            {
            //                PropertyNameCaseInsensitive = true,
            //            }));
            //    }
            //}

            //return bookCovers;
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

                if(cancellationTokenSource != null)
                {
                    cancellationTokenSource.Dispose();
                    cancellationTokenSource = null;
                }
            }
        }
    }
}
