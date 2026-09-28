/*
  Catálogo fuente de verdad — Terraza prolongación 27 (MATBERT SRL), IdEmpresa=55.
  Origen: Music\ListadoProductos.pdf
  Actualiza precios, costo, existencia y stock mínimo.
  Inserta faltantes. Desactiva lo que no está en el listado (no borra).
  Sincroniza AlmacenExistencias del almacén principal.
*/
USE AlahiaPos_Prod;
GO
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
GO

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Prod.', 16, 1);
    RETURN;
END;

DECLARE @IdEmpresa INT = 55;
DECLARE @IdAlmacen INT, @IdProveedor INT, @IdUnidad INT, @IdArea INT;
DECLARE @CatCarwash INT, @CatCervezas INT, @CatLicores INT, @CatBebidas INT;
DECLARE @CatSnacks INT, @CatCigarros INT, @CatAccesorios INT;

IF NOT EXISTS (
    SELECT 1 FROM dbo.Empresas
    WHERE IdEmpresa = @IdEmpresa AND NombreComercial LIKE N'Terraza%'
)
BEGIN
    RAISERROR(N'IdEmpresa 55 no es Terraza 27 / MATBERT.', 16, 1);
    RETURN;
END;

SELECT TOP 1 @IdAlmacen = IdAlmacen FROM dbo.Almacenes
WHERE IdEmpresa = @IdEmpresa AND Activo = 1 ORDER BY EsPrincipal DESC, IdAlmacen;

SELECT TOP 1 @IdProveedor = IdProveedor FROM dbo.Productos
WHERE IdEmpresa = @IdEmpresa AND IdProveedor IS NOT NULL AND IdProveedor > 0
ORDER BY IdProducto;

SELECT TOP 1 @IdUnidad = IdUnidadMedida FROM dbo.Productos
WHERE IdEmpresa = @IdEmpresa AND IdUnidadMedida IS NOT NULL;

SELECT TOP 1 @IdArea = IdArea FROM dbo.Areas WHERE IdEmpresa = @IdEmpresa ORDER BY IdArea;

SELECT @CatCarwash = MAX(CASE WHEN Nombre = N'CARWASH' THEN IdCategoria END),
       @CatCervezas = MAX(CASE WHEN Nombre = N'CERVEZAS' THEN IdCategoria END),
       @CatLicores = MAX(CASE WHEN Nombre = N'LICORES' THEN IdCategoria END),
       @CatBebidas = MAX(CASE WHEN Nombre = N'BEBIDAS' THEN IdCategoria END),
       @CatSnacks = MAX(CASE WHEN Nombre = N'SNACKS' THEN IdCategoria END),
       @CatCigarros = MAX(CASE WHEN Nombre LIKE N'CIGARRILLOS%' THEN IdCategoria END),
       @CatAccesorios = MAX(CASE WHEN Nombre = N'ACCESORIOS' THEN IdCategoria END)
FROM dbo.Categorias WHERE IdEmpresa = @IdEmpresa;

IF @IdAlmacen IS NULL OR @IdProveedor IS NULL
BEGIN
    RAISERROR(N'Falta almacén o proveedor en Terraza 55.', 16, 1);
    RETURN;
END;

IF OBJECT_ID('tempdb..#Cat') IS NOT NULL DROP TABLE #Cat;
CREATE TABLE #Cat (
    IdFila INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(200) NOT NULL,
    BarCode VARCHAR(30) NOT NULL,
    Cantidad DECIMAL(18,2) NOT NULL,
    StockMinimo DECIMAL(18,2) NOT NULL,
    PrecioCompra DECIMAL(18,2) NOT NULL,
    PrecioVenta DECIMAL(18,2) NOT NULL,
    EsServicio BIT NOT NULL DEFAULT 0,
    IdCategoria INT NULL,
    IdProducto INT NULL
);

