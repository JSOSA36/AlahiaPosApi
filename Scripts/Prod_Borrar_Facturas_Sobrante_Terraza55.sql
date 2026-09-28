/*
  Terraza 55: borrar facturas con productos fuera de ListadoProductos.pdf
  y esos productos. Backup: Music\\Terraza-Facturas-Backup-2026-09-03.pdf
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

IF NOT EXISTS (
    SELECT 1 FROM dbo.Empresas
    WHERE IdEmpresa = 55 AND NombreComercial LIKE N'Terraza%'
)
BEGIN
    RAISERROR(N'IdEmpresa 55 no es Terraza 27.', 16, 1);
    RETURN;
END;

IF OBJECT_ID('tempdb..#Cat') IS NOT NULL DROP TABLE #Cat;
CREATE TABLE #Cat (Nombre NVARCHAR(200) NOT NULL, BarCode VARCHAR(30) NOT NULL);
INSERT INTO #Cat (Nombre, BarCode) VALUES
(N'Casillero Del Diablo Red Blend', N'7804320746555'),
(N'Brownie', N'A00007'),
(N'Little Trees New Car Scent', N'076171101891'),
(N'Little Trees Coconut', N'076171103178'),
(N'Pistacho', N'A000013'),
(N'Lavado De Camion Sin Fulgon', N'A000031'),
(N'Galleta Emperador 72g', N'7500478013616'),
(N'Galletas Qiin De Chocolate', N'7468339191869'),
(N'Semillas mixta', N'A000010'),
(N'Semillas de cajuil', N'A000011'),
(N'Almendras', N'A000012'),
(N'Master Of Mixes Sour Apple Mixer', N'070491060241'),
(N'Jugo 100% Naranja Con Azucar Mediano', N'790330021089'),
(N'Jugo 100% Naranja Sin Azucar Mediano', N'790330021140'),
(N'Jugo 100% Naranja con Azucar Grande', N'790330021072'),
(N'Jugo 100% Naranja Sin Azucar Grande', N'790330021126'),
(N'Frutop 1 Litro', N'7468783291221'),
(N'Kola Real 1 Litro', N'7461063094574'),
(N'Presidente Black Peq', N'7463172803764'),
(N'Trident 3 Chicles', N'7702133863264'),
(N'Fireball 200ml', N'088004144739'),
(N'Barra Bon o Bon Snack Chocolate Relleno', N'7502230950153'),
(N'Bon o Bon Bola Chocolate Relleno', N'75027971'),
(N'Paleta', N'A000019'),
(N'Cool Heaven Soda Carbonatada', N'7461063095366'),
(N'Jugo Rica Naranja 200ml', N'7460111102568'),
(N'Jugo Rica Orange Mediano 500ml', N'7460111102513'),
(N'Vive100 G', N'7702354251093'),
(N'Clorets 2.8g', N'75020460'),
(N'Da pa To NatuChips', N'7460496804774'),
(N'Club Social Original', N'7622201717544'),
(N'Agua Planeta Azul', N'701891100014'),
(N'CIGARRO DON TABACO', N'A000034'),
(N'Winasorb', N'7451079003561'),
(N'Sal Andrews', N'74410330'),
(N'Alka-Seltzer', N'011418462311'),
(N'Troyano Black Lbel', N'4584156584417'),
(N'Red Rock Merengue 400ml', N'7463172803528'),
(N'Red Rock Naranja 400ml', N'7463172803559'),
(N'Red Rock Uva 400ml', N'7463172803580'),
(N'Ron Brugal Triple Reserva', N'7460855238066'),
(N'Florentinas Dulce De Leche 83g', N'7501000601745'),
(N'Galleta Emperador 36g', N'7500478012404'),
(N'Funda De Hielo', N'A00006'),
(N'Vaso De Hielo', N'A00003'),
(N'Agua Tonica Canada Dry 400ml', N'7465383000321'),
(N'Jugo Del Valle Coctel De Frutas 200ml', N'025000134043'),
(N'Lays Clasicas 80g', N'A00008'),
(N'Chicharron Limon 27g', N'7464113824121'),
(N'Cool Heaven Saborizada', N'7461063097148'),
(N'Menta Halls', N'A00001'),
(N'Picaderas Sencilla', N''),
(N'Picaderas Completa', N''),
(N'Lavado Camiones Ramon', N'A000086'),
(N'Lavado Camioneta Sencillo', N'A000061'),
(N'Lavado Motor', N'A000060'),
(N'Lavado Vehiculos Ramon y Maria', N''),
(N'Bohemia Peq 355ml', N'74618903'),
(N'Trident Splash', N'7622201685386'),
(N'Trident 8.5g', N'7622201776664'),
(N'Miller Grande 650ml', N'034100005696'),
(N'La Benedicta 330ml', N'7460736904042'),
(N'Stella Artois 330ml', N'7501064199141'),
(N'Smirnoff 330ml', N'082000723844'),
(N'Miller Peq 355ml', N'A000035'),
(N'Modelo Peq Especial Negra', N'75031589'),
(N'Bohemia Mediana 650ml', N'74628711'),
(N'One Peq 355ml', N'74601325'),
(N'Modelo Peq Especial Rubia', N'7463172802996'),
(N'Heineken Grande 650ml', N'8712000030582'),
(N'Modelo Grande Especial Rubia', N'A00005'),
(N'CoronaExtra 330ml', N'7503034941200'),
(N'Heineken Peq 330ml', N'072890004994'),
(N'Brahma Light 355ml', N'7468973200194'),
(N'One Mediana 650ml', N'74601127'),
(N'Brahma Light 650ml', N'7468973200200'),
(N'Presidente Peq 355ml', N'74621774'),
(N'Presidente Mediana 650ml', N'74601561'),
(N'Granberry Classic 450ml', N'031200008091'),
(N'Aloe Original', N'8809125063011'),
(N'Motts Peq 296ml', N'01489438'),
(N'Sprite Limon', N'049000057690'),
(N'Red Rock Merengue 450ml', N'7468973202969'),
(N'Frutop Peq 300ml', N'7468783290002'),
(N'Malta Morena 355ml', N'74650309'),
(N'Extracto De Malta Lowenbrau', N'7463172802613'),
(N'Coca Cola Sin Azucar 400ml', N'049000071993'),
(N'Pepsi 400ml', N'7463172803290'),
(N'Frutop 450ml', N'7461063095595'),
(N'Red Rock Naranja 450ml', N'7468973202983'),
(N'Coca Cola Original 400ml', N'049000057638'),
(N'7 Up Limon 450ml', N'7468973203003'),
(N'V8 Splash Strawberry Kiwi', N'7462275402843'),
(N'Motts Grande 946ml', N'01480000032'),
(N'Sprite Limon 1.25l', N'049000031577'),
(N'V8 Splash Fruit Medley', N'7462275402829'),
(N'Coco Rico 450ml', N'7467003480339'),
(N'Red Rock Uva 450ml', N'7468973202976'),
(N'Red Rock Manzana Verde 450ml', N'7468973202952'),
(N'Red Rock Frambuesa 450ml', N'7468973202945'),
(N'Kola Real 400ml', N'7461063094819'),
(N'Kola Real Cola Negra 10oz', N'7468783290545'),
(N'Agua Perrier Grande', N'A00004'),
(N'Agua Enriquillo Tonica Amarilla', N'7468973202341'),
(N'Gatore', N'7460548000154'),
(N'Agua Enriquillo Carbonatada Azul', N'7468973202358'),
(N'Agua Cool Heaven', N'7461063094345'),
(N'Generade 592ml', N'7468783291122'),
(N'Generade 250ml', N'7461063097346'),
(N'Ashley USB Fast Charger 2.1A', N'001162'),
(N'Four Loko Mango', N'849806003859'),
(N'Ciclon Energy Drink Original 250ml', N'830207000004'),
(N'911 Energy Drink Original 500ml', N'7467003480551'),
(N'Red Bull Energy Drink 355ml', N'9002490212148'),
(N'Four Loko Pupple', N'849806002746'),
(N'Clamato 221ml', N'01484035'),
(N'Ciclon Energy Drink Original 500ml', N'830207000707'),
(N'Four Loko Blue', N'849806002319'),
(N'Four Loko Green', N'849806001855'),
(N'Red Bull Energy Drink 250ml', N'9002490204006'),
(N'Four Loko Gold', N'849806001756'),
(N'Four Loko Sandia', N'849806001206'),
(N'Marlboro', N'74601172'),
(N'Newport Freezing Point', N'7421000501350'),
(N'Pall Mall', N'7421000594642'),
(N'Vuse Go 500 Puffs 34ml', N'7702303923477'),
(N'Vuse Go 1500 Puffs 34ml', N'7702303621267'),
(N'Galletas Ritz Sandwich Con Queso', N'7622201390013'),
(N'Oreo Sandwich Original Tubo 108g', N'7622201693190'),
(N'Chokis Rellena', N'7501000604685'),
(N'Galletas Ritz Saladas Tubo 525g', N'7622201390037'),
(N'Oreo Sandwich original 36g', N'7590011251100'),
(N'Crackets Saladas 43g', N'7500478005543'),
(N'VEEV Now Blueberry 1800 Puffs 5ml', N'7406135040264'),
(N'Mini Chokis', N'7501000610228'),
(N'VEEV Now Stawberry 1800 Puffs 5ml', N'7406135040240'),
(N'Florentinas Sabor a Fresa', N'7501000601738'),
(N'Oreo Sandwich Original 54g', N'7702133009037'),
(N'Mamut 30g', N'7501000636921'),
(N'Crackets Mini Sandwich', N'7500478027118'),
(N'Pringles Original 37g', N'038000846731'),
(N'Yummi Nuts Maní con Limón', N'750894671205'),
(N'Taqueritos Chile Toreado 34g', N'750894612550'),
(N'Doritos 32g', N'7460496804002'),
(N'Platanitos 18g', N'7460496800530'),
(N'Ruffles 29g', N'721282402787'),
(N'Hojuelitas 23g', N'7460496803623'),
(N'Cheetos 23g', N'7460496804293'),
(N'NatuChips Natural 32g', N'7460496800523'),
(N'Six Eigth Nine Red Wine', N'051497322618'),
(N'Lays Queso Blanco 80g', N'7460496804767'),
(N'Frontera Cabernet Sauvignon', N'7804320559001'),
(N'Carlos Rossi Red', N'085000001882'),
(N'Lays 35g', N'7460496803944'),
(N'120 Reserva Especial Santa Rita', N'7804330311101'),
(N'Jhonnie Walker Gold Label Reserve 750ml', N'5000267107776'),
(N'De Todito', N'7464113826507'),
(N'Something Special', N'80432402795'),
(N'Chivas Regal 12', N'08043240039'),
(N'Rose la Sichera', N'7460736980121'),
(N'Buchaman´s de Luxe 12', N'50196388'),
(N'120 Reserva Especial Santa Rita Merlot', N'7804330341108'),
(N'De Todito Mofongo 75g', N'7460496804781'),
(N'Buchaman´s Two Souls', N'5000196006539'),
(N'JP. Chenet Ice Edition Rosada', N'3500610093708'),
(N'Jhonnie Walker Red Label', N'5000267014203'),
(N'Ballantine´s Finest', N'5010106111536'),
(N'JP. Chenet Ice Edition Blanca', N'3500610085338'),
(N'Carlos Rossi Fruity Red', N'085000022030'),
(N'Jhonnie Walker Black Label Aged 12 Years', N'5000267190150'),
(N'Jhonnie Walker Double Black', N'5000267116419'),
(N'Old Par 12 750ml', N'5000281003160'),
(N'Passaport Blended Scotch', N'5000299210048'),
(N'Fireball Cinnamon Whisky 350ml', N'088004144722'),
(N'Dewar´s White Label 750ml', N'5000277001101'),
(N'Fireball Cinnamon Whisky 750ml', N'088004146689'),
(N'Black & White Blended Scotch Whisky 700ml', N'50196135'),
(N'Dewar´s Blended Scotch Whisky Aged 12 Years', N'5000277002542'),
(N'Wilianm Lawson´s Blended Scotch Whisky 700ml', N'5010752000307'),
(N'Fireball Cinnamon Whisky 50ml', N'08800414470'),
(N'King´s Label Black Whisky 700ml', N'7460522300829'),
(N'Brugal Leyenda Edicion 5 Aniversario 700ml', N'7460855233269'),
(N'Brugal Leyenda 700ml', N'7460855208694'),
(N'Absolut Vodka 750ml', N'7312040017010'),
(N'Barceló Gran Dark Gran Añejo 700ml', N'7461323129695'),
(N'Brugal Añejo 350ml', N'7461323129169'),
(N'Brugal XV 700ML', N'7460855235270'),
(N'Barceló Blanco Añejado 350ml', N'7461323129619'),
(N'Barceló Imperial 700ml', N'7461323129459'),
(N'Brugal Extra Viejo 350ml', N'7460855234983'),
(N'Barceló Gran Platinium 700ml', N'7461323129084'),
(N'Stoli Vodka 750ml', N'4750021000157'),
(N'Brugal Doble Reserva 700ml', N'7460855233498'),
(N'Barceló Gran Añejo 700ml', N'7461323129350'),
(N'Barceló Blanco Añejado 700ml', N'7461323129480'),
(N'Brugal Añejo 700ml', N'74620708'),
(N'Brugal Extra Viejo 700ml', N'74610044'),
(N'Smirnoff Vodka 750ml', N'5410316518536'),
(N'Barceló Gran Platinium 350ml', N'7461323129053'),
(N'Brugal XV 350ML', N'7460855235263'),
(N'Brugal 1988 Doblemente Añejado 700ml', N'7460855230206'),
(N'LAVADO SENCILLO- YIPETA', N'A000095'),
(N'LAVADO SENCILLO- CARRO', N'A000096'),
(N'Smirnoff Raspberry', N'082000789727'),
(N'Vino Frontera After Midnidht', N'7804320628165'),
(N'Four Loko Peq Sandia', N'A000018'),
(N'Lavado De Camion Con Fulgon', N'A000027'),
(N'Enerup Cherry Peq', N'7468783291924'),
(N'Enerup Grande', N'7468783291931'),
(N'Bolon', N'A000032'),
(N'Cervezas 1906 Peq', N'8412598004964'),
(N'Monster Energy Taurina', N'070847029106'),
(N'Marlboro Med. Caja', N'A000044'),
(N'Coqui', N'A000045'),
(N'Bob Chocolate', N'A000047'),
(N'Ron Leon Jimenez 1903', N'A000048'),
(N'Mani mix', N'A000049'),
(N'Mani con sal', N'A000050'),
(N'Media Funda De Hielo', N'A000052'),
(N'Johnnie Walker Red Label Ful', N'A000053'),
(N'Malta Morena 8onz', N'A000054'),
(N'1 Litro de Coca Cola Biliguer', N'A000055'),
(N'Medio Litro Coca Coca', N'A000056'),
(N'Brugal Doble Reserva', N'A000058'),
(N'Heineken Peq 0 Alcohol', N'A000062'),
(N'Cubetazo De Heineken Peq', N'A000063'),
(N'Carlo Rossi', N'A000066'),
(N'Cerveza 5,0 Original Tricolor', N'A000067'),
(N'Tequila Frio', N'A000068'),
(N'Lavado De Buggy', N'A000069'),
(N'Servicio De Acetuna', N'A000070'),
(N'Combo De Doble Reservas', N'A000071'),
(N'Combo De Extraviejo', N'A000072'),
(N'Combo De Xv', N'A000073'),
(N'Lavado De Camion Grande Internacional', N'A000074'),
(N'Agua Dasani', N'A000075'),
(N'Kola Real Citrus', N'A000076'),
(N'kola real frutos tropicales', N'A000077'),
(N'Lavado Camion Mitsubishi Fuso Con Tanque', N'A000078'),
(N'Ron Isla De Oro Dorado', N'A000079'),
(N'Cerveza Michelob Ultra', N'A000080'),
(N'Lavado Completo De Camion Con Fulgun', N'A000081'),
(N'Lavado De Camion Con Fulgon Kola Real', N'A000083'),
(N'Chicharron Flamin Hot 27g', N'A000085'),
(N'Cerveza Coors Light', N'A000087'),
(N'Ponche Crema', N'A000089'),
(N'Lavado Con Interior A Vapor De Camion Doble Cabina', N'A000091'),
(N'Lavado Con Interior A Vapor De Camion Doble Cabina', N'A000092'),
(N'Lavado Con Interior A Vapor De Camion Con Cabina', N'A000093'),
(N'Kola Real Mediano', N'A000094'),
(N'Lavado De SUV XL', N'A000097'),
(N'Encerado A Mano', N'A000098'),
(N'Interior Sin Desarme, Carro En Ledel', N'A000099'),
(N'Interior Sin Desarme Carro En Tela', N'A0000100'),
(N'Desarme Carro En Ledel', N'A0000101'),
(N'Desarme Carton En Tela', N'A0000102'),
(N'Interior Sin Desarme En Ledel De Yipeta', N'A0000103'),
(N'Interior Sin Derrame En Tela De Yipeta', N'A0000104'),
(N'Desarme Tela De Yipeta', N'A0000105'),
(N'Desarme Ledel De Yipeta', N'A0000106'),
(N'Tratamiento De Ozono', N'A0000107'),
(N'Lavado De Motor A Vapor', N'A0000108'),
(N'Lavado Premium', N'A0000109'),
(N'Retiro De Mancha De Cristales, Carro', N'A0000110'),
(N'Retiro De Mancha De Cristales, SUV', N'A0000111'),
(N'One Jumbo', N'A0000112'),
(N'Cranberry 946ml', N'A0000113'),
(N'Vive 100 Peq', N'A0000114'),
(N'Vela De Cumpleaños', N'A0000115'),
(N'Caja De Fosforo', N'A0000116'),
(N'Lavado De Minibus', N'A0000117'),
(N'Platanito Grande', N'A0000118'),
(N'Trojan', N'A0000120'),
(N'Ron Sibone 1920', N'A0000121'),
(N'Vino Frontera Merlot', N'A0000122'),
(N'Alka-Seltzer Azul', N'A0000123'),
(N'Preservativo Te Quiero', N'A0000124'),
(N'LAVADO DE CAMIONETA GRANDE', N'A0000125'),
(N'Lavado Camion Pequeño', N'A0000126'),
(N'Matine de One P', N'A0000127'),
(N'Matine de Brahma peq', N'A0000128'),
(N'Lavado Especial Carro', N'A0000129'),
(N'Lavado Especial Yipeta', N'A0000130'),
(N'TEQUILA PATRON', N'A0000131'),
(N'TEQUILA DON JULIO', N'A0000132'),
(N'Ginebra Bombay 0.7L', N'A0000133'),
(N'Ginebra Tanqueray 0.7L', N'A0000134'),
(N'Lavado Carro Peq', N'A0000135'),
(N'Lavado Yipeta Peq', N'A0000136'),
(N'Lavado Camion Grande Con Fulgon 16p', N'A0000137'),
(N'Servicio De Chicharron 1lbra', N'A0000139'),
(N'Servicio De Chicharron Media Libra', N'A0000140'),
(N'Servio De Chicharron', N'A0000141'),
(N'Plato Del Dia', N'A0000142'),
(N'Servicio Chuleta Y Frito', N'A0000143'),
(N'Picadera Fria', N'A0000144'),
(N'Alitas a La BBQ', N'A0000145'),
(N'Kings Pride Kanel', N'A0000146'),
(N'Cerveza Corona Sin Alcohol', N'A0000147'),
(N'Lays Limon Grande', N'A0000148'),
(N'Cerveza El Gallo', N'A0000149'),
(N'Kings Pride 700ml', N'A0000151'),
(N'Agua Con Gas Cool Heaven', N'A0000153'),
(N'Sponggy', N'A0000154'),
(N'Dulce', N'A0000155'),
(N'Cerveza Blue Moon', N'A0000156'),
(N'Cerveza El Sol', N'A0000157'),
(N'Shot De Tequila Tiscaz', N'A0000158'),
(N'Mojito De Limon', N'A0000159'),
(N'Servicio De Picadera', N'A0000160'),
(N'Servicio De Picadera', N'A0000161'),
(N'Servicio De Picadera', N'A0000162'),
(N'Cuba Libre', N'A0000163'),
(N'Lay De Limon P', N'A0000164'),
(N'Cubetazo De Miller', N'A0000165'),
(N'Omeprazol', N'A0000166'),
(N'Diclo K Forte', N'A0000167'),
(N'Agua De Toronja', N'A0000168'),
(N'Sanpellegrino', N'A0000169'),
(N'Agua De Coco Goya', N'A0000170'),
(N'Limon', N'A0000171'),
(N'LAVADO DE CARRO R', N'A0000172'),
(N'Camioneta Grande', N'A0000173'),
(N'Trago De Extraviejo', N'j126463126s'),
(N'Agua Mineral Con Gas', N'A0000174'),
(N'Trago De Dewars', N'A0000175'),
(N'Dorito Grande', N'A0000176'),
(N'Agua San Pellegrino', N'A0000177'),
(N'Agua San Benedetto', N'A0000178'),
(N'Encendedor', N'A0000179'),
(N'Trident 30.6g', N'A0000180'),
(N'LAVADO SENCILLO DE CAMIONETA', N'A0000181'),
(N'LAVADO CAMIONETA SENCILLO', N'A0000182'),
(N'agua perrier grande.2', N'A0000183'),
(N'Hojuelitas Grande De Queso', N'A0000184'),
(N'Cheetos Grande', N'A0000185'),
(N'Acqua Panna', N'A0000186'),
(N'Agua De Coco Goya G', N'A0000187'),
(N'Vodka Stolichnaya', N'A0000188'),
(N'Trago De Poliakov', N'A0000189'),
(N'Trago De Siboney', N'A0000190'),
(N'Trago De Sambuca', N'A0000191'),
(N'Poliakov Botella', N'A0000192'),
(N'Siboney Blanco', N'A0000193'),
(N'Tiscaz Blanco', N'A0000194'),
(N'Villa Cardea Sambuca', N'A0000195'),
(N'Coors Beer', N'A0000196'),
(N'Frutop Carton Peq', N'A0000197'),
(N'Frutop Carton Gd', N'A0000198'),
(N'Margarita Casica', N'A0000199');

IF OBJECT_ID('tempdb..#Extra') IS NOT NULL DROP TABLE #Extra;
SELECT p.IdProducto, p.Nombre, ISNULL(p.CodigoBarra,'') CodigoBarra
INTO #Extra
FROM dbo.Productos p
WHERE p.IdEmpresa = 55
  AND NOT EXISTS (
      SELECT 1 FROM #Cat c
      WHERE (c.BarCode <> '' AND p.CodigoBarra = c.BarCode)
         OR LOWER(LTRIM(RTRIM(p.Nombre))) = LOWER(LTRIM(RTRIM(c.Nombre)))
  );

IF OBJECT_ID('tempdb..#Fac') IS NOT NULL DROP TABLE #Fac;
SELECT DISTINCT h.IdFacturaHeader
INTO #Fac
FROM dbo.FacturaHeaders h
INNER JOIN dbo.FacturaDetalles d ON d.IdFacturaHeader = h.IdFacturaHeader
INNER JOIN #Extra x ON x.IdProducto = d.IdProducto
WHERE h.IdEmpresa = 55;

SELECT 'AntesFacturas' t, COUNT(*) c FROM #Fac
UNION ALL SELECT 'AntesExtras', COUNT(*) FROM #Extra
UNION ALL SELECT 'AntesDetalles', COUNT(*) FROM dbo.FacturaDetalles d INNER JOIN #Fac f ON f.IdFacturaHeader = d.IdFacturaHeader;

BEGIN TRAN;

UPDATE p SET p.IdMovimientoFinanciero = NULL
FROM dbo.PagosFacturasClientes p
INNER JOIN #Fac f ON f.IdFacturaHeader = p.IdFacturaHeader;

UPDATE g SET g.IdMovimientoFinanciero = NULL
FROM dbo.Ingresos g
INNER JOIN #Fac f ON f.IdFacturaHeader = g.IdFacturaHeader;

DELETE m
FROM dbo.MovimientoFinanciero m
INNER JOIN #Fac f ON f.IdFacturaHeader = m.ReferenciaId
WHERE m.IdEmpresa = 55
  AND m.ReferenciaTipo IN (N'FACTURA', N'Factura', N'VENTA', N'Venta');

DELETE p
FROM dbo.PagosFacturasClientes p
INNER JOIN #Fac f ON f.IdFacturaHeader = p.IdFacturaHeader;

DELETE g
FROM dbo.Ingresos g
INNER JOIN #Fac f ON f.IdFacturaHeader = g.IdFacturaHeader;

DELETE d
FROM dbo.MovimientosInventarioDetalle d
INNER JOIN dbo.MovimientosInventario m ON m.Id = d.IdMovimientoInventario
INNER JOIN #Fac f ON m.Referencia = N'Factura #' + CONVERT(varchar(12), f.IdFacturaHeader)
WHERE m.IdEmpresa = 55;

DELETE m
FROM dbo.MovimientosInventario m
INNER JOIN #Fac f ON m.Referencia = N'Factura #' + CONVERT(varchar(12), f.IdFacturaHeader)
WHERE m.IdEmpresa = 55;

DELETE d
FROM dbo.FacturaDetalles d
INNER JOIN #Fac f ON f.IdFacturaHeader = d.IdFacturaHeader;

DELETE h
FROM dbo.FacturaHeaders h
INNER JOIN #Fac f ON f.IdFacturaHeader = h.IdFacturaHeader
WHERE h.IdEmpresa = 55;

DELETE d
FROM dbo.MovimientosInventarioDetalle d
INNER JOIN #Extra x ON x.IdProducto = d.IdProducto;

DELETE m
FROM dbo.MovimientosInventario m
WHERE m.IdEmpresa = 55
  AND NOT EXISTS (
      SELECT 1 FROM dbo.MovimientosInventarioDetalle d
      WHERE d.IdMovimientoInventario = m.Id
  );

IF OBJECT_ID(N'dbo.PaqueteDetalles', N'U') IS NOT NULL
    DELETE d FROM dbo.PaqueteDetalles d
    WHERE d.IdProducto IN (SELECT IdProducto FROM #Extra)
       OR d.IdProductoPadre IN (SELECT IdProducto FROM #Extra);

IF OBJECT_ID(N'dbo.RecetasDetalles', N'U') IS NOT NULL
    DELETE d FROM dbo.RecetasDetalles d INNER JOIN #Extra x ON x.IdProducto = d.IdProducto;

IF OBJECT_ID(N'dbo.DescuentoDetalle', N'U') IS NOT NULL
    DELETE d FROM dbo.DescuentoDetalle d INNER JOIN #Extra x ON x.IdProducto = d.IdProducto;

IF OBJECT_ID(N'dbo.Citas', N'U') IS NOT NULL
    UPDATE c SET c.IdProducto = NULL
    FROM dbo.Citas c INNER JOIN #Extra x ON x.IdProducto = c.IdProducto;

DELETE e FROM dbo.AlmacenExistencias e INNER JOIN #Extra x ON x.IdProducto = e.IdProducto;
DELETE v FROM dbo.Variaciones v INNER JOIN #Extra x ON x.IdProducto = v.IdProducto;
DELETE c FROM dbo.EmpleadoServicioComisions c INNER JOIN #Extra x ON x.IdProducto = c.IdProducto;

DELETE p
FROM dbo.Productos p
INNER JOIN #Extra x ON x.IdProducto = p.IdProducto
WHERE p.IdEmpresa = 55;

COMMIT TRAN;

SELECT 'FacturasRestantesSobrante' t, COUNT(*) c
FROM dbo.FacturaHeaders h
INNER JOIN dbo.FacturaDetalles d ON d.IdFacturaHeader = h.IdFacturaHeader
INNER JOIN dbo.Productos p ON p.IdProducto = d.IdProducto
WHERE h.IdEmpresa = 55
  AND NOT EXISTS (
      SELECT 1 FROM #Cat c
      WHERE (c.BarCode <> '' AND p.CodigoBarra = c.BarCode)
         OR LOWER(LTRIM(RTRIM(p.Nombre))) = LOWER(LTRIM(RTRIM(c.Nombre)))
  )
UNION ALL SELECT 'ExtrasRestantes', COUNT(*) FROM dbo.Productos p
WHERE p.IdEmpresa = 55
  AND NOT EXISTS (
      SELECT 1 FROM #Cat c
      WHERE (c.BarCode <> '' AND p.CodigoBarra = c.BarCode)
         OR LOWER(LTRIM(RTRIM(p.Nombre))) = LOWER(LTRIM(RTRIM(c.Nombre)))
  )
UNION ALL SELECT 'ProductosEmpresa', COUNT(*) FROM dbo.Productos WHERE IdEmpresa = 55;
GO
