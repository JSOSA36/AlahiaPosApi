"""
Seed demos verticales en AlahiaPos_Prod para demos de calle.
Password admin: DemoAlahia2026 (SHA256)

Reutiliza:
- 61 La Leyenda Hotdog → comida (añade usuario demo si falta)
- 56 De Laura Pastelería → repostería (extiende vigencia)
- 58 TotalClean → carwash (añade usuario demo si falta)
Crea / completa:
- Alahia Demo Ferretería
- Alahia Demo Repuestos
- Saloon Demo (22) o Alahia Demo Belleza
- Alahia Demo Carwash / Alahia Demo Repostería si no se quiere tocar clientes reales
"""
from __future__ import annotations

import hashlib
import subprocess
import uuid
from pathlib import Path

SERVER = r"144.126.143.154\SQLEXPRESS,1433"
DB = "AlahiaPos_Prod"
USER = "sa"
PASSWORD = "JoelAriel8787"
DEMO_PASS = "DemoAlahia2026"
PASS_HASH = hashlib.sha256(DEMO_PASS.encode("utf-8")).hexdigest()

# Códigos por vertical (alineados a DemoVerticalPresets.cs)
PRESETS = {
    "comida": [
        "EMPRESA","USUARIOS","PERFILES","PARAMETROS","DASHBOARD","PRODUCTOS","CATEGORIAS",
        "CLIENTES","POS","ORDENES","HISTORICO_FACTURAS","NCF_SECUENCIAS","IMPRESION_TERMICA",
        "DESCUENTOS","EMPLEADOS","TICKETS","LISTADO_DEVOLUCIONES","NOTAS_CREDITO_APLICADAS",
        "REPORTE_VENTA","REPORTE_607","CUENTAS_COBRAR","CIERRE_CAJA","LISTADO_CAJA","MOVIMIENTO_CAJA",
        "CENTRO_PRODUCCION","PRODUCCION_CONFIG","PRODUCCION_GESTIONAR","PRODUCCION_PRIORIDAD",
        "PRODUCCION_CANCELAR","CUENTAS_FINANCIERAS","METODO_PAGO_CUENTAS","MOVIMIENTO_FINANCIERO",
        "GASTOS","INGRESOS","LISTADO_PAGOS","ACTIVOS_FIJOS","CONTABILIDAD","CONTABILIDAD_CONFIGURACION_INTEGRACION",
    ],
    "reposteria": [
        "EMPRESA","USUARIOS","PERFILES","PARAMETROS","DASHBOARD","PRODUCTOS","CATEGORIAS",
        "CLIENTES","POS","ORDENES","HISTORICO_FACTURAS","NCF_SECUENCIAS","IMPRESION_TERMICA",
        "DESCUENTOS","EMPLEADOS","TICKETS","LISTADO_DEVOLUCIONES","NOTAS_CREDITO_APLICADAS",
        "REPORTE_VENTA","REPORTE_607","CUENTAS_COBRAR","CIERRE_CAJA","LISTADO_CAJA","MOVIMIENTO_CAJA",
        "ALMACENES","MOVIMIENTO_INVENTARIO","PROVEEDORES","FACTURAS_COMPRA","ORDENES_COMPRA",
        "CUENTAS_PAGAR_PROVEEDOR","CUENTAS_FINANCIERAS","METODO_PAGO_CUENTAS","MOVIMIENTO_FINANCIERO",
        "GASTOS","INGRESOS","LISTADO_PAGOS","ACTIVOS_FIJOS","CONTABILIDAD","CONTABILIDAD_CONFIGURACION_INTEGRACION",
        "CENTRO_PRODUCCION","PRODUCCION_CONFIG","PRODUCCION_GESTIONAR","PRODUCCION_PRIORIDAD",
        "PRODUCCION_CANCELAR","BIZCOCHO_ENCARGO","CONDUCES",
    ],
    "carwash": [
        "EMPRESA","USUARIOS","PERFILES","PARAMETROS","DASHBOARD","PRODUCTOS","CATEGORIAS",
        "CLIENTES","POS","ORDENES","HISTORICO_FACTURAS","NCF_SECUENCIAS","IMPRESION_TERMICA",
        "DESCUENTOS","EMPLEADOS","TICKETS","LISTADO_DEVOLUCIONES","NOTAS_CREDITO_APLICADAS",
        "REPORTE_VENTA","REPORTE_607","CUENTAS_COBRAR","CIERRE_CAJA","LISTADO_CAJA","MOVIMIENTO_CAJA",
        "CUENTAS_FINANCIERAS","METODO_PAGO_CUENTAS","MOVIMIENTO_FINANCIERO","GASTOS","INGRESOS",
        "LISTADO_PAGOS","ACTIVOS_FIJOS","CONTABILIDAD","CONTABILIDAD_CONFIGURACION_INTEGRACION",
        "AREAS","EMPLEADOS_COMISION","REPORTE_COMISIONES","REPORTE_SERVICIOS","CONSUMO_LAVADORES",
        "CITAS","CUMPLEANEROS",
    ],
    "salon": [
        "EMPRESA","USUARIOS","PERFILES","PARAMETROS","DASHBOARD","PRODUCTOS","CATEGORIAS",
        "CLIENTES","POS","ORDENES","HISTORICO_FACTURAS","NCF_SECUENCIAS","IMPRESION_TERMICA",
        "DESCUENTOS","EMPLEADOS","TICKETS","LISTADO_DEVOLUCIONES","NOTAS_CREDITO_APLICADAS",
        "REPORTE_VENTA","REPORTE_607","CUENTAS_COBRAR","CIERRE_CAJA","LISTADO_CAJA","MOVIMIENTO_CAJA",
        "CUENTAS_FINANCIERAS","METODO_PAGO_CUENTAS","MOVIMIENTO_FINANCIERO","GASTOS","INGRESOS",
        "LISTADO_PAGOS","ACTIVOS_FIJOS","CONTABILIDAD","CONTABILIDAD_CONFIGURACION_INTEGRACION",
        "AREAS","EMPLEADOS_COMISION","REPORTE_COMISIONES","REPORTE_SERVICIOS","CITAS",
        "HORARIO_ESTILISTA","CUMPLEANEROS","HISTORIAL_SERVICIOS",
    ],
    "ferreteria": [
        "EMPRESA","USUARIOS","PERFILES","PARAMETROS","DASHBOARD","PRODUCTOS","CATEGORIAS",
        "CLIENTES","POS","ORDENES","HISTORICO_FACTURAS","NCF_SECUENCIAS","IMPRESION_TERMICA",
        "DESCUENTOS","EMPLEADOS","TICKETS","LISTADO_DEVOLUCIONES","NOTAS_CREDITO_APLICADAS",
        "REPORTE_VENTA","REPORTE_607","CUENTAS_COBRAR","CIERRE_CAJA","LISTADO_CAJA","MOVIMIENTO_CAJA",
        "ALMACENES","MOVIMIENTO_INVENTARIO","PROVEEDORES","FACTURAS_COMPRA","ORDENES_COMPRA",
        "CUENTAS_PAGAR_PROVEEDOR","CUENTAS_FINANCIERAS","METODO_PAGO_CUENTAS","MOVIMIENTO_FINANCIERO",
        "GASTOS","INGRESOS","LISTADO_PAGOS","ACTIVOS_FIJOS","CONTABILIDAD",
        "CONTABILIDAD_CONFIGURACION_INTEGRACION","CONDUCES",
    ],
    "repuestos": [
        "EMPRESA","USUARIOS","PERFILES","PARAMETROS","DASHBOARD","PRODUCTOS","CATEGORIAS",
        "CLIENTES","POS","ORDENES","HISTORICO_FACTURAS","NCF_SECUENCIAS","IMPRESION_TERMICA",
        "DESCUENTOS","EMPLEADOS","TICKETS","LISTADO_DEVOLUCIONES","NOTAS_CREDITO_APLICADAS",
        "REPORTE_VENTA","REPORTE_607","CUENTAS_COBRAR","CIERRE_CAJA","LISTADO_CAJA","MOVIMIENTO_CAJA",
        "ALMACENES","MOVIMIENTO_INVENTARIO","PROVEEDORES","FACTURAS_COMPRA","ORDENES_COMPRA",
        "CUENTAS_PAGAR_PROVEEDOR","CUENTAS_FINANCIERAS","METODO_PAGO_CUENTAS","MOVIMIENTO_FINANCIERO",
        "GASTOS","INGRESOS","LISTADO_PAGOS","ACTIVOS_FIJOS","CONTABILIDAD",
        "CONTABILIDAD_CONFIGURACION_INTEGRACION","CONDUCES",
    ],
}

