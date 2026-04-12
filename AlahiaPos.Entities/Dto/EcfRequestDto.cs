
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto;
public class EcfRequestDto
{
    public Stream Excel { get; set; }

    public string RNC { get; set; }

    public string RutaCertificado { get; set; }

    public string PasswordCertificado { get; set; }
}