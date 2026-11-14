using Microsoft.Data.Sqlite;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using StravaStats.BusinessObjects.Cities;

namespace StravaStats.Services
{
    public class CitiesService
    {
        SqliteConnectionStringBuilder? connectionStringBuilder;
        public Dictionary<string, Region> Regions;
        public Dictionary<string, City> Cities;

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
            LoadAllCities();
            LoadAllRegions();
        }

        public async Task LoadAllRegions()
        {
            Regions = [];
            if (connectionStringBuilder is null)
            {
                throw new InvalidOperationException("CitiesService is not initialized.");
            }
            using var connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
            await connection.OpenAsync();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM parents";
            using var reader = cmd.ExecuteReader();
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
                    { "name", name },
                    { "fill", "rgba(0,0,0,0.1)" },
                    { "stroke", "#f00" }
                });

                var region = new BusinessObjects.Cities.Region
                {
                    Geometry = geometry,
                    Feature = feature,
                    Id = id,
                    Name = name,
                    Country = country
                };

                Regions.TryAdd(region.Id, region);
            }
        }

        public async Task LoadAllCities()
        {
            Cities = [];
            if (connectionStringBuilder is null)
            {
                throw new InvalidOperationException("CitiesService is not initialized.");
            }
            using var connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
            await connection.OpenAsync();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM cities";
            using var reader = cmd.ExecuteReader();
            var geoReader = new GeoPackageGeoReader();
            var geoSerializer = GeoJsonSerializer.Create();
            while (reader.Read())
            {
                if (reader.IsDBNull(1) || reader.IsDBNull(2) || reader.IsDBNull(3) || reader.IsDBNull(4) || reader.IsDBNull(5))
                    continue; // Skip rows with null values

                Geometry geometry = geoReader.Read(reader.GetFieldValue<byte[]>(1));
                string id = reader.GetString(2);
                string name = reader.GetString(3);
                string country = reader.GetString(4);
                string parent = reader.GetString(5);
                var feature = new Feature(geometry, new AttributesTable
                {
                    { "name", name },
                    { "fill", "rgba(100,255,100,0.2)" },
                    { "stroke", "#0f0" }
                });

                var city = new BusinessObjects.Cities.City
                {
                    Geometry = geometry,
                    Feature = feature,
                    Id = id,
                    Name = name,
                    ParentId = parent
                };

                Cities.TryAdd(city.Id, city);
            }
        }

        public async Task<List<BaseRegion>> GetAllRegionsAsync()
        {
            if (Regions is null)
                await LoadAllRegions();
            return Regions.Select(kv => (BaseRegion)kv.Value).ToList();
        }

        public async Task<List<BaseRegion>> GetAllCities()
        {
            if (Cities is null)
                await LoadAllCities();
            return Cities.Select(kv => (BaseRegion)kv.Value).ToList();
        }

        public (City, Region)? GetCityByCoordinates(double lat, double lon)
        {
            if (Cities is null)
                throw new InvalidOperationException("Cities data is not loaded.");
            if(Regions is null)
                throw new InvalidOperationException("Regions data is not loaded.");

            var factory = new GeometryFactory(new PrecisionModel(), 4326);
            Point point = factory.CreatePoint(new Coordinate(lon, lat));
            Region? region = null;
            foreach (var r in Regions.Values)
            {
                if (r.Geometry.Contains(point))
                {
                    region = r;
                    break;
                }
            }
            if (region is null)
                return null;

            var citiesInRegion = Cities.Values.Where(c => c.ParentId == region?.Id);
            foreach (var city in citiesInRegion)
            {
                if (city.Geometry.Contains(point))
                {
                    return (city, region);
                }
            }
            return null;
        }
    }
}
