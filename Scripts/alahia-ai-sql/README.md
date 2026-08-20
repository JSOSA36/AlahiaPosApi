# Alahia AI — SQL solo lectura (Fase 1)

Solo **AlahiaPos_Dev**. No aplicar a Prod sin autorización.

## Qué hace

1. Login/usuario `alahia_ai_ro` (solo SELECT).
2. Schema `ai` con vistas `ai.v_*` filtradas por `SESSION_CONTEXT('IdEmpresa')`.
3. Deny sobre `dbo` para el usuario AI.
4. Backend `AiSqlExecutor` + flag `AlahiaAi:Sql:Enabled`.

## Aplicar

```bash
sqlcmd ... -d AlahiaPos_Dev -i Scripts/alahia-ai-sql/01_AlahiaAi_ReadOnly_RLS_Dev.sql
```

## Activar en API

En `appsettings.json` (o Development):

```json
"AlahiaAi": {
  "Sql": {
    "Enabled": true,
    "ConnectionString": "Server=...;Database=AlahiaPos_Dev;User Id=alahia_ai_ro;Password=...;Encrypt=False"
  }
}
```

Con `Enabled: true` y un LLM configurado, el chat intenta generar/ejecutar SELECT sobre `ai.v_*` antes del gateway de intents.

## Prueba aislamiento

```sql
-- como alahia_ai_ro
SELECT COUNT(*) FROM ai.v_FacturaHeaders; -- 0 sin contexto
EXEC sp_set_session_context @key=N'IdEmpresa', @value=59, @read_only=1;
SELECT COUNT(*) FROM ai.v_FacturaHeaders; -- solo emp 59
SELECT COUNT(*) FROM dbo.FacturaHeaders;  -- DENY
```
