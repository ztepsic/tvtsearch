using System.Web.Http;

namespace TvTube.Search {
    public static class WebApiConfig {
        public static void Register(HttpConfiguration config) {
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute("TvTubeSearchApi.Ping", "api/tvtube/ping", new {
                Controller = "TvTubeSearch",
                Action = "Ping"
            });

            config.Routes.MapHttpRoute("TvTubeSearchApi.GetAllIndexes", "api/tvtube/search/getallindexes", new {
                Controller = "TvTubeSearch",
                Action = "GetAllIndexes"
            });


            config.Routes.MapHttpRoute("TvTubeSearchApi.RemoveIndex", "api/tvtube/search/removeindex/{id}", new {
                Controller = "TvTubeSearch",
                Action = "RemoveIndex"
            });
            
            config.Routes.MapHttpRoute("TvTubeSearchApi.ClearIndex", "api/tvtube/search/clearindex", new {
                Controller = "TvTubeSearch",
                Action = "ClearIndex"
            });
            
            config.Routes.MapHttpRoute("TvTubeSearchApi.CreateIndex", "api/tvtube/search/createindex", new {
                Controller = "TvTubeSearch",
                Action = "CreateIndex"
            });
            
            config.Routes.MapHttpRoute("TvTubeSearchApi", "api/tvtube/search/{searchTerm}", new {
                Controller = "TvTubeSearch",
                Action = "Search",
                searchTerm = RouteParameter.Optional
            });
            
            config.Routes.MapHttpRoute("DefaultApi", "api/{controller}/{id}", new {
                id = RouteParameter.Optional
            });
        }
    }
}
