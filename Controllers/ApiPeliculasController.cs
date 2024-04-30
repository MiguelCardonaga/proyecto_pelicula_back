using System;
using System.Data;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using peliculasproyecto.AppCode.Converters;


namespace peliculasproyecto.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApiPeliculasController : ControllerBase
    {
        private readonly UI db;
        private readonly HttpClient httpClient;
        private readonly string tmdbApiKey = "327750dcddd61c2069099c5a2e4788c3"; // Clave API de TMDb

        public ApiPeliculasController(UI db, IHttpClientFactory httpClientFactory)
        {
            this.db = db;
            this.httpClient = httpClientFactory.CreateClient();
            this.httpClient.BaseAddress = new Uri("https://api.themoviedb.org/3/");
        }


        [HttpGet("ListarPeliculas")]
        public async Task<IActionResult> GetPeliculas()
        {
            var endpoint = $"discover/movie?api_key={tmdbApiKey}&language=es&sort_by=popularity.desc";

            HttpResponseMessage response = await httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                string jsonString = await response.Content.ReadAsStringAsync();
                var peliculas = JsonSerializer.Deserialize<TMDBResponse>(jsonString);
                return Ok(peliculas);
            }
            else
            {
                return StatusCode((int)response.StatusCode);
            }
        }


        [HttpGet("BuscarPelicula")]
        public async Task<IActionResult> BuscarPelicula([FromQuery] int id, [FromQuery] string nombre)
        {
            var endpoint = $"search/movie?api_key={tmdbApiKey}&language=es&query={nombre}";
            var response = await httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                string jsonString = await response.Content.ReadAsStringAsync();
                var peliculasExternas = JsonSerializer.Deserialize<TMDBResponse>(jsonString).results;

                // Filtrar para obtener solo las primeras 3 películas
                var primerasTresPeliculas = peliculasExternas.Take(3).ToList();

                // Iterar sobre las películas seleccionadas y obtener la URL de la imagen
                foreach (var pelicula in primerasTresPeliculas)
                {
                    // Comprobar si la película tiene una ruta de imagen
                    if (!string.IsNullOrEmpty(pelicula.poster_path))
                    {
                        // Construir la URL completa de la imagen utilizando la base URL de TMDb
                        pelicula.poster_path = $"https://image.tmdb.org/t/p/w500/{pelicula.poster_path}";
                    }
                }

                return Ok(primerasTresPeliculas);
            }
            else
            {
                return StatusCode((int)response.StatusCode);
            }
        }

        [HttpDelete("EliminarPeliculaFavorita")]
        public async Task<IActionResult> EliminarPeliculaFavorita([FromQuery] string usuario, [FromQuery] string fav_pelicula)
        {
            try
            {
                // Crear el comando SQL para eliminar la película favorita
                var commandText = "DELETE FROM PeliculasFavoritas WHERE usuario = @Usuario AND fav_pelicula = @Pelicula";
                var command = new SqlCommand(commandText);
                command.Parameters.AddWithValue("@Usuario", usuario);
                command.Parameters.AddWithValue("@Pelicula", fav_pelicula);

                // Ejecutar el comando SQL
                await db.Ejecutar(command);

                // Retornar una respuesta exitosa con un objeto JSON
                return Ok(new { message = "Pelicula favorita eliminada correctamente" });
            }
            catch (Exception ex)
            {
                // Si ocurre un error, retornar un error interno del servidor
                return StatusCode(500, new { error = $"Error al eliminar la película favorita: {ex.Message}" });
            }
        }



    }

    public class TMDBResponse
    {
   
        public TMDBPelicula[] results { get; set; }
    }

    public class TMDBPelicula
    {
        public int id { get; set; }
        public string title { get; set; }
        public string overview { get; set; }
        public string poster_path { get; set; }
        public string backdrop_path { get; set; }
        public string release_date { get; set; }
        public double vote_average { get; set; }
        public double popularity { get; set; }
    }

    public class UsuarioNuevoModel
    {
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string rol { get; set; }
    }

    public class PeliculaFavorita
    {
    
        public string usuario { get; set; }
        public string fav_pelicula { get; set; }
    }

    public class UsuarioActualizarModel
    {
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Email { get; set; }
        public int Rol { get; set; }
        public string Password { get; set; }
    }

    public class UI
    {
        private string cadenaSQL { get; set; } = string.Empty;

        public UI(string cadenaSQL)
        {
            this.cadenaSQL = cadenaSQL;
        }

        public async Task<DataTable> Table(SqlCommand command)
        {
            var dt = new DataTable();

            using (var cnn = new SqlConnection(this.cadenaSQL))
            {
                command.Connection = cnn;
                using (var adapter = new SqlDataAdapter(command))
                {
                    await cnn.OpenAsync();
                    adapter.Fill(dt);
                }
            }

            return dt;
        }

        public async Task<ContentResult> TableResult(SqlCommand command)
        {
            var dt = await this.Table(command);
            var options = new JsonSerializerOptions()
            {
                Converters = { new DataTableConverter() }
            };

            string jsonDataTable = JsonSerializer.Serialize(dt, options);

            var content = new ContentResult();
            content.Content = jsonDataTable;
            content.ContentType = "application/json";

            return content;
        }

        public async Task<int> Escalar(SqlCommand command)
        {
            using (var cnn = new SqlConnection(this.cadenaSQL))
            {
                command.Connection = cnn;
                await cnn.OpenAsync();
                return (int)await command.ExecuteScalarAsync();
            }
        }

        public async Task Ejecutar(SqlCommand command)
        {
            using (var cnn = new SqlConnection(this.cadenaSQL))
            {
                command.Connection = cnn;
                await cnn.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
        }


    }

}
