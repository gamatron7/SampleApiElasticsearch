using Nest;
using SampleApiElasticsearch.Models;

namespace SampleApiElasticsearch.Extensions
{
    public static class ElasticSearchExtensions
    {
        public static void AddElasticSearch(this IServiceCollection services, IConfiguration configuration)
        {
            var url = configuration["ELKConfiguration:Uri"];
            if (string.IsNullOrEmpty(url))
                throw new FormatException("The value of ELKConfiguration:Uri can't be null or empty");

            var idxDefault = configuration["ELKConfiguration:Indexes:IdxDefault"];
            int.TryParse(configuration["ELKConfiguration:Indexes:NumberOfReplicas"], out int numberOfReplicas);
            int.TryParse(configuration["ELKConfiguration:Indexes:NumberOfShards"], out int numberOfShards);

            var idxBooks = configuration["ELKConfiguration:Indexes:IdxBooks"];
            var idxAuthors = configuration["ELKConfiguration:Indexes:IdxAuthors"];

            var settings = new ConnectionSettings(new Uri(url)).PrettyJson().DefaultIndex(idxDefault);

            AddDefaultMappings(settings);

            var client = new ElasticClient(settings);
            services.AddSingleton<IElasticClient>(client);

            CreateIndex<Book>(numberOfReplicas, numberOfShards, idxBooks, client);
            CreateIndex<Author>(numberOfReplicas, numberOfShards, idxAuthors, client);
        }

        private static void AddDefaultMappings(ConnectionSettings settings)
        {
            settings.DefaultMappingFor<Book>(b => b.Ignore(x => x.Editorial));
            settings.DefaultMappingFor<Author>(a => a.Ignore(x => x.Gender));
        }

        private static void CreateIndex<T>(int numberOfReplicas, int numberOfShards, string? idxName, ElasticClient client) where T : class
        {
            if (!client.Indices.Exists(idxName).Exists)
            {
                client.Indices.Create(idxName, i => i
                    .Map<T>(x => x.AutoMap())
                    .Settings(s => s
                        .NumberOfReplicas(numberOfReplicas)
                        .NumberOfShards(numberOfShards))
                );
            }
        }
    }
}
