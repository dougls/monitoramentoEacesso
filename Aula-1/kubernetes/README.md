# Stack Zabbix no Kubernetes - Aula 1

## 📋 Arquivos da Stack

### Configuração Base
- **namespace.yaml**: Namespace `monitoring` para isolar recursos
- **postgres-secret.yaml**: Credenciais do PostgreSQL (usuário, senha, database)

### Banco de Dados
- **postgres-deployment.yaml**: PostgreSQL 15 Alpine + Service
  - Usa `emptyDir` para AWS Learner Lab (dados temporários)
  - Para produção: descomentar PVC no arquivo
- **postgres-pvc.yaml**: PVC para produção (NÃO usar no Learner Lab)

### Zabbix Server
- **zabbix-server-deployment.yaml**: Zabbix Server + Service
  - **Schema automático**: A imagem `zabbix/zabbix-server-pgsql:alpine-6.4-latest` cria o schema automaticamente na primeira execução
  - **Sem InitContainers necessários**: Deployment simplificado

### Zabbix Web
- **zabbix-web-deployment.yaml**: Interface Web + NodePort 30080
  - Acesso: `http://<NODE_IP>:30080`
  - Login padrão: `Admin` / `zabbix`

### Zabbix Agent
- **zabbix-agent-daemonset.yaml**: Agent em cada node do cluster
  - `hostNetwork: true` e `privileged: true` para coletar métricas do host
  - 1 pod por node (DaemonSet)

## 🚀 Ordem de Deploy

```bash
# 1. Namespace (se não existir)
kubectl apply -f namespace.yaml

# 2. PostgreSQL
kubectl apply -f postgres-secret.yaml -n monitoring
kubectl apply -f postgres-deployment.yaml -n monitoring
kubectl wait --for=condition=ready pod -l app=postgres -n monitoring --timeout=300s

# 3. Zabbix Server (cria schema automaticamente)
kubectl apply -f zabbix-server-deployment.yaml -n monitoring
kubectl wait --for=condition=ready pod -l app=zabbix-server -n monitoring --timeout=600s

# Ver logs (deve mostrar criação do schema)
kubectl logs -n monitoring deployment/zabbix-server --tail=30

# 4. Zabbix Web
kubectl apply -f zabbix-web-deployment.yaml -n monitoring
kubectl wait --for=condition=ready pod -l app=zabbix-web -n monitoring --timeout=300s

# 5. Zabbix Agent
kubectl apply -f zabbix-agent-daemonset.yaml -n monitoring
kubectl wait --for=condition=ready pod -l app=zabbix-agent -n monitoring --timeout=120s
```

## 🔍 Verificação e Troubleshooting

### Ver logs do Zabbix Server
```bash
kubectl logs -n monitoring deployment/zabbix-server --tail=50
# Deve mostrar: "server #0 started [main process]"
```

### Verificar todos os pods
```bash
kubectl get pods -n monitoring
```

### Acessar Zabbix Web
```bash
# Obter IP do node
kubectl get nodes -o wide

# Acessar: http://<NODE_IP>:30080
# Login: Admin / zabbix
```

## ⚠️ Importante - AWS Learner Lab

- **NÃO usar PVC**: O arquivo `postgres-pvc.yaml` está comentado
- **Dados temporários**: Com `emptyDir`, dados são perdidos ao reiniciar o pod do PostgreSQL
- **Para produção**: Descomentar PVC no `postgres-deployment.yaml` e aplicar `postgres-pvc.yaml`

## 🎯 Sobre a Inicialização do Schema

A imagem `zabbix/zabbix-server-pgsql:alpine-6.4-latest` **cria o schema automaticamente** na primeira execução quando detecta que o database existe mas está vazio.

Nos logs você verá:
```
** Database 'zabbix' already exists. Please be careful with database owner!
** Table 'zabbix.dbversion' already exists.
```

Isso significa que o Zabbix criou todas as tabelas necessárias automaticamente. **Não é necessário usar InitContainers ou Jobs separados.**

## 📊 Recursos Alocados

| Componente | CPU Request | CPU Limit | Memory Request | Memory Limit |
|------------|-------------|-----------|----------------|--------------|
| PostgreSQL | 250m | 500m | 256Mi | 512Mi |
| Zabbix Server | 500m | 1000m | 512Mi | 1Gi |
| Zabbix Web | 250m | 500m | 256Mi | 512Mi |
| Zabbix Agent | 100m | 200m | 64Mi | 128Mi |

**Total por node (2 nodes):**
- CPU: ~1.1 cores
- Memory: ~1.2 GB

**Compatível com:** 2x t3.medium (2 vCPU, 4GB RAM cada)