PRODUCTOS = {
    "ferreteria": [
        ("Cemento gris 42.5kg", 380, False, 50),
        ("Varilla 3/8", 220, False, 100),
        ("Pintura blanca galón", 650, False, 30),
        ("Brocha 2\"", 85, False, 40),
        ("Tornillo drywall caja", 150, False, 25),
        ("Cable THHN #12 m", 28, False, 200),
    ],
    "repuestos": [
        ("Filtro de aceite", 350, False, 40),
        ("Pastillas de freno", 1200, False, 20),
        ("Bujía NGK", 280, False, 50),
        ("Aceite 5W30 1L", 450, False, 60),
        ("Amortiguador delantero", 2800, False, 8),
        ("Batería 12V", 4500, False, 10),
    ],
    "salon": [
        ("Corte dama", 800, True, 0),
        ("Tinte completo", 2500, True, 0),
        ("Manicure", 600, True, 0),
        ("Pedicure", 700, True, 0),
        ("Keratina", 4500, True, 0),
        ("Shampoo tratamiento", 950, False, 15),
    ],
    "carwash": [
        ("Lavado básico", 300, True, 0),
        ("Lavado full detail", 1200, True, 0),
        ("Encerado", 800, True, 0),
        ("Motor", 500, True, 0),
        ("Aspirado interior", 250, True, 0),
        ("Aromatizante", 150, False, 40),
    ],
    "reposteria": [
        ("Bizcocho 1/4 lb", 800, False, 10),
        ("Bizcocho 1 lb", 1800, False, 8),
        ("Cupcake unidad", 120, False, 40),
        ("Galleta decorada", 80, False, 50),
        ("Encargo personalizado", 0, True, 0),
    ],
    "comida": [
        ("Hotdog clásico", 150, False, 0),
        ("Hotdog especial", 220, False, 0),
        ("Papas fritas", 120, False, 0),
        ("Refresco 12oz", 80, False, 0),
    ],
}


