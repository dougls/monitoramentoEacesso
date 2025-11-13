# Vídeo 3.3 - Stack Completa de Observabilidade

**Aula**: 3 - Grafana no Kubernetes  
**Vídeo**: 3.3  
**Temas**: kube-prometheus-stack; Loki; Tempo; Observabilidade completa  

**Stack Completa de Observabilidade:**

```mermaid
graph TB
    subgraph "Grafana - Visualização Unificada"
        GRAF[Grafana Dashboard]
    end
    
    subgraph "kube-prometheus-stack"
        PROM[Prometheus Server]
        AM[AlertManager]
        NE[Node Exporter]
        KSM[kube-state-metrics]
    end
    
    subgraph "Pilares de Observabilidade"
        LOKI[Loki<br/>Logs]
        TEMPO[Tempo<br/>Traces]
    end
    
    subgraph "Aplicações"
        APP[Weather API<br/>/metrics]
    end
    
    PROM -->|Metrics| GRAF
    LOKI -->|Logs| GRAF
    TEMPO -->|Traces| GRAF
    
    APP -->|/metrics| PROM
    APP -->|logs| LOKI
    APP -->|traces| TEMPO
    
    NE -->|Node Metrics| PROM
    KSM -->|K8s Metrics| PROM
    AM -->|Alerts| GRAF
```

**3 Pilares da Observabilidade:**
- **Metrics** (Prometheus): O que está acontecendo?
- **Logs** (Loki): Por que está acontecendo?
- **Traces** (Tempo): Onde está acontecendo?

**Organização de Namespaces:**
- `monitoring`: Zabbix + Prometheus (Aulas 1 e 2)
- `observability`: kube-prometheus-stack + Loki + Tempo (Aula 3)

---

## ⚡ Parte 1: kube-prometheus-stack

### Passo 1: Instalar Helm

```bash
# Verificar Helm
helm version

# Se não estiver instalado:
# macOS: brew install helm
# Linux: curl https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash
```

### Passo 2: Adicionar Repositórios

```bash
# Adicionar repos
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo add grafana https://grafana.github.io/helm-charts
helm repo update
```

### Passo 3: Criar Namespace Observability

```bash
# Criar namespace separada para stack de observabilidade
kubectl create namespace observability

# Verificar namespaces
kubectl get namespaces
```

### Passo 4: Deploy Stack Completa

```bash
# Deploy kube-prometheus-stack na namespace observability
helm install kube-prometheus-stack prometheus-community/kube-prometheus-stack \
  --namespace observability \
  --set prometheus.service.type=NodePort \
  --set prometheus.service.nodePort=30090 \
  --set grafana.service.type=NodePort \
  --set grafana.service.nodePort=30300

# Aguardar pods (2-3 min)
kubectl wait --for=condition=ready pod -l "release=kube-prometheus-stack" -n observability --timeout=300s
```

**O que foi instalado:**
- Prometheus + AlertManager
- Grafana com dashboards prontos
- Node Exporter + kube-state-metrics

### Passo 4: Acessar Grafana

```bash
# Obter senha
kubectl get secret kube-prometheus-stack-grafana -n observability -o jsonpath="{.data.admin-password}" | base64 --decode
echo

# Acessar
kubectl port-forward svc/kube-prometheus-stack-grafana 3000:80 -n observability &
open http://localhost:3000
# Login: admin / <senha_obtida>
```

### Passo 5: Explorar Dashboards Prontos

**No Grafana:**
1. **Dashboards → Browse**
2. Ver dashboards:
   - Kubernetes / Compute Resources / Cluster
   - Node Exporter / Nodes
   - Prometheus / Overview

**Dashboards profissionais prontos!**

---

## 📝 Parte 2: Adicionar Loki

### Passo 6: Deploy Loki e Promtail

