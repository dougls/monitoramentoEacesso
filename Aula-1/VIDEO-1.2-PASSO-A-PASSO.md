# 🎬 Vídeo 1.2 - Configuração Inicial e Coleta de Dados

**Aula**: 1 - Zabbix no Kubernetes  
**Vídeo**: 1.2  
**Temas**: Finalizar deploy; Configuração web; Conceitos; Coleta de dados  
**Tempo estimado**: 20 minutos

---

## 🚀 Parte 1: Deploy Completo no EKS (10 min)

### Passo 1: Deploy PostgreSQL

```bash
cd ~/monitoramentoEacesso/Aula-1/kubernetes

# Aplicar Secret
kubectl apply -f postgres-secret.yaml -n monitoring

# Aplicar Deployment (usando emptyDir para Learner Lab)
kubectl apply -f postgres-deployment.yaml -n monitoring

# Aguardar Ready
kubectl wait --for=condition=ready pod -l app=postgres -n monitoring --timeout=300s

# Verificar
kubectl get pods -n monitoring -l app=postgres
```

### Passo 2: Deploy Zabbix Server

```bash
# Aplicar deployment (Zabbix cria schema automaticamente na primeira execução)
kubectl apply -f zabbix-server-deployment.yaml -n monitoring

# Aguardar Ready (pode demorar 2-3 min na primeira vez)
kubectl wait --for=condition=ready pod -l app=zabbix-server -n monitoring --timeout=600s

# Ver logs do Zabbix Server
kubectl logs -n monitoring deployment/zabbix-server --tail=30

# Deve mostrar:
# - "Table 'zabbix.dbversion' already exists" (criou schema)
# - "server #0 started [main process]" (servidor rodando)
```

### Passo 3: Deploy Zabbix Web (NodePort 30080)

```bash
# Aplicar deployment (já configurado com NodePort 30080)
kubectl apply -f zabbix-web-deployment.yaml -n monitoring

# Aguardar Ready
kubectl wait --for=condition=ready pod -l app=zabbix-web -n monitoring --timeout=300s

# Verificar serviço
kubectl get svc -n monitoring zabbix-web
```

### Passo 4: Deploy Zabbix Agent (DaemonSet)

```bash
# Aplicar DaemonSet
kubectl apply -f zabbix-agent-daemonset.yaml -n monitoring

# Verificar agents (1 por node)
kubectl get pods -n monitoring -l app=zabbix-agent

# Aguardar agents prontos
kubectl wait --for=condition=ready pod -l app=zabbix-agent -n monitoring --timeout=120s
```

---

## 🌐 Parte 2: Configuração Inicial Web (5 min)

### Passo 5: Acessar Zabbix Web

```bash
# Obter IP do node
kubectl get nodes -o wide

# Acessar: http://<NODE_IP>:30080
# Ou port-forward:
kubectl port-forward svc/zabbix-web 8080:80 -n monitoring &
open http://localhost:8080
```

### Passo 6: Login e Tour

**Login:**
- Usuário: `Admin`
- Senha: `zabbix`

### Passo 7: Tour Inicial + Conceitos

**No Zabbix Web:**
- **Dashboard**: Visão geral do sistema
- **Monitoring**: Dados em tempo real
- **Configuration**: Configurações e templates

**Conceitos importantes:**
- **HOST**: Dispositivo monitorado (servidor, switch, etc)
- **ITEM**: Métrica específica (CPU, memória, disco)
- **TRIGGER**: Condição que gera alerta (CPU > 80%)
- **TEMPLATE**: Conjunto de items/triggers pré-configurados

---

## 🔍 Parte 3: Preparar para Configuração (5 min)

### Passo 6: Verificar Agents

```bash
# Ver agents rodando nos nodes
kubectl get pods -n monitoring -l app=zabbix-agent -o wide

# Ver IPs dos nodes para próxima aula
kubectl get nodes -o wide
```

**Próxima aula:** Configuração completa de hosts e coleta!

---

**Duração**: ~20 minutos  
**Próximo**: VIDEO-1.3-PASSO-A-PASSO.md
