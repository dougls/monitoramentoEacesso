# 🎬 Vídeo 1.3 - Templates, Triggers e Visualização

**Aula**: 1 - Zabbix no Kubernetes  
**Vídeo**: 1.3  
**Temas**: Templates; Triggers; Simulação; Dashboards; Limpeza  

---

## 📊 Parte 1: Criar Primeiro Host

### Passo 1: Obter IP do Node

```bash
# Ver IPs dos nodes (não dos pods agents)
kubectl get nodes -o wide
# Anotar o INTERNAL-IP do primeiro node
```

### Passo 2: Criar Host

**No Zabbix Web:**
1. **Configuration → Hosts**
2. **Create host**

**Configuração:**
```
Host name: k8s-node-1
Visible name: Kubernetes Node 1
Groups: Linux servers
Interface: Agent, IP: <INTERNAL-IP_DO_NODE>, Port: 10050
Templates: Linux by Zabbix agent
```

**IMPORTANTE**: Use o IP do NODE, não do pod agent!

3. **Add**

### Passo 3: Verificar Conectividade do Agent

```bash
# Verificar se agent está rodando
kubectl get pods -n monitoring -l app=zabbix-agent

# Se der erro de acesso, redeployar agent com nova configuração
kubectl delete -f zabbix-agent-daemonset.yaml -n monitoring
kubectl apply -f zabbix-agent-daemonset.yaml -n monitoring
kubectl wait --for=condition=ready pod -l app=zabbix-agent -n monitoring --timeout=120s

# Testar conectividade do Zabbix Server para o Agent
kubectl exec -n monitoring deployment/zabbix-server -- \
  zabbix_get -s <INTERNAL-IP_DO_NODE> -k system.uname

# Se funcionar, deve retornar informações do sistema
```

### Passo 4: Verificar Coleta

```bash
# Aguardar 2 minutos para coleta
sleep 120
```

**No Zabbix Web:**
1. **Monitoring → Latest data**
2. Host: `k8s-node-1`
3. Ver métricas coletadas:
   - CPU utilization
   - Memory usage
   - Disk space
   - Network traffic

**Se não aparecer dados:**
- Verificar se host está "Available" (verde)
- Configuration → Hosts → k8s-node-1 → ver status

---

## 🚨 Parte 2: Triggers

### Passo 5: Ver Triggers Existentes

**No Zabbix Web:**
1. **Configuration → Hosts**
2. Clicar em **k8s-node-1**
3. Aba **Triggers**

**Triggers do template:**
- High CPU utilization
- Lack of available memory
- Disk space is low

### Passo 6: Criar Trigger Customizada

**No Zabbix Web:**
1. **Configuration → Hosts → k8s-node-1 → Triggers**
2. **Create trigger**

**Configuração:**
```
Name: CPU muito alta
Severity: Warning
Expression: avg(/k8s-node-1/system.cpu.util,5m)>80
```

### Passo 7: Simular Problema

```bash
# OPCIONAL: Instalar metrics-server (apenas para usar kubectl top)
# O Zabbix NÃO precisa do metrics-server, ele coleta métricas diretamente do OS
kubectl apply -f https://github.com/kubernetes-sigs/metrics-server/releases/latest/download/components.yaml

# Gerar carga de CPU no node
kubectl run stress-test --image=polinux/stress -n monitoring -- \
  stress --cpu 4 --timeout 600s

# Verificar pod criado
kubectl get pods -n monitoring stress-test

# OPCIONAL: Ver uso de CPU com kubectl (requer metrics-server)
# kubectl top nodes
# kubectl top pods -n monitoring

# Aguardar 5-6 minutos para o Zabbix detectar
sleep 360
```

**No Zabbix Web:**
1. **Monitoring → Problems**
2. Ver problema: "CPU muito alta" (ou trigger do template)
3. Ver gráfico de CPU aumentando

### Passo 8: Resolver Problema

```bash
# Deletar pod de stress
kubectl delete pod stress-test -n monitoring

# Aguardar problema desaparecer
```

---

## 📊 Parte 3: Dashboard

### Passo 9: Criar Dashboard

**No Zabbix Web:**
1. **Monitoring → Dashboards**
2. **Create dashboard**
3. Nome: `Monitoramento Kubernetes`

### Passo 10: Adicionar Widgets

**Widget 1 - CPU:**
1. **Add widget → Graph (classic)**
2. Host: k8s-node-1, Graph: CPU utilization

**Widget 2 - Memória:**
1. **Add widget → Graph (classic)**
2. Host: k8s-node-1, Graph: Memory utilization

**Widget 3 - Problemas:**
1. **Add widget → Problems**
2. Host groups: Linux servers

### Passo 11: Salvar Dashboard

1. Organizar widgets
2. **Save changes**

---

## 🧹 Parte 4: Limpeza

### Passo 12: Deletar Recursos

```bash
# Deletar namespace (remove todos os recursos)
kubectl delete namespace monitoring

# Aguardar conclusão
kubectl wait --for=delete namespace/monitoring --timeout=300s
```

### Passo 13: Deletar Cluster EKS

```bash
# Deletar node group
aws eks delete-nodegroup \
  --cluster-name monitoring-lab \
  --nodegroup-name workers \
  --region us-east-1 \
  --profile fiapaws

# Aguardar
aws eks wait nodegroup-deleted \
  --cluster-name monitoring-lab \
  --nodegroup-name workers \
  --region us-east-1 \
  --profile fiapaws

# Deletar cluster
aws eks delete-cluster \
  --name monitoring-lab \
  --region us-east-1 \
  --profile fiapaws

# Aguardar
aws eks wait cluster-deleted \
  --name monitoring-lab \
  --region us-east-1 \
  --profile fiapaws
```

---

**FIM DA AULA 1**
