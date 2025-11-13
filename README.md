# Monitoramento e Acesso

**Instituição**: FIAP  
**Curso**: Pós-Graduação Técnica - Arquitetura de Sistemas .NET

---

## 📖 Material das Aulas

### Aula 1 - Zabbix no Kubernetes
- [VIDEO-1.1-PASSO-A-PASSO.md](./Aula-1/VIDEO-1.1-PASSO-A-PASSO.md)
- [VIDEO-1.2-PASSO-A-PASSO.md](./Aula-1/VIDEO-1.2-PASSO-A-PASSO.md)
- [VIDEO-1.3-PASSO-A-PASSO.md](./Aula-1/VIDEO-1.3-PASSO-A-PASSO.md)

### Aula 2 - Prometheus + .NET no Kubernetes
- [VIDEO-2.1-PASSO-A-PASSO.md](./Aula-2/VIDEO-2.1-PASSO-A-PASSO.md)
- [VIDEO-2.2-PASSO-A-PASSO.md](./Aula-2/VIDEO-2.2-PASSO-A-PASSO.md)
- [VIDEO-2.3-PASSO-A-PASSO.md](./Aula-2/VIDEO-2.3-PASSO-A-PASSO.md)

### Aula 3 - Grafana no Kubernetes
- [VIDEO-3.1-PASSO-A-PASSO.md](./Aula-3/VIDEO-3.1-PASSO-A-PASSO.md)
- [VIDEO-3.2-PASSO-A-PASSO.md](./Aula-3/VIDEO-3.2-PASSO-A-PASSO.md)
- [VIDEO-3.3-PASSO-A-PASSO.md](./Aula-3/VIDEO-3.3-PASSO-A-PASSO.md)

---

## 🚀 Quick Start

### Pré-requisitos Gerais

- **Docker** e **Docker Compose** instalados
- **kubectl** configurado (para deploy no Kubernetes)
- **.NET 8 SDK** (opcional, para desenvolvimento)
- **Mínimo 8GB RAM** disponível
- **Portas disponíveis**: 3000, 5000, 8080, 9090, 10050, 10051

### Executando Localmente

Cada aula possui seu próprio `docker-compose.yaml` para execução independente:

```bash
# Aula 1 - Zabbix
cd Aula-1
docker-compose up -d

# Aula 2 - Prometheus + App .NET
cd Aula-2
docker-compose up -d

# Aula 3 - Stack Completa (Zabbix + Prometheus + Grafana)
cd Aula-3
docker-compose up -d
```

### Acessando os Serviços

| Serviço | URL | Credenciais |
|---------|-----|-------------|
| Grafana | http://localhost:3000 | admin / admin |
| Zabbix Web | http://localhost:8080 | Admin / zabbix |
| Prometheus | http://localhost:9090 | - |
| Weather API | http://localhost:5001/swagger | - |
| Métricas API | http://localhost:5001/metrics | - |

---

## ☸️ Deploy no Kubernetes (AWS EKS)

### Pré-requisitos

- Cluster EKS configurado
- kubectl configurado para acessar o cluster
- AWS CLI configurado
- Permissões para criar LoadBalancers (ELB)
- ECR para hospedar imagens Docker

### Deploy Completo

```bash
# 1. Criar namespace
kubectl apply -f Aula-1/kubernetes/namespace.yaml

# 2. Deploy Aula 1 - Zabbix
kubectl apply -f Aula-1/kubernetes/

# 3. Aguardar Zabbix estar pronto
kubectl wait --for=condition=ready pod -l app=zabbix-server -n monitoring --timeout=300s

# 4. Build e push da imagem .NET para ECR
cd Aula-2/dotnet-app
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin <ACCOUNT_ID>.dkr.ecr.us-east-1.amazonaws.com
docker build -t weather-api:latest .
docker tag weather-api:latest <ACCOUNT_ID>.dkr.ecr.us-east-1.amazonaws.com/weather-api:latest
docker push <ACCOUNT_ID>.dkr.ecr.us-east-1.amazonaws.com/weather-api:latest

# 5. Atualizar manifesto com imagem ECR
# Editar Aula-2/kubernetes/weather-api-deployment.yaml

# 6. Deploy Aula 2 - Prometheus + Weather API
kubectl apply -f Aula-2/kubernetes/

# 7. Aguardar Prometheus estar pronto
kubectl wait --for=condition=ready pod -l app=prometheus -n monitoring --timeout=300s

# 8. Deploy Aula 3 - Grafana
kubectl apply -f Aula-3/kubernetes/

# 9. Verificar status
kubectl get all -n monitoring

# 10. Obter URLs dos LoadBalancers
kubectl get svc -n monitoring
```

### Acessar Serviços no EKS

