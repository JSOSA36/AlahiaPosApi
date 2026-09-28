using AlahiaPos.Entities.Domain;
using AlahiaPosApi.Controllers;
using Xunit;

namespace AlahiaPosApi.Pase.Tests
{
    /// <summary>
    /// ERP vivo no registra SesionAuthFilter. Anular/eliminar no pueden
    /// depender de SesionHttp.TryGet ni olvidar FacturaCargo.
    /// </summary>
    public class AnularEliminarOrdenContratoTests
    {
        [Fact]
        public void Anular_resuelve_usuario_como_el_cobro_no_exige_token_de_filtro()
        {
            var src = PaseRepo.ReadApiFile(Path.Combine("Controllers", "FacturaHeaderController.cs"));
            Assert.Contains("IdUsuarioCobroResolver.ResolverAsync", src);
            Assert.Contains("AnularFactura", src);
            Assert.DoesNotContain(
                "if (sesion == null)\r\n                return Unauthorized(new { message = SesionHttp.UnauthorizedToken });",
                src.Replace("\n", "\r\n"));
            var anular = Recorte(src, "public async Task<IActionResult> AnularFactura", "public async Task<decimal> TotalVentaDia");
            Assert.Contains("ResolverIdUsuarioCobroAsync", anular);
            Assert.DoesNotContain("UnauthorizedToken", anular);
        }

        [Fact]
        public void Eliminar_orden_borra_cargos_y_no_facturas_tipo_1()
        {
            var svc = PaseRepo.ReadDataAccessFile(Path.Combine("Servicios", "IFacturaHeaderServices.cs"));
            var eliminar = Recorte(svc, "EliminarFacturaCompleta(int idFactura)", "GetAllFacturaFacturaHeader");
            Assert.Contains("_FacturaCargos", eliminar);
            Assert.Contains("IdTipoDocumentos == 1", eliminar);
            Assert.Contains("header.Clientes = null", eliminar);
            Assert.Contains("header.Empleados = null", eliminar);

            var ctrl = PaseRepo.ReadApiFile(Path.Combine("Controllers", "FacturaHeaderController.cs"));
            var del = Recorte(ctrl, "public async Task<IActionResult> EliminarFactura(int id)", "CuentaxCobrar");
            Assert.Contains("PuedeEliminarOrden", del);
            Assert.Contains("Las facturas se anulan, no se eliminan", del);
        }

        [Fact]
        public void Login_da_permiso_efectivo_al_administrador()
        {
            var src = PaseRepo.ReadApiFile(Path.Combine("Controllers", "LoginController.cs"));
            Assert.Contains("puedeEliminarOrden = usuarioDb.PuedeEliminarOrden || esAdministrador", src);
            Assert.Contains("puedeAnularFactura = usuarioDb.PuedeAnularFactura || esAdministrador", src);
        }

        [Fact]
        public void Administrador_puede_anular_sin_toggle()
        {
            var admin = new Usuarios
            {
                IdUsuario = 1,
                PuedeAnularFactura = false,
                Perfil = new Perfiles { Nombre = "Administrador" }
            };
            Assert.True(FacturaHeaderController.TienePermisoOperativo(admin, admin.PuedeAnularFactura));

            var cajero = new Usuarios
            {
                IdUsuario = 2,
                PuedeAnularFactura = false,
                Perfil = new Perfiles { Nombre = "Cajero" }
            };
            Assert.False(FacturaHeaderController.TienePermisoOperativo(cajero, cajero.PuedeAnularFactura));

            cajero.PuedeAnularFactura = true;
            Assert.True(FacturaHeaderController.TienePermisoOperativo(cajero, cajero.PuedeAnularFactura));
        }

        [Fact]
        public void Frontend_anula_enviando_idUsuario()
        {
            var path = PaseRepo.FindFrontendFile(Path.Combine(
                "src", "app", "Modales", "anular-factura", "anular-factura.component.ts"));
            if (path == null)
                return;
            var src = File.ReadAllText(path);
            Assert.Contains("idUsuario: this.parametros.IdUsuario", src);
        }

        private static string Recorte(string src, string desde, string hasta)
        {
            var i = src.IndexOf(desde, StringComparison.Ordinal);
            Assert.True(i >= 0, "No se encontró: " + desde);
            var j = src.IndexOf(hasta, i, StringComparison.Ordinal);
            Assert.True(j > i, "No se encontró cierre: " + hasta);
            return src[i..j];
        }
    }
}
