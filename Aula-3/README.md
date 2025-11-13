# Aula 3 - Visualização Unificada com Grafana

## 📚 Estrutura da Aula

### Vídeo 1 - O Poder da Visualização com Grafana (20 min)
**Temas abordados:**
- Apresentação do Grafana
- Conceito de "single pane of glass"
- Adição do Grafana ao Docker Compose
- Configuração das fontes de dados (Zabbix e Prometheus)

**Pontos-chave para abordar:**
- Por que Grafana? (Visualização unificada, flexibilidade, comunidade)
- Múltiplas fontes de dados em um único dashboard
- Provisioning automático de datasources
- Plugins e extensibilidade

### Vídeo 2 - Construindo um Dashboard Unificado (20 min)
**Temas abordados:**
- Criação de um dashboard do zero
- Adição de painéis com dados do Prometheus e do Zabbix
- Organização e customização do layout

**Pontos-chave para abordar:**
- Tipos de visualizações (Time Series, Gauge, Stat, Table)
- Queries PromQL no Grafana
- Variáveis e templates
- Alertas no Grafana

### Vídeo 3 - Conclusão e Próximos Passos em Observabilidade (20 min)
**Temas abordados:**
- Recapitulação da stack construída
- Sugestões para aprofundamento (Alertmanager, Loki, Tracing)

**Pontos-chave para abordar:**
- Os 3 pilares da observabilidade: Métricas, Logs, Traces
- Alertmanager para gerenciamento de alertas
- Loki para agregação de logs
- Jaeger/Tempo para distributed tracing
- OpenTelemetry como padrão

---

## 🚀 Quick Start - Docker Compose (Local)

### Pré-requisitos
- Docker e Docker Compose instalados
- Portas disponíveis: 3000 (Grafana), 8080 (Zabbix), 9090 (Prometheus), 5000 (Weather API)
- Mínimo 8GB RAM disponível

### Subindo a Stack Completa

```bash
# Na pasta Aula-3
docker-compose up -d

# Verificar status
docker-compose ps

# Acompanhar logs do Grafana
docker-compose logs -f grafana

# Aguardar inicialização (pode levar 3-5 minutos)
```

### Acessando os Serviços

- **Grafana**: http://localhost:3000 (admin/admin)
- **Zabbix Web**: http://localhost:8080 (Admin/zabbix)
- **Prometheus**: http://localhost:9090
- **Weather API**: http://localhost:5001/swagger
- **Métricas da API**: http://localhost:5001/metrics

### Primeira Configuração do Grafana

1. **Login inicial**
   - Acesse http://localhost:3000
   - Usuário: `admin`
   - Senha: `admin`
   - (Será solicitado trocar a senha)

2. **Verificar Data Sources**
   - Menu → Connections → Data Sources
   - Verificar que Prometheus e Zabbix estão configurados
   - Testar conexão com "Save & Test"

3. **Ativar Plugin Zabbix**
   - Menu → Administration → Plugins
   - Buscar "Zabbix"
   - Clicar em "Enable"

4. **Importar Dashboard**
   - Menu → Dashboards → Import
   - Upload do arquivo `grafana/dashboards/weather-api-dashboard.json`
   - Ou usar ID de dashboard da comunidade

---

## ☸️ Deploy no Kubernetes (AWS EKS)

### Pré-requisitos
- Cluster EKS com Zabbix e Prometheus já deployados (Aulas 1 e 2)
- kubectl configurado

### Deploy do Grafana

```bash
# 1. Criar ConfigMap com datasources
kubectl apply -f kubernetes/grafana-configmap.yaml

# 2. Criar PVC para Grafana
kubectl apply -f kubernetes/grafana-pvc.yaml

# 3. Deploy Grafana
kubectl apply -f kubernetes/grafana-deployment.yaml

# 4. Aguardar Grafana estar pronto
kubectl wait --for=condition=ready pod -l app=grafana -n monitoring --timeout=300s

# 5. Verificar status
kubectl get all -n monitoring
```

### Acessar Grafana no EKS

