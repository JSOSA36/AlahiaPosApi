using System.Globalization;

namespace AlahiaPos.Entities.Delivery
{
    public static class DeliveryCotizacionMotor
    {
        public const string MensajeSinGps = "Comparta su ubicación para calcular el delivery.";
        public const string MensajeSinRuta = "No pudimos calcular la ruta de delivery. Intente de nuevo en unos minutos.";
        public const string MensajeFuera = "Esta dirección se encuentra fuera del área de delivery.";
        public const string MensajeFueraAviso = "Esta dirección está fuera del área habitual de delivery. Puede continuar con el costo indicado.";
        public const string MensajeSinOrigen = "La sucursal no tiene ubicación para calcular el delivery.";

        public static DeliveryTarifa? Elegir(DeliveryTarifa? sucursal, DeliveryTarifa? empresa)
            => sucursal ?? empresa;

        public static bool TryCoord(string? latitud, string? longitud, out decimal lat, out decimal lng)
        {
            lat = 0;
            lng = 0;
            if (!decimal.TryParse((latitud ?? "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out lat))
                return false;
            if (!decimal.TryParse((longitud ?? "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out lng))
                return false;
            if (lat == 0 || lng == 0) return false;
            if (lat < -90 || lat > 90 || lng < -180 || lng > 180) return false;
            return true;
        }

        public static bool TryOrigen(
            string? sucursalLat,
            string? sucursalLng,
            string? empresaLat,
            string? empresaLng,
            out decimal lat,
            out decimal lng,
            out bool usoEmpresa)
        {
            usoEmpresa = false;
            if (TryCoord(sucursalLat, sucursalLng, out lat, out lng))
                return true;
            usoEmpresa = true;
            return TryCoord(empresaLat, empresaLng, out lat, out lng);
        }

        public static decimal ClaveCoord(decimal valor)
        {
            var escala = 1000m;
            return decimal.Truncate(valor * escala) / escala;
        }

        public static DeliverySnapshot Preparar(DeliveryTarifa? tarifa, bool esDelivery, bool tieneGps, bool tieneOrigen)
        {
            if (!esDelivery)
                return Cero(true);

            if (tarifa == null || !tarifa.DeliveryHabilitado || !tarifa.CalculoPorDistancia)
                return Cero(true);

            if (!tieneGps)
            {
                if (tarifa.ExigirGps)
                    return Bloqueado(MensajeSinGps, true);
                return Cero(true);
            }

            if (!tieneOrigen)
                return Bloqueado(MensajeSinOrigen, true);

            return new DeliverySnapshot
            {
                PuedeConfirmar = false,
                LlamarProveedor = true,
                CalculoAutomatico = true
            };
        }

        public static DeliverySnapshot ConDistancia(
            DeliveryTarifa tarifa,
            DeliveryMedicion medicion,
            decimal origenLat,
            decimal origenLng,
            decimal destinoLat,
            decimal destinoLng,
            DateTime fechaCalculo)
        {
            if (!medicion.Ok)
                return Bloqueado(MensajeSinRuta, true);

            var costo = DeliveryTarifaCalculador.Calcular(
                tarifa.TarifaBase,
                medicion.DistanciaKm,
                tarifa.PrecioPorKilometro,
                tarifa.CostoMinimo,
                tarifa.Redondeo);

            var accion = (tarifa.AccionFueraDeZona ?? "").Trim().ToUpperInvariant();
            var hayTope = tarifa.DistanciaMaximaKm is > 0 && accion != "SIN_LIMITE";
            var fuera = hayTope && medicion.DistanciaKm > tarifa.DistanciaMaximaKm!.Value;

            var snapshot = new DeliverySnapshot
            {
                Costo = costo,
                DistanciaKm = medicion.DistanciaKm,
                TarifaBase = tarifa.TarifaBase,
                PrecioKm = tarifa.PrecioPorKilometro,
                Redondeo = tarifa.Redondeo,
                Minimo = tarifa.CostoMinimo,
                Proveedor = string.IsNullOrWhiteSpace(medicion.Proveedor) ? tarifa.Proveedor : medicion.Proveedor,
                OrigenLatitud = origenLat,
                OrigenLongitud = origenLng,
                DestinoLatitud = destinoLat,
                DestinoLongitud = destinoLng,
                FechaCalculo = fechaCalculo,
                DuracionSegundos = medicion.DuracionSegundos,
                CalculoAutomatico = true,
                FueraDeZona = fuera,
                PuedeConfirmar = true
            };

            if (!fuera)
                return snapshot;

            if (accion == "RECHAZAR")
            {
                return new DeliverySnapshot
                {
                    Costo = costo,
                    DistanciaKm = medicion.DistanciaKm,
                    TarifaBase = snapshot.TarifaBase,
                    PrecioKm = snapshot.PrecioKm,
                    Redondeo = snapshot.Redondeo,
                    Minimo = snapshot.Minimo,
                    Proveedor = snapshot.Proveedor,
                    OrigenLatitud = origenLat,
                    OrigenLongitud = origenLng,
                    DestinoLatitud = destinoLat,
                    DestinoLongitud = destinoLng,
                    FechaCalculo = fechaCalculo,
                    DuracionSegundos = medicion.DuracionSegundos,
                    CalculoAutomatico = true,
                    FueraDeZona = true,
                    PuedeConfirmar = false,
                    Mensaje = MensajeFuera
                };
            }

            return new DeliverySnapshot
            {
                Costo = snapshot.Costo,
                DistanciaKm = snapshot.DistanciaKm,
                TarifaBase = snapshot.TarifaBase,
                PrecioKm = snapshot.PrecioKm,
                Redondeo = snapshot.Redondeo,
                Minimo = snapshot.Minimo,
                Proveedor = snapshot.Proveedor,
                OrigenLatitud = snapshot.OrigenLatitud,
                OrigenLongitud = snapshot.OrigenLongitud,
                DestinoLatitud = snapshot.DestinoLatitud,
                DestinoLongitud = snapshot.DestinoLongitud,
                FechaCalculo = snapshot.FechaCalculo,
                DuracionSegundos = snapshot.DuracionSegundos,
                CalculoAutomatico = true,
                FueraDeZona = true,
                PuedeConfirmar = true,
                Mensaje = MensajeFueraAviso
            };
        }

        public static DeliverySnapshot CopiarTarifa(DeliveryTarifa tarifa, decimal costo, decimal km, DateTime fecha)
        {
            return new DeliverySnapshot
            {
                Costo = costo,
                DistanciaKm = km,
                TarifaBase = tarifa.TarifaBase,
                PrecioKm = tarifa.PrecioPorKilometro,
                Redondeo = tarifa.Redondeo,
                Minimo = tarifa.CostoMinimo,
                Proveedor = tarifa.Proveedor,
                FechaCalculo = fecha,
                PuedeConfirmar = true,
                CalculoAutomatico = true
            };
        }

        private static DeliverySnapshot Cero(bool puede)
            => new()
            {
                PuedeConfirmar = puede,
                Costo = 0,
                DistanciaKm = 0,
                CalculoAutomatico = false
            };

        private static DeliverySnapshot Bloqueado(string mensaje, bool automatico)
            => new()
            {
                PuedeConfirmar = false,
                Costo = 0,
                DistanciaKm = 0,
                CalculoAutomatico = automatico,
                Mensaje = mensaje
            };
    }
}
