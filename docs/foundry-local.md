# Factory IQ en mode Foundry Local

Factory IQ peut exécuter ses cinq agents avec **Foundry Local**, le runtime Microsoft
qui exécute un modèle directement sur le poste ou la passerelle edge du client.
L'inférence, les prompts et les réponses restent locaux. Azure n'est pas requis
pour ce mode après le téléchargement initial du modèle et des composants nécessaires.

## Foundry Local

Foundry Local fournit :

- un catalogue curaté de modèles optimisés pour CPU, GPU et NPU ;
- la détection automatique du matériel et de l'accélérateur disponible ;
- le téléchargement et la mise en cache des modèles ;
- un SDK .NET et une API de chat compatible avec les formats OpenAI ;
- un fonctionnement offline une fois le modèle mis en cache.

Le catalogue et le CLI Foundry Local sont susceptibles d'évoluer. Consulter la
documentation officielle pour les prérequis, les alias disponibles et les licences :

- [Présentation de Foundry Local](https://learn.microsoft.com/en-us/azure/foundry-local/what-is-foundry-local)
- [Guide de démarrage](https://learn.microsoft.com/en-us/azure/foundry-local/get-started)
- [Référence du SDK](https://learn.microsoft.com/en-us/azure/foundry-local/reference/reference-sdk-current)
- [Référence du CLI](https://learn.microsoft.com/en-us/azure/foundry-local/reference/reference-cli)

Installer et vérifier Foundry Local selon le système d'exploitation du poste :

```text
Windows : winget install Microsoft.FoundryLocal
macOS   : brew tap microsoft/foundrylocal && brew install foundrylocal

foundry --version
foundry model list --filter task=chat-completion
```

## Agents locaux

Les cinq rôles de Factory IQ sont disponibles en mode local :

- Operations ;
- Maintenance ;
- Quality ;
- Plant Manager ;
- Continuous Improvement.

Les profils métier sont partagés avec le mode cloud, mais les instructions locales
interdisent d'inventer des mesures, alarmes, KPI, ordres de travail ou historiques.
En mode local, les outils IQ distants (Fabric Data Agent, Foundry IQ, Work IQ et
Web IQ) ne sont pas configurés.

Les contrats et points d'extension des outils locaux sont dans :

```text
src/foundry-agents/shared/FactoryIQ.Agents.Shared/Local/Tools/
```

## Configuration

Le mode cloud reste le mode par défaut :

```text
AI_RUNTIME=cloud
MODEL_DEPLOYMENT_NAME=gpt-4o
```

Pour utiliser Foundry Local :

```text
AI_RUNTIME=local
MODEL_DEPLOYMENT_NAME_LOCAL=phi-4-mini
```

`MODEL_DEPLOYMENT_NAME_LOCAL` est un alias du catalogue Foundry Local. Ce n'est
pas un déploiement Azure OpenAI et il ne remplace pas `MODEL_DEPLOYMENT_NAME`.

Au premier démarrage, l'application :

1. initialise Foundry Local ;
2. télécharge les composants d'exécution nécessaires ;
3. télécharge Phi-4 si l'alias n'est pas présent dans le cache ;
4. charge le modèle ;
5. réutilise le cache aux démarrages suivants.

La valeur par défaut peut être remplacée si l'alias n'est pas disponible sur le
matériel ou dans la version installée de Foundry Local. Vérifier l'alias et sa
licence avec `foundry model list` et `foundry model info <alias> --license`.

Exemple Windows :

```powershell
$env:AI_RUNTIME = "local"
$env:MODEL_DEPLOYMENT_NAME_LOCAL = "phi-4-mini"
dotnet run --project src/foundry-agents/agents/FactoryIQ.Agents.Operations
```

## Outils à implémenter par le client

Les outils locaux sont volontairement des coquilles. Ils ne contiennent aucune
donnée de démonstration et ne simulent aucune machine.

### MQTT

`Local/Tools/Mqtt/MqttMachineDataTool.cs` est le point d'intégration pour un
broker MQTT local. Le client doit définir les topics, l'authentification, le
format des messages, la rétention et la normalisation ISA-95.

### OPC UA

`Local/Tools/OpcUa/OpcUaMachineDataTool.cs` est le point d'intégration pour un
serveur OPC UA exposé par le PLC, le SCADA ou l'historian. Le client doit définir
les endpoints, certificats, NodeIds, unités, qualité des mesures et politiques
de reconnexion.

### Fichiers locaux

`Local/Tools/Files/LocalFileDataTool.cs` est le point d'intégration pour les
exports réels du client (JSON, CSV, Parquet ou documents de procédures). Le
client doit définir le répertoire, les schémas, les règles de validation, les
permissions et la stratégie de rotation.

Les contrats métier sont centralisés dans `Local/Tools/Contracts/` :

- état d'équipement ;
- alarmes ;
- télémétrie ;
- synthèse de performance ;
- documents locaux.

Les méthodes non implémentées lèvent explicitement une erreur. Elles ne
retournent jamais une valeur par défaut présentée comme une donnée industrielle.
Les outils devront être exposés aux agents avec des opérations métier contrôlées,
et non avec un accès libre aux protocoles ou à une base de données.

## Déploiement dans une usine

Un déploiement type place Factory IQ sur un PC industriel ou une passerelle edge :

```text
Machines / PLC / SCADA
          |
   Broker MQTT / serveur OPC UA / exports
          |
   Adaptateurs locaux Factory IQ
          |
       Cinq agents
          |
      Foundry Local
```

Le client doit prévoir :

- une machine compatible avec Phi-4 et l'accélérateur retenu ;
- un préchargement du modèle avant l'isolement du réseau ;
- une connectivité réseau vers les sources industrielles autorisées ;
- des comptes techniques et certificats dédiés ;
- une segmentation entre réseau industriel, edge et postes utilisateurs ;
- un stockage protégé pour le cache, les configurations et les journaux ;
- une procédure de mise à jour des agents, des outils et du modèle ;
- une supervision locale et une procédure de diagnostic ;
- une stratégie de déploiement par usine, ligne ou zone.

Foundry Local est orienté vers l'inférence locale pour un utilisateur ou un
poste. Pour plusieurs utilisateurs ou une capacité centralisée, le client devra
évaluer une architecture de serving dédiée plutôt que de transformer Foundry
Local en serveur multi-utilisateurs.