INSERT INTO #Cat (Nombre, BarCode, Cantidad, StockMinimo, PrecioCompra, PrecioVenta)
VALUES
(N'Casillero Del Diablo Red Blend', N'7804320746555', 0.00, 10.00, 640.00, 1000.00),
(N'Brownie', N'A00007', 0.00, 10.00, 50.00, 70.00),
(N'Little Trees New Car Scent', N'076171101891', 9.00, 10.00, 64.00, 150.00),
(N'Little Trees Coconut', N'076171103178', 3.00, 10.00, 64.00, 150.00),
(N'Pistacho', N'A000013', 10.00, 10.00, 50.00, 75.00),
(N'Lavado De Camion Sin Fulgon', N'A000031', 29.00, 10.00, 0.00, 800.00),
(N'Galleta Emperador 72g', N'7500478013616', 2.00, 10.00, 48.00, 65.00),
(N'Galletas Qiin De Chocolate', N'7468339191869', 11.00, 5.00, 10.00, 15.00),
(N'Semillas mixta', N'A000010', 30.00, 10.00, 50.00, 75.00),
(N'Semillas de cajuil', N'A000011', 17.00, 10.00, 50.00, 75.00),
(N'Almendras', N'A000012', 15.00, 10.00, 50.00, 75.00),
(N'Master Of Mixes Sour Apple Mixer', N'070491060241', 1.00, 10.00, 460.00, 600.00),
(N'Jugo 100% Naranja Con Azucar Mediano', N'790330021089', 8.00, 10.00, 51.00, 75.00),
(N'Jugo 100% Naranja Sin Azucar Mediano', N'790330021140', 11.00, 10.00, 59.00, 80.00),
(N'Jugo 100% Naranja con Azucar Grande', N'790330021072', 0.00, 10.00, 95.00, 130.00),
(N'Jugo 100% Naranja Sin Azucar Grande', N'790330021126', 0.00, 10.00, 114.00, 160.00),
(N'Frutop 1 Litro', N'7468783291221', 70.00, 10.00, 36.00, 50.00),
(N'Kola Real 1 Litro', N'7461063094574', 19.00, 20.00, 32.00, 100.00),
(N'Presidente Black Peq', N'7463172803764', 0.00, 10.00, 100.00, 150.00),
(N'Trident 3 Chicles', N'7702133863264', 19.00, 10.00, 0.00, 25.00),
(N'Fireball 200ml', N'088004144739', 10.00, 10.00, 414.00, 500.00),
(N'Barra Bon o Bon Snack Chocolate Relleno', N'7502230950153', 29.00, 10.00, 17.00, 20.00),
(N'Bon o Bon Bola Chocolate Relleno', N'75027971', -6.00, 10.00, 11.00, 15.00),
(N'Paleta', N'A000019', 28.00, 10.00, 5.00, 10.00),
(N'Cool Heaven Soda Carbonatada', N'7461063095366', 32.00, 10.00, 38.00, 50.00),
(N'Jugo Rica Naranja 200ml', N'7460111102568', 8.00, 10.00, 22.00, 30.00),
(N'Jugo Rica Orange Mediano 500ml', N'7460111102513', 31.00, 10.00, 54.00, 75.00),
(N'Vive100 G', N'7702354251093', 9.00, 10.00, 38.00, 75.00),
(N'Clorets 2.8g', N'75020460', 142.00, 10.00, 2.50, 5.00),
(N'Da pa To NatuChips', N'7460496804774', 35.00, 10.00, 62.00, 85.00),
(N'Club Social Original', N'7622201717544', 0.00, 10.00, 7.00, 10.00),
(N'Agua Planeta Azul', N'701891100014', 32.00, 10.00, 18.00, 25.00),
(N'CIGARRO DON TABACO', N'A000034', 39.00, 10.00, 250.00, 400.00),
(N'Winasorb', N'7451079003561', 19.00, 10.00, 18.00, 25.00),
(N'Sal Andrews', N'74410330', 15.00, 10.00, 22.00, 30.00),
(N'Alka-Seltzer', N'011418462311', 90.00, 10.00, 27.00, 50.00),
(N'Troyano Black Lbel', N'4584156584417', 19.00, 10.00, 125.00, 150.00),
(N'Red Rock Merengue 400ml', N'7463172803528', 15.00, 10.00, 28.00, 40.00),
(N'Red Rock Naranja 400ml', N'7463172803559', 1.00, 10.00, 28.00, 40.00),
(N'Red Rock Uva 400ml', N'7463172803580', 18.00, 10.00, 34.00, 40.00),
(N'Ron Brugal Triple Reserva', N'7460855238066', 2.00, 10.00, 925.00, 1500.00),
(N'Florentinas Dulce De Leche 83g', N'7501000601745', 3.00, 10.00, 48.00, 60.00),
(N'Galleta Emperador 36g', N'7500478012404', 5.00, 10.00, 19.00, 25.00),
(N'Funda De Hielo', N'A00006', 86.00, 30.00, 40.00, 100.00),
(N'Vaso De Hielo', N'A00003', 608.00, 10.00, 0.00, 15.00),
(N'Agua Tonica Canada Dry 400ml', N'7465383000321', 33.00, 10.00, 62.00, 75.00),
(N'Jugo Del Valle Coctel De Frutas 200ml', N'025000134043', 28.00, 10.00, 18.00, 25.00),
(N'Lays Clasicas 80g', N'A00008', 14.00, 10.00, 59.00, 85.00),
(N'Chicharron Limon 27g', N'7464113824121', 17.00, 10.00, 32.00, 35.00),
(N'Cool Heaven Saborizada', N'7461063097148', 12.00, 10.00, 18.33, 40.00),
(N'Menta Halls', N'A00001', 2796.00, 10.00, 2.00, 2.50),
(N'Picaderas Sencilla', N'', 48.00, 10.00, 0.00, 300.00),
(N'Picaderas Completa', N'', 50.00, 10.00, 0.00, 400.00),
(N'Lavado Camiones Ramon', N'A000086', 100.00, 10.00, 0.00, 1.00),
(N'Lavado Camioneta Sencillo', N'A000061', 95.00, 10.00, 0.00, 500.00),
(N'Lavado Motor', N'A000060', 402.00, 10.00, 0.00, 150.00),
(N'Lavado Vehiculos Ramon y Maria', N'', 50.00, 10.00, 0.00, 0.00),
(N'Bohemia Peq 355ml', N'74618903', 196.00, 25.00, 92.00, 125.00),
(N'Trident Splash', N'7622201685386', 2.00, 10.00, 58.00, 75.00),
(N'Trident 8.5g', N'7622201776664', 71.00, 10.00, 9.00, 35.00),
(N'Miller Grande 650ml', N'034100005696', 0.00, 10.00, 113.28, 225.00),
(N'La Benedicta 330ml', N'7460736904042', 0.00, 10.00, 92.00, 125.00),
(N'Stella Artois 330ml', N'7501064199141', 8.00, 10.00, 121.00, 180.00),
(N'Smirnoff 330ml', N'082000723844', 83.00, 10.00, 135.00, 175.00),
(N'Miller Peq 355ml', N'A000035', 66.00, 10.00, 108.00, 150.00),
(N'Modelo Peq Especial Negra', N'75031589', 0.00, 10.00, 121.00, 170.00),
(N'Bohemia Mediana 650ml', N'74628711', 30.00, 25.00, 135.18, 175.00),
(N'One Peq 355ml', N'74601325', 51.00, 50.00, 84.00, 125.00),
(N'Modelo Peq Especial Rubia', N'7463172802996', 16.00, 10.00, 105.26, 160.00),
(N'Heineken Grande 650ml', N'8712000030582', 26.00, 15.00, 170.58, 225.00),
(N'Modelo Grande Especial Rubia', N'A00005', 0.00, 10.00, 137.00, 225.00),
(N'CoronaExtra 330ml', N'7503034941200', 2.00, 50.00, 113.75, 200.00),
(N'Heineken Peq 330ml', N'072890004994', 24.00, 15.00, 128.50, 175.00),
(N'Brahma Light 355ml', N'7468973200194', 64.00, 50.00, 84.00, 125.00),
(N'One Mediana 650ml', N'74601127', 44.00, 50.00, 128.12, 175.00),
(N'Brahma Light 650ml', N'7468973200200', 62.00, 50.00, 128.00, 175.00),
(N'Presidente Peq 355ml', N'74621774', 1285.00, 10.00, 92.00, 150.00),
(N'Presidente Mediana 650ml', N'74601561', 1981.00, 10.00, 143.73, 200.00),
(N'Granberry Classic 450ml', N'031200008091', 3.00, 10.00, 100.00, 135.00),
(N'Aloe Original', N'8809125063011', 0.00, 10.00, 95.00, 150.00),
(N'Motts Peq 296ml', N'01489438', 15.00, 10.00, 63.00, 100.00),
(N'Sprite Limon', N'049000057690', 46.00, 10.00, 0.00, 50.00),
(N'Red Rock Merengue 450ml', N'7468973202969', 29.00, 10.00, 32.00, 40.00),
(N'Frutop Peq 300ml', N'7468783290002', 46.00, 10.00, 16.00, 20.00),
(N'Malta Morena 355ml', N'74650309', 0.00, 10.00, 35.41, 75.00),
(N'Extracto De Malta Lowenbrau', N'7463172802613', 0.00, 10.00, 61.00, 100.00),
(N'Coca Cola Sin Azucar 400ml', N'049000071993', 0.00, 10.00, 22.00, 40.00),
(N'Pepsi 400ml', N'7463172803290', 0.00, 10.00, 28.00, 40.00),
(N'Frutop 450ml', N'7461063095595', 33.00, 10.00, 22.00, 30.00),
(N'Red Rock Naranja 450ml', N'7468973202983', 0.00, 10.00, 28.00, 40.00),
(N'Coca Cola Original 400ml', N'049000057638', 21.00, 10.00, 22.00, 40.00),
(N'7 Up Limon 450ml', N'7468973203003', 20.00, 10.00, 28.00, 40.00),
(N'V8 Splash Strawberry Kiwi', N'7462275402843', 0.00, 10.00, 100.00, 125.00),
(N'Motts Grande 946ml', N'01480000032', 9.00, 10.00, 157.00, 250.00),
(N'Sprite Limon 1.25l', N'049000031577', 50.00, 10.00, 100.00, 125.00),
(N'V8 Splash Fruit Medley', N'7462275402829', 0.00, 10.00, 100.00, 125.00),
(N'Coco Rico 450ml', N'7467003480339', 24.00, 10.00, 29.00, 40.00),
(N'Red Rock Uva 450ml', N'7468973202976', 0.00, 10.00, 28.00, 40.00),
(N'Red Rock Manzana Verde 450ml', N'7468973202952', 0.00, 10.00, 28.00, 40.00),
(N'Red Rock Frambuesa 450ml', N'7468973202945', 36.00, 10.00, 28.00, 40.00),
(N'Kola Real 400ml', N'7461063094819', 76.00, 20.00, 17.00, 25.00),
(N'Kola Real Cola Negra 10oz', N'7468783290545', 46.00, 10.00, 14.00, 20.00),
(N'Agua Perrier Grande', N'A00004', 3.00, 10.00, 180.00, 300.00),
(N'Agua Enriquillo Tonica Amarilla', N'7468973202341', 59.00, 10.00, 44.00, 60.00),
(N'Gatore', N'7460548000154', 0.00, 10.00, 44.00, 75.00),
(N'Agua Enriquillo Carbonatada Azul', N'7468973202358', 29.00, 10.00, 44.00, 60.00),
(N'Agua Cool Heaven', N'7461063094345', 9089.00, 10.00, 7.50, 20.00),
(N'Generade 592ml', N'7468783291122', 35.00, 10.00, 58.00, 75.00),
(N'Generade 250ml', N'7461063097346', 0.00, 10.00, 16.00, 25.00),
(N'Ashley USB Fast Charger 2.1A', N'001162', 0.00, 10.00, 125.00, 150.00),
(N'Four Loko Mango', N'849806003859', 0.00, 10.00, 275.00, 450.00),
(N'Ciclon Energy Drink Original 250ml', N'830207000004', 0.00, 10.00, 118.00, 150.00),
(N'911 Energy Drink Original 500ml', N'7467003480551', 33.00, 10.00, 40.00, 80.00),
(N'Red Bull Energy Drink 355ml', N'9002490212148', 47.00, 10.00, 117.00, 200.00),
(N'Four Loko Pupple', N'849806002746', 0.00, 10.00, 275.00, 450.00),
(N'Clamato 221ml', N'01484035', 3.00, 10.00, 45.00, 100.00),
(N'Ciclon Energy Drink Original 500ml', N'830207000707', 26.00, 10.00, 102.00, 200.00),
(N'Four Loko Blue', N'849806002319', 0.00, 10.00, 275.00, 450.00),
(N'Four Loko Green', N'849806001855', 0.00, 10.00, 275.00, 450.00),
(N'Red Bull Energy Drink 250ml', N'9002490204006', 35.00, 10.00, 79.00, 150.00),
(N'Four Loko Gold', N'849806001756', 48.00, 10.00, 275.00, 450.00),
(N'Four Loko Sandia', N'849806001206', 1.00, 10.00, 112.00, 450.00),
(N'Marlboro', N'74601172', 38.00, 10.00, 20.00, 25.00),
(N'Newport Freezing Point', N'7421000501350', 7.00, 10.00, 20.00, 25.00),
(N'Pall Mall', N'7421000594642', 46.00, 10.00, 20.00, 25.00),
(N'Vuse Go 500 Puffs 34ml', N'7702303923477', 1.00, 10.00, 400.00, 450.00),
(N'Vuse Go 1500 Puffs 34ml', N'7702303621267', 2.00, 10.00, 800.00, 850.00),
(N'Galletas Ritz Sandwich Con Queso', N'7622201390013', 8.00, 10.00, 16.00, 35.00),
(N'Oreo Sandwich Original Tubo 108g', N'7622201693190', 3.00, 10.00, 41.00, 60.00),
(N'Chokis Rellena', N'7501000604685', 0.00, 10.00, 50.00, 65.00),
(N'Galletas Ritz Saladas Tubo 525g', N'7622201390037', 9.00, 10.00, 28.00, 40.00),
(N'Oreo Sandwich original 36g', N'7590011251100', 0.00, 10.00, 16.00, 25.00),
(N'Crackets Saladas 43g', N'7500478005543', 3.00, 10.00, 28.00, 35.00),
(N'VEEV Now Blueberry 1800 Puffs 5ml', N'7406135040264', 1.00, 10.00, 496.00, 850.00),
(N'Mini Chokis', N'7501000610228', 13.00, 10.00, 52.00, 60.00),
(N'VEEV Now Stawberry 1800 Puffs 5ml', N'7406135040240', 1.00, 10.00, 496.00, 850.00),
(N'Florentinas Sabor a Fresa', N'7501000601738', 4.00, 10.00, 58.00, 65.00),
(N'Oreo Sandwich Original 54g', N'7702133009037', 0.00, 10.00, 24.00, 30.00),
(N'Mamut 30g', N'7501000636921', 19.00, 10.00, 20.00, 25.00),
(N'Crackets Mini Sandwich', N'7500478027118', 14.00, 10.00, 42.00, 60.00),
(N'Pringles Original 37g', N'038000846731', 50.00, 10.00, 62.00, 75.00),
(N'Yummi Nuts Maní con Limón', N'750894671205', 0.00, 10.00, 20.00, 25.00),
(N'Taqueritos Chile Toreado 34g', N'750894612550', 49.00, 10.00, 16.00, 20.00),
(N'Doritos 32g', N'7460496804002', 18.00, 10.00, 32.00, 30.00),
(N'Platanitos 18g', N'7460496800530', 1.00, 10.00, 13.00, 15.00),
(N'Ruffles 29g', N'721282402787', 19.00, 10.00, 25.00, 35.00),
(N'Hojuelitas 23g', N'7460496803623', 18.00, 10.00, 13.00, 15.00),
(N'Cheetos 23g', N'7460496804293', 10.00, 10.00, 13.00, 15.00),
(N'NatuChips Natural 32g', N'7460496800523', 33.00, 10.00, 25.00, 40.00),
(N'Six Eigth Nine Red Wine', N'051497322618', 50.00, 10.00, 1200.00, 1500.00),
(N'Lays Queso Blanco 80g', N'7460496804767', 26.00, 10.00, 61.00, 85.00),
(N'Frontera Cabernet Sauvignon', N'7804320559001', 1.00, 10.00, 591.00, 900.00),
(N'Carlos Rossi Red', N'085000001882', 0.00, 10.00, 414.00, 600.00),
(N'Lays 35g', N'7460496803944', 37.00, 10.00, 15.00, 35.00),
(N'120 Reserva Especial Santa Rita', N'7804330311101', 2.00, 10.00, 640.00, 950.00),
(N'Jhonnie Walker Gold Label Reserve 750ml', N'5000267107776', 2.00, 10.00, 3450.00, 6000.00),
(N'De Todito', N'7464113826507', 38.00, 10.00, 33.00, 40.00),
(N'Something Special', N'80432402795', 3.00, 10.00, 790.00, 1050.00),
(N'Chivas Regal 12', N'08043240039', 1.00, 10.00, 1750.00, 2500.00),
(N'Rose la Sichera', N'7460736980121', 50.00, 10.00, 470.00, 550.00),
(N'Buchaman´s de Luxe 12', N'50196388', 3.00, 10.00, 2200.00, 3300.00),
(N'120 Reserva Especial Santa Rita Merlot', N'7804330341108', 0.00, 10.00, 640.00, 950.00),
(N'De Todito Mofongo 75g', N'7460496804781', 0.00, 10.00, 61.00, 85.00),
(N'Buchaman´s Two Souls', N'5000196006539', 0.00, 10.00, 2900.00, 3850.00),
(N'JP. Chenet Ice Edition Rosada', N'3500610093708', 3.00, 10.00, 750.00, 1100.00),
(N'Jhonnie Walker Red Label', N'5000267014203', 5.00, 10.00, 925.00, 2200.00),
(N'Ballantine´s Finest', N'5010106111536', 4.00, 10.00, 850.00, 1750.00),
(N'JP. Chenet Ice Edition Blanca', N'3500610085338', 2.00, 10.00, 750.00, 1100.00),
(N'Carlos Rossi Fruity Red', N'085000022030', 0.00, 10.00, 414.00, 600.00),
(N'Jhonnie Walker Black Label Aged 12 Years', N'5000267190150', 8.00, 10.00, 2150.00, 3500.00),
(N'Jhonnie Walker Double Black', N'5000267116419', 1.00, 10.00, 2835.00, 4200.00),
(N'Old Par 12 750ml', N'5000281003160', 3.00, 10.00, 1540.00, 2850.00),
(N'Passaport Blended Scotch', N'5000299210048', 2.00, 10.00, 680.00, 1100.00),
(N'Fireball Cinnamon Whisky 350ml', N'088004144722', 2.00, 10.00, 930.00, 850.00),
(N'Dewar´s White Label 750ml', N'5000277001101', 3.00, 10.00, 800.00, 1200.00),
(N'Fireball Cinnamon Whisky 750ml', N'088004146689', 8.00, 10.00, 1425.00, 1600.00),
(N'Black & White Blended Scotch Whisky 700ml', N'50196135', 3.00, 10.00, 750.00, 1450.00),
(N'Dewar´s Blended Scotch Whisky Aged 12 Years', N'5000277002542', 1.00, 10.00, 1450.00, 2200.00),
(N'Wilianm Lawson´s Blended Scotch Whisky 700ml', N'5010752000307', 7.00, 10.00, 639.00, 1100.00),
(N'Fireball Cinnamon Whisky 50ml', N'08800414470', 8.00, 10.00, 92.00, 150.00),
(N'King´s Label Black Whisky 700ml', N'7460522300829', 2.00, 10.00, 624.00, 800.00),
(N'Brugal Leyenda Edicion 5 Aniversario 700ml', N'7460855233269', 4.00, 10.00, 1310.00, 2950.00),
(N'Brugal Leyenda 700ml', N'7460855208694', 0.00, 10.00, 1135.00, 1600.00),
(N'Absolut Vodka 750ml', N'7312040017010', 0.00, 10.00, 1300.00, 1500.00),
(N'Barceló Gran Dark Gran Añejo 700ml', N'7461323129695', 1.00, 10.00, 584.00, 1250.00),
(N'Brugal Añejo 350ml', N'7461323129169', 2.00, 10.00, 300.00, 500.00),
(N'Brugal XV 700ML', N'7460855235270', 4.00, 10.00, 658.00, 1150.00),
(N'Barceló Blanco Añejado 350ml', N'7461323129619', 0.00, 10.00, 250.00, 400.00),
(N'Barceló Imperial 700ml', N'7461323129459', 2.00, 10.00, 1038.00, 1500.00),
(N'Brugal Extra Viejo 350ml', N'7460855234983', 3.00, 10.00, 294.00, 500.00),
(N'Barceló Gran Platinium 700ml', N'7461323129084', 3.00, 10.00, 462.00, 850.00),
(N'Stoli Vodka 750ml', N'4750021000157', 0.00, 10.00, 1300.00, 1500.00),
(N'Brugal Doble Reserva 700ml', N'7460855233498', 4.00, 10.00, 795.00, 1534.00),
(N'Barceló Gran Añejo 700ml', N'7461323129350', 1.00, 10.00, 530.00, 850.00),
(N'Barceló Blanco Añejado 700ml', N'7461323129480', 3.00, 10.00, 458.00, 600.00),
(N'Brugal Añejo 700ml', N'74620708', 1.00, 10.00, 517.00, 800.00),
(N'Brugal Extra Viejo 700ml', N'74610044', 3.00, 10.00, 575.00, 950.00),
(N'Smirnoff Vodka 750ml', N'5410316518536', -1.00, 10.00, 772.00, 1100.00),
(N'Barceló Gran Platinium 350ml', N'7461323129053', 0.00, 10.00, 258.00, 450.00),
(N'Brugal XV 350ML', N'7460855235263', 2.00, 10.00, 355.00, 550.00),
(N'Brugal 1988 Doblemente Añejado 700ml', N'7460855230206', 1.00, 10.00, 2035.00, 2850.00),
(N'LAVADO SENCILLO- YIPETA', N'A000095', 238.00, 10.00, 0.00, 500.00),
(N'LAVADO SENCILLO- CARRO', N'A000096', 377.00, 10.00, 0.00, 400.00),
(N'Smirnoff Raspberry', N'082000789727', 3.00, 10.00, 125.00, 175.00),
(N'Vino Frontera After Midnidht', N'7804320628165', 1.00, 5.00, 491.00, 900.00),
(N'Four Loko Peq Sandia', N'A000018', 0.00, 10.00, 165.00, 300.00),
(N'Lavado De Camion Con Fulgon', N'A000027', 987.00, 10.00, 0.00, 1200.00),
(N'Enerup Cherry Peq', N'7468783291924', 96.00, 10.00, 23.00, 40.00),
(N'Enerup Grande', N'7468783291931', 8.00, 15.00, 31.00, 60.00),
(N'Bolon', N'A000032', 10.00, 15.00, 10.00, 10.00),
(N'Cervezas 1906 Peq', N'8412598004964', 10.00, 0.00, 150.00, 250.00),
(N'Monster Energy Taurina', N'070847029106', 0.00, 0.00, 115.00, 200.00),
(N'Marlboro Med. Caja', N'A000044', 0.00, 15.00, 268.00, 250.00),
(N'Coqui', N'A000045', 4.00, 5.00, 10.00, 15.00),
(N'Bob Chocolate', N'A000047', 59.00, 15.00, 11.00, 15.00),
(N'Ron Leon Jimenez 1903', N'A000048', 1.00, 2.00, 1973.00, 2700.00),
(N'Mani mix', N'A000049', 50.00, 5.00, 124.00, 40.00),
(N'Mani con sal', N'A000050', 22.00, 5.00, 81.00, 25.00),
(N'Media Funda De Hielo', N'A000052', 1908.00, 50.00, 35.00, 50.00),
(N'Johnnie Walker Red Label Ful', N'A000053', 0.00, 2.00, 1875.00, 2600.00),
(N'Malta Morena 8onz', N'A000054', 0.00, 5.00, 35.00, 50.00),
(N'1 Litro de Coca Cola Biliguer', N'A000055', 1.00, 2.00, 60.00, 125.00),
(N'Medio Litro Coca Coca', N'A000056', 0.00, 4.00, 50.00, 75.00),
(N'Brugal Doble Reserva', N'A000058', 7.00, 0.00, 820.00, 1300.00),
(N'Heineken Peq 0 Alcohol', N'A000062', 40.00, 3.00, 0.00, 160.00),
(N'Cubetazo De Heineken Peq', N'A000063', 67.00, 0.00, 0.00, 650.00),
(N'Carlo Rossi', N'A000066', 21.00, 0.00, 379.00, 600.00),
(N'Cerveza 5,0 Original Tricolor', N'A000067', 0.00, 0.00, 95.00, 200.00),
(N'Tequila Frio', N'A000068', 8.00, 5.00, 195.00, 300.00),
(N'Lavado De Buggy', N'A000069', 991.00, 0.00, 0.00, 1300.00),
(N'Servicio De Acetuna', N'A000070', 44.00, 0.00, 0.00, 175.00),
(N'Combo De Doble Reservas', N'A000071', 0.00, 0.00, 0.00, 1250.00),
(N'Combo De Extraviejo', N'A000072', 0.00, 0.00, 0.00, 750.00),
(N'Combo De Xv', N'A000073', 12.00, 0.00, 0.00, 950.00),
(N'Lavado De Camion Grande Internacional', N'A000074', 48.00, 0.00, 0.00, 1600.00),
(N'Agua Dasani', N'A000075', 73.00, 0.00, 18.00, 30.00),
(N'Kola Real Citrus', N'A000076', 10.00, 5.00, 0.00, 25.00),
(N'kola real frutos tropicales', N'A000077', 41.00, 20.00, 212.72, 25.00),
(N'Lavado Camion Mitsubishi Fuso Con Tanque', N'A000078', 49.00, 0.00, 0.00, 1416.00),
(N'Ron Isla De Oro Dorado', N'A000079', 18.00, 0.00, 0.00, 450.00),
(N'Cerveza Michelob Ultra', N'A000080', 33.00, 10.00, 108.33, 225.00),
(N'Lavado Completo De Camion Con Fulgun', N'A000081', 722.00, 10.00, 0.00, 1770.00),
(N'Lavado De Camion Con Fulgon Kola Real', N'A000083', 1000.00, 20.00, 0.00, 700.00),
(N'Chicharron Flamin Hot 27g', N'A000085', 0.00, 10.00, 0.00, 35.00),
(N'Cerveza Coors Light', N'A000087', 41.00, 10.00, 111.00, 180.00),
(N'Ponche Crema', N'A000089', 0.00, 0.00, 695.00, 900.00),
(N'Lavado Con Interior A Vapor De Camion Doble Cabina', N'A000091', 49.00, 0.00, 0.00, 2000.00),
(N'Lavado Con Interior A Vapor De Camion Doble Cabina', N'A000092', 49.00, 0.00, 0.00, 2550.00),
(N'Lavado Con Interior A Vapor De Camion Con Cabina', N'A000093', 100.00, 0.00, 0.00, 3500.00),
(N'Kola Real Mediano', N'A000094', 2.00, 0.00, 0.00, 35.00),
(N'Lavado De SUV XL', N'A000097', 100.00, 0.00, 0.00, 600.00),
(N'Encerado A Mano', N'A000098', 500.00, 0.00, 0.00, 1500.00),
(N'Interior Sin Desarme, Carro En Ledel', N'A000099', 500.00, 0.00, 0.00, 2500.00),
(N'Interior Sin Desarme Carro En Tela', N'A0000100', 500.00, 0.00, 0.00, 3000.00),
(N'Desarme Carro En Ledel', N'A0000101', 499.00, 0.00, 0.00, 4500.00),
(N'Desarme Carton En Tela', N'A0000102', 500.00, 0.00, 0.00, 5000.00),
(N'Interior Sin Desarme En Ledel De Yipeta', N'A0000103', 500.00, 0.00, 0.00, 3000.00),
(N'Interior Sin Derrame En Tela De Yipeta', N'A0000104', 500.00, 0.00, 0.00, 3300.00),
(N'Desarme Tela De Yipeta', N'A0000105', 499.00, 0.00, 0.00, 5500.00),
(N'Desarme Ledel De Yipeta', N'A0000106', 500.00, 0.00, 0.00, 5000.00),
(N'Tratamiento De Ozono', N'A0000107', 469.00, 0.00, 0.00, 700.00),
(N'Lavado De Motor A Vapor', N'A0000108', 500.00, 0.00, 0.00, 1600.00),
(N'Lavado Premium', N'A0000109', 494.00, 0.00, 0.00, 3000.00),
(N'Retiro De Mancha De Cristales, Carro', N'A0000110', 500.00, 0.00, 0.00, 600.00),
(N'Retiro De Mancha De Cristales, SUV', N'A0000111', 500.00, 0.00, 0.00, 700.00),
(N'One Jumbo', N'A0000112', 0.00, 0.00, 141.00, 250.00),
(N'Cranberry 946ml', N'A0000113', 48.00, 0.00, 0.00, 200.00),
(N'Vive 100 Peq', N'A0000114', 0.00, 0.00, 0.00, 50.00),
(N'Vela De Cumpleaños', N'A0000115', 20.00, 0.00, 0.00, 75.00),
(N'Caja De Fosforo', N'A0000116', 1.00, 0.00, 0.00, 5.00),
(N'Lavado De Minibus', N'A0000117', 494.00, 0.00, 0.00, 700.00),
(N'Platanito Grande', N'A0000118', 1.00, 0.00, 100.00, 150.00),
(N'Trojan', N'A0000120', 3.00, 0.00, 190.00, 250.00),
(N'Ron Sibone 1920', N'A0000121', 1.00, 0.00, 850.00, 1850.00),
(N'Vino Frontera Merlot', N'A0000122', 1.00, 0.00, 0.00, 900.00),
(N'Alka-Seltzer Azul', N'A0000123', 20.00, 10.00, 18.00, 40.00),
(N'Preservativo Te Quiero', N'A0000124', 45.00, 0.00, 0.00, 75.00),
(N'LAVADO DE CAMIONETA GRANDE', N'A0000125', 66.00, 0.00, 0.00, 600.00),
(N'Lavado Camion Pequeño', N'A0000126', 198.00, 0.00, 0.00, 600.00),
(N'Matine de One P', N'A0000127', 43.00, 0.00, 0.00, 100.00),
(N'Matine de Brahma peq', N'A0000128', 50.00, 0.00, 0.00, 100.00),
(N'Lavado Especial Carro', N'A0000129', 7.00, 0.00, 0.00, 300.00),
(N'Lavado Especial Yipeta', N'A0000130', 45.00, 0.00, 0.00, 350.00),
(N'TEQUILA PATRON', N'A0000131', 1.00, 0.00, 3810.00, 4750.00),
(N'TEQUILA DON JULIO', N'A0000132', 0.00, 0.00, 5280.00, 6200.00),
(N'Ginebra Bombay 0.7L', N'A0000133', 0.00, 0.00, 2015.00, 2800.00),
(N'Ginebra Tanqueray 0.7L', N'A0000134', 1.00, 0.00, 2095.00, 2950.00),
(N'Lavado Carro Peq', N'A0000135', 452.00, 0.00, 0.00, 300.00),
(N'Lavado Yipeta Peq', N'A0000136', 89.00, 0.00, 0.00, 400.00),
(N'Lavado Camion Grande Con Fulgon 16p', N'A0000137', 49.00, 0.00, 0.00, 1000.00),
(N'Servicio De Chicharron 1lbra', N'A0000139', 26.00, 0.00, 0.00, 500.00),
(N'Servicio De Chicharron Media Libra', N'A0000140', 48.00, 0.00, 0.00, 250.00),
(N'Servio De Chicharron', N'A0000141', 43.00, 0.00, 0.00, 300.00),
(N'Plato Del Dia', N'A0000142', 49.00, 0.00, 0.00, 200.00),
(N'Servicio Chuleta Y Frito', N'A0000143', 46.00, 0.00, 0.00, 300.00),
(N'Picadera Fria', N'A0000144', 25.00, 0.00, 0.00, 200.00),
(N'Alitas a La BBQ', N'A0000145', 23.00, 0.00, 0.00, 300.00),
(N'Kings Pride Kanel', N'A0000146', 17.00, 0.00, 135.00, 250.00),
(N'Cerveza Corona Sin Alcohol', N'A0000147', 6.00, 0.00, 0.00, 150.00),
(N'Lays Limon Grande', N'A0000148', 6.00, 0.00, 0.00, 150.00),
(N'Cerveza El Gallo', N'A0000149', 34.00, 0.00, 0.00, 200.00),
(N'Kings Pride 700ml', N'A0000151', 2.00, 0.00, 460.00, 850.00),
(N'Agua Con Gas Cool Heaven', N'A0000153', 50.00, 0.00, 32.00, 50.00),
(N'Sponggy', N'A0000154', 28.00, 0.00, 0.00, 15.00),
(N'Dulce', N'A0000155', 12.00, 0.00, 0.00, 15.00),
(N'Cerveza Blue Moon', N'A0000156', 9.00, 0.00, 140.00, 200.00),
(N'Cerveza El Sol', N'A0000157', 16.00, 0.00, 116.00, 160.00),
(N'Shot De Tequila Tiscaz', N'A0000158', 5.00, 0.00, 0.00, 250.00),
(N'Mojito De Limon', N'A0000159', 50.00, 0.00, 0.00, 350.00),
(N'Servicio De Picadera', N'A0000160', 25.00, 0.00, 0.00, 300.00),
(N'Servicio De Picadera', N'A0000161', 25.00, 0.00, 0.00, 400.00),
(N'Servicio De Picadera', N'A0000162', 23.00, 0.00, 0.00, 500.00),
(N'Cuba Libre', N'A0000163', 30.00, 0.00, 0.00, 150.00),
(N'Lay De Limon P', N'A0000164', 6.00, 0.00, 0.00, 20.00),
(N'Cubetazo De Miller', N'A0000165', 2.00, 0.00, 0.00, 650.00),
(N'Omeprazol', N'A0000166', 80.00, 0.00, 5.00, 25.00),
(N'Diclo K Forte', N'A0000167', 74.00, 0.00, 0.00, 40.00),
(N'Agua De Toronja', N'A0000168', 46.00, 0.00, 0.00, 40.00),
(N'Sanpellegrino', N'A0000169', 3.00, 0.00, 0.00, 250.00),
(N'Agua De Coco Goya', N'A0000170', 19.00, 0.00, 102.00, 200.00),
(N'Limon', N'A0000171', 50.00, 0.00, 0.00, 50.00),
(N'LAVADO DE CARRO R', N'A0000172', 6.00, 0.00, 0.00, 350.00),
(N'Camioneta Grande', N'A0000173', 99.00, 0.00, 0.00, 700.00),
(N'Trago De Extraviejo', N'j126463126s', 198.00, 100.00, 950.00, 200.00),
(N'Agua Mineral Con Gas', N'A0000174', 6.00, 0.00, 169.00, 250.00),
(N'Trago De Dewars', N'A0000175', 250.00, 210.00, 200.00, 280.00),
(N'Dorito Grande', N'A0000176', 3.00, 0.00, 0.00, 85.00),
(N'Agua San Pellegrino', N'A0000177', 3.00, 0.00, 0.00, 300.00),
(N'Agua San Benedetto', N'A0000178', 3.00, 0.00, 0.00, 350.00),
(N'Encendedor', N'A0000179', 25.00, 0.00, 0.00, 50.00),
(N'Trident 30.6g', N'A0000180', 29.00, 0.00, 0.00, 80.00),
(N'LAVADO SENCILLO DE CAMIONETA', N'A0000181', 50.00, 0.00, 0.00, 590.00),
(N'LAVADO CAMIONETA SENCILLO', N'A0000182', 47.00, 0.00, 0.00, 708.00),
(N'agua perrier grande.2', N'A0000183', 3.00, 2.00, 300.00, 500.00),
(N'Hojuelitas Grande De Queso', N'A0000184', 5.00, 0.00, 104.00, 125.00),
(N'Cheetos Grande', N'A0000185', 6.00, 0.00, 70.00, 100.00),
(N'Acqua Panna', N'A0000186', 46.00, 0.00, 75.00, 150.00),
(N'Agua De Coco Goya G', N'A0000187', 1.00, 0.00, 0.00, 300.00),
(N'Vodka Stolichnaya', N'A0000188', 1.00, 0.00, 0.00, 1600.00),
(N'Trago De Poliakov', N'A0000189', 49.00, 0.00, 0.00, 280.00),
(N'Trago De Siboney', N'A0000190', 40.00, 0.00, 0.00, 300.00),
(N'Trago De Sambuca', N'A0000191', 50.00, 0.00, 0.00, 300.00),
(N'Poliakov Botella', N'A0000192', 2.00, 0.00, 820.00, 1800.00),
(N'Siboney Blanco', N'A0000193', 1.00, 0.00, 1144.00, 1000.00),
(N'Tiscaz Blanco', N'A0000194', 1.00, 0.00, 1085.00, 1700.00),
(N'Villa Cardea Sambuca', N'A0000195', 4.00, 0.00, 894.00, 1800.00),
(N'Coors Beer', N'A0000196', 35.00, 0.00, 0.00, 180.00),
(N'Frutop Carton Peq', N'A0000197', 41.00, 0.00, 0.00, 25.00),
(N'Frutop Carton Gd', N'A0000198', 10.00, 0.00, 0.00, 100.00),
(N'Margarita Casica', N'A0000199', 50.00, 0.00, 0.00, 350.00);

