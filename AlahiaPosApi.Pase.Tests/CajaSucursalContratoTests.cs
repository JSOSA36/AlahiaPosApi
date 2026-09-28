using Xunit;

namespace AlahiaPosApi.Pase.Tests
{
    public class CajaSucursalContratoTests
    {
        [Fact]
        public void Cierre_filtra_por_sucursal_de_consulta()
        {
            var src = PaseRepo.ReadApiFile(Path.Combine("Controllers", "CajaCierreController.cs"));
            Assert.Contains("SucursalConsultaHttp.ResolverAsync", src);
            Assert.Contains("scope.Incluye", src);
        }

        [Fact]
        public void Apertura_existe_como_controlador()
        {
            var src = PaseRepo.ReadApiFile(Path.Combine("Controllers", "CajaAperturaController.cs"));
            Assert.Contains("CajaApertura", src);
        }

        [Fact]
        public void UltimoCierre_es_del_usuario_no_de_toda_la_empresa()
        {
            var ctrl = PaseRepo.ReadApiFile(Path.Combine("Controllers", "CajaCierreController.cs"));
            Assert.Contains("IdUsuarioCobroResolver.ResolverAsync", ctrl);
            Assert.Contains("GetUltimoCierreAsync", ctrl);

            var iface = PaseRepo.ReadEntitiesFile(Path.Combine("Interfaces", "ICajaCierreService.cs"));
            Assert.Contains("int idUsuario", iface);

            var svc = PaseRepo.ReadDataAccessFile(Path.Combine("Servicios", "CajaCierreServices.cs"));
            Assert.Contains("x.IdUsuario", svc);
            Assert.Contains("idUsuario", svc);

            var feCierre = PaseRepo.FindFrontendFile(Path.Combine(
                "src", "app", "Components", "cierre-caja", "cierre-caja.component.ts"));
            if (feCierre == null)
                return;

            var cierreSrc = File.ReadAllText(feCierre);
            Assert.Contains("reimprimirUltimoCierre", cierreSrc);
            Assert.Contains("cajaRecienCerrada", cierreSrc);
            Assert.Contains("Cerrar sesión", File.ReadAllText(
                PaseRepo.FindFrontendFile(Path.Combine(
                    "src", "app", "Components", "cierre-caja", "cierre-caja.component.html"))!));
        }
    }
}
