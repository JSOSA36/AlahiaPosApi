namespace AlahiaPosApi.Auth
{
    /// <summary>
    /// Identidad de la petición: siempre sale de Usuarios.Token, nunca del cliente.
    /// </summary>
    public sealed class SesionActual
    {
        public int IdUsuario { get; init; }
        public int IdEmpresa { get; init; }
        public int IdSucursal { get; set; }
        public int IdPerfil { get; init; }
        public bool Estado { get; init; }
        public bool EsEmpresaSistema { get; init; }
        public string UserName { get; init; } = "";
    }
}
