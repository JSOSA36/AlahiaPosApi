-- Actualiza Políticas del Servicio → v1.6
-- Agrega 4.1 Horario de atención, 4.2 Límites del plan, 4.3 Actualización por crecimiento
USE AlahiaPos_Dev;
GO

IF EXISTS (SELECT 1 FROM dbo.PoliticasVersion WHERE NumeroVersion = N'1.6')
BEGIN
    PRINT 'v1.6 ya existe — se omite inserción.';
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
        N'1.6',
        N'Políticas del Servicio - Alahia ERP',
        N'Las presentes políticas aplican a todos los productos y servicios ofrecidos por MacroBits SRL, incluyendo Alahia ERP, implementaciones, certificaciones de Facturación Electrónica (e-CF), desarrollos personalizados, consultorías, capacitaciones y cualquier otro servicio contratado, salvo que exista un contrato específico que establezca condiciones diferentes.

Principios de Operación de Alahia ERP

Alahia ERP opera bajo un modelo de automatización, trazabilidad y autoservicio. El uso de la plataforma implica la aceptación de que los procesos relacionados con suscripciones, pagos, soporte, notificaciones y administración del servicio serán gestionados mediante la propia plataforma, garantizando transparencia, igualdad de condiciones y registro de todas las operaciones.

0. Suspensión del Servicio y Cargos (Importante)

Para garantizar la continuidad y calidad del servicio, la plataforma Alahia ERP podrá suspender automáticamente el acceso cuando ocurra alguna de las siguientes situaciones:

• Incumplimiento en los pagos.
• Uso indebido del sistema.
• Incumplimiento de estas políticas.
• Actividades que comprometan la seguridad del servicio.
• Uso de módulos o funcionalidades no contratadas.
• Cualquier otra situación que represente un riesgo para MacroBits.

Durante el período de suspensión el cliente no podrá acceder al sistema hasta regularizar la situación a través de la plataforma.

La suspensión del servicio no es la única consecuencia del incumplimiento. La reactivación o reconexión del servicio tras una suspensión generará un cargo adicional por reconexión, el cual deberá ser pagado y validado a través de la plataforma antes de restablecer el acceso.

Los siguientes servicios podrán generar cargos adicionales:

• Reactivación o reconexión del servicio tras suspensión.
• Implementaciones.
• Configuraciones especiales.
• Migración de información.
• Visitas presenciales.
• Capacitaciones personalizadas.
• Adaptaciones, personalizaciones y desarrollos a medida.
• Integraciones.
• Desarrollos.
• Corrección de errores ocasionados por el cliente.
• Reprocesos.
• Recuperación de información.
• Servicios fuera del alcance contratado.

MacroBits notificará previamente cualquier costo adicional.

La suspensión por falta de pago no elimina las obligaciones económicas pendientes, ni exonera el pago del cargo por reactivación o reconexión, ni genera derecho a descuentos, devoluciones o ampliaciones del período contratado.

1. Plan Contratado

El cliente tendrá acceso únicamente a las funcionalidades y servicios incluidos en el plan, propuesta comercial o cotización aceptada.

2. Funcionalidades Contratadas

Alahia ERP es una plataforma modular.

Cada cliente tendrá acceso únicamente a los módulos contratados.

Las funcionalidades no contratadas serán consideradas servicios adicionales y podrán requerir una nueva cotización.

La existencia de un módulo dentro del ERP no implica que forme parte del servicio contratado.

3. Naturaleza del Sistema y Personalizaciones

Alahia ERP es un sistema diseñado y ofrecido para uso general (modelo SaaS estándar). No constituye un desarrollo personalizado ni una solución construida a la medida de un cliente particular.

El cliente recibe la plataforma con las funcionalidades estándar del plan contratado, las cuales son comunes para todos los usuarios de la misma configuración comercial.

Cualquier adaptación, personalización, modificación de flujos, pantallas, reportes, reglas de negocio, integraciones específicas u otro desarrollo a medida solicitado por el cliente deberá ser cotizado y pagado de forma independiente como servicio profesional adicional.

La contratación del servicio no otorga derecho a exigir cambios en el producto estándar, ni obliga a MacroBits a modificar la plataforma para satisfacer requerimientos particulares de un cliente.

Las mejoras generales de la plataforma son decididas y liberadas por MacroBits a su criterio, y podrán estar disponibles según el plan contratado, sin que ello implique personalización individual.

4. Soporte Técnico

Todo el soporte técnico será gestionado exclusivamente mediante el Centro de Soporte integrado en Alahia ERP.

