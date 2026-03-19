# Guia de Monitoramento de Logs - Supabase

Com a nova configuração, os logs da aplicação são salvos diretamente na tabela `Logs` do seu banco de dados no Supabase.

## 1. Visualização Rápida (Table Editor)

1. Acesse o [Supabase Dashboard](https://supabase.com/dashboard).
2. Vá em **Table Editor** (ícone de tabela no menu lateral).
3. Selecione a tabela `Logs`.
4. Use o botão **Filter** para buscar por níveis específicos:
   - `level` = `Error`
   - `level` = `Warning`
5. Use o botão **Sort** para ordenar por `timestamp` (Descendente) para ver os logs mais recentes primeiro.

## 2. Análise Avançada (SQL Editor)

Você pode executar consultas SQL para entender melhor o comportamento do sistema. Vá em **SQL Editor** e tente:

### Ver erros das últimas 24 horas:
```sql
SELECT timestamp, message, exception 
FROM "Logs" 
WHERE level = 'Error' 
  AND timestamp > now() - interval '24 hours'
ORDER BY timestamp DESC;
```

### Contagem de logs por nível:
```sql
SELECT level, count(*) 
FROM "Logs" 
GROUP BY level;
```

## 3. Retenção de Logs

> [!WARNING]
> Como os logs são salvos no banco de dados, a tabela pode crescer rapidamente. 
> Recomenda-se executar periodicamente (ou via Cron Job) uma limpeza:

```sql
DELETE FROM "Logs" WHERE timestamp < now() - interval '30 days';
```

---
*Configurado com Serilog (Console, File, PostgreSQL).*
