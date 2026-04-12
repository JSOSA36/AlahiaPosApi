using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    public class Perfiles 
    {
        [Key]
        public int IdPerfil { get; set; }

        // 🔗 Empresa (cada empresa define sus perfiles)
        public int IdEmpresa { get; set; }

        [ForeignKey(nameof(IdEmpresa))]
        public Empresas Empresa { get; set; }

        // 📛 Nombre del perfil
        // Ej: Administrador, Cajero, Recepción
        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        // 📝 Descripción opcional
        [MaxLength(255)]
        public string? Descripcion { get; set; }

        // 📊 Estado
        public bool Activo { get; set; } = true;

        // 🕒 Auditoría
        //public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // 🔗 Relación futura
        // Aquí luego colgamos:
        // - PerfilModulos (qué módulos ve este perfil)
        // - Usuarios (si decides usar perfil base)
    }
}
