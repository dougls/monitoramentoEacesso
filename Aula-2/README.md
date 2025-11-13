# Aula 2 - Prometheus e Monitoramento de Aplicações .NET

## 📚 Estrutura da Aula

### Vídeo 1 - Prometheus e a Filosofia de Monitoramento Pull (20 min)
**Temas abordados:**
- Apresentação do Prometheus
- Comparativo do modelo Pull vs. Push (Zabbix)
- Arquitetura do Prometheus
- Adição do Prometheus e de uma app .NET ao Docker Compose

**Pontos-chave para abordar:**
- Modelo Pull: Prometheus busca métricas vs Push: Agent envia métricas
- Vantagens do modelo Pull (service discovery, controle centralizado)
- Formato de métricas do Prometheus (OpenMetrics)
- Time Series Database (TSDB)

### Vídeo 2 - Coletando Métricas da Aplicação .NET (20 min)
**Temas abordados:**
- Instrumentação de código .NET para expor métricas
- Análise do endpoint /metrics
- Introdução à linguagem de consulta PromQL

**Pontos-chave para abordar:**
- Biblioteca prometheus-net
- Tipos de métricas: Counter, Gauge, Histogram, Summary
- Labels e dimensionalidade
- Queries básicas em PromQL

### Vídeo 3 - Zabbix vs Prometheus: Quando Usar Cada Um? (20 min)
**Temas abordados:**
- Comparativo técnico entre as ferramentas
- Cenários ideais para Zabbix (infraestrutura) e Prometheus (aplicação)
- A sinergia de usar ambos em conjunto

**Pontos-chave para abordar:**
- Zabbix: Melhor para infraestrutura, SNMP, agentless
- Prometheus: Melhor para aplicações cloud-native, microserviços
- Integração entre ambos
- Quando usar cada ferramenta

---

## 🚀 Quick Start - Docker Compose (Local)

### Pré-requisitos
- Docker e Docker Compose instalados
- .NET 8 SDK (opcional, para desenvolvimento local)
- Portas disponíveis: 8080 (Zabbix), 9090 (Prometheus), 5000 (Weather API)
- Mínimo 6GB RAM disponível

### Subindo a Stack Completa

```bash
# Na pasta Aula-2
docker-compose up -d

# Verificar status
docker-compose ps

# Acompanhar logs
docker-compose logs -f prometheus
docker-compose logs -f weather-api
```

### Acessando os Serviços

- **Zabbix Web**: http://localhost:8080 (Admin/zabbix)
- **Prometheus**: http://localhost:9090
- **Weather API**: http://localhost:5001/swagger
- **Métricas da API**: http://localhost:5001/metrics

### Testando a Aplicação

```bash
# Requisição normal
curl http://localhost:5001/WeatherForecast

# Requisição lenta (para ver latência)
curl http://localhost:5001/WeatherForecast/slow

# Requisição com erro (para ver métricas de erro)
curl http://localhost:5001/WeatherForecast/error

# Ver métricas
curl http://localhost:5001/metrics
```

### Gerando Carga para Demonstração

```bash
# Instalar hey (ferramenta de load test)
# macOS
brew install hey

# Linux
go install github.com/rakyll/hey@latest

# Gerar carga
hey -n 1000 -c 10 http://localhost:5001/WeatherForecast

# Gerar carga no endpoint lento
hey -n 100 -c 5 http://localhost:5001/WeatherForecast/slow
```

---

## ☸️ Deploy no Kubernetes (AWS EKS)

### Pré-requisitos
- Cluster EKS configurado (pode usar o mesmo da Aula 1)
- kubectl configurado
- AWS ECR para hospedar a imagem Docker da aplicação .NET

### 1. Build e Push da Imagem .NET para ECR

```bash
# Fazer login no ECR
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin <ACCOUNT_ID>.dkr.ecr.us-east-1.amazonaws.com

# Criar repositório (se não existir)
aws ecr create-repository --repository-name weather-api --region us-east-1

# Build da imagem
cd dotnet-app
docker build -t weather-api:latest .

# Tag para ECR
docker tag weather-api:latest <ACCOUNT_ID>.dkr.ecr.us-east-1.amazonaws.com/weather-api:latest

# Push para ECR
docker push <ACCOUNT_ID>.dkr.ecr.us-east-1.amazonaws.com/weather-api:latest
```

### 2. Atualizar Manifesto com Imagem ECR

Editar `kubernetes/weather-api-deployment.yaml` e substituir:
```yaml
image: <YOUR_ECR_REPOSITORY>/weather-api:latest
```

Por:
```yaml
image: <ACCOUNT_ID>.dkr.ecr.us-east-1.amazonaws.com/weather-api:latest
```