UPDATE #Cat SET EsServicio = 1
WHERE Nombre LIKE N'%lavado%' OR Nombre LIKE N'%Lavado%' OR Nombre LIKE N'%LAVADO%'
   OR Nombre LIKE N'%picadera%' OR Nombre LIKE N'%Picadera%'
   OR Nombre LIKE N'%servicio%' OR Nombre LIKE N'%Servicio%' OR Nombre LIKE N'%Servio%'
   OR Nombre LIKE N'%Interior%' OR Nombre LIKE N'%Desarme%' OR Nombre LIKE N'%Encerado%'
   OR Nombre LIKE N'%Tratamiento%' OR Nombre LIKE N'%Combo%' OR Nombre LIKE N'%Cubetazo%'
   OR Nombre LIKE N'%Mojito%' OR Nombre LIKE N'%Cuba Libre%' OR Nombre LIKE N'%Margarita%'
   OR Nombre LIKE N'%Shot %' OR Nombre LIKE N'Shot %' OR Nombre LIKE N'%Trago%'
   OR Nombre LIKE N'%Plato Del Dia%' OR Nombre LIKE N'%Alitas%' OR Nombre LIKE N'%Matine%';

UPDATE c SET c.IdCategoria = CASE
    WHEN c.EsServicio = 1 OR c.Nombre LIKE N'%LAVADO%' OR c.Nombre LIKE N'%Lavado%'
         OR c.Nombre LIKE N'%Encerado%' OR c.Nombre LIKE N'%Desarme%' THEN @CatCarwash
    WHEN c.Nombre LIKE N'%Cerveza%' OR c.Nombre LIKE N'%Presidente%' OR c.Nombre LIKE N'%Heineken%'
         OR c.Nombre LIKE N'%Corona%' OR c.Nombre LIKE N'%Modelo%' OR c.Nombre LIKE N'%Bohemia%'
         OR c.Nombre LIKE N'%Brahma%' OR c.Nombre LIKE N'%Miller%' OR c.Nombre LIKE N'%Malta%'
         THEN @CatCervezas
    WHEN c.Nombre LIKE N'%Ron%' OR c.Nombre LIKE N'%Whisky%' OR c.Nombre LIKE N'%Vodka%'
         OR c.Nombre LIKE N'%Brugal%' OR c.Nombre LIKE N'%Barcel%' OR c.Nombre LIKE N'%Walker%'
         OR c.Nombre LIKE N'%Tequila%' OR c.Nombre LIKE N'%Ginebra%' THEN @CatLicores
    WHEN c.Nombre LIKE N'%Marlboro%' OR c.Nombre LIKE N'%Vuse%' OR c.Nombre LIKE N'%Cigarr%'
         OR c.Nombre LIKE N'%Newport%' OR c.Nombre LIKE N'%Pall Mall%' OR c.Nombre LIKE N'%VEEV%'
         THEN @CatCigarros
    WHEN c.Nombre LIKE N'%Ashley%' OR c.Nombre LIKE N'%Duracell%' OR c.Nombre LIKE N'%USB%'
         THEN @CatAccesorios
    WHEN c.Nombre LIKE N'%Jugo%' OR c.Nombre LIKE N'%Kola%' OR c.Nombre LIKE N'%Agua%'
         OR c.Nombre LIKE N'%Coca%' OR c.Nombre LIKE N'%Pepsi%' OR c.Nombre LIKE N'%Frutop%'
         OR c.Nombre LIKE N'%Red Bull%' OR c.Nombre LIKE N'%Gator%' THEN @CatBebidas
    ELSE @CatSnacks