Cada solicitud generará automáticamente un ticket con su historial, seguimiento y trazabilidad.

Los tickets serán incorporados automáticamente a la cola de trabajo y atendidos conforme a su nivel de prioridad y orden de recepción.

MacroBits podrá reclasificar la prioridad de un ticket cuando sea necesario para garantizar una correcta administración del soporte.

El sistema notificará automáticamente al cliente cuando existan respuestas, cambios de estado o solicitudes de información relacionadas con sus tickets.

No se ofrecerá soporte técnico por WhatsApp, llamadas telefónicas, mensajes personales, redes sociales o cualquier otro medio distinto al Centro de Soporte de la plataforma, salvo autorización expresa de MacroBits para casos excepcionales.

El soporte incluido cubre únicamente incidencias relacionadas con el funcionamiento de los módulos contratados.

Las solicitudes fuera del alcance contratado podrán ser evaluadas y cotizadas como servicios adicionales.

4.1 Horario de Atención del Soporte Técnico

El soporte técnico incluido en los planes de Alahia ERP será prestado exclusivamente dentro del horario laboral de MacroBits SRL, de lunes a viernes, de 9:00 a.m. a 5:00 p.m., excepto días feriados o no laborables.

Las solicitudes registradas fuera de este horario serán recibidas automáticamente por el Centro de Soporte y atendidas durante el siguiente horario laboral, respetando su prioridad y orden de recepción.

La atención fuera del horario laboral únicamente podrá realizarse cuando exista un acuerdo previo o un servicio especial contratado por el cliente.

4.2 Límites del Plan Contratado

Cada plan comercial de Alahia ERP posee límites específicos previamente definidos por MacroBits SRL y comunicados al cliente durante el proceso de cotización o contratación.

Estos límites podrán incluir, entre otros:

• Cantidad máxima de usuarios.
• Límite de ingresos mensuales.
• Cantidad de comprobantes electrónicos (e-CF).
• Módulos incluidos.
• Empresas o sucursales permitidas.
• Cualquier otra característica definida para el plan contratado.

El cliente reconoce y acepta que estos límites forman parte integral de las condiciones del servicio contratado.

4.3 Actualización del Plan por Crecimiento

Cuando la empresa alcance alguno de los límites establecidos para el plan contratado, Alahia ERP podrá requerir la actualización a un plan superior que permita continuar utilizando la plataforma de acuerdo con el crecimiento y necesidades reales de la empresa.

El sistema notificará previamente al cliente cuando se aproxime o alcance alguno de los límites establecidos.

Una vez alcanzado dicho límite, el cliente deberá migrar al plan correspondiente para continuar utilizando las funcionalidades del servicio.

La diferencia de precio entre el plan actual y el nuevo plan será aplicada automáticamente en la siguiente factura de renovación o en la facturación correspondiente, conforme a las políticas comerciales vigentes de MacroBits SRL.

La actualización del plan tiene como finalidad garantizar la disponibilidad de los recursos necesarios para la operación del cliente y mantener la calidad del servicio ofrecido.

5. Soporte Presencial

El soporte incluido en todos los planes es exclusivamente remoto.

Si el cliente requiere asistencia presencial en sus instalaciones, dicho servicio deberá ser previamente coordinado y tendrá un costo adicional.

Los gastos de traslado, viáticos y cualquier otro costo asociado a visitas presenciales serán asumidos por el cliente.

6. Soporte Operativo

El soporte incluido en los planes de Alahia ERP está orientado exclusivamente a resolver incidencias relacionadas con el funcionamiento de la plataforma.

El soporte no incluye la operación diaria del negocio del cliente, ni la ejecución de procesos administrativos, contables, fiscales o comerciales.

MacroBits no realiza tareas como registrar facturas, compras, gastos, productos, clientes, inventarios, conciliaciones bancarias, nóminas, declaraciones fiscales, configuraciones contables u otras actividades propias de la operación del cliente.

El soporte no contempla capacitación continua sobre el uso del sistema ni acompañamiento permanente para la ejecución de procesos internos de la empresa.

Las solicitudes relacionadas con asesorías, capacitaciones adicionales, parametrizaciones, migraciones de información, carga masiva de datos, configuraciones especiales o acompañamiento operativo podrán ser evaluadas y cotizadas como servicios profesionales independientes.

El cliente es responsable de contar con personal capacitado para operar el sistema y administrar la información registrada en la plataforma.

Cuando una solicitud no corresponda a una incidencia técnica sino a un servicio operativo o de consultoría, MacroBits podrá reclasificar el ticket y presentar una cotización antes de iniciar cualquier trabajo.