def sqlcmd(query: str) -> str:
    tmp = Path(__file__).with_suffix(".tmp.sql")
    full = "SET QUOTED_IDENTIFIER ON;\nSET ANSI_NULLS ON;\nSET NOCOUNT ON;\n" + query
    tmp.write_text(full, encoding="utf-8-sig")
    try:
        r = subprocess.run(
            [
                "sqlcmd",
                "-S", SERVER,
                "-U", USER,
                "-P", PASSWORD,
                "-d", DB,
                "-I",  # quoted identifier ON
                "-b",  # abort on error
                "-i", str(tmp),
                "-W",
                "-s", "|",
                "-h", "-1",
            ],
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
        )
        out = (r.stdout or "") + (r.stderr or "")
        if r.returncode != 0:
            raise RuntimeError(out)
        return out
    finally:
        if tmp.exists():
            tmp.unlink()


def esc(s: str) -> str:
    return s.replace("'", "''")


def ensure_empresa(nombre: str, correo: str, telefono: str) -> int:
    q = f"""
SET NOCOUNT ON;
DECLARE @id INT = (SELECT TOP 1 IdEmpresa FROM Empresas WHERE CorreElectronico = N'{esc(correo)}');
IF @id IS NULL
  SET @id = (SELECT TOP 1 IdEmpresa FROM Empresas WHERE NombreComercial = N'{esc(nombre)}');
IF @id IS NULL
BEGIN
  INSERT INTO Empresas (
    NombreComercial, RNC, Direccion, Telefono, CorreElectronico,
    FechaInseccion, FechaTerminacion, GuidPublico, Estado, EstadoServicio,
    PagadoServicio, PoliticasAceptadas, EsEmpresaSistema, IdPlan, LimiteUsuario,
    MontoServicio, PrecioPlanEspecialUsd, UsaSSL, PuertoSMTP,
    PrimaryColor, SecondaryColor, TertiaryColor, titleColor,
    LimiteFacturacion, CargoAdicional, CargoReconexionDop, ReconexionPendiente
  ) VALUES (
    N'{esc(nombre)}', N'000000000', N'Santo Domingo, RD', N'{esc(telefono)}', N'{esc(correo)}',
    CAST(GETDATE() AS date), DATEADD(year, 5, GETDATE()), NEWID(), 1, N'ACTIVA',
    1, 1, 0, 1, 5,
    0, 0, 1, 587,
    N'#0a3d91', N'#f5c518', N'#072a66', N'#072a66',
    0, 0, 0, 0
  );
  SET @id = SCOPE_IDENTITY();
END
ELSE
BEGIN
  UPDATE Empresas SET
    FechaTerminacion = DATEADD(year, 5, GETDATE()),
    Estado = 1,
    EstadoServicio = N'ACTIVA',
    MontoServicio = 0,
    PagadoServicio = 1
  WHERE IdEmpresa = @id;
END
SELECT @id;
"""
    out = sqlcmd(q).strip().splitlines()
    for line in out:
        line = line.strip()
        if line.isdigit():
            return int(line)
    raise RuntimeError(f"No IdEmpresa for {nombre}: {out}")


