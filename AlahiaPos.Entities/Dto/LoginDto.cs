using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class LoginDto
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string? Token { get; set; }
        public string DeviceId { get; set; }
    }
}