```bash
# Obter URL do LoadBalancer
GRAFANA_URL=$(kubectl get svc grafana -n monitoring -o jsonpath='{.status.loadBalancer.ingress[0].hostname}')
echo "Grafana: http://$GRAFANA_URL"

# Ou usar port-forward para acesso local
kubectl port-forward svc/grafana 3000:80 -n monitoring
# Acessar: http://localhost:3000
```

### Deploy Completo da Stack (Todas as Aulas)

```bash
# Namespace
kubectl apply -f ../Aula-1/kubernetes/namespace.yaml

# Aula 1 - Zabbix
kubectl apply -f ../Aula-1/kubernetes/

# Aguardar Zabbix estar pronto
kubectl wait --for=condition=ready pod -l app=zabbix-server -n monitoring --timeout=300s

# Aula 2 - Prometheus + Weather API
kubectl apply -f ../Aula-2/kubernetes/

# Aguardar Prometheus estar pronto
kubectl wait --for=condition=ready pod -l app=prometheus -n monitoring --timeout=300s

# Aula 3 - Grafana
kubectl apply -f kubernetes/

# Verificar tudo
kubectl get all -n monitoring
```

---

## 📊 Criando Dashboards no Grafana

### Dashboard 1: Weather API Overview

**Painéis sugeridos:**

1. **Request Rate** (Time Series)
```promql
rate(weather_requests_total[5m])
```

2. **Average Response Time** (Gauge)
```promql
rate(weather_request_duration_seconds_sum[5m]) / rate(weather_request_duration_seconds_count[5m])
```

3. **Active Requests** (Stat)
```promql
weather_active_requests
```

4. **Response Time Percentiles** (Time Series)
```promql
histogram_quantile(0.95, rate(weather_request_duration_seconds_bucket[5m]))
histogram_quantile(0.99, rate(weather_request_duration_seconds_bucket[5m]))
```

5. **Error Rate** (Time Series)
```promql
rate(weather_requests_total{endpoint="/WeatherForecast/error"}[5m])
```

6. **Throughput by Endpoint** (Bar Gauge)
```promql
sum by (endpoint) (rate(weather_requests_total[5m]))
```

### Dashboard 2: Infrastructure Overview (Zabbix + Prometheus)

**Painéis sugeridos:**

1. **CPU Usage** (Time Series - Zabbix)
   - Data Source: Zabbix
   - Item: `system.cpu.util`

2. **Memory Usage** (Gauge - Zabbix)
   - Data Source: Zabbix
   - Item: `vm.memory.size[available]`

3. **Container CPU** (Time Series - Prometheus)
```promql
rate(container_cpu_usage_seconds_total[5m])
```

4. **Container Memory** (Time Series - Prometheus)
```promql
container_memory_usage_bytes
```

5. **Network Traffic** (Time Series - Zabbix)
   - Data Source: Zabbix
   - Item: `net.if.in` / `net.if.out`

### Dashboard 3: RED Metrics (Rate, Errors, Duration)

**Painéis sugeridos:**

1. **Rate** - Requests per second
```promql
sum(rate(weather_requests_total[5m]))
```

2. **Errors** - Error rate
```promql
sum(rate(weather_requests_total{endpoint="/WeatherForecast/error"}[5m])) / sum(rate(weather_requests_total[5m]))
```

3. **Duration** - Response time
```promql
histogram_quantile(0.95, rate(weather_request_duration_seconds_bucket[5m]))
```

---

## 🎯 Roteiro para Gravação

### Vídeo 1 (20 min)

**[0-5 min] Introdução ao Grafana**
- História (Torkel Ödegaard, 2014)
- Casos de uso (empresas que usam)
- Vantagens: Open source, flexível, grande comunidade
- Conceito de "single pane of glass"

**[5-10 min] Arquitetura e Conceitos**
- Data Sources (Prometheus, Zabbix, InfluxDB, etc)
- Dashboards e Panels
- Queries e Transformations
- Alerting
- Plugins

