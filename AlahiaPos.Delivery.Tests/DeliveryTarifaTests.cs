using AlahiaPos.Entities.Delivery;
using Xunit;

namespace AlahiaPos.Delivery.Tests;

public class DeliveryTarifaCalculadorTests
{
    [Fact]
    public void Ejemplo_5_5_km_redondea_a_190()
    {
        var costo = DeliveryTarifaCalculador.Calcular(50m, 5.5m, 25m, 100m, 10m);
        Assert.Equal(190m, costo);
    }

    [Fact]
    public void Un_kilometro_queda_en_el_minimo()
    {
        var costo = DeliveryTarifaCalculador.Calcular(50m, 1m, 25m, 100m, 10m);
        Assert.Equal(100m, costo);
    }

    [Fact]
    public void Por_debajo_del_minimo_sube_al_minimo()
    {
        var costo = DeliveryTarifaCalculador.Calcular(50m, 0m, 25m, 100m, 0m);
        Assert.Equal(100m, costo);
    }

    [Fact]
    public void Redondeo_hacia_arriba_al_multiplo()
    {
        Assert.Equal(190m, DeliveryTarifaCalculador.RedondearArriba(187.50m, 10m));
        Assert.Equal(190m, DeliveryTarifaCalculador.RedondearArriba(181m, 10m));
        Assert.Equal(180m, DeliveryTarifaCalculador.RedondearArriba(180m, 10m));
        Assert.Equal(190m, DeliveryTarifaCalculador.RedondearArriba(187.50m, 5m));
    }

    [Fact]
    public void Usa_decimal_sin_perder_centavos()
    {
        var costo = DeliveryTarifaCalculador.Calcular(50m, 1.1m, 25m, 0m, 0m);
        Assert.Equal(77.50m, costo);
    }

    [Fact]
    public void Transferencia_puede_dejar_el_delivery_para_el_efectivo()
    {
        Assert.False(DeliveryTarifaCalculador.CobrarAlEntregar(true, true, 190m, false));
        Assert.True(DeliveryTarifaCalculador.CobrarAlEntregar(true, true, 190m, true));
        Assert.False(DeliveryTarifaCalculador.CobrarAlEntregar(true, false, 190m, true));
        Assert.Equal(1040m, DeliveryTarifaCalculador.MontoATransferir(1040m, 190m, false));
        Assert.Equal(850m, DeliveryTarifaCalculador.MontoATransferir(1040m, 190m, true));
    }

    [Fact]
    public void El_precio_enviado_por_el_cliente_no_entra_en_la_formula()
    {
        var enviadoPorCliente = 1.00m;
        var costo = DeliveryTarifaCalculador.Calcular(50m, 5.5m, 25m, 100m, 10m);
        Assert.Equal(190m, costo);
        Assert.NotEqual(enviadoPorCliente, costo);
    }
}

public class DeliveryCotizacionMotorTests
{
    private static DeliveryTarifa Tarifa(
        bool habilitado = true,
        bool calculo = true,
        decimal max = 0,
        string accion = "SIN_LIMITE",
        bool exigirGps = true,
        decimal baseTarifa = 50m,
        decimal precio = 25m) => new()
    {
        DeliveryHabilitado = habilitado,
        CalculoPorDistancia = calculo,
        TarifaBase = baseTarifa,
        PrecioPorKilometro = precio,
        CostoMinimo = 100m,
        Redondeo = 10m,
        DistanciaMaximaKm = max > 0 ? max : null,
        AccionFueraDeZona = accion,
        Proveedor = "OSRM",
        MinutosCache = 30,
        ExigirGps = exigirGps
    };

    [Fact]
    public void Delivery_desactivado_cuesta_cero()
    {
        var snap = DeliveryCotizacionMotor.Preparar(Tarifa(habilitado: false), true, true, true);
        Assert.True(snap.PuedeConfirmar);
        Assert.Equal(0m, snap.Costo);
        Assert.False(snap.LlamarProveedor);
    }

    [Fact]
    public void Sin_configuracion_cuesta_cero()
    {
        var snap = DeliveryCotizacionMotor.Preparar(null, true, true, true);
        Assert.True(snap.PuedeConfirmar);
        Assert.Equal(0m, snap.Costo);
        Assert.False(snap.CalculoAutomatico);
    }

    [Fact]
    public void Recoger_no_llama_al_proveedor()
    {
        var snap = DeliveryCotizacionMotor.Preparar(Tarifa(), esDelivery: false, tieneGps: false, tieneOrigen: false);
        Assert.True(snap.PuedeConfirmar);
        Assert.Equal(0m, snap.Costo);
        Assert.Equal(0m, snap.DistanciaKm);
        Assert.False(snap.LlamarProveedor);
    }

    [Fact]
    public void Sin_gps_impide_confirmar()
    {
        var snap = DeliveryCotizacionMotor.Preparar(Tarifa(), true, tieneGps: false, tieneOrigen: true);
        Assert.False(snap.PuedeConfirmar);
        Assert.Equal(DeliveryCotizacionMotor.MensajeSinGps, snap.Mensaje);
        Assert.Equal(0m, snap.Costo);
        Assert.False(snap.LlamarProveedor);
    }

    [Fact]
    public void Fallo_del_proveedor_no_inventa_precio()
    {
        var snap = DeliveryCotizacionMotor.ConDistancia(
            Tarifa(),
            new DeliveryMedicion { Ok = false, Error = "timeout", Proveedor = "OSRM" },
            18.4m, -69.9m, 18.5m, -69.8m, new DateTime(2026, 10, 6));
        Assert.False(snap.PuedeConfirmar);
        Assert.Equal(0m, snap.Costo);
        Assert.Equal(DeliveryCotizacionMotor.MensajeSinRuta, snap.Mensaje);
    }

