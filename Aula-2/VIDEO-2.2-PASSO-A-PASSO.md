# 🎬 Vídeo 2.2 - Instrumentação .NET e PromQL

**Aula**: 2 - Prometheus + .NET no Kubernetes  
**Vídeo**: 2.2  
**Temas**: Instrumentação; /metrics; PromQL  

---

## 💻 Parte 1: Instrumentação .NET

### Passo 1: Examinar Código

```bash
cd ~/monitoramentoEacesso/Aula-2/dotnet-app

# Ver dependência
cat WeatherApi.csproj | grep prometheus-net

# Ver configuração
cat Program.cs
```

**Linhas importantes:**
```csharp
app.UseMetricServer();    // Expõe /metrics
app.UseHttpMetrics();     // Métricas HTTP automáticas
```

### Passo 2: 4 Tipos de Métricas

**1. Counter** (sempre aumenta):
```csharp
RequestCounter.WithLabels("/WeatherForecast").Inc();
```

**2. Histogram** (distribuição):
```csharp
using (RequestDuration.NewTimer()) { /* código */ }
```

**3. Gauge** (sobe/desce):
```csharp
ActiveRequests.Inc(); // incrementa
ActiveRequests.Dec(); // decrementa
```

**4. Summary** (estatísticas):
```csharp
TemperatureSummary.Observe(temperature);
```

---

## 📊 Parte 2: Análise /metrics

### Passo 3: Gerar Dados

```bash
# Port forward Weather API
kubectl port-forward svc/weather-api 5001:80 -n monitoring &

# Gerar requisições
for i in {1..50}; do
  curl -s http://localhost:5001/WeatherForecast > /dev/null
done
```

### Passo 4: Ver Métricas

```bash
# Ver todas as métricas
curl http://localhost:5001/metrics

# Filtrar métricas customizadas
curl http://localhost:5001/metrics | grep ^weather_
```

**Formato Prometheus:**
```
# HELP weather_requests_total Total de requisições
# TYPE weather_requests_total counter
weather_requests_total{endpoint="/WeatherForecast"} 50
```

---

## 🔍 Parte 3: PromQL

### Passo 5: Queries Básicas

```bash
# Acessar Prometheus
kubectl port-forward svc/prometheus 9090:9090 -n monitoring &
open http://localhost:9090
```

**Query 1 - Métrica bruta:**
```promql
weather_requests_total
```

**Query 2 - Taxa por segundo:**
```promql
rate(weather_requests_total[5m])
```

**Query 3 - Taxa por minuto:**
```promql
rate(weather_requests_total[5m]) * 60
```

### Passo 6: Agregações

**Query 4 - Soma total:**
```promql
sum(rate(weather_requests_total[5m]))
```

**Query 5 - Por endpoint:**
```promql
sum by (endpoint) (rate(weather_requests_total[5m]))
```

### Passo 7: Latência

**Query 6 - Latência média (ms):**
```promql
rate(weather_request_duration_seconds_sum[5m]) / 
rate(weather_request_duration_seconds_count[5m]) * 1000
```

**Query 7 - Percentil 95:**
```promql
histogram_quantile(0.95, 
  rate(weather_request_duration_seconds_bucket[5m])
) * 1000
```

---

**Próximo**: VIDEO-2.3-PASSO-A-PASSO.md