7. Implementación

Los servicios de implementación, configuración inicial, migración de datos y puesta en marcha podrán cotizarse de forma independiente.

8. Capacitación

La plataforma incluye videos y material de apoyo.

Las capacitaciones personalizadas serán cotizadas por separado.

9. Pagos, Renovaciones y Automatización del Servicio

Alahia ERP administra automáticamente el ciclo de facturación, renovaciones, recordatorios, suspensiones y reactivaciones de cada empresa.

Las facturas de renovación serán generadas automáticamente conforme al ciclo de facturación establecido por la plataforma.

El sistema enviará automáticamente las notificaciones correspondientes antes del vencimiento del servicio.

Todos los pagos deberán registrarse exclusivamente mediante la plataforma de Alahia ERP.

El funcionamiento del servicio es completamente automatizado. La plataforma es el único mecanismo autorizado para validar pagos, administrar renovaciones, aplicar suspensiones y realizar reactivaciones del servicio.

No se realizarán activaciones, renovaciones, suspensiones, excepciones ni validaciones manuales de pago mediante solicitudes realizadas por WhatsApp, llamadas telefónicas, correos electrónicos o cualquier otro medio externo a la plataforma.

Si al finalizar el período establecido no existe un pago validado por el sistema, el servicio podrá ser suspendido automáticamente hasta que la situación sea regularizada.

La regularización del servicio suspendido implica, además del pago de las obligaciones pendientes, el pago del cargo por reactivación o reconexión establecido por la plataforma. El acceso no será restablecido hasta que ambos conceptos hayan sido registrados y validados en Alahia ERP.

El cliente es responsable de registrar correctamente sus pagos utilizando los mecanismos habilitados por la plataforma.

MacroBits no será responsable por retrasos, suspensiones o interrupciones del servicio ocasionados por pagos no registrados o reportados fuera de la plataforma.

10. Conectividad (Importante)

Alahia ERP requiere una conexión a Internet estable y de buena calidad.

Es responsabilidad del cliente disponer de:

• Internet estable.
• Equipos adecuados.
• Energía eléctrica.
• Infraestructura tecnológica funcional.

Las fallas ocasionadas por Internet, energía, equipos del cliente o terceros no se consideran fallas del servicio.

11. Seguridad y Confidencialidad

MacroBits protegerá la información del cliente utilizando medidas razonables de seguridad.

La información no será compartida salvo obligación legal o autorización expresa.

12. Política de Pagos y No Reembolsos (Importante)

Todos los pagos realizados a MacroBits SRL son definitivos y no son reembolsables.

Aplica para:

• Suscripciones.
• Cargos por reactivación o reconexión.
• Adaptaciones, personalizaciones y desarrollos a medida.
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

13. Facturación Electrónica

Es responsabilidad del cliente verificar la información antes de emitir un comprobante electrónico.

Las correcciones ocasionadas por errores del cliente o por no seguir las instrucciones suministradas por MacroBits podrán generar cargos adicionales.

MacroBits no será responsable por sanciones derivadas de información incorrecta suministrada por el cliente.

14. Cambios en el Servicio

MacroBits podrá realizar mejoras, actualizaciones y modificaciones a la plataforma.

Dichas mejoras forman parte de la evolución del producto estándar y no constituyen una personalización individual para un cliente específico.

15. Responsabilidad del Cliente

El cliente será responsable de:

• La información registrada.
• Sus usuarios.
• Sus credenciales.
• El cumplimiento de sus obligaciones fiscales.
• El uso adecuado del sistema.

16. Limitación de Responsabilidad

MacroBits no será responsable por daños ocasionados por:

• Fallas de Internet.
• Energía eléctrica.
• Equipos del cliente.
• Servicios de terceros.
• Casos fortuitos.

17. Cancelación

La cancelación del servicio no genera devolución de dinero.

18. Nuevas Funcionalidades

Las nuevas funcionalidades podrán requerir nuevos planes, licencias o costos adicionales.

La existencia de una nueva funcionalidad no implica que esté incluida dentro del plan contratado.

Las nuevas funcionalidades se incorporan al producto de uso general y no equivalen a un desarrollo personalizado para un cliente particular, salvo que se contrate y pague expresamente un servicio de personalización.',
        N'Publicada',
        GETDATE(),
        GETDATE(),
        N'v1.6: Horario soporte 9-5, límites de plan y actualización por crecimiento'
    );

    PRINT 'Políticas v1.6 publicada en AlahiaPos_Dev';
END
GO
