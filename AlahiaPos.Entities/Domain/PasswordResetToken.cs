using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public class PasswordResetToken
    {
        [Key]
        public int Id { get; set; }
        public int IdEmpleado { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime Expira { get; set; }
        public bool Usado { get; set; }
    }
}
