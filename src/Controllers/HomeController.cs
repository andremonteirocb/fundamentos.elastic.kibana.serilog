﻿using Fundamentos.Elastic.Kibana.Serilog.Data;
using Fundamentos.Elastic.Kibana.Serilog.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fundamentos.Elastic.Kibana.Serilog.Controllers
{
    public class HomeController : Controller
    {
        private readonly ElasticContext _context;
        private readonly IElasticClient _elasticClient;
        private readonly ILogger<HomeController> _logger;       
        public HomeController(
            ILogger<HomeController> logger,
            IElasticClient elasticClient,
            ElasticContext context)
        {
            _elasticClient = elasticClient;
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            _logger.LogInformation("LogInformation");

            return View();
        }

        public IActionResult Privacy()
        {
            _logger.LogWarning("LogWarning");

            return View();
        }

        public async Task<IActionResult> Inserir(int quantidade = 1, Guid? publicacaoId = null)
        {
            //var query = new QueryContainerDescriptor<Publicacao>().MatchAll();
            //var response = await _elasticClient.DeleteByQueryAsync<Publicacao>(q => q
            //    .Query(_ => query)
            //    .Index("publicacao")
            //);

            //if (!response.IsValid)
            //    throw new Exception(response.ServerError?.ToString(), response.OriginalException);

            //var publicacoes = new List<Publicacao>();
            //publicacaoId = publicacaoId == null ? Guid.NewGuid() : publicacaoId;
            //for (var i = 0; i <= quantidade; i++)
            //{
            //    var success = i % 2 == 0 ? true : false;
            //    var message = success ? "Parabéns e-mail enviado com sucesso" : "Falha ao enviar o e-mail";
            //    var publicacao = new Publicacao(success, message, publicacaoId);

            //    publicacoes.Add(publicacao);
            //}

            //await _elasticClient.IndexManyAsync(publicacoes);

            var movies = new List<Movies>();
            for (var i = 0; i < quantidade; i++)
            {
                var movie = new Movies(Guid.NewGuid().ToString(), new Random().Next(2022, 2022), $"Filme {i}");
                movies.Add(movie);
            }

            var resultInsert = await _elasticClient.IndexManyAsync(movies);
            if (!resultInsert.IsValid) throw new Exception(resultInsert.ServerError?.ToString(), resultInsert.OriginalException);

            var movieUpdated = movies[0];
            movieUpdated.title = "Filme Updated";
            var resultUpdate = await _elasticClient.UpdateAsync(DocumentPath<Movies>.Id(movieUpdated.Id).Index("movies"), x=> x.Doc(movieUpdated));
            if (!resultUpdate.IsValid) throw new Exception(resultUpdate.ServerError?.ToString(), resultUpdate.OriginalException);

            movieUpdated.title = "Filme Updated Partial";
            var request = new UpdateRequest<Movies, object>("movies", movieUpdated.Id)
            {
                Doc = movieUpdated
            };
            var resultUpdatePartial = await _elasticClient.UpdateAsync(request);
            if (!resultUpdatePartial.IsValid) throw new Exception(resultUpdatePartial.ServerError?.ToString(), resultUpdatePartial.OriginalException);

            //var search = new SearchDescriptor<Publicacao>("publicacao").MatchAll();
            //var result = await _elasticClient.SearchAsync<Publicacao>(search);

            //if (!result.IsValid)
            //    throw new Exception(result.ServerError?.ToString(), result.OriginalException);

            return RedirectToAction("Logs", new { publicacaoId = publicacaoId });
        }

        [HttpGet]
        public IActionResult Logs(string publicacaoId = "d0d91d15-23a1-4d4a-855c-c699b2dddd00")
        {
            ViewBag.PublicacaoId = publicacaoId;
            return View(new PublicacaoViewModel());
        }

        [HttpGet]
        public IActionResult InserirSql()
        {
            _context.Publicacao.Add(new Publicacao(true, "teste"));
            _context.SaveChanges();

            return View();
        }

        [HttpGet]
        public IActionResult ConsultarSql()
        {
            var publicacoes = _context.Publicacao.ToList();
            return View(publicacoes);
        }

        [HttpGet]
        public IActionResult Exception()
        {
            var number = Convert.ToInt32("d0d91d15-23a1-4d4a-855c-c699b2dddd00");
            return View();
        }

        [HttpPost]
        public IActionResult ObterLogs(string publicacaoId = "d0d91d15-23a1-4d4a-855c-c699b2dddd00")
        {
            var model = new PublicacaoViewModel();
            //var filters = new List<Func<QueryContainerDescriptor<Publicacao>, QueryContainer>>();
            //filters.Add(fq => fq.Terms(t => t.Field(f => f.Success).Terms(true)));
            //filters.Add(fq => fq.Terms(t => t.Field(f => f.PublicacaoId).Terms(new[] { "2b480ccf-39c3-4aef-9278-7e05f8d7a789" })));
            //filters.Add(fq => fq.MatchPhrase(p => p.Field(field).Query("true")));
            //filters.Add(fq => fq.MatchPhrase(p => p.Field("message").Query("Parabéns e-mail enviado com sucesso")));

            //var result = _elasticClient.Search<Publicacao>(s =>
            //    s.Index("publicacao")
            //        .Query(q => q.Bool(b => b.Filter(filters)))
            //        .Size(100));

            var query = new QueryContainerDescriptor<Publicacao>().MatchPhrase(p => p.Field(x => x.Success).Query("true"));
            var query2 = new QueryContainerDescriptor<Publicacao>().MatchPhrase(p => p.Field(x => x.PublicacaoId).Query(publicacaoId));
            var result = _elasticClient.Search<Publicacao>(s =>
                s.Index("publicacao")
                  .Query(_ => query && query2)
                  .Size(5)
                  .TrackTotalHits());

            if (!result.IsValid)
                throw new Exception(result.ServerError?.ToString(), result.OriginalException);

            model.TotalSucesso = result.Total;
            model.PublicacoesSucesso = result.Documents.ToList();

            //filters = new List<Func<QueryContainerDescriptor<Publicacao>, QueryContainer>>();
            //filters.Add(fq => fq.Terms(t => t.Field(f => f.Success).Terms(false)));
            //filters.Add(fq => fq.Terms(t => t.Field(f => f.PublicacaoId).Terms(new[] { "c971ba91-28dc-4a2d-90b0-91d98e57dd46" })));
            //filters.Add(fq => fq.MatchPhrase(p => p.Field(field).Query("false")));
            //filters.Add(fq => fq.MatchPhrase(p => p.Field("message").Query("Falha ao enviar o e-mail")));

            result = _elasticClient.Search<Publicacao>(s =>
                s.Index("publicacao")
                  .Query(q =>
                    q.MatchPhrase(p => p.Field(x => x.Success).Query("false")) &&
                    q.MatchPhrase(p => p.Field(x => x.PublicacaoId).Query(publicacaoId))
                  )
                  .Size(5)
                  .TrackTotalHits());

            if (!result.IsValid)
                throw new Exception(result.ServerError?.ToString(), result.OriginalException);

            model.TotalFalha = result.Total;
            model.PublicacoesFalha = result.Documents.ToList();

            //query = new QueryContainerDescriptor<Publicacao>().MatchPhrase(p => p.Field(x => x.PublicacaoId).Query(publicacaoId));
            //result = _elasticClient.Search<Publicacao>(s =>
            //    s.Index("publicacao")
            //        .Query(_ => query)
            //        .Aggregations(s => s.Sum("TotalSucesso", sa => sa.Field(o => o.TotalSucesso))
            //            .Sum("TotalFalha", sa => sa.Field(p => p.TotalFalha))));

            //if (!result.IsValid)
            //    throw new Exception(result.ServerError?.ToString(), result.OriginalException);

            //var totalSucesso = NestExtensions.ObterBucketAggregationDouble(result.Aggregations, "TotalSucesso");
            //var totalFalha = NestExtensions.ObterBucketAggregationDouble(result.Aggregations, "TotalFalha");

            //model.TotalFalha = totalFalha;
            //model.TotalSucesso = totalSucesso;

            //query = new QueryContainerDescriptor<Publicacao>().Match(p => p.Field(field).Query(value));
            //result = _elasticClient.Search<Publicacao>(s =>
            //     s.Index("publicacao")
            //      .Query(_ => query));

            //query = new QueryContainerDescriptor<Publicacao>().Term(p => field, value);
            //result = _elasticClient.Search<Publicacao>(s =>
            //    s.Index("publicacao")
            //        .Query(_ => query));

            //query = new QueryContainerDescriptor<Publicacao>().MatchPhrasePrefix(p => p.Field(field).Query(value));
            //result = _elasticClient.Search<Publicacao>(s =>
            //    s.Index("publicacao")
            //        .Query(_ => query));

            //query = new QueryContainerDescriptor<Publicacao>().Wildcard(p => p.Field(f => f.PublicacaoId).Value(field + "*"));
            //result = _elasticClient.Search<Publicacao>(s =>
            //    s.Index("publicacao")
            //        .Query(_ => query));

            //query = new QueryContainerDescriptor<Publicacao>().Wildcard(p => p.Field(f => f.PublicacaoId).Value(field + "*"));
            //result = _elasticClient.Search<Publicacao>(s =>
            //    s.Index("publicacao")
            //        .Query(_ => query));

            //var search = new SearchDescriptor<Publicacao>("publicacao").Query(q => q.Term(x => x.PublicacaoId.ToString().ToLowerInvariant(), field.ToString().ToLowerInvariant()));
            //result = _elasticClient.Search<Publicacao>(search);

            ViewBag.PublicacaoId = publicacaoId;
            return View("Logs", model);
        }

        public IActionResult Error()
        {
            _logger.LogError("Testando View de Error");
            return RedirectToAction("Index");
        }
    }

    public static class NestExtensions
    {
        public static double ObterBucketAggregationDouble(AggregateDictionary agg, string bucket)
        {
            return agg.BucketScript(bucket).Value.HasValue ? agg.BucketScript(bucket).Value.Value : 0;
        }
    }
}
