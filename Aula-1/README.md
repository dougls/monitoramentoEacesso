# Aula 1 - Introdução ao Monitoramento com Zabbix

## 📚 Estrutura da Aula

### Vídeo 1 - Introdução ao Monitoramento e Arquitetura Zabbix (20 min)
**Temas abordados:**
- Importância do monitoramento em arquiteturas modernas
- Arquitetura do Zabbix (Server, Database, Web UI, Agent)
- Hands-on inicial com Docker Compose para subir a stack Zabbix

**Pontos-chave para abordar:**
- Por que monitorar? (Disponibilidade, Performance, Capacidade, Segurança)
- Modelo Push do Zabbix (Agent → Server)
- Componentes e suas responsabilidades

### Vídeo 2 - Configuração Inicial e Coleta de Dados (20 min)
**Temas abordados:**
- Configuração inicial via wizard web
- Conceitos chave (Host, Item, Trigger)
- Verificação da coleta de dados e teste de itens

**Pontos-chave para abordar:**
- **Host**: Dispositivo/servidor a ser monitorado
- **Item**: Métrica individual (CPU, memória, disco)
- **Trigger**: Condição que gera alerta
- Demonstração prática de criação de itens

### Vídeo 3 - Templates, Triggers e Visualização (20 min)
**Temas abordados:**
- Uso de templates para configuração em massa
- Criação e funcionamento de Triggers
- Simulação de um problema
- Visualização com gráficos e dashboards no Zabbix

**Pontos-chave para abordar:**
- Templates: Reutilização de configurações
- Expressões de Triggers
- Dashboards customizados

---

## 🚀 Quick Start - Docker Compose (Local)

### Pré-requisitos
- Docker e Docker Compose instalados
- Portas disponíveis: 8080 (Web UI), 10051 (Server), 10050 (Agent)
- Mínimo 4GB RAM disponível

### Subindo a Stack

```bash
# Na pasta Aula-1
docker-compose up -d

# Verificar status dos containers
docker-compose ps

# Acompanhar logs do servidor
docker-compose logs -f zabbix-server

# Aguardar inicialização (pode levar 2-3 minutos)
```

### Acessando o Zabbix

- **URL**: http://localhost:8080
- **Usuário padrão**: Admin
- **Senha padrão**: zabbix

### Comandos Úteis

```bash
# Parar a stack
docker-compose down

# Parar e remover volumes (reset completo)
docker-compose down -v

# Ver logs de um serviço específico
docker-compose logs postgres-server
docker-compose logs zabbix-web

# Reiniciar um serviço
docker-compose restart zabbix-server
```

---

## ☸️ Deploy no Kubernetes (AWS EKS)

### Pré-requisitos
- Cluster EKS configurado
- kubectl configurado para acessar o cluster
- Permissões para criar LoadBalancer (ELB)

### Deploy Passo a Passo

```bash
# 1. Criar namespace
kubectl apply -f kubernetes/namespace.yaml

# 2. Criar secret com credenciais do banco
kubectl apply -f kubernetes/postgres-secret.yaml

# 3. Criar PVC para PostgreSQL
kubectl apply -f kubernetes/postgres-pvc.yaml

# 4. Deploy PostgreSQL
kubectl apply -f kubernetes/postgres-deployment.yaml

# 5. Aguardar PostgreSQL estar pronto
kubectl wait --for=condition=ready pod -l app=postgres -n monitoring --timeout=300s

# 6. Deploy Zabbix Server
kubectl apply -f kubernetes/zabbix-server-deployment.yaml

# 7. Aguardar Zabbix Server estar pronto
kubectl wait --for=condition=ready pod -l app=zabbix-server -n monitoring --timeout=300s

# 8. Deploy Zabbix Web
kubectl apply -f kubernetes/zabbix-web-deployment.yaml

# 9. Deploy Zabbix Agent (DaemonSet - roda em todos os nodes)
kubectl apply -f kubernetes/zabbix-agent-daemonset.yaml
```

### Deploy Completo (um comando)

```bash
# Aplicar todos os manifestos
kubectl apply -f kubernetes/

# Verificar status
kubectl get all -n monitoring
```

### Acessando no EKS

```bash
# Obter URL do LoadBalancer
kubectl get svc zabbix-web -n monitoring

# Aguardar o ELB ser provisionado (pode levar alguns minutos)
# A URL estará na coluna EXTERNAL-IP
```

### Comandos Úteis Kubernetes

```bash
# Ver pods
kubectl get pods -n monitoring

# Ver logs
kubectl logs -f deployment/zabbix-server -n monitoring

# Descrever pod (troubleshooting)
kubectl describe pod <pod-name> -n monitoring

# Acessar shell do container
kubectl exec -it <pod-name> -n monitoring -- sh

# Deletar tudo
kubectl delete namespace monitoring
```

