using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public static class GlobalParamter
    {
        public static int IdEmpleado { get; set; }
        public static string UserName { get; set; } = "";
        public static int IdEmpresa { get; set; }

        public static string Rol { get; set; } = "";
        public static string NombreEmpresa { get; set; } = "";
        public static string DireccionEmpresa { get; set; } = "";
        public static string TelefonoEmpresa { get; set; } = "";
        public static string CedulaRnc { get; set; } = "";
        public static bool _OK = false;
    }
}