```bash
# Grafana
GRAFANA_URL=$(kubectl get svc grafana -n monitoring -o jsonpath='{.status.loadBalancer.ingress[0].hostname}')
echo "Grafana: http://$GRAFANA_URL"

# Zabbix
ZABBIX_URL=$(kubectl get svc zabbix-web -n monitoring -o jsonpath='{.status.loadBalancer.ingress[0].hostname}')
echo "Zabbix: http://$ZABBIX_URL"

# Prometheus
PROMETHEUS_URL=$(kubectl get svc prometheus -n monitoring -o jsonpath='{.status.loadBalancer.ingress[0].hostname}')
echo "Prometheus: http://$PROMETHEUS_URL:9090"

# Weather API
WEATHER_API_URL=$(kubectl get svc weather-api -n monitoring -o jsonpath='{.status.loadBalancer.ingress[0].hostname}')
echo "Weather API: http://$WEATHER_API_URL"
```

---

## 🏗️ Arquitetura da Solução

```
┌─────────────────────────────────────────────────────────────┐
│                         GRAFANA                              │
│                  (Visualização Unificada)                    │
│                     http://localhost:3000                    │
└────────────────┬──────────────────────┬─────────────────────┘
                 │                      │
                 │                      │
        ┌────────▼────────┐    ┌───────▼────────┐
        │   PROMETHEUS    │    │     ZABBIX     │
        │  (Pull Model)   │    │  (Push Model)  │
        │  :9090          │    │  :8080         │
        └────────┬────────┘    └───────┬────────┘
                 │                      │
                 │                      │
        ┌────────▼────────┐    ┌───────▼────────┐
        │  WEATHER API    │    │  ZABBIX AGENT  │
        │  (.NET 8)       │    │  (Host Mon.)   │
        │  :5000          │    │  :10050        │
        │  /metrics       │    │                │
        └─────────────────┘    └────────────────┘
```

### Componentes

1. **Zabbix** (Aula 1)
   - PostgreSQL: Banco de dados
   - Zabbix Server: Núcleo de processamento
   - Zabbix Web: Interface web
   - Zabbix Agent: Coleta de métricas

2. **Prometheus** (Aula 2)
   - Prometheus Server: TSDB e coleta
   - Weather API: Aplicação .NET instrumentada

3. **Grafana** (Aula 3)
   - Grafana Server: Visualização
   - Datasources: Prometheus + Zabbix

---

## 🛠️ Tecnologias Utilizadas

### Monitoramento
- **Zabbix 6.4** - Monitoramento de infraestrutura
- **Prometheus** (latest) - Monitoramento de aplicações
- **Grafana** (latest) - Visualização e dashboards

### Aplicação
- **.NET 8** - Framework da aplicação
- **ASP.NET Core** - Web API
- **prometheus-net** - Biblioteca de métricas

### Infraestrutura
- **Docker** & **Docker Compose** - Containerização local
- **Kubernetes** - Orquestração
- **AWS EKS** - Kubernetes gerenciado
- **AWS ECR** - Registry de imagens
- **PostgreSQL 15** - Banco de dados do Zabbix

---

## 📊 Métricas Coletadas

### Zabbix (Infraestrutura)
- CPU utilization
- Memory usage
- Disk space
- Network traffic
- Process monitoring
- Service availability

### Prometheus (Aplicação .NET)

**Métricas Automáticas** (prometheus-net):
- `http_requests_received_total` - Total de requisições HTTP
- `http_request_duration_seconds` - Duração das requisições
- `process_cpu_seconds_total` - CPU do processo
- `process_working_set_bytes` - Memória do processo
- `dotnet_collection_count_total` - Garbage collections

**Métricas Customizadas** (Weather API):
- `weather_requests_total` - Total de requisições ao endpoint
- `weather_request_duration_seconds` - Duração por endpoint
- `weather_active_requests` - Requisições ativas
- `weather_temperature_celsius` - Temperaturas geradas

---

## 📝 Exercícios Práticos

### Nível Básico
1. Subir a stack localmente e acessar todas as interfaces
2. Criar um host no Zabbix e associar um template
3. Gerar carga na Weather API e observar métricas no Prometheus
4. Importar um dashboard pronto no Grafana

### Nível Intermediário
1. Criar triggers customizadas no Zabbix
2. Adicionar métricas customizadas na Weather API
3. Criar queries PromQL para calcular percentis
4. Construir um dashboard do zero no Grafana

### Nível Avançado
1. Deploy completo no EKS
2. Configurar alertas no Grafana com notificações
3. Implementar Loki para agregação de logs
4. Adicionar distributed tracing com OpenTelemetry

---

## 🎓 Projeto Final Sugerido

