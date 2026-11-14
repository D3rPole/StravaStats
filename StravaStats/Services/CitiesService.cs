using Microsoft.Data.Sqlite;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace StravaStats.Services
{
    public class CitiesService
    {
        SqliteConnectionStringBuilder? connectionStringBuilder;

        public CitiesService(IConfiguration configuration)
        {
            string citiesFilePath = configuration.GetValue<string>("CitiesFile");
            if (string.IsNullOrWhiteSpace(citiesFilePath) || !File.Exists(citiesFilePath))
            {
                throw new FileNotFoundException($"Cities file not found: {citiesFilePath}");
            }
            connectionStringBuilder = new SqliteConnectionStringBuilder
            {
                DataSource = citiesFilePath,
                Mode = SqliteOpenMode.ReadOnly
            };
        }

        public async Task<List<BusinessObjects.Cities.BaseRegion>> GetAllRegionsAsync()
        {
            if (connectionStringBuilder is null)
            {
                throw new InvalidOperationException("CitiesService is not initialized.");
            }
            using var connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
            await connection.OpenAsync();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM parents";
            using var reader = cmd.ExecuteReader();
            List<BusinessObjects.Cities.BaseRegion> regions = new List<BusinessObjects.Cities.BaseRegion>();
            var geoReader = new GeoPackageGeoReader();
            var geoSerializer = GeoJsonSerializer.Create();
            while (reader.Read())
            {
                if (reader.IsDBNull(1) || reader.IsDBNull(2) || reader.IsDBNull(3) || reader.IsDBNull(4))
                    continue; // Skip rows with null values

                Geometry geometry = geoReader.Read(reader.GetFieldValue<byte[]>(1));
                string id = reader.GetString(2);
                string name = reader.GetString(3);
                string country = reader.GetString(4);
                var feature = new Feature(geometry, new AttributesTable
                {
                    { "id", id },
                    { "name", name },
                    { "country", country }
                });
                using var sw = new StringWriter();
                geoSerializer.Serialize(sw, feature);
                string geoJson = sw.ToString();

                var region = new BusinessObjects.Cities.Region
                {
                    Geometry = geometry,
                    Feature = geoJson,
                    Id = id,
                    Name = name,
                    Country = country
                };

                regions.Add(region);
            }
            return regions;
        }
    }
}