```bash
# Deploy Loki
kubectl apply -f kubernetes/loki-simple.yaml

# Aguardar Loki estar pronto
kubectl wait --for=condition=ready pod -l app=loki -n observability --timeout=120s

# Aguardar mais 20s para o Ingester ficar pronto
sleep 20

# Deploy Promtail (coleta logs dos pods)
kubectl apply -f kubernetes/promtail.yaml

# Verificar Promtail rodando em todos os nodes
kubectl get pods -n observability | grep promtail
```

### Passo 7: Adicionar Datasource Loki

**No Grafana:**
1. **Connections → Data sources → Add data source**
2. Selecione **Loki**
3. Configure:
   - **Name**: Loki
   - **URL**: `http://loki.observability.svc.cluster.local:3100`
4. Clique em **Save & test**

### Passo 8: Testar Logs

**No Grafana:**
1. **Explore**
2. Datasource: **Loki**
3. Query: `{namespace="observability"}`
4. **Run query**

**Ver logs dos pods!**

---

## 🔍 Parte 3: Adicionar Tempo

### Passo 9: Deploy Tempo

```bash
# Deploy Tempo
helm install tempo grafana/tempo \
  --namespace observability

# Aguardar
kubectl wait --for=condition=ready pod -l "app.kubernetes.io/name=tempo" -n observability --timeout=180s
```

### Passo 10: Adicionar Datasource Tempo

**No Grafana:**
1. **Connections → Data sources → Add data source**
2. **Tempo**
3. Configure:
   - **Name**: Tempo
   - **URL**: `http://tempo.observability.svc.cluster.local:3200`
4. **Save & test**

---

## 🎯 Parte 4: Stack Completa

### Passo 11: Verificar 3 Pilares

**No Grafana:**
1. **Connections → Data sources**
2. Ver datasources:
   - ✅ **Prometheus** (métricas)
   - ✅ **Loki** (logs)
   - ✅ **Tempo** (traces)

**Observabilidade completa!**

### Passo 12: Demonstrar Correlação

**1. Métricas (Prometheus):**
- Dashboard: Kubernetes / Compute Resources / Cluster
- Ver CPU/memória

**2. Logs (Loki):**
- Explore → Loki
- Query: `{namespace="monitoring"} |= "error"`

**3. Traces (Tempo):**
- Explore → Tempo
- Query: `{service.name="weather-api"}`
- Ver traces das requisições HTTP

### Passo 13: Testar Traces da Weather API

**Expor Weather API via port-forward:**
```bash
# Port-forward da Weather API
kubectl port-forward -n monitoring svc/weather-api 8081:80 &

# Aguardar port-forward estar pronto
sleep 2
```

**Gerar tráfego na API:**
```bash
# Fazer várias requisições
for i in {1..20}; do
  curl -s http://localhost:8081/WeatherForecast > /dev/null
  echo "Request $i sent"
  sleep 1
done
```

**Parar port-forward:**
```bash
# Matar processo port-forward
pkill -f "port-forward.*weather-api"
```

**No Grafana - Explore:**
1. Selecione datasource **Tempo**
2. **Search**:
   - Service Name: `weather-api`
   - Clique em **Run query**
3. Ver lista de traces
4. Clicar em um trace para ver detalhes:
   - Duração total
   - Spans (etapas da requisição)
   - Atributos HTTP (método, status code, URL)

**Correlação Métricas → Traces:**
1. No Prometheus, veja latência alta
2. No Tempo, encontre o trace específico
3. Analise onde está o gargalo!

---

## 🧹 Parte 5: Limpeza

### Passo 14: Limpeza Final

```bash
# Deletar releases Helm
helm uninstall kube-prometheus-stack -n observability
helm uninstall tempo -n observability

# Deletar namespace observability
kubectl delete namespace observability

# Deletar cluster
aws eks delete-nodegroup \
  --cluster-name monitoring-lab \
  --nodegroup-name workers \
  --region us-east-1 \
  --profile fiapaws

aws eks delete-cluster \
  --name monitoring-lab \
  --region us-east-1 \
  --profile fiapaws
```

---

**FIM DA AULA 3** 🎓