### 3. Deploy no EKS

```bash
# Se ainda não criou o namespace (da Aula 1)
kubectl apply -f ../Aula-1/kubernetes/namespace.yaml

# Deploy Prometheus
kubectl apply -f kubernetes/prometheus-configmap.yaml
kubectl apply -f kubernetes/prometheus-pvc.yaml
kubectl apply -f kubernetes/prometheus-deployment.yaml

# Aguardar Prometheus estar pronto
kubectl wait --for=condition=ready pod -l app=prometheus -n monitoring --timeout=300s

# Deploy Weather API
kubectl apply -f kubernetes/weather-api-deployment.yaml

# Verificar status
kubectl get all -n monitoring
```

### 4. Acessar Serviços

```bash
# Obter URLs dos LoadBalancers
kubectl get svc -n monitoring

# Prometheus
PROMETHEUS_URL=$(kubectl get svc prometheus -n monitoring -o jsonpath='{.status.loadBalancer.ingress[0].hostname}')
echo "Prometheus: http://$PROMETHEUS_URL:9090"

# Weather API
WEATHER_API_URL=$(kubectl get svc weather-api -n monitoring -o jsonpath='{.status.loadBalancer.ingress[0].hostname}')
echo "Weather API: http://$WEATHER_API_URL"
echo "Métricas: http://$WEATHER_API_URL/metrics"
```

---

## 📊 Explorando Métricas no Prometheus

### Métricas Automáticas (prometheus-net)

```promql
# Taxa de requisições HTTP por segundo
rate(http_requests_received_total[5m])

# Duração média das requisições
rate(http_request_duration_seconds_sum[5m]) / rate(http_request_duration_seconds_count[5m])

# Percentil 95 de latência
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))

# Total de requisições por código de status
sum by (code) (http_requests_received_total)
```

### Métricas Customizadas da Weather API

```promql
# Total de requisições ao endpoint de weather
weather_requests_total

# Taxa de requisições por minuto
rate(weather_requests_total[1m])

# Requisições ativas no momento
weather_active_requests

# Duração média das requisições
rate(weather_request_duration_seconds_sum[5m]) / rate(weather_request_duration_seconds_count[5m])

# Percentil 99 de latência
histogram_quantile(0.99, rate(weather_request_duration_seconds_bucket[5m]))

# Temperatura média gerada
rate(weather_temperature_celsius_sum[5m]) / rate(weather_temperature_celsius_count[5m])
```

### Queries Úteis para Demonstração

```promql
# Comparar latência entre endpoints normal e lento
rate(weather_request_duration_seconds_sum{endpoint="/WeatherForecast"}[5m]) / rate(weather_request_duration_seconds_count{endpoint="/WeatherForecast"}[5m])

# Taxa de erros
sum(rate(weather_requests_total{endpoint="/WeatherForecast/error"}[5m]))

# Throughput total
sum(rate(weather_requests_total[5m]))
```

---

## 🎯 Roteiro para Gravação

### Vídeo 1 (20 min)

**[0-5 min] Introdução ao Prometheus**
- História e origem (SoundCloud, 2012)
- Parte do CNCF (Cloud Native Computing Foundation)
- Casos de uso: Kubernetes, microserviços, cloud-native

**[5-10 min] Pull vs Push**
- Desenhar diagrama comparativo
- **Push (Zabbix)**: Agent → Server
  - Vantagens: Simples, funciona com firewall
  - Desvantagens: Servidor precisa estar sempre disponível
- **Pull (Prometheus)**: Server ← Target
  - Vantagens: Service discovery, controle centralizado, targets podem ser efêmeros
  - Desvantagens: Precisa acesso de rede aos targets

**[10-15 min] Arquitetura do Prometheus**
- Prometheus Server (TSDB + Retrieval + HTTP Server)
- Exporters (Node Exporter, Blackbox, etc)
- Pushgateway (para jobs batch)
- Alertmanager (para alertas)
- Grafana (visualização)

**[15-20 min] Hands-on Docker Compose**
- Mostrar docker-compose.yaml
- Explicar configuração do Prometheus (prometheus.yml)
- Subir a stack: `docker-compose up -d`
- Acessar Prometheus UI
- Mostrar targets e status

### Vídeo 2 (20 min)

**[0-5 min] Instrumentação .NET**
- Mostrar código da Weather API
- Biblioteca prometheus-net
- Middleware `UseHttpMetrics()` e `MapMetrics()`
- Métricas automáticas vs customizadas

