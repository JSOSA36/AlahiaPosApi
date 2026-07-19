using System;
using System.Linq;
using System.Xml.Linq;
using AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto;
using AlahiaPos.Entities.Dto.Fiscal;

var builder = new DgiiXmlBuilder();
var firma = DateTime.Now;

FiscalDocumentoElectronico BaseDoc(int tipo, string encf) => new()
{
    Encabezado = new FiscalDocumentoEncabezado
    {
        TipoEcf = tipo,
        Encf = encf,
        TipoIngreso = 1,
        TipoPago = 1,
        FechaEmision = DateTime.Today,
        FechaVencimientoSecuencia = new DateTime(2028, 12, 31),
        RncEmisor = "133524899",
        RazonSocialEmisor = "FLAWLESS LAUNDRY SRL",
        DireccionEmisor = "Santo Domingo",
        RncComprador = "101609291",
        RazonSocialComprador = "CLIENTE PRUEBA",
        MontoGravadoTotal = 1000m,
        MontoGravadoI1 = 1000m,
        TotalItbis = 180m,
        TotalItbis1 = 180m,
        MontoTotal = 1180m,
        IndicadorNotaCredito = tipo == 34 ? 0 : null
    },
    Lineas =
    {
        new FiscalDocumentoLinea
        {
            NumeroLinea = 1,
            IndicadorFacturacion = 1,
            NombreItem = "Servicio prueba",
            EsBien = false,
            Cantidad = 1,
            PrecioUnitario = 1000m,
            MontoItem = 1000m
        }
    },
    FormasPago = { new FiscalDocumentoFormaPagoDgii { FormaPago = 1, Monto = 1180m } }
};

void Assert(bool cond, string msg)
{
    if (!cond) throw new Exception("FAIL: " + msg);
    Console.WriteLine("OK: " + msg);
}

// E31: debe tener FechaVencimiento + TablaFormasPago
{
    var xml = builder.Build(BaseDoc(31, "E310000000001"), firma);
    var x = XDocument.Parse(xml);
    Assert(x.Descendants("FechaVencimientoSecuencia").Any(), "E31 tiene FechaVencimientoSecuencia");
    Assert(x.Descendants("TablaFormasPago").Any(), "E31 tiene TablaFormasPago");
    Assert(!x.Descendants("IndicadorNotaCredito").Any(), "E31 sin IndicadorNotaCredito");
    Assert(!x.Descendants("InformacionReferencia").Any(), "E31 sin InformacionReferencia");
}

// E32: sin FechaVencimiento; con TablaFormasPago
{
    var xml = builder.Build(BaseDoc(32, "E320000000001"), firma);
    var x = XDocument.Parse(xml);
    Assert(!x.Descendants("FechaVencimientoSecuencia").Any(), "E32 sin FechaVencimientoSecuencia");
    Assert(x.Descendants("TablaFormasPago").Any(), "E32 tiene TablaFormasPago");
}

// E33: referencia + FechaVencimiento + TablaFormasPago
{
    var d = BaseDoc(33, "E330000000001");
    d.Referencia = new FiscalDocumentoReferencia
    {
        NcfModificado = "E310000000001",
        FechaNcfModificado = DateTime.Today,
        CodigoModificacion = 1
    };
    var xml = builder.Build(d, firma);
    var x = XDocument.Parse(xml);
    Assert(x.Descendants("FechaVencimientoSecuencia").Any(), "E33 tiene FechaVencimientoSecuencia");
    Assert(x.Descendants("InformacionReferencia").Any(), "E33 tiene InformacionReferencia");
    Assert(x.Descendants("TablaFormasPago").Any(), "E33 tiene TablaFormasPago");
}

// E34: IndicadorNotaCredito + Referencia; SIN TablaFormasPago ni FechaVencimiento
{
    var d = BaseDoc(34, "E340000000001");
    d.Referencia = new FiscalDocumentoReferencia
    {
        NcfModificado = "E310000000001",
        FechaNcfModificado = DateTime.Today,
        CodigoModificacion = 1
    };
    var xml = builder.Build(d, firma);
    var x = XDocument.Parse(xml);
    Assert(x.Descendants("IndicadorNotaCredito").Any(), "E34 tiene IndicadorNotaCredito");
    Assert(x.Descendants("InformacionReferencia").Any(), "E34 tiene InformacionReferencia");
    Assert(!x.Descendants("TablaFormasPago").Any(), "E34 NO tiene TablaFormasPago (prohibido)");
    Assert(!x.Descendants("FechaVencimientoSecuencia").Any(), "E34 sin FechaVencimientoSecuencia");
}

