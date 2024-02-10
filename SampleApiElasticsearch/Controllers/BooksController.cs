using Nest;
using Microsoft.AspNetCore.Mvc;
using SampleApiElasticsearch.Models;

namespace SampleApiElasticsearch.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly IElasticClient _elasticClient;
        private readonly string _index; 

        public BooksController(IElasticClient elasticClient, IConfiguration configuration)
        {
            _elasticClient = elasticClient;

            var idx = configuration["ELKConfiguration:Indexes:IdxBooks"];
            if (string.IsNullOrEmpty(idx))
                throw new FormatException("The value of ELKConfiguration:Indexes:IdxBooks can't be null or empty");

            _index = idx;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string keyword)
        {
            var results = await _elasticClient.SearchAsync<Book>(
                s => s.Query(
                    q => q.QueryString(
                        qs => qs.Query('*' + keyword + '*')
                    )
                )
                .Index(_index)
                .Size(1000)
            );

            return Ok(results.Documents.ToList());
        }

        [HttpPost]
        public async Task<IActionResult> Post(Book book)
        {
            await _elasticClient.CreateAsync(book, c => c.Index(_index));

            return Ok(book);
        }

        [HttpPost("add-list")]
        public async Task<IActionResult> AddBulk(IList<Book> books)
        {
            await _elasticClient.BulkAsync(b => b
                .Index(_index)
                .IndexMany(books)
            );
            
            return Ok();
        }
    }
}