END
FROM #Cat c;

;WITH DupBar AS (
    SELECT IdFila,
           ROW_NUMBER() OVER (
               PARTITION BY BarCode
               ORDER BY IdFila DESC
           ) rn
    FROM #Cat
    WHERE BarCode <> ''
)
DELETE c FROM #Cat c
INNER JOIN DupBar d ON d.IdFila = c.IdFila AND d.rn > 1;

UPDATE c SET c.IdProducto = p.IdProducto
FROM #Cat c
INNER JOIN dbo.Productos p
    ON p.IdEmpresa = @IdEmpresa AND c.BarCode <> '' AND p.CodigoBarra = c.BarCode;

UPDATE c SET c.IdProducto = p.IdProducto
FROM #Cat c
INNER JOIN dbo.Productos p
    ON p.IdEmpresa = @IdEmpresa
   AND LTRIM(RTRIM(LOWER(p.Nombre))) = LTRIM(RTRIM(LOWER(c.Nombre)))
WHERE c.IdProducto IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM #Cat x
      WHERE x.IdProducto = p.IdProducto
  );

BEGIN TRAN;

UPDATE p SET
    p.Nombre = CASE WHEN LEN(c.Nombre) >= 3 THEN c.Nombre ELSE p.Nombre END,
    p.Descripcion = CASE WHEN LEN(c.Nombre) >= 3 THEN c.Nombre ELSE p.Descripcion END,
    p.PrecioVenta = c.PrecioVenta,
    p.Precio1 = c.PrecioVenta,
    p.PrecioCompra = c.PrecioCompra,
    p.Stock = c.Cantidad,
    p.Cantidad = c.Cantidad,
    p.Disponibles = c.Cantidad,
    p.CodigoBarra = CASE WHEN c.BarCode <> '' THEN c.BarCode ELSE p.CodigoBarra END,
    p.IsActivo = 1,
    p.SeVende = 1,
    p.EsServicio = c.EsServicio,
    p.TipoOperacion = N'VENTA',
    p.IdAlmacen = ISNULL(p.IdAlmacen, @IdAlmacen)