// E41: FechaVencimiento; sin TipoIngresos; Retencion antes de NombreItem; BienoServicio=2
{
    var d = BaseDoc(41, "E410000000001");
    d.Encabezado.IndicadorMontoGravado = 0;
    d.Encabezado.TotalIsrRetencion = 100m;
    d.Lineas[0].EsBien = false;
    d.Lineas[0].IndicadorAgenteRetencionoPercepcion = 1;
    d.Lineas[0].MontoItbisRetenido = 0m;
    d.Lineas[0].MontoIsrRetenido = 100m;
    var xml = builder.Build(d, firma);
    var x = XDocument.Parse(xml);
    Assert(x.Descendants("FechaVencimientoSecuencia").Any(), "E41 tiene FechaVencimientoSecuencia");
    Assert(!x.Descendants("TipoIngresos").Any(), "E41 sin TipoIngresos");
    Assert(x.Descendants("Retencion").Any(), "E41 tiene Retencion");
    Assert(x.Descendants("MontoISRRetenido").Any(), "E41 tiene MontoISRRetenido");
    Assert(x.Descendants("IndicadorBienoServicio").First().Value == "2", "E41 IndicadorBienoServicio=2");
    Assert(x.Descendants("TotalISRRetencion").Any(), "E41 tiene TotalISRRetencion");
    var item = x.Descendants("Item").First();
    var kids = item.Elements().Select(e => e.Name.LocalName).ToList();
    var idxRet = kids.IndexOf("Retencion");
    var idxNom = kids.IndexOf("NombreItem");
    Assert(idxRet >= 0 && idxNom > idxRet, "E41 Retencion antes de NombreItem");
}

// E43: sin Comprador; sin TipoIngresos/TablaFormasPago; IndicadorFacturacion=4; Totales solo exento
{
    var d = BaseDoc(43, "E430000000001");
    d.Encabezado.MontoExento = 1180m;
    d.Encabezado.MontoGravadoTotal = 0;
    d.Encabezado.MontoGravadoI1 = 0;
    d.Encabezado.TotalItbis = 0;
    d.Encabezado.TotalItbis1 = 0;
    d.Lineas[0].IndicadorFacturacion = 4;
    d.Lineas[0].EsBien = true;
    var xml = builder.Build(d, firma);
    var x = XDocument.Parse(xml);
    Assert(x.Descendants("FechaVencimientoSecuencia").Any(), "E43 tiene FechaVencimientoSecuencia");
    Assert(!x.Descendants("Comprador").Any(), "E43 sin Comprador");
    Assert(!x.Descendants("TipoIngresos").Any(), "E43 sin TipoIngresos");
    Assert(!x.Descendants("TablaFormasPago").Any(), "E43 sin TablaFormasPago");
    Assert(!x.Descendants("TotalITBIS").Any(), "E43 sin TotalITBIS");
    Assert(x.Descendants("IndicadorFacturacion").First().Value == "4", "E43 IndicadorFacturacion=4");
    Assert(x.Descendants("MontoExento").Any(), "E43 tiene MontoExento");
}

// E44: Comprador + TipoIngresos + FechaVencimiento; IndicadorFacturacion=4; Totales exento
{
    var d = BaseDoc(44, "E440000000001");
    d.Encabezado.MontoExento = 1180m;
    d.Encabezado.MontoGravadoTotal = 0;
    d.Encabezado.MontoGravadoI1 = 0;
    d.Encabezado.TotalItbis = 0;
    d.Encabezado.TotalItbis1 = 0;
    d.Lineas[0].IndicadorFacturacion = 4;
    var xml = builder.Build(d, firma);
    var x = XDocument.Parse(xml);
    Assert(x.Descendants("FechaVencimientoSecuencia").Any(), "E44 tiene FechaVencimientoSecuencia");
    Assert(x.Descendants("Comprador").Any(), "E44 tiene Comprador");
    Assert(x.Descendants("TipoIngresos").Any(), "E44 tiene TipoIngresos");
    Assert(x.Descendants("TablaFormasPago").Any(), "E44 tiene TablaFormasPago");
    Assert(!x.Descendants("IndicadorMontoGravado").Any(), "E44 sin IndicadorMontoGravado");
    Assert(!x.Descendants("TotalITBIS").Any(), "E44 sin TotalITBIS");
    Assert(x.Descendants("IndicadorFacturacion").First().Value == "4", "E44 IndicadorFacturacion=4");
}

