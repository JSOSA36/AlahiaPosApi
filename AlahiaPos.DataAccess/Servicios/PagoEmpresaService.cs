using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class PagoEmpresaService : IPagoEmpresaService
    {
        private readonly IRepository<PagoEmpresa> _repository;
        private readonly IRepository<Empresas> _empresaRepository;

        public PagoEmpresaService(
            IRepository<PagoEmpresa> repository,
            IRepository<Empresas> empresaRepository)
        {
            _repository = repository;
            _empresaRepository = empresaRepository;
        }

        // ===========================================================
        // 🔥 CREAR PAGO (CLIENTE SUBE VOUCHER)
        // ===========================================================
        public async Task CrearPagoAsync(CrearPagoDto dto)
        {
            var pago = new PagoEmpresa
            {
                IdEmpresa = dto.IdEmpresa,
                Monto = dto.Monto,
                ArchivoUrl = dto.ArchivoUrl,
                FechaSubida = DateTime.Now,
                Estado = "PENDIENTE"
            };

            await _repository.Save(pago);
        }

        // ===========================================================
        // 📄 LISTAR PAGOS (ADMIN)
        // ===========================================================
        public async Task<List<PagoEmpresaDto>> ObtenerPagosAsync()
        {
            var pagos = await _repository.GetAllAsync();
            var empresas = await _empresaRepository.GetAllAsync();

            var result = (
                from p in pagos
                join e in empresas on p.IdEmpresa equals e.IdEmpresa
                orderby p.FechaSubida descending
                select new PagoEmpresaDto
                {
                    Id = p.Id,
                    IdEmpresa = (int)p.IdEmpresa,
                    NombreEmpresa=e.NombreComercial,
                    Monto = (int)p.Monto,
                    FechaSubida = (DateTime)p.FechaSubida,
                    ArchivoUrl = p.ArchivoUrl,
                    Estado = p.Estado,
                    Observacion = p.Observacion,
                    FechaValidacion = p.FechaValidacion,
                    UsuarioValida = p.UsuarioValida
                }
            ).ToList();

            return result;
        }

        // ===========================================================
        // ✅ APROBAR / ❌ RECHAZAR PAGO
        // ===========================================================
        public async Task ValidarPagoAsync(ValidarPagoDto dto)
        {
            var pago = await _repository.GetByIdAsync(dto.IdPago);
            if (pago == null)
                throw new Exception("El pago no existe.");

            if (pago.Estado != "PENDIENTE")
                throw new Exception("Este pago ya fue procesado.");

            var empresa = await _empresaRepository.GetByIdAsync((int)pago.IdEmpresa);
            if (empresa == null)
                throw new Exception("Empresa no encontrada.");

            // =======================================================
            // ✅ APROBAR
            // =======================================================
            if (dto.Estado == "APROBADO")
            {
                pago.Estado = "APROBADO";
                pago.FechaSubida = DateTime.Now;
                pago.UsuarioValida = dto.UsuarioValida;

                // 🔥 ACTIVAR SERVICIO
                empresa.PagadoServicio = true;
                empresa.EstadoServicio = "ACTIVO";
                empresa.FechaUltimoPago = DateTime.Now;

                _empresaRepository.Update(empresa.IdEmpresa, empresa);
            }
            // =======================================================
            // ❌ RECHAZAR
            // =======================================================
            else if (dto.Estado == "RECHAZADO")
            {
                pago.Estado = "RECHAZADO";
                pago.Observacion = dto.Observacion;
                pago.FechaSubida = DateTime.Now;
                pago.UsuarioValida = dto.UsuarioValida;
            }
            else
            {
                throw new Exception("Estado inválido.");
            }

            _repository.Update(pago.Id, pago);
        }
    }
}