def sync_modules(id_empresa: int, codes: list[str]) -> None:
    values = ",".join(f"(N'{esc(c)}')" for c in codes)
    q = f"""
SET NOCOUNT ON;
DECLARE @IdEmpresa INT = {id_empresa};

-- Activar / insertar módulos deseados
;WITH Wanted AS (
  SELECT m.Id AS ModuloId
  FROM Modulos m
  JOIN (VALUES {values}) v(Codigo) ON m.Codigo = v.Codigo
  WHERE m.Activo = 1
)
MERGE Empresa_Modulos AS t
USING Wanted AS s
ON t.EmpresaId = @IdEmpresa AND t.ModuloId = s.ModuloId
WHEN MATCHED THEN UPDATE SET Activo = 1, FechaActivacion = ISNULL(FechaActivacion, GETDATE()), FechaDesactivacion = NULL
WHEN NOT MATCHED THEN INSERT (EmpresaId, ModuloId, Activo, FechaActivacion)
VALUES (@IdEmpresa, s.ModuloId, 1, GETDATE());

-- Desactivar módulos no deseados (excepto internos)
UPDATE em SET Activo = 0, FechaDesactivacion = GETDATE()
FROM Empresa_Modulos em
JOIN Modulos m ON m.Id = em.ModuloId
WHERE em.EmpresaId = @IdEmpresa AND em.Activo = 1
  AND m.Codigo NOT IN (SELECT Codigo FROM (VALUES {values}) v(Codigo))
  AND m.Codigo NOT IN (N'ALAHIA_AI', N'MACROBITS_ADMIN', N'EMPRESAS_ADMIN', N'SUSCRIPCIONES_COBROS', N'PAGO_SUSCRIPCION', N'TICKETS_ADMIN');

-- Perfil Administrador
DECLARE @IdPerfil INT = (
  SELECT TOP 1 IdPerfil FROM Perfiles WHERE IdEmpresa = @IdEmpresa AND Nombre = N'Administrador'
);
IF @IdPerfil IS NULL
BEGIN
  INSERT INTO Perfiles (Nombre, Descripcion, IdEmpresa, Activo)
  VALUES (N'Administrador', N'Perfil administrador demo', @IdEmpresa, 1);
  SET @IdPerfil = SCOPE_IDENTITY();
END
ELSE
  UPDATE Perfiles SET Activo = 1 WHERE IdPerfil = @IdPerfil;

-- Roles = módulos activos empresa
DELETE FROM PerfilRoles WHERE IdPerfil = @IdPerfil;
INSERT INTO PerfilRoles (IdPerfil, IdModulo, Activo, FechaInsercion, IdEmpresa)
SELECT @IdPerfil, em.ModuloId, 1, GETDATE(), @IdEmpresa
FROM Empresa_Modulos em
WHERE em.EmpresaId = @IdEmpresa AND em.Activo = 1;

SELECT @IdPerfil;
"""
    sqlcmd(q)


