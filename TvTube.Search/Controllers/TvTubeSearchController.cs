using System.Collections.Generic;
using System.Web.Http;
using TvTube.Search.Models;
using TvTube.Search.Repositories;
using TvTube.Search.Services;

namespace TvTube.Search.Controllers {
    public class TvTubeSearchController : ApiController {

        [HttpGet]
        [HttpHead]
        public string Ping() {
            return "Pong";
        }

        [HttpGet]
        public IEnumerable<TvChannel> Search(string searchTerm) {
            return TvTubeLuceneSearchService.Search(searchTerm, "");
        }

        [HttpGet]
        public IEnumerable<TvChannel> GetAllIndexes() {
            return TvTubeLuceneSearchService.GetAllIndexRecords();
        }

        [HttpGet]
        public IHttpActionResult CreateIndex() {
            TvTubeLuceneSearchService.AddUpdateLuceneIndex(new TvChannelsRepository().GetAll());
            return Ok(TvTubeLuceneSearchService.LuceneDir);
        }

        [HttpGet]
        public IHttpActionResult ClearIndex() {
            TvTubeLuceneSearchService.ClearLuceneIndex();
            return Ok(TvTubeLuceneSearchService.LuceneDir);
        }

        [HttpGet]
        public IHttpActionResult RemoveIndex(int id) {
            TvTubeLuceneSearchService.ClearLuceneIndexRecord(id);
            return Ok(TvTubeLuceneSearchService.LuceneDir);
        }
    }
}