    [Fact]
    public void Fuera_de_zona_rechaza()
    {
        var snap = DeliveryCotizacionMotor.ConDistancia(
            Tarifa(max: 10m, accion: "RECHAZAR"),
            new DeliveryMedicion { Ok = true, DistanciaKm = 12.4m, Proveedor = "OSRM" },
            18.4m, -69.9m, 18.6m, -69.7m, new DateTime(2026, 10, 6));
        Assert.False(snap.PuedeConfirmar);
        Assert.True(snap.FueraDeZona);
        Assert.Equal(DeliveryCotizacionMotor.MensajeFuera, snap.Mensaje);
    }

    [Fact]
    public void Fuera_de_zona_avisa_y_permite()
    {
        var snap = DeliveryCotizacionMotor.ConDistancia(
            Tarifa(max: 10m, accion: "AVISAR_Y_PERMITIR"),
            new DeliveryMedicion { Ok = true, DistanciaKm = 12m, Proveedor = "OSRM" },
            18.4m, -69.9m, 18.6m, -69.7m, new DateTime(2026, 10, 6));
        Assert.True(snap.PuedeConfirmar);
        Assert.True(snap.FueraDeZona);
        Assert.True(snap.Costo > 0);
    }

    [Fact]
    public void Sin_limite_calcula_aunque_haya_tope_guardado()
    {
        var snap = DeliveryCotizacionMotor.ConDistancia(
            Tarifa(max: 1m, accion: "SIN_LIMITE"),
            new DeliveryMedicion { Ok = true, DistanciaKm = 5.5m, Proveedor = "OSRM" },
            18.4m, -69.9m, 18.5m, -69.8m, new DateTime(2026, 10, 6));
        Assert.True(snap.PuedeConfirmar);
        Assert.False(snap.FueraDeZona);
        Assert.Equal(190m, snap.Costo);
    }

    [Fact]
    public void Cambio_de_tarifa_no_altera_el_snapshot()
    {
        var tarifa = Tarifa();
        var fecha = new DateTime(2026, 10, 6, 10, 0, 0);
        var snap = DeliveryCotizacionMotor.CopiarTarifa(tarifa, 190m, 5.5m, fecha);
        tarifa.TarifaBase = 80m;
        tarifa.PrecioPorKilometro = 40m;
        Assert.Equal(50m, snap.TarifaBase);
        Assert.Equal(25m, snap.PrecioKm);
        Assert.Equal(190m, snap.Costo);
        Assert.Equal(fecha, snap.FechaCalculo);
    }

    [Fact]
    public void Empresas_distintas_tienen_tarifas_distintas()
    {
        var a = DeliveryTarifaCalculador.Calcular(50m, 5.5m, 25m, 100m, 10m);
        var b = DeliveryTarifaCalculador.Calcular(20m, 5.5m, 10m, 0m, 0m);
        Assert.Equal(190m, a);
        Assert.Equal(75.00m, b);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void La_sucursal_gana_sobre_la_empresa()
    {
        var sucursal = Tarifa(baseTarifa: 80m, precio: 10m);
        var empresa = Tarifa(baseTarifa: 50m, precio: 25m);
        var elegida = DeliveryCotizacionMotor.Elegir(sucursal, empresa);
        Assert.Equal(80m, elegida!.TarifaBase);
    }

    [Fact]
    public void Sin_fila_de_sucursal_usa_la_empresa()
    {
        var empresa = Tarifa(baseTarifa: 40m);
        var elegida = DeliveryCotizacionMotor.Elegir(null, empresa);
        Assert.Equal(40m, elegida!.TarifaBase);
    }

    [Fact]
    public void Origen_usa_la_sucursal_y_la_empresa_solo_si_falta()
    {
        Assert.True(DeliveryCotizacionMotor.TryOrigen("18.5000", "-69.9000", "18.1000", "-69.1000", out var lat, out var lng, out var usoEmpresa));
        Assert.False(usoEmpresa);
        Assert.Equal(18.5000m, lat);
        Assert.Equal(-69.9000m, lng);

        Assert.True(DeliveryCotizacionMotor.TryOrigen("", "", "18.1000", "-69.1000", out lat, out lng, out usoEmpresa));
        Assert.True(usoEmpresa);
        Assert.Equal(18.1000m, lat);
    }

    [Fact]
    public void Coordenadas_cercanas_comparten_clave_de_cache()
    {
        var a = DeliveryCotizacionMotor.ClaveCoord(18.48641m);
        var b = DeliveryCotizacionMotor.ClaveCoord(18.48699m);
        Assert.Equal(a, b);
        Assert.NotEqual(a, DeliveryCotizacionMotor.ClaveCoord(18.48710m));
    }
}

public class ClienteDireccionCoincidenciaTests
{
    [Fact]
    public void Dos_direcciones_con_el_mismo_gps_son_la_misma()
    {
        Assert.True(ClienteDireccionCoincidencia.MismaUbicacion(
            "Calle 1", 18.48641m, -69.93110m,
            "Calle 1 apto 2", 18.48680m, -69.93140m));
    }

    [Fact]
    public void Copiar_al_pedido_no_sigue_el_texto_original()
    {
        var guardada = "Carmen Renata 3";
        var copia = ClienteDireccionCoincidencia.CopiarTexto(guardada);
        guardada = "Otra calle";
        Assert.Equal("Carmen Renata 3", copia);
    }

    [Fact]
    public void Direcciones_distintas_sin_gps_no_coinciden()
    {
        Assert.False(ClienteDireccionCoincidencia.MismaUbicacion(
            "Calle A", null, null, "Calle B", null, null));
    }
}