def ensure_admin(id_empresa: int, correo: str) -> None:
    q = f"""
SET NOCOUNT ON;
DECLARE @IdEmpresa INT = {id_empresa};
DECLARE @Correo NVARCHAR(200) = N'{esc(correo)}';
DECLARE @IdPerfil INT = (
  SELECT TOP 1 IdPerfil FROM Perfiles WHERE IdEmpresa = @IdEmpresa AND Nombre = N'Administrador' AND Activo = 1
);
IF @IdPerfil IS NULL
  SET @IdPerfil = (SELECT TOP 1 IdPerfil FROM Perfiles WHERE IdEmpresa = @IdEmpresa AND Activo = 1);

DECLARE @IdEmp INT = (
  SELECT TOP 1 IdEmpleados FROM Empleados WHERE IdEmpresa = @IdEmpresa AND Nombre = N'Administrador'
);
IF @IdEmp IS NULL
BEGIN
  INSERT INTO Empleados (
    Nombre, Ocupacion, Estado, IdEmpresa, FechaInseccion, FechaIngreso, FechaNacimiento,
    ComisionServicio, ComisionProductos, Salario
  )
  VALUES (
    N'Administrador', N'Administrador', 1, @IdEmpresa, GETDATE(), GETDATE(), '1990-01-01',
    0, 0, 0
  );
  SET @IdEmp = SCOPE_IDENTITY();
END

IF NOT EXISTS (SELECT 1 FROM Usuarios WHERE Correo = @Correo OR UserName = @Correo)
BEGIN
  INSERT INTO Usuarios (
    IdEmpresa, UserName, Correo, PasswordHash, Estado, IdPerfil, IdEmpleado, FechaCreacion,
    PuedeEliminarOrden, PuedeEliminarItemCarrito, PuedeDisminuirCantidadCarrito, PuedeEditarPrecioCarrito
  )
  VALUES (
    @IdEmpresa, @Correo, @Correo, N'{PASS_HASH}', 1, @IdPerfil, @IdEmp, GETDATE(),
    1, 1, 1, 1
  );
END
ELSE
BEGIN
  UPDATE Usuarios SET
    PasswordHash = N'{PASS_HASH}',
    Estado = 1,
    IdEmpresa = @IdEmpresa,
    IdPerfil = @IdPerfil,
    IdEmpleado = ISNULL(@IdEmp, IdEmpleado),
    PuedeEliminarOrden = 1,
    PuedeEliminarItemCarrito = 1,
    PuedeDisminuirCantidadCarrito = 1,
    PuedeEditarPrecioCarrito = 1
  WHERE Correo = @Correo OR UserName = @Correo;
END
SELECT COUNT(*) FROM Usuarios WHERE IdEmpresa = @IdEmpresa AND Estado = 1;
"""
    print(" admin", sqlcmd(q).strip())


def ensure_catalog(id_empresa: int, vertical: str) -> None:
    items = PRODUCTOS.get(vertical, [])
    if not items:
        return
    q = f"""
DECLARE @IdEmpresa INT = {id_empresa};
IF NOT EXISTS (SELECT 1 FROM Categorias WHERE IdEmpresa = @IdEmpresa AND Nombre = N'Productos')
  INSERT INTO Categorias (Nombre, Descripcion, Tipo, IsActiva, FechaInseccion, Prioridad, IdEmpresa, TipoOperacion)
  VALUES (N'Productos', N'', N'Producto', 1, GETDATE(), 1, @IdEmpresa, N'VENTA');
IF NOT EXISTS (SELECT 1 FROM Categorias WHERE IdEmpresa = @IdEmpresa AND Nombre = N'Servicios')
  INSERT INTO Categorias (Nombre, Descripcion, Tipo, IsActiva, FechaInseccion, Prioridad, IdEmpresa, TipoOperacion)
  VALUES (N'Servicios', N'', N'Servicio', 1, GETDATE(), 2, @IdEmpresa, N'VENTA');

DECLARE @CatProd INT = (SELECT TOP 1 IdCategoria FROM Categorias WHERE IdEmpresa=@IdEmpresa AND Nombre=N'Productos');
DECLARE @CatServ INT = (SELECT TOP 1 IdCategoria FROM Categorias WHERE IdEmpresa=@IdEmpresa AND Nombre=N'Servicios');

IF NOT EXISTS (SELECT 1 FROM Proveedores WHERE IdEmpresa=@IdEmpresa AND NombreComercial=N'Demo Proveedor')
  INSERT INTO Proveedores (NombreComercial, IsActivo, FechaInseccion, IdEmpresa, RNC, Telefono, Direccion, Email)
  VALUES (N'Demo Proveedor', 1, GETDATE(), @IdEmpresa, N'', N'', N'', N'');

DECLARE @IdProv INT = (SELECT TOP 1 IdProveedor FROM Proveedores WHERE IdEmpresa=@IdEmpresa ORDER BY IdProveedor);
"""
    for nombre, precio, es_serv, stock in items:
        cat = "@CatServ" if es_serv else "@CatProd"
        costo = 0 if es_serv else round(precio * 0.6, 2)
        q += f"""
IF NOT EXISTS (SELECT 1 FROM Productos WHERE IdEmpresa=@IdEmpresa AND Nombre=N'{esc(nombre)}')
INSERT INTO Productos (
  Nombre, EsServicio, IdProveedor, Cantidad, Stock, PrecioVenta, Precio1, Precio2, Precio3,
  PrecioCompra, IdCategoria, IdEmpresa, IsActivo, ControlarStock, TipoOperacion, TipoComportamiento,
  Itbis, CodigoBarra, Rentado, Descuento, PorcientoDescuento, PorcientoGanancia,
  SeCompra, SeAlquila, SeVende, Ganancia, FechaInseccion, PrecioDolar, PrecioExterno,
  DuracionServicio, DisponibleEnCitas
) VALUES (
  N'{esc(nombre)}', {1 if es_serv else 0}, @IdProv, {stock}, {stock if not es_serv else 0}, {precio}, {precio}, 0, 0,
  {costo}, {cat}, @IdEmpresa, 1, {0 if es_serv else 1}, N'VENTA', N'{'Servicio' if es_serv else 'Inventario'}',
  1, N'', 0, 0, 0, 0,
  {0 if es_serv else 1}, 0, 1, 0, GETDATE(), 0, 0,
  {30 if es_serv else 0}, {1 if es_serv else 0}
);
"""
    q += "SELECT COUNT(*) FROM Productos WHERE IdEmpresa=@IdEmpresa;"
    print(" catalog", sqlcmd(q).strip())


def main():
    demos = [
        # Dedicadas para calle (credenciales conocidas)
        ("Alahia Demo Ferretería", "demo.ferreteria@alahiapos.com", "8090000001", "ferreteria"),
        ("Alahia Demo Repuestos", "demo.repuestos@alahiapos.com", "8090000002", "repuestos"),
        ("Alahia Demo Belleza", "demo.belleza@alahiapos.com", "8090000003", "salon"),
        ("Alahia Demo Carwash", "demo.carwash@alahiapos.com", "8090000004", "carwash"),
        ("Alahia Demo Repostería", "demo.reposteria@alahiapos.com", "8090000005", "reposteria"),
        ("Alahia Demo Comida", "demo.comida@alahiapos.com", "8090000006", "comida"),
    ]

    results = []
    for nombre, correo, tel, vertical in demos:
        eid = ensure_empresa(nombre, correo, tel)
        sync_modules(eid, PRESETS[vertical])
        ensure_admin(eid, correo)
        ensure_catalog(eid, vertical)
        results.append((eid, nombre, correo, vertical))
        print(f"OK {eid} {nombre} ({vertical})")

    # Extender vigencia Laura (cliente real / referencia)
    sqlcmd("""
UPDATE Empresas
SET FechaTerminacion = DATEADD(year, 2, GETDATE()), Estado = 1, EstadoServicio = N'ACTIVA'
WHERE IdEmpresa = 56;
""")
    print("OK extendida vigencia De Laura Pastelería (56)")

    print("\n=== CREDENCIALES DEMO (password DemoAlahia2026) ===")
    for eid, nombre, correo, vertical in results:
        print(f"{vertical:12} | Id={eid:3} | {correo} | {nombre}")

    print("\nReferencias reales (credenciales del cliente):")
    print("comida      | Id=61  | La Leyenda Hotdog")
    print("carwash     | Id=58  | AUTOSERVICIOSTOTALCLEAN")
    print("reposteria  | Id=56  | De Laura Pastelería")
    print("salon       | Id=1   | Dismerling Beauty Salón")


if __name__ == "__main__":
    main()