**[5-10 min] Tipos de Métricas**
- **Counter**: Sempre cresce (total de requisições)
```csharp
RequestCounter.WithLabels("GET", "/WeatherForecast").Inc();
```
- **Gauge**: Sobe e desce (requisições ativas)
```csharp
ActiveRequests.Inc();
ActiveRequests.Dec();
```
- **Histogram**: Distribuição de valores (latência)
```csharp
RequestDuration.WithLabels("GET", "/WeatherForecast").NewTimer()
```
- **Summary**: Similar ao Histogram (temperatura)

**[10-15 min] Explorando /metrics**
- Acessar http://localhost:5001/metrics
- Explicar formato OpenMetrics
- Mostrar métricas automáticas (http_*, process_*, dotnet_*)
- Mostrar métricas customizadas (weather_*)

**[15-20 min] Introdução ao PromQL**
- Acessar Prometheus UI
- Queries básicas:
  - `weather_requests_total`
  - `rate(weather_requests_total[5m])`
  - `histogram_quantile(0.95, rate(weather_request_duration_seconds_bucket[5m]))`
- Gerar carga com `hey` e ver métricas mudando
- Mostrar gráficos

### Vídeo 3 (20 min)

**[0-7 min] Comparativo Técnico**
Criar tabela comparativa:

| Aspecto | Zabbix | Prometheus |
|---------|--------|------------|
| Modelo | Push | Pull |
| Melhor para | Infraestrutura | Aplicações |
| Armazenamento | PostgreSQL/MySQL | TSDB local |
| Alertas | Built-in | Alertmanager |
| Visualização | Built-in | Grafana |
| Service Discovery | Limitado | Excelente |
| Dimensionalidade | Limitada | Alta (labels) |

**[7-12 min] Quando Usar Cada Um**
- **Zabbix**:
  - Monitoramento de infraestrutura tradicional
  - SNMP devices (switches, routers)
  - Agentless monitoring
  - Empresas com equipe de infra tradicional
  
- **Prometheus**:
  - Aplicações cloud-native
  - Microserviços e containers
  - Kubernetes
  - Métricas de aplicação (RED: Rate, Errors, Duration)

**[12-17 min] Usando Ambos em Conjunto**
- Zabbix para infraestrutura (servidores, rede, storage)
- Prometheus para aplicações (.NET, Java, Go)
- Grafana como "single pane of glass"
- Demonstrar ambos rodando juntos no docker-compose

**[17-20 min] Próximos Passos**
- Teaser da Aula 3 (Grafana)
- Mencionar Alertmanager
- Mencionar exporters (Node Exporter, Windows Exporter)
- Mencionar integração com Kubernetes

---

## 📝 Checklist de Preparação

### Antes de Gravar

- [ ] Testar docker-compose completo
- [ ] Verificar que Weather API está expondo métricas
- [ ] Preparar queries PromQL
- [ ] Instalar `hey` para load testing
- [ ] Testar geração de carga
- [ ] Preparar comparativo Zabbix vs Prometheus

### Durante a Gravação

- [ ] Mostrar código .NET com instrumentação
- [ ] Explicar cada tipo de métrica
- [ ] Demonstrar /metrics endpoint
- [ ] Executar queries PromQL ao vivo
- [ ] Gerar carga e mostrar métricas mudando

---

## 🔧 Troubleshooting

### Prometheus não encontra targets

```bash
# Verificar configuração
docker-compose exec prometheus cat /etc/prometheus/prometheus.yml

# Verificar logs
docker-compose logs prometheus

# Verificar conectividade
docker-compose exec prometheus wget -O- http://weather-api:8080/metrics
```

### Weather API não expõe métricas

```bash
# Verificar se endpoint existe
curl http://localhost:5001/metrics

# Ver logs da aplicação
docker-compose logs weather-api

# Rebuild se necessário
docker-compose up -d --build weather-api
```

### Erro ao fazer build da aplicação .NET

```bash
# Limpar e rebuild
cd dotnet-app
dotnet clean
dotnet restore
dotnet build

# Ou rebuild do container
docker-compose build --no-cache weather-api
```

---

## 📚 Recursos Adicionais

- [Documentação Prometheus](https://prometheus.io/docs/)
- [PromQL Cheat Sheet](https://promlabs.com/promql-cheat-sheet/)
- [prometheus-net GitHub](https://github.com/prometheus-net/prometheus-net)
- [Prometheus Best Practices](https://prometheus.io/docs/practices/)

---

## 🎓 Exercícios para Alunos

1. Adicionar métricas customizadas na Weather API (ex: contador de previsões por temperatura)
2. Criar alertas no Prometheus (ex: latência > 1s)
3. Instrumentar uma aplicação .NET própria com prometheus-net
4. Criar queries PromQL para calcular SLI/SLO
5. (Desafio) Configurar Prometheus para fazer scrape de múltiplas instâncias da API
