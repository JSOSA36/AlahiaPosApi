using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class EmpresaServices : IEmpresas
    {
        private readonly IRepository<Empresas> repository;

        public EmpresaServices(IRepository<Empresas> repository)
        {
            this.repository = repository;
        }

        // 🔹 EXISTENTES
        public async Task<Empresas> GetEmpresaById(int Id)
        {
            return await repository.GetByIdAsync(Id);
        }

        public void UpdateEmpresas(int Id, Empresas empresas)
        {
            repository.Update(Id, empresas);
        }

        public async Task InsertEmpresas(Empresas empresas)
        {
            await repository.Save(empresas);
        }

        public async Task<Empresas> GetEmpresaByGUID(Guid Id)
        {
            return await repository.GetByExpresionAsync(c => c.GuidPublico == Id);
        }

        // ===============================
        // 🔥 NUEVOS MÉTODOS (PAGOS)
        // ===============================
        public async Task ActualizarEstadoEmpresa(int empresaId)
        {
            var emp = await repository.GetByIdAsync(empresaId);
            if (emp == null) return;

            var hoy = DateTime.Now.Day;

            // 🔄 RESET CICLO (DÍA 30) → solo una vez por día
            if (hoy == 30 && emp.PagadoServicio)
            {
                emp.PagadoServicio = false;
            }

            // 🟢 SI PAGÓ → ACTIVO
            if (emp.PagadoServicio)
            {
                emp.EstadoServicio = "ACTIVO";
            }
            else
            {
                // 🟡 PERÍODO DE PAGO (30 y 1–5)
                if (hoy == 30 || hoy <= 5)
                {
                    emp.EstadoServicio = "VENCIDO";
                }
                // 🔴 BLOQUEADO (día 6 en adelante)
                else
                {
                    emp.EstadoServicio = "BLOQUEADO";
                }
            }

             repository.Update(empresaId, emp);
        }
        public async Task MarcarPago(int empresaId)
        {
            var empresa = await repository.GetByIdAsync(empresaId);

            if (empresa == null) return;

            empresa.PagadoServicio = true;
            empresa.EstadoServicio = "ACTIVO";
            empresa.FechaUltimoPago = DateTime.Now;

            // Próximo pago día 30 del mes actual
            empresa.FechaProximoPago = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 30);

            repository.Update(empresaId, empresa);
        }

        public async Task MarcarPendiente(int empresaId)
        {
            var empresa = await repository.GetByIdAsync(empresaId);

            if (empresa == null) return;

            empresa.EstadoServicio = "PENDIENTE";

            repository.Update(empresaId, empresa);
        }

        public async Task ActualizarEstadoAutomatico()
        {
            var hoy = DateTime.Now.Day;

            var empresas = await repository.GetAllAsync();

            foreach (var emp in empresas)
            {
                // 🔄 RESET CICLO → DÍA 30
                if (hoy == 30)
                {
                    emp.PagadoServicio = false;
                }

                // ✅ SI PAGÓ
                if (emp.PagadoServicio)
                {
                    emp.EstadoServicio = "ACTIVO";
                }
                else
                {
                    // 🟡 Antes del día 6 → vencido
                    if (hoy <= 5 || hoy == 30)
                    {
                        emp.EstadoServicio = "VENCIDO";
                    }
                    // 🔴 Día 6 en adelante → bloqueado
                    else if (hoy >= 6)
                    {
                        emp.EstadoServicio = "BLOQUEADO";
                    }
                }

                 repository.Update(emp.IdEmpresa, emp);
            }
        }

        public bool PuedeOperar(Empresas empresa)
        {
            return empresa.EstadoServicio != "BLOQUEADO";
        }
        public AlertaPagoDto ObtenerAlertaPago(Empresas empresa)
        {
            var hoy = DateTime.Now.Day;

            // 🟢 Si ya pagó → no molestar
            //if (empresa.PagadoServicio)
            //    return null;

            // 🟢 Día 30 (inicio del ciclo)
            //if (hoy == 30)
            //{
                return new AlertaPagoDto
                {
                    Tipo = "info",
                    Mensaje = "Tu servicio está pendiente de pago. Puedes realizarlo antes del día 5 para evitar interrupciones."
                };
            //}

            //// 🟡 Día 3 (recordatorio)
            //if (hoy == 3)
            //{
            //    return new AlertaPagoDto
            //    {
            //        Tipo = "advertencia",
            //        Mensaje = "Recordatorio: tu servicio vence pronto. Evita la suspensión realizando el pago."
            //    };
            //}

            //// 🟠 Día 5 (último aviso)
            //if (hoy == 5)
            //{
            //    return new AlertaPagoDto
            //    {
            //        Tipo = "advertencia",
            //        Mensaje = "Hoy es el último día para realizar el pago y evitar la suspensión del servicio."
            //    };
            //}

            //// 🔴 Día 6 (suspensión)
            //if (hoy == 6)
            //{
            //    return new AlertaPagoDto
            //    {
            //        Tipo = "critico",
            //        Mensaje = "Tu servicio ha sido suspendido por falta de pago."
            //    };
            //}

            //return null;
        }

    }
}