FROM dbo.Productos p
INNER JOIN #Cat c ON c.IdProducto = p.IdProducto
WHERE p.IdEmpresa = @IdEmpresa;

INSERT INTO dbo.Productos (
    Nombre, Descripcion, Rentado, Disponibles, IdProveedor, Cantidad, Stock, PrecioVenta,
    IdUnidadMedida, IdCategoria, IdAlmacen, CodigoBarra, Descuento, Precio1, Precio2, Precio3,
    PorcientoDescuento, PorcientoGanancia, PrecioCompra, Itbis, Nota, SeCompra, SeAlquila, SeVende,
    ControlarStock, IsActivo, Ganancia, FechaInseccion, TipoProducto, PrecioDolar,
    Imagen1, Imagen2, Imagen3, IdCocina, EsProductoBelleza, IdEmpresa, EsServicio, IdArea,
    DuracionServicio, DisponibleEnCitas, TipoOperacion, TipoComportamiento
)
SELECT
    c.Nombre, c.Nombre, 0, c.Cantidad, @IdProveedor, c.Cantidad, c.Cantidad, c.PrecioVenta,
    @IdUnidad, ISNULL(c.IdCategoria, @CatSnacks), @IdAlmacen, NULLIF(c.BarCode, ''),
    0, c.PrecioVenta, 0, 0, 0, 0, c.PrecioCompra,
    CASE WHEN c.EsServicio = 1 THEN 0 ELSE 1 END, NULL,
    CASE WHEN c.EsServicio = 1 THEN 0 ELSE 1 END, 0, 1,
    CASE WHEN c.EsServicio = 1 THEN 0 ELSE 1 END, 1, 0, CAST(GETDATE() AS date),
    CASE WHEN c.EsServicio = 1 THEN N'Servicio' ELSE N'Producto' END, 0,
    NULL, NULL, NULL, NULL, 0, @IdEmpresa, c.EsServicio, @IdArea,
    0, 0, N'VENTA', CASE WHEN c.EsServicio = 1 THEN N'Servicio' ELSE N'Inventario' END
FROM #Cat c
WHERE c.IdProducto IS NULL;

UPDATE c SET c.IdProducto = p.IdProducto
FROM #Cat c
INNER JOIN dbo.Productos p
    ON p.IdEmpresa = @IdEmpresa
   AND (
        (c.BarCode <> '' AND p.CodigoBarra = c.BarCode)
        OR LTRIM(RTRIM(LOWER(p.Nombre))) = LTRIM(RTRIM(LOWER(c.Nombre)))
   )
WHERE c.IdProducto IS NULL;

DELETE e
FROM dbo.AlmacenExistencias e
INNER JOIN dbo.Productos p ON p.IdProducto = e.IdProducto
WHERE p.IdEmpresa = @IdEmpresa
  AND NOT EXISTS (SELECT 1 FROM #Cat c WHERE c.IdProducto = p.IdProducto);

DELETE v
FROM dbo.Variaciones v
INNER JOIN dbo.Productos p ON p.IdProducto = v.IdProducto
WHERE p.IdEmpresa = @IdEmpresa
  AND NOT EXISTS (SELECT 1 FROM #Cat c WHERE c.IdProducto = p.IdProducto);

DELETE c
FROM dbo.EmpleadoServicioComisions c
INNER JOIN dbo.Productos p ON p.IdProducto = c.IdProducto
WHERE p.IdEmpresa = @IdEmpresa
  AND NOT EXISTS (SELECT 1 FROM #Cat x WHERE x.IdProducto = p.IdProducto);

DELETE p
FROM dbo.Productos p
WHERE p.IdEmpresa = @IdEmpresa
  AND NOT EXISTS (SELECT 1 FROM #Cat c WHERE c.IdProducto = p.IdProducto);

UPDATE e SET
    e.Existencia = c.Cantidad,
    e.StockMinimo = c.StockMinimo,
    e.CostoPromedio = CASE WHEN c.PrecioCompra > 0 THEN c.PrecioCompra ELSE e.CostoPromedio END,
    e.IdEmpresa = @IdEmpresa,
    e.FechaUltimoMovimiento = GETDATE()
FROM dbo.AlmacenExistencias e
INNER JOIN #Cat c ON c.IdProducto = e.IdProducto
WHERE e.IdAlmacen = @IdAlmacen AND c.IdProducto IS NOT NULL;

INSERT INTO dbo.AlmacenExistencias (
    IdAlmacen, IdProducto, Existencia, CostoPromedio, StockMinimo, IdEmpresa, FechaUltimoMovimiento
)
SELECT @IdAlmacen, d.IdProducto, d.Cantidad, d.PrecioCompra, d.StockMinimo, @IdEmpresa, GETDATE()
FROM (
    SELECT IdProducto,
           MAX(Cantidad) Cantidad,
           MAX(StockMinimo) StockMinimo,
           MAX(PrecioCompra) PrecioCompra
    FROM #Cat
    WHERE IdProducto IS NOT NULL
    GROUP BY IdProducto
) d
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.AlmacenExistencias x
    WHERE x.IdAlmacen = @IdAlmacen AND x.IdProducto = d.IdProducto
);

COMMIT;

SELECT 'CatalogoPDF' t, COUNT(*) c FROM #Cat
UNION ALL SELECT 'ConProducto', COUNT(*) FROM #Cat WHERE IdProducto IS NOT NULL
UNION ALL SELECT 'SinMatch', COUNT(*) FROM #Cat WHERE IdProducto IS NULL
UNION ALL SELECT 'Activos', COUNT(*) FROM dbo.Productos WHERE IdEmpresa = @IdEmpresa AND IsActivo = 1
UNION ALL SELECT 'Inactivos', COUNT(*) FROM dbo.Productos WHERE IdEmpresa = @IdEmpresa AND IsActivo = 0;

SELECT Nombre FROM #Cat WHERE IdProducto IS NULL;
SELECT TOP 20 p.Nombre, p.CodigoBarra, p.Precio1, p.Stock, e.Existencia
FROM dbo.Productos p
LEFT JOIN dbo.AlmacenExistencias e ON e.IdProducto = p.IdProducto AND e.IdAlmacen = @IdAlmacen
WHERE p.IdEmpresa = @IdEmpresa AND p.IsActivo = 1
ORDER BY p.Nombre;
GO
