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

    public class UsuariosController : ControllerBase
    {

        private readonly UI db;
        private readonly HttpClient httpClient;

        public UsuariosController(UI db, IHttpClientFactory httpClientFactory)
        {
            this.db = db;
            this.httpClient = httpClientFactory.CreateClient();
        }


        [HttpGet("ListarUsuarios")]
        public async Task<IActionResult> GetPelicula()
        {
            var sqlCommand = new SqlCommand("SELECT Nombre, Apellido, Email, Password, rol FROM Usuarios"); // Ejemplo de consulta directa en el controlador
            return await db.TableResult(sqlCommand);
        }


        [HttpGet("GetUsuario")]
        public async Task<IActionResult> GetUsuario([FromQuery] string email, [FromQuery] string contrasena)
        {
            var sqlCommand = new SqlCommand("SELECT Nombre, Apellido, Email, Password, rol FROM Usuarios WHERE Email = @Email");
            sqlCommand.Parameters.AddWithValue("@Email", email);

            var dt = await db.Table(sqlCommand);

            if (dt.Rows.Count == 0)
            {
                // El usuario no está registrado
                return NotFound("Usuario no registrado");
            }
            else
            {
                // Usuario encontrado, verificar la contraseña
                var usuario = dt.Rows[0];
                string contraseñaGuardada = usuario["Password"].ToString();

                if (contraseñaGuardada == contrasena)
                {
                    // Contraseña correcta, devolver los detalles del usuario
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
                else
                {
                    // Contraseña incorrecta
                    return BadRequest("Usuario o contraseña incorrecta");
                }
            }
        }


        [HttpPost("RegistrarUsuario")]
        public async Task<IActionResult> RegistrarUsuario([FromBody] UsuarioNuevoModel nuevoUsuario)
        {
            // Validar si el correo ya existe en la base de datos
            var verificarCorreo = new SqlCommand("SELECT COUNT(*) FROM Usuarios WHERE Email = @Email");
            verificarCorreo.Parameters.AddWithValue("@Email", nuevoUsuario.Email);

            var existencia = await db.Escalar(verificarCorreo);

            if (existencia > 0)
            {
                // Si el correo ya existe, devolver un error
                return BadRequest(new { message = "El correo ya existe." });
            }
            else
            {
                // Insertar el nuevo usuario en la base de datos
                var insertarUsuario = new SqlCommand("INSERT INTO Usuarios (Nombre, Apellido, Email, Password, Rol) VALUES (@Nombre, @Apellido, @Email, @Password, @Rol)");
                insertarUsuario.Parameters.AddWithValue("@Nombre", nuevoUsuario.Nombre);
                insertarUsuario.Parameters.AddWithValue("@Apellido", nuevoUsuario.Apellido);
                insertarUsuario.Parameters.AddWithValue("@Email", nuevoUsuario.Email);
                insertarUsuario.Parameters.AddWithValue("@Password", nuevoUsuario.Password);
                insertarUsuario.Parameters.AddWithValue("@Rol", nuevoUsuario.rol);

                await db.Ejecutar(insertarUsuario);

                // Devolver un mensaje de éxito
                return Ok(new { message = "Usuario registrado exitosamente." });
            }
        }


        [HttpPost("PeliculaFavorita")]
        public async Task<IActionResult> PeliculaFavorita([FromBody] PeliculaFavorita peliculaFavorita)
        {
            var verificarPeliculaFav = new SqlCommand("SELECT COUNT(*) FROM PeliculasFavoritas WHERE usuario = @usuario AND fav_pelicula = @fav_pelicula");
            verificarPeliculaFav.Parameters.AddWithValue("@usuario", peliculaFavorita.usuario);
            verificarPeliculaFav.Parameters.AddWithValue("@fav_pelicula", peliculaFavorita.fav_pelicula);

            var existencia = await db.Escalar(verificarPeliculaFav);

            if (existencia > 0)
            {
                // Si la película ya está registrada como favorita para ese usuario, devolver un error
                return BadRequest(new { message = "La película ya está registrada como favorita para este usuario." });
            }
            else
            {
                var insertarFavs = new SqlCommand("INSERT INTO PeliculasFavoritas (usuario, fav_pelicula) VALUES (@usuario, @fav_pelicula)");
                insertarFavs.Parameters.AddWithValue("@usuario", peliculaFavorita.usuario);
                insertarFavs.Parameters.AddWithValue("@fav_pelicula", peliculaFavorita.fav_pelicula);

                await db.Ejecutar(insertarFavs);

                return Ok(new { message = "Pelicula registrada en favoritas." });
            }
        }

        [HttpGet("PeliculasFavoritas/{usuario}")]
        public async Task<IActionResult> GetPeliculasFavoritas(string usuario)
        {
            try
            {
                // Consulta SQL para seleccionar las películas favoritas del usuario
                var consulta = new SqlCommand("SELECT fav_pelicula FROM PeliculasFavoritas WHERE usuario = @usuario");
                consulta.Parameters.AddWithValue("@usuario", usuario);

                // Ejecutar la consulta y obtener los resultados
                var dt = await db.Table(consulta);

                // Convertir el resultado a una lista de strings con los IDs de las películas favoritas
                List<string> peliculasFavoritas = new List<string>();
                foreach (DataRow row in dt.Rows)
                {
                    peliculasFavoritas.Add(row["fav_pelicula"].ToString());
                }

                return Ok(peliculasFavoritas);
            }
            catch (Exception ex)
            {
                // Manejo de errores
                return StatusCode(500, new { message = "Error al obtener las películas favoritas del usuario.", error = ex.Message });
            }
        }


        [HttpPut("ActualizarUsuario")]
        public async Task<IActionResult> ActualizarUsuario([FromBody] UsuarioActualizarModel usuarioActualizar)
        {
            try
            {
                // Verificar si el usuario existe en la base de datos
                var verificarUsuario = new SqlCommand("SELECT COUNT(*) FROM Usuarios WHERE Email = @Email");
                verificarUsuario.Parameters.AddWithValue("@Email", usuarioActualizar.Email);

                var existencia = await db.Escalar(verificarUsuario);

                if (existencia == 0)
                {
                    // Si el usuario no existe, devolver un error
                    return BadRequest(new { message = "El usuario no existe." });
                }
                else
                {
                    // Actualizar los datos del usuario en la base de datos
                    var actualizarUsuario = new SqlCommand("UPDATE Usuarios SET Nombre = @Nombre, Apellido = @Apellido, Rol = @Rol, Password = @Password WHERE Email = @Email");
                    actualizarUsuario.Parameters.AddWithValue("@Nombre", usuarioActualizar.Nombre);
                    actualizarUsuario.Parameters.AddWithValue("@Apellido", usuarioActualizar.Apellido);
                    actualizarUsuario.Parameters.AddWithValue("@Rol", usuarioActualizar.Rol);
                    actualizarUsuario.Parameters.AddWithValue("@Password", usuarioActualizar.Password);
                    actualizarUsuario.Parameters.AddWithValue("@Email", usuarioActualizar.Email);

                    await db.Ejecutar(actualizarUsuario);

                    // Devolver un mensaje de éxito
                    return Ok(new { message = "Usuario actualizado exitosamente." });
                }
            }
            catch (Exception ex)
            {
                // Manejo de errores
                return StatusCode(500, new { message = "Error al actualizar el usuario.", error = ex.Message });
            }
        }


        [HttpDelete("EliminarUsuario")]
        public async Task<IActionResult> EliminarUsuario([FromQuery] string email)
        {
            try
            {
                // Verificar si el usuario existe en la base de datos
                var verificarUsuario = new SqlCommand("SELECT COUNT(*) FROM Usuarios WHERE Email = @Email");
                verificarUsuario.Parameters.AddWithValue("@Email", email);

                var existencia = await db.Escalar(verificarUsuario);

                if (existencia == 0)
                {
                    // Si el usuario no existe, devolver un error
                    return BadRequest(new { message = "El usuario no existe." });
                }
                else
                {
                    // Eliminar el usuario de la base de datos
                    var eliminarUsuario = new SqlCommand("DELETE FROM Usuarios WHERE Email = @Email");
                    eliminarUsuario.Parameters.AddWithValue("@Email", email);

                    await db.Ejecutar(eliminarUsuario);

                    // Devolver un mensaje de éxito
                    return Ok(new { message = "Usuario eliminado exitosamente." });
                }
            }
            catch (Exception ex)
            {
                // Manejo de errores
                return StatusCode(500, new { message = "Error al eliminar el usuario.", error = ex.Message });
            }
        }


    }



}
