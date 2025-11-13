# 🎬 Vídeo 3.2 - Datasources e Dashboards

**Aula**: 3 - Grafana no Kubernetes  
**Vídeo**: 3.2  
**Temas**: Conectar datasources; Deploy serviços; Gerar carga; Dashboards  

---

## 🔌 Parte 1: Conectar Datasources

### Passo 1: Verificar/Deploy Prometheus

```bash
# Verificar se Prometheus está rodando
kubectl get pods -n monitoring -l app=prometheus

# Se não estiver, deploy rápido:
cd ~/monitoramentoEacesso/Aula-2/kubernetes
kubectl apply -f prometheus-configmap.yaml -n monitoring
kubectl apply -f prometheus-deployment.yaml -n monitoring
kubectl wait --for=condition=ready pod -l app=prometheus -n monitoring --timeout=300s
```

### Passo 2: Verificar/Deploy Zabbix

```bash
# Verificar se Zabbix está rodando
kubectl get pods -n monitoring -l app=zabbix-server

# Se não estiver, deploy rápido:
cd ~/monitoramentoEacesso/Aula-1/kubernetes
kubectl apply -f postgres-secret.yaml -n monitoring
kubectl apply -f postgres-deployment.yaml -n monitoring
kubectl apply -f zabbix-server-deployment.yaml -n monitoring
kubectl apply -f zabbix-web-deployment.yaml -n monitoring
kubectl wait --for=condition=ready pod -l app=zabbix-server -n monitoring --timeout=300s
```

### Passo 3: Configurar Datasources no Grafana

**No Grafana:**
1. **Connections → Data sources**
2. Verificar datasources provisionados:
   - **Prometheus**: `http://prometheus:9090`
   - **Zabbix**: Configurar se necessário

### Passo 4: Testar Prometheus

1. Clicar em **Prometheus**
2. **Save & test**
3. Mensagem: "Successfully queried the Prometheus API"

---

## 🚀 Parte 2: Gerar Carga para Testes

### Passo 5: Gerar Dados

```bash
# Gerar carga na Weather API
kubectl port-forward svc/weather-api 5001:80 -n monitoring &

# Fazer múltiplas requisições
for i in {1..100}; do
  curl -s http://localhost:5001/WeatherForecast > /dev/null
  echo "Request $i completed"
done

# Parar port-forward
pkill -f "kubectl port-forward.*weather-api"
```

### Passo 6: Verificar Métricas

**No Prometheus:**
```bash
# Acessar Prometheus
kubectl port-forward svc/prometheus 9090:9090 -n monitoring &
open http://localhost:9090

# Consultas para testar:
# http_requests_total
# weather_api_requests_total
```

---

## 🎨 Parte 3: Criar Dashboard

### Passo 7: Novo Dashboard

**No Grafana:**
1. **Dashboards → New → New Dashboard**
2. **Add visualization**

### Passo 8: Painel 1 - Taxa de Requisições

**Configuração:**
```
Datasource: Prometheus
Query: sum(rate(weather_requests_total[5m])) * 60
Title: Taxa de Requisições (req/min)
Visualization: Time series
Unit: req/min
```

### Passo 9: Painel 2 - Latência P95

**Add → Visualization**
```
Datasource: Prometheus
Query: histogram_quantile(0.95, rate(weather_request_duration_seconds_bucket[5m])) * 1000
Title: Latência P95 (ms)
Unit: milliseconds
```

### Passo 10: Painel 3 - Requisições Ativas (Gauge)

**Add → Visualization**
```
Datasource: Prometheus
Query: weather_active_requests
Title: Requisições Ativas
Visualization: Gauge
Min: 0, Max: 50
```

### Passo 11: Organizar Layout

**Arrastar e redimensionar:**
```
┌─────────────────┬─────────────────┐
│ Taxa Req        │ Latência P95    │
├─────────────────┴─────────────────┤
│ Requisições Ativas (gauge)        │
└───────────────────────────────────┘
```

### Passo 12: Salvar Dashboard

1. **Save** (ícone disquete)
2. Nome: `Weather API Monitoring`
3. **Save**

---

## 🔧 Parte 4: Recursos Avançados (0 min - Opcional)

### Passo 13: Adicionar Variáveis (Opcional)

**Dashboard settings → Variables → Add variable**
```
Name: endpoint
Type: Query
Data source: Prometheus
Query: label_values(weather_requests_total, endpoint)
```

### Passo 14: Configurar Refresh (Opcional)

**Dashboard settings:**
- Auto refresh: `10s`
- Time range: `Last 1 hour`

---

**Próximo**: VIDEO-3.3-PASSO-A-PASSO.md
