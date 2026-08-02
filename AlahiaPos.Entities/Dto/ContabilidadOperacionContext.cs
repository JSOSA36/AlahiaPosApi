namespace AlahiaPos.Entities.Dto
{
    /// <summary>
    /// Contexto por request (scoped) para que el motor contable
    /// comunique advertencias al publicador sin tumbar el ERP.
    /// </summary>
    public class ContabilidadOperacionContext
    {
        public string? UltimaAdvertencia { get; set; }
        public bool AsientoGenerado { get; set; }
        public bool HuboErrorOMapeoFaltante { get; set; }

        public void Reset()
        {
            UltimaAdvertencia = null;
            AsientoGenerado = false;
            HuboErrorOMapeoFaltante = false;
        }

        public void MarcarAdvertencia(string mensaje)
        {
            HuboErrorOMapeoFaltante = true;
            AsientoGenerado = false;
            UltimaAdvertencia = mensaje;
        }

        public void MarcarOk()
        {
            AsientoGenerado = true;
            HuboErrorOMapeoFaltante = false;
            UltimaAdvertencia = null;
        }
    }
}