**Objetivo**: Criar uma stack de observabilidade completa para uma aplicação .NET própria

**Requisitos Mínimos**:
- ✅ Aplicação .NET instrumentada com prometheus-net
- ✅ Zabbix monitorando infraestrutura
- ✅ Prometheus coletando métricas da aplicação
- ✅ Grafana com pelo menos 2 dashboards
- ✅ Alertas configurados
- ✅ Deploy no Kubernetes (local ou EKS)
- ✅ Documentação completa (README + Runbook)

**Entregáveis**:
1. Código-fonte da aplicação
2. Manifestos Kubernetes ou Docker Compose
3. Dashboards Grafana (JSON)
4. Documentação de arquitetura
5. Runbook para troubleshooting
6. Vídeo de demonstração (5-10 min)

**Critérios de Avaliação**:
- Funcionalidade (30%)
- Qualidade do código (20%)
- Documentação (20%)
- Dashboards e visualizações (15%)
- Alertas e observabilidade (15%)

---

## 🔧 Troubleshooting Comum

### Problema: Containers não iniciam

```bash
# Verificar logs
docker-compose logs <service-name>

# Verificar recursos
docker stats

# Limpar e reiniciar
docker-compose down -v
docker-compose up -d
```

### Problema: Porta já em uso

```bash
# Verificar portas em uso
lsof -i :8080
lsof -i :9090
lsof -i :3000

# Matar processo
kill -9 <PID>
```

### Problema: Prometheus não coleta métricas

```bash
# Verificar targets no Prometheus
# http://localhost:9090/targets

# Testar endpoint manualmente
curl http://localhost:5000/metrics

# Verificar conectividade
docker-compose exec prometheus wget -O- http://weather-api:8080/metrics
```

### Problema: Grafana não conecta nos datasources

```bash
# Verificar logs
docker-compose logs grafana

# Testar conectividade
docker-compose exec grafana wget -O- http://prometheus:9090/-/healthy
docker-compose exec grafana wget -O- http://zabbix-web:8080

# Recriar datasources
# Deletar e recriar via UI ou provisioning
```

---

## 📚 Recursos Adicionais

### Documentação Oficial
- [Zabbix Documentation](https://www.zabbix.com/documentation/current)
- [Prometheus Documentation](https://prometheus.io/docs/)
- [Grafana Documentation](https://grafana.com/docs/)
- [prometheus-net GitHub](https://github.com/prometheus-net/prometheus-net)

### Cursos e Tutoriais
- [Prometheus Up & Running (O'Reilly)](https://www.oreilly.com/library/view/prometheus-up/9781492034131/)
- [Grafana Fundamentals](https://grafana.com/tutorials/grafana-fundamentals/)
- [CNCF Observability Landscape](https://landscape.cncf.io/card-mode?category=observability-and-analysis)

### Comunidade
- [Prometheus Community](https://prometheus.io/community/)
- [Grafana Community](https://community.grafana.com/)
- [Zabbix Forum](https://www.zabbix.com/forum)
- [CNCF Slack](https://slack.cncf.io/)

### Dashboards Prontos
- [Grafana Dashboards](https://grafana.com/grafana/dashboards/)
- [Awesome Prometheus](https://github.com/roaldnefs/awesome-prometheus)

---

## 👨‍🏫 Sobre o Instrutor

**Instituição**: FIAP  
**Curso**: Pós-Graduação Técnica - Arquitetura de Sistemas .NET  
**Disciplina**: Monitoramento e Acesso

---

## 📄 Licença

Este material é destinado exclusivamente para fins educacionais na FIAP.

---

## 🤝 Contribuições

Sugestões e melhorias são bem-vindas! Entre em contato através dos canais oficiais da FIAP.

---

## 📞 Suporte

Para dúvidas sobre o material:
- **Portal FIAP**: [https://fiap.com.br](https://fiap.com.br)
- **Pós Tech**: [https://postech.fiap.com.br](https://postech.fiap.com.br)

---

**Última atualização**: Novembro 2024  
**Versão**: 1.0

---

## 🎯 Próximos Passos

Após concluir esta disciplina, recomendamos:

1. **Site Reliability Engineering (SRE)**
   - Estudo de SLIs, SLOs e SLAs
   - Error budgets
   - Incident management

2. **OpenTelemetry**
   - Padrão unificado para observabilidade
   - Instrumentação automática
   - Traces, metrics e logs

3. **Kubernetes Avançado**
   - Service Mesh (Istio, Linkerd)
   - Observabilidade nativa
   - Operators e CRDs

4. **FinOps e Otimização**
   - Monitoramento de custos
   - Otimização de recursos
   - Rightsizing

**Boa sorte na sua jornada de observabilidade! 🚀**
