
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto;

public class DtoReceptor
{
    /// <summary>
    /// RNC o Cédula del receptor
    /// Obligatorio para e-CF 31
    /// </summary>
    public string? RncOCedula { get; set; }

    /// <summary>
    /// Nombre o razón social
    /// </summary>
    public string? RazonSocial { get; set; }

    /// <summary>
    /// Correo electrónico para envío del e-CF
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Dirección del receptor
    /// </summary>
    public string? Direccion { get; set; }

    /// <summary>
    /// Municipio
    /// </summary>
    public string? Municipio { get; set; }

    /// <summary>
    /// Provincia
    /// </summary>
    public string? Provincia { get; set; }

    /// <summary>
    /// Tipo de identificación
    /// 1 = RNC
    /// 2 = Cédula
    /// 3 = Pasaporte
    /// </summary>
    public int? TipoIdentificacion { get; set; }
}