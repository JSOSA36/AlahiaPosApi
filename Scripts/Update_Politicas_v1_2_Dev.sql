-- Actualiza Políticas del Servicio → v1.2 (AlahiaPos_Dev)
-- Agrega §5 Soporte Operativo y renumeración de secciones posteriores
USE AlahiaPos_Dev;
GO

IF EXISTS (SELECT 1 FROM dbo.PoliticasVersion WHERE NumeroVersion = N'1.2')
BEGIN
    PRINT 'v1.2 ya existe — se omite inserción.';
END
ELSE
BEGIN
    UPDATE dbo.PoliticasVersion
    SET Estado = N'Archivada'
    WHERE Estado = N'Publicada';

    INSERT INTO dbo.PoliticasVersion
        (NumeroVersion, Titulo, Contenido, Estado, FechaCreacion, FechaPublicacion, Notas)
    VALUES
    (
        N'1.2',
        N'Políticas del Servicio - Alahia ERP',
        N'Las presentes políticas aplican a todos los productos y servicios ofrecidos por MacroBits SRL, incluyendo Alahia ERP, implementaciones, certificaciones de Facturación Electrónica (e-CF), desarrollos personalizados, consultorías, capacitaciones y cualquier otro servicio contratado, salvo que exista un contrato específico que establezca condiciones diferentes.

0. Suspensión del Servicio y Cargos (Importante)

Para garantizar la continuidad y calidad del servicio, la plataforma Alahia ERP podrá suspender automáticamente el acceso cuando ocurra alguna de las siguientes situaciones:

• Incumplimiento en los pagos.
• Uso indebido del sistema.
• Incumplimiento de estas políticas.
• Actividades que comprometan la seguridad del servicio.
• Uso de módulos o funcionalidades no contratadas.
• Cualquier otra situación que represente un riesgo para MacroBits.

Durante el período de suspensión el cliente no podrá acceder al sistema hasta regularizar la situación a través de la plataforma.

Los siguientes servicios podrán generar cargos adicionales:

• Implementaciones.
• Configuraciones especiales.
• Migración de información.
• Visitas presenciales.
• Capacitaciones personalizadas.
• Integraciones.
• Desarrollos.
• Corrección de errores ocasionados por el cliente.
• Reprocesos.
• Recuperación de información.
• Servicios fuera del alcance contratado.

MacroBits notificará previamente cualquier costo adicional.

La suspensión por falta de pago no elimina las obligaciones económicas pendientes ni genera derecho a descuentos, devoluciones o ampliaciones del período contratado.

1. Plan Contratado

El cliente tendrá acceso únicamente a las funcionalidades y servicios incluidos en el plan, propuesta comercial o cotización aceptada.

2. Funcionalidades Contratadas

Alahia ERP es una plataforma modular.

Cada cliente tendrá acceso únicamente a los módulos contratados.

Las funcionalidades no contratadas serán consideradas servicios adicionales y podrán requerir una nueva cotización.

La existencia de un módulo dentro del ERP no implica que forme parte del servicio contratado.

3. Soporte Técnico

Todo el soporte técnico será gestionado exclusivamente mediante el Centro de Soporte integrado en Alahia ERP.

Cada solicitud generará automáticamente un ticket con su historial, seguimiento y trazabilidad.

Los tickets serán incorporados automáticamente a la cola de trabajo y atendidos conforme a su nivel de prioridad y orden de recepción.

MacroBits podrá reclasificar la prioridad de un ticket cuando sea necesario para garantizar una correcta administración del soporte.

El sistema notificará automáticamente al cliente cuando existan respuestas, cambios de estado o solicitudes de información relacionadas con sus tickets.

No se ofrecerá soporte técnico por WhatsApp, llamadas telefónicas, mensajes personales, redes sociales o cualquier otro medio distinto al Centro de Soporte de la plataforma, salvo autorización expresa de MacroBits para casos excepcionales.

El soporte incluido cubre únicamente incidencias relacionadas con el funcionamiento de los módulos contratados.

Las solicitudes fuera del alcance contratado podrán ser evaluadas y cotizadas como servicios adicionales.

4. Soporte Presencial

El soporte incluido en todos los planes es exclusivamente remoto.

Si el cliente requiere asistencia presencial en sus instalaciones, dicho servicio deberá ser previamente coordinado y tendrá un costo adicional.

Los gastos de traslado, viáticos y cualquier otro costo asociado a visitas presenciales serán asumidos por el cliente.

5. Soporte Operativo

El soporte incluido en los planes de Alahia ERP está orientado exclusivamente a resolver incidencias relacionadas con el funcionamiento de la plataforma.

El soporte no incluye la operación diaria del negocio del cliente, ni la ejecución de procesos administrativos, contables, fiscales o comerciales.

MacroBits no realiza tareas como registrar facturas, compras, gastos, productos, clientes, inventarios, conciliaciones bancarias, nóminas, declaraciones fiscales, configuraciones contables u otras actividades propias de la operación del cliente.

El soporte no contempla capacitación continua sobre el uso del sistema ni acompañamiento permanente para la ejecución de procesos internos de la empresa.

Las solicitudes relacionadas con asesorías, capacitaciones adicionales, parametrizaciones, migraciones de información, carga masiva de datos, configuraciones especiales o acompañamiento operativo podrán ser evaluadas y cotizadas como servicios profesionales independientes.

El cliente es responsable de contar con personal capacitado para operar el sistema y administrar la información registrada en la plataforma.

Cuando una solicitud no corresponda a una incidencia técnica sino a un servicio operativo o de consultoría, MacroBits podrá reclasificar el ticket y presentar una cotización antes de iniciar cualquier trabajo.

6. Implementación

Los servicios de implementación, configuración inicial, migración de datos y puesta en marcha podrán cotizarse de forma independiente.

7. Capacitación

La plataforma incluye videos y material de apoyo.

Las capacitaciones personalizadas serán cotizadas por separado.

8. Pagos, Renovaciones y Automatización del Servicio

Alahia ERP administra automáticamente el ciclo de facturación, renovaciones, recordatorios, suspensiones y reactivaciones de cada empresa.

Las facturas de renovación serán generadas automáticamente conforme al ciclo de facturación establecido por la plataforma.

El sistema enviará automáticamente las notificaciones correspondientes antes del vencimiento del servicio.

Todos los pagos deberán registrarse exclusivamente mediante la plataforma de Alahia ERP.

El funcionamiento del servicio es completamente automatizado. La plataforma es el único mecanismo autorizado para validar pagos, administrar renovaciones, aplicar suspensiones y realizar reactivaciones del servicio.

No se realizarán activaciones, renovaciones, suspensiones, excepciones ni validaciones manuales de pago mediante solicitudes realizadas por WhatsApp, llamadas telefónicas, correos electrónicos o cualquier otro medio externo a la plataforma.

Si al finalizar el período establecido no existe un pago validado por el sistema, el servicio podrá ser suspendido automáticamente hasta que la situación sea regularizada.

El cliente es responsable de registrar correctamente sus pagos utilizando los mecanismos habilitados por la plataforma.

MacroBits no será responsable por retrasos, suspensiones o interrupciones del servicio ocasionados por pagos no registrados o reportados fuera de la plataforma.

9. Conectividad (Importante)

Alahia ERP requiere una conexión a Internet estable y de buena calidad.

Es responsabilidad del cliente disponer de:

• Internet estable.
• Equipos adecuados.
• Energía eléctrica.
• Infraestructura tecnológica funcional.

Las fallas ocasionadas por Internet, energía, equipos del cliente o terceros no se consideran fallas del servicio.

10. Seguridad y Confidencialidad

MacroBits protegerá la información del cliente utilizando medidas razonables de seguridad.

La información no será compartida salvo obligación legal o autorización expresa.

11. Política de Pagos y No Reembolsos (Importante)

Todos los pagos realizados a MacroBits SRL son definitivos y no son reembolsables.

Aplica para:

• Suscripciones.
• Implementaciones.
• Certificaciones e-CF.
• Desarrollos.
• Integraciones.
• Consultorías.
• Capacitaciones.
• Equipos.
• Licencias.
• Soporte especializado.
• Cualquier otro servicio.

Una vez iniciado o reservado un servicio no procederán devoluciones totales ni parciales.

12. Facturación Electrónica

Es responsabilidad del cliente verificar la información antes de emitir un comprobante electrónico.

Las correcciones ocasionadas por errores del cliente o por no seguir las instrucciones suministradas por MacroBits podrán generar cargos adicionales.

MacroBits no será responsable por sanciones derivadas de información incorrecta suministrada por el cliente.

13. Cambios en el Servicio

MacroBits podrá realizar mejoras, actualizaciones y modificaciones a la plataforma.

14. Responsabilidad del Cliente

El cliente será responsable de:

• La información registrada.
• Sus usuarios.
• Sus credenciales.
• El cumplimiento de sus obligaciones fiscales.
• El uso adecuado del sistema.

15. Limitación de Responsabilidad

MacroBits no será responsable por daños ocasionados por:

• Fallas de Internet.
• Energía eléctrica.
• Equipos del cliente.
• Servicios de terceros.
• Casos fortuitos.

16. Cancelación

La cancelación del servicio no genera devolución de dinero.

17. Nuevas Funcionalidades

Las nuevas funcionalidades podrán requerir nuevos planes, licencias o costos adicionales.

La existencia de una nueva funcionalidad no implica que esté incluida dentro del plan contratado.',
        N'Publicada',
        GETDATE(),
        GETDATE(),
        N'v1.2: agrega §5 Soporte Operativo'
    );

    PRINT 'Políticas v1.2 publicada en AlahiaPos_Dev';
END
GO
