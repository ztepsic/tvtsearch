using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using Dapper;
using MySql.Data.MySqlClient;
using TvTube.Search.Models;

namespace TvTube.Search.Repositories {
    public class TvChannelsRepository {
        private static IDbConnection getConnection() {
            return new MySqlConnection(ConfigurationManager.ConnectionStrings["TvTubeDb"].ConnectionString);
        }

        public TvChannel Get(int id) {
            return GetAll().SingleOrDefault(x => x.Id.Equals(id));
        }

        public IEnumerable<TvChannel> GetAll() {
            IDbConnection connection = getConnection();
            connection.Open();
            IEnumerable<TvChannel> enumerable = connection.Query<TvChannel>(@"
                select tvch.tv_channel_id Id
                ,      tvch.tv_channel_name Name
                ,      tvch.tv_channel_description Description
                ,      cntr.country_name Country
                from zt_tv_channels tvch
                join zt_countries cntr ON
                    tvch.country_id = cntr.country_id
                where tv_channel_published = 1;",
                null, null, true, new int?(), new CommandType?());
            connection.Close();
            return enumerable;
        }
    }
}
