using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using TvTube.Search.Models;
using Version = Lucene.Net.Util.Version;

namespace TvTube.Search.Services {
    public static class TvTubeLuceneSearchService {

        public static string LuceneDir = Path.Combine(HttpContext.Current.Request.PhysicalApplicationPath, "lucene_index");

        private static FSDirectory tempDirectory;

        private static FSDirectory Directory {
            get {
                if (tempDirectory == null)
                    tempDirectory = FSDirectory.Open(new DirectoryInfo(LuceneDir));
                if (IndexWriter.IsLocked(tempDirectory))
                    IndexWriter.Unlock(tempDirectory);
                string path = Path.Combine(LuceneDir, "write.lock");
                if (File.Exists(path))
                    File.Delete(path);
                return tempDirectory;
            }
        }

        private static IEnumerable<TvChannel> mapLuceneToDataList(IEnumerable<Document> hits) {
            return hits.Select(mapLuceneDocumentToData).ToList();
        }

        private static IEnumerable<TvChannel> mapLuceneToDataList(IEnumerable<ScoreDoc> hits, IndexSearcher searcher) {
            return hits.Select(hit => mapLuceneDocumentToData(searcher.Doc(hit.Doc))).ToList();
        }

        public static TvChannel mapLuceneDocumentToData(Document doc) {
            return new TvChannel {
                Id = Convert.ToInt32(doc.Get("Id")),
                Name = doc.Get("Name"),
                Description = doc.Get("Description")
            };
        }

        private static Query parseQuery(string searchQuery, QueryParser parser) {
            try {
                return parser.Parse(searchQuery.Trim());
            } catch (ParseException ex) {
                return parser.Parse(QueryParser.Escape(searchQuery.Trim()));
            }
        }

        public static IEnumerable<TvChannel> GetAllIndexRecords() {
            if (!System.IO.Directory.EnumerateFiles(LuceneDir).Any())
                return new List<TvChannel>();
            IndexSearcher indexSearcher = new IndexSearcher(Directory, false);
            IndexReader indexReader = IndexReader.Open(Directory, false);
            List<Document> list = new List<Document>();
            TermDocs termDocs = indexReader.TermDocs();
            while (termDocs.Next())
                list.Add(indexSearcher.Doc(termDocs.Doc));
            indexReader.Dispose();
            indexSearcher.Dispose();
            return mapLuceneToDataList(list);
        }

        public static IEnumerable<TvChannel> Search(string input, string fieldName = "") {
            if (string.IsNullOrEmpty(input))
                return new List<TvChannel>();
            input = string.Join(" ", input.Trim().Replace("-", " ").Split(' ').Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Trim() + "*"));
            return search(input, fieldName);
        }

        public static IEnumerable<TvChannel> SearchDefault(string input, string fieldName = "") {
            if (!string.IsNullOrEmpty(input))
                return Search(input, fieldName);
            return new List<TvChannel>();
        }

        private static IEnumerable<TvChannel> search(string searchQuery, string searchField = "") {
            if (string.IsNullOrEmpty(searchQuery.Replace("*", "").Replace("?", "")))
                return new List<TvChannel>();
            using (IndexSearcher searcher = new IndexSearcher(Directory, false)) {
                int n = 1000;
                StandardAnalyzer standardAnalyzer = new StandardAnalyzer(Version.LUCENE_30);
                if (!string.IsNullOrEmpty(searchField)) {
                    QueryParser parser = new QueryParser(Version.LUCENE_30, searchField, standardAnalyzer);
                    Query query = parseQuery(searchQuery, parser);
                    IEnumerable<TvChannel> enumerable = mapLuceneToDataList(searcher.Search(query, n).ScoreDocs, searcher);
                    standardAnalyzer.Close();
                    searcher.Dispose();
                    return enumerable;
                }
                MultiFieldQueryParser fieldQueryParser = new MultiFieldQueryParser(Version.LUCENE_30, new string[3]
                {
                    "Id",
                    "Name",
                    "Description"
                }, standardAnalyzer);
                Query query1 = parseQuery(searchQuery, fieldQueryParser);
                IEnumerable<TvChannel> enumerable1 = mapLuceneToDataList(searcher.Search(query1, null, n, Sort.RELEVANCE).ScoreDocs, searcher);
                standardAnalyzer.Close();
                searcher.Dispose();
                return enumerable1;
            }
        }

        public static void AddUpdateLuceneIndex(TvChannel tvChannel) {
            AddUpdateLuceneIndex(new List<TvChannel> {
                tvChannel
            });
        }

        public static void AddUpdateLuceneIndex(IEnumerable<TvChannel> tvChannels) {
            if (!System.IO.Directory.Exists(LuceneDir))
                System.IO.Directory.CreateDirectory(LuceneDir);
            StandardAnalyzer standardAnalyzer = new StandardAnalyzer(Version.LUCENE_30);
            using (IndexWriter writer = new IndexWriter(Directory, standardAnalyzer, IndexWriter.MaxFieldLength.UNLIMITED)) {
                foreach (TvChannel tvChannel in tvChannels)
                    addToLuceneIndex(tvChannel, writer);
                standardAnalyzer.Close();
                writer.Dispose();
            }
        }

        private static void addToLuceneIndex(TvChannel tvChannel, IndexWriter writer) {
            TermQuery termQuery = new TermQuery(new Term("Id", tvChannel.Id.ToString()));
            writer.DeleteDocuments(termQuery);
            Document doc = new Document();
            doc.Add(new Field("Id", tvChannel.Id.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
            doc.Add(new Field("Name", tvChannel.Name, Field.Store.YES, Field.Index.ANALYZED));
            doc.Add(new Field("Description", tvChannel.Description, Field.Store.YES, Field.Index.ANALYZED));
            writer.AddDocument(doc);
        }

        public static void ClearLuceneIndexRecord(int recordId) {
            StandardAnalyzer standardAnalyzer = new StandardAnalyzer(Version.LUCENE_30);
            using (IndexWriter indexWriter = new IndexWriter(Directory, standardAnalyzer, IndexWriter.MaxFieldLength.UNLIMITED)) {
                TermQuery termQuery = new TermQuery(new Term("Id", recordId.ToString()));
                indexWriter.DeleteDocuments(termQuery);
                standardAnalyzer.Close();
                indexWriter.Dispose();
            }
        }

        public static bool ClearLuceneIndex() {
            try {
                StandardAnalyzer standardAnalyzer = new StandardAnalyzer(Version.LUCENE_30);
                using (IndexWriter indexWriter = new IndexWriter(Directory, standardAnalyzer, true, IndexWriter.MaxFieldLength.UNLIMITED)) {
                    indexWriter.DeleteAll();
                    standardAnalyzer.Close();
                    indexWriter.Dispose();
                }
            } catch (Exception ex) {
                return false;
            }
            return true;
        }

        public static void Optimize() {
            StandardAnalyzer standardAnalyzer = new StandardAnalyzer(Version.LUCENE_30);
            using (IndexWriter indexWriter = new IndexWriter(Directory, standardAnalyzer, IndexWriter.MaxFieldLength.UNLIMITED)) {
                standardAnalyzer.Close();
                indexWriter.Optimize();
                indexWriter.Dispose();
            }
        }
    }
}