---

## 🎯 Roteiro para Gravação

### Vídeo 1 (20 min)

**[0-5 min] Introdução**
- Apresentação do instrutor e contexto da disciplina
- Por que monitoramento é crítico em arquiteturas .NET
- Casos de uso reais (downtime, performance degradation)

**[5-10 min] Arquitetura Zabbix**
- Desenhar/mostrar diagrama da arquitetura
- Explicar cada componente:
  - PostgreSQL: Armazena configurações e dados históricos
  - Zabbix Server: Núcleo do processamento
  - Zabbix Web: Interface de gerenciamento
  - Zabbix Agent: Coleta dados dos hosts

**[10-15 min] Hands-on Docker Compose**
- Mostrar o arquivo docker-compose.yaml
- Explicar cada serviço
- Executar `docker-compose up -d`
- Mostrar logs e status

**[15-20 min] Primeiro Acesso**
- Acessar http://localhost:8080
- Login inicial
- Tour rápido pela interface
- Mostrar que o Zabbix Server já está sendo monitorado

### Vídeo 2 (20 min)

**[0-5 min] Conceitos Fundamentais**
- Host: O que é e para que serve
- Item: Tipos de itens (Zabbix agent, SNMP, JMX, etc)
- Trigger: Expressões e severidades

**[5-12 min] Configurando Host**
- Configuration → Hosts → Create host
- Adicionar o Zabbix-Docker-Host
- Associar template "Linux by Zabbix agent"
- Verificar interface do agent

**[12-18 min] Criando Items Customizados**
- Criar item de CPU usage
- Criar item de memória
- Testar coleta com "Latest data"

**[18-20 min] Verificação**
- Monitoring → Latest data
- Ver dados sendo coletados
- Explicar intervalo de coleta

### Vídeo 3 (20 min)

**[0-5 min] Templates**
- O que são templates
- Templates built-in do Zabbix
- Como criar template customizado

**[5-10 min] Triggers**
- Criar trigger para CPU > 80%
- Criar trigger para memória > 90%
- Explicar expressões e funções (avg, last, etc)

**[10-15 min] Simulação de Problema**
- Usar stress test no container
```bash
docker exec -it zabbix-agent sh
# Instalar stress (se necessário)
stress --cpu 4 --timeout 60s
```
- Mostrar trigger sendo acionada
- Ver em Problems

**[15-20 min] Dashboards**
- Criar dashboard customizado
- Adicionar widgets (graphs, problems, data overview)
- Salvar e compartilhar

---

## 📝 Checklist de Preparação

### Antes de Gravar

- [ ] Testar docker-compose localmente
- [ ] Verificar que todas as portas estão disponíveis
- [ ] Preparar exemplos de triggers
- [ ] Ter stress test pronto
- [ ] Limpar histórico do browser
- [ ] Aumentar fonte do terminal e browser

### Durante a Gravação

- [ ] Mostrar comandos antes de executar
- [ ] Explicar output dos comandos
- [ ] Pausar para aguardar inicialização
- [ ] Destacar pontos importantes na UI

### Após Gravação

- [ ] Testar deploy no EKS (se for mostrar)
- [ ] Documentar problemas encontrados
- [ ] Preparar FAQ para alunos

---

## 🔧 Troubleshooting

### Container não inicia

```bash
# Ver logs detalhados
docker-compose logs <service-name>

# Verificar recursos
docker stats
```

### Não consegue acessar Web UI

```bash
# Verificar se porta está em uso
lsof -i :8080

# Verificar se container está rodando
docker ps | grep zabbix-web
```

### Zabbix Server não conecta no PostgreSQL

```bash
# Verificar se PostgreSQL está pronto
docker-compose exec postgres-server pg_isready -U zabbix

# Ver logs do server
docker-compose logs zabbix-server
```

### No Kubernetes - Pod não inicia

```bash
# Ver eventos
kubectl describe pod <pod-name> -n monitoring

# Ver logs
kubectl logs <pod-name> -n monitoring

# Verificar PVC
kubectl get pvc -n monitoring
```

---

## 📚 Recursos Adicionais

- [Documentação Oficial Zabbix](https://www.zabbix.com/documentation/current)
- [Zabbix Templates](https://www.zabbix.com/integrations)
- [Zabbix Community](https://www.zabbix.com/forum)

---

## 🎓 Exercícios para Alunos

1. Adicionar um novo host para monitorar
2. Criar um template customizado para aplicações .NET
3. Configurar trigger para alertar quando disco > 85%
4. Criar dashboard com métricas de CPU, memória e disco
5. (Desafio) Integrar Zabbix com Slack para notificações