// E45: como E31 + RNC comprador obligatorio; Totales con ITBIS; sin retención en Totales
{
    var d = BaseDoc(45, "E450000000001");
    d.Encabezado.IndicadorMontoGravado = 0;
    var xml = builder.Build(d, firma);
    var x = XDocument.Parse(xml);
    Assert(x.Descendants("FechaVencimientoSecuencia").Any(), "E45 tiene FechaVencimientoSecuencia");
    Assert(x.Descendants("TipoIngresos").Any(), "E45 tiene TipoIngresos");
    Assert(x.Descendants("Comprador").Any(), "E45 tiene Comprador");
    Assert(x.Descendants("RNCComprador").Any(), "E45 tiene RNCComprador");
    Assert(x.Descendants("TotalITBIS").Any(), "E45 tiene TotalITBIS");
    Assert(!x.Descendants("TotalITBISRetenido").Any(), "E45 sin TotalITBISRetenido");
    Assert(x.Descendants("IndicadorMontoGravado").Any(), "E45 tiene IndicadorMontoGravado");
}

// E46: TipoIngresos; IndicadorFacturacion=3; Totales I3 tasa cero; sin IndicadorMontoGravado
{
    var d = BaseDoc(46, "E460000000001");
    d.Encabezado.MontoGravadoTotal = 1000m;
    d.Encabezado.MontoGravadoI1 = 0;
    d.Encabezado.MontoGravadoI3 = 1000m;
    d.Encabezado.TotalItbis = 0;
    d.Encabezado.TotalItbis1 = 0;
    d.Encabezado.TotalItbis3 = 0;
    d.Encabezado.MontoTotal = 1000m;
    d.Lineas[0].IndicadorFacturacion = 3;
    d.Lineas[0].MontoItem = 1000m;
    d.Lineas[0].PrecioUnitario = 1000m;
    d.FormasPago.Clear();
    d.FormasPago.Add(new FiscalDocumentoFormaPagoDgii { FormaPago = 1, Monto = 1000m });
    var xml = builder.Build(d, firma);
    var x = XDocument.Parse(xml);
    Assert(x.Descendants("FechaVencimientoSecuencia").Any(), "E46 tiene FechaVencimientoSecuencia");
    Assert(x.Descendants("TipoIngresos").Any(), "E46 tiene TipoIngresos");
    Assert(!x.Descendants("IndicadorMontoGravado").Any(), "E46 sin IndicadorMontoGravado");
    Assert(x.Descendants("IndicadorFacturacion").First().Value == "3", "E46 IndicadorFacturacion=3");
    Assert(x.Descendants("MontoGravadoI3").Any(), "E46 tiene MontoGravadoI3");
    Assert(x.Descendants("ITBIS3").Any(), "E46 tiene ITBIS3");
    Assert(!x.Descendants("MontoGravadoI1").Any(), "E46 sin MontoGravadoI1");
}

// E47: sin Comprador/TipoIngresos; IndicadorFacturacion=4; Retencion solo ISR
{
    var d = BaseDoc(47, "E470000000001");
    d.Encabezado.MontoExento = 1000m;
    d.Encabezado.MontoGravadoTotal = 0;
    d.Encabezado.MontoGravadoI1 = 0;
    d.Encabezado.TotalItbis = 0;
    d.Encabezado.TotalItbis1 = 0;
    d.Encabezado.MontoTotal = 1000m;
    d.Encabezado.TotalIsrRetencion = 100m;
    d.Lineas[0].IndicadorFacturacion = 4;
    d.Lineas[0].EsBien = false;
    d.Lineas[0].IndicadorAgenteRetencionoPercepcion = 1;
    d.Lineas[0].MontoIsrRetenido = 100m;
    d.FormasPago.Clear();
    d.FormasPago.Add(new FiscalDocumentoFormaPagoDgii { FormaPago = 1, Monto = 1000m });
    var xml = builder.Build(d, firma);
    var x = XDocument.Parse(xml);
    Assert(x.Descendants("FechaVencimientoSecuencia").Any(), "E47 tiene FechaVencimientoSecuencia");
    Assert(!x.Descendants("Comprador").Any(), "E47 sin Comprador");
    Assert(!x.Descendants("TipoIngresos").Any(), "E47 sin TipoIngresos");
    Assert(x.Descendants("IndicadorFacturacion").First().Value == "4", "E47 IndicadorFacturacion=4");
    Assert(x.Descendants("Retencion").Any(), "E47 tiene Retencion");
    Assert(x.Descendants("MontoISRRetenido").Any(), "E47 tiene MontoISRRetenido");
    Assert(!x.Descendants("MontoITBISRetenido").Any(), "E47 sin MontoITBISRetenido");
    Assert(x.Descendants("TotalISRRetencion").Any(), "E47 tiene TotalISRRetencion");
}

Console.WriteLine("SMOKE OK");
