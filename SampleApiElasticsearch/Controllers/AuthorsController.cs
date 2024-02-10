using Nest;
using Microsoft.AspNetCore.Mvc;
using SampleApiElasticsearch.Models;
using System.Net;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;
using System.Reflection.Metadata;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;

namespace SampleApiElasticsearch.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthorsController : ControllerBase
    {
        private readonly IElasticClient _elasticClient;
        private readonly string _index;

        public AuthorsController(IElasticClient elasticClient, IConfiguration configuration)
        {
            _elasticClient = elasticClient;

            var idx = configuration["ELKConfiguration:Indexes:IdxAuthors"];
            if (string.IsNullOrEmpty(idx))
                throw new FormatException("The value of ELKConfiguration:Indexes:IdxAuthors can't be null or empty");

            _index = idx;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string? query)
        {
            ISearchResponse<Author> results;

            if (!string.IsNullOrWhiteSpace(query))
            {
                results = await _elasticClient.SearchAsync<Author>(s => s
                    .Query(q => q
                        .Match(m => m
                            .Field(f => f.FirstName)
                            .Query(query)
                        )
                    )
                    .Size(1000)
                    .Index(_index)
                );
            }
            else
            {
                results = await _elasticClient.SearchAsync<Author>(s => s
                 .Query(q => q
                    .MatchAll()
                 )
                 .Size(1000)
                 .Index(_index)
                );
            }

            return Ok(results.Documents.ToList());
        }

        [HttpPost]
        public async Task<IActionResult> Post(Author author)
        {
            await _elasticClient.CreateAsync(author, c => c.Index(_index));
            return Ok(author);
        }

        [HttpPost("add-list")]
        public async Task<IActionResult> AddBulk(IList<Author> Authors)
        {
            await _elasticClient.BulkAsync(b => b
                .Index(_index)
                .IndexMany(Authors)
            );

            return Ok();
        }

        [HttpPut]
        public async Task<IActionResult> Put(Author author)
        {
            var indexResponse = await _elasticClient.IndexAsync(author, idx => idx.Index(_index));
            if (!indexResponse.IsValid)
            {
                return BadRequest("The task could not be completed");
            }

            return Ok();
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _elasticClient.DeleteByQueryAsync<Author>(q => q
                    .Query(q => q
                        .Match(m => m
                            .Field(f => f.Id)
                            .Query(id.ToString())
                        )
                    )
                    .Index(_index)
                );


            if (!response.IsValid)
            {
                return BadRequest("The task could not be completed");
            }

            return Ok();

        }
    }
}