**[10-15 min] Hands-on Docker Compose**
- Mostrar docker-compose.yaml
- Explicar configuração do Grafana
- Provisioning de datasources (datasources.yml)
- Subir a stack: `docker-compose up -d`
- Aguardar inicialização

**[15-20 min] Primeiro Acesso e Configuração**
- Login no Grafana
- Tour pela interface
- Verificar Data Sources (Prometheus e Zabbix)
- Testar conexões
- Ativar plugin Zabbix

### Vídeo 2 (20 min)

**[0-5 min] Criando Dashboard do Zero**
- New Dashboard → Add visualization
- Selecionar Prometheus como data source
- Explicar interface de edição

**[5-12 min] Adicionando Painéis**
1. **Request Rate**
   - Time Series panel
   - Query: `rate(weather_requests_total[5m])`
   - Customizar cores e legenda

2. **Response Time**
   - Gauge panel
   - Query: média de duração
   - Configurar thresholds (verde < 0.5s, amarelo < 1s, vermelho > 1s)

3. **Active Requests**
   - Stat panel
   - Query: `weather_active_requests`

4. **Percentiles**
   - Time Series panel
   - Múltiplas queries (p50, p95, p99)

**[12-17 min] Integrando Dados do Zabbix**
- Adicionar painel com dados do Zabbix
- Mostrar CPU/Memory do host
- Comparar com métricas do Prometheus
- Demonstrar "single pane of glass"

**[17-20 min] Organização e Customização**
- Organizar layout (drag and drop)
- Adicionar variáveis (time range, refresh)
- Configurar refresh automático
- Salvar dashboard
- Exportar JSON

### Vídeo 3 (20 min)

**[0-5 min] Recapitulação da Jornada**
- Aula 1: Zabbix para infraestrutura
- Aula 2: Prometheus para aplicações
- Aula 3: Grafana para visualização unificada
- Mostrar stack completa funcionando

**[5-10 min] Os 3 Pilares da Observabilidade**
- **Métricas** (o que já temos)
  - Zabbix, Prometheus, Grafana
  - Responde: "O que está acontecendo?"
  
- **Logs** (próximo passo)
  - Loki (Grafana Labs)
  - Elasticsearch + Kibana (ELK)
  - Responde: "Por que está acontecendo?"
  
- **Traces** (distributed tracing)
  - Jaeger, Tempo
  - OpenTelemetry
  - Responde: "Onde está acontecendo?"

**[10-15 min] Próximos Passos**

1. **Alertmanager**
   - Gerenciamento centralizado de alertas
   - Roteamento e agrupamento
   - Integração com Slack, PagerDuty, etc

2. **Loki**
   - Agregação de logs
   - Integração com Grafana
   - PromQL-like para logs

3. **Tracing**
   - Instrumentação com OpenTelemetry
   - Jaeger ou Tempo
   - Visualização de traces no Grafana

4. **Service Mesh**
   - Istio, Linkerd
   - Métricas automáticas
   - Observabilidade out-of-the-box

**[15-20 min] Boas Práticas e Conclusão**
- Definir SLIs, SLOs e SLAs
- Implementar alertas significativos (evitar alert fatigue)
- Documentar runbooks
- Cultura de observabilidade
- Monitoramento como código (GitOps)
- Agradecimentos e encerramento

---

## 📝 Checklist de Preparação

### Antes de Gravar

- [ ] Testar stack completa localmente
- [ ] Verificar que todos os datasources estão funcionando
- [ ] Preparar dashboards de exemplo
- [ ] Ter queries PromQL prontas
- [ ] Gerar carga na aplicação para ter dados
- [ ] Preparar slides para pilares da observabilidade

### Durante a Gravação

- [ ] Demonstrar criação de dashboard ao vivo
- [ ] Mostrar integração Prometheus + Zabbix
- [ ] Explicar cada tipo de painel
- [ ] Demonstrar customizações
- [ ] Mostrar exportação de dashboard

### Demonstrações Importantes

1. **Single Pane of Glass**
   - Dashboard com métricas de Prometheus E Zabbix
   - Mostrar que é possível correlacionar dados

2. **Alerting**
   - Criar alerta simples no Grafana
   - Mostrar como configurar notificações

3. **Variáveis**
   - Criar variável para selecionar endpoint
   - Demonstrar dashboard dinâmico

---

## 🔧 Troubleshooting

### Grafana não conecta no Prometheus

```bash
# Verificar se Prometheus está acessível
docker-compose exec grafana wget -O- http://prometheus:9090/-/healthy

# Ver logs do Grafana
docker-compose logs grafana

# Testar datasource manualmente
# Grafana UI → Data Sources → Prometheus → Save & Test
```

### Plugin Zabbix não instala

```bash
# Verificar logs
docker-compose logs grafana | grep plugin

# Reinstalar plugin manualmente
docker-compose exec grafana grafana-cli plugins install alexanderzobnin-zabbix-app

# Reiniciar Grafana
docker-compose restart grafana
```

### Dashboard não mostra dados

```bash
# Verificar time range (canto superior direito)
# Verificar se há dados no período selecionado

# Testar query no Prometheus diretamente
curl 'http://localhost:9090/api/v1/query?query=weather_requests_total'

# Gerar carga na aplicação
hey -n 1000 -c 10 http://localhost:5000/WeatherForecast
```

### No Kubernetes - Grafana não acessa outros serviços

```bash
# Verificar DNS interno
kubectl exec -it deployment/grafana -n monitoring -- nslookup prometheus

# Verificar conectividade
kubectl exec -it deployment/grafana -n monitoring -- wget -O- http://prometheus:9090/-/healthy

# Ver logs
kubectl logs deployment/grafana -n monitoring
```

---

## 📚 Recursos Adicionais

- [Documentação Grafana](https://grafana.com/docs/)
- [Grafana Dashboards](https://grafana.com/grafana/dashboards/) - Comunidade
- [Grafana Play](https://play.grafana.org/) - Ambiente de testes
- [Grafana YouTube Channel](https://www.youtube.com/c/Grafana)
- [ObservabilityCON](https://grafana.com/about/events/observabilitycon/) - Conferência anual

### Dashboards Prontos para Importar

- **Node Exporter Full**: ID 1860
- **Prometheus Stats**: ID 2
- **Kubernetes Cluster Monitoring**: ID 7249
- **.NET Core Monitoring**: ID 10427

---

## 🎓 Exercícios para Alunos

1. Criar dashboard customizado para a Weather API
2. Adicionar alertas para latência > 1s e error rate > 5%
3. Integrar Grafana com Slack para notificações
4. Criar variáveis para filtrar por endpoint
5. (Desafio) Implementar Loki e adicionar logs ao dashboard
6. (Desafio) Criar dashboard com métricas do Kubernetes (se deployado no EKS)

---

## 🏆 Projeto Final Sugerido

**Objetivo**: Criar uma stack de observabilidade completa para uma aplicação .NET

**Requisitos**:
1. Aplicação .NET instrumentada com prometheus-net
2. Zabbix monitorando infraestrutura
3. Prometheus coletando métricas da aplicação
4. Grafana com dashboards unificados
5. Alertas configurados
6. Deploy no Kubernetes (EKS)
7. Documentação completa

**Entregáveis**:
- Código da aplicação
- Manifestos Kubernetes
- Dashboards Grafana (JSON)
- Documentação de arquitetura
- Runbook para troubleshooting

---

## 🎬 Conclusão do Curso

Parabéns por concluir a disciplina de **Monitoramento e Acesso**!

Você agora possui conhecimento sobre:
- ✅ Monitoramento de infraestrutura com Zabbix
- ✅ Monitoramento de aplicações com Prometheus
- ✅ Visualização unificada com Grafana
- ✅ Deploy em Kubernetes (EKS)
- ✅ Instrumentação de aplicações .NET
- ✅ Boas práticas de observabilidade

**Continue aprendendo**:
- Explore OpenTelemetry
- Implemente distributed tracing
- Estude SRE (Site Reliability Engineering)
- Participe da comunidade CNCF

**Boa sorte na sua jornada de observabilidade! 🚀**
