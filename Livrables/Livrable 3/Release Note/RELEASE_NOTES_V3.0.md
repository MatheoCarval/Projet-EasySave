# EasySave - Release Notes v3.0

**Date de publication :** 26 février 2026
**Version :** 3.0.0
**Société :** ProSoft

---

## Contexte

La version 3.0 d'EasySave constitue une refonte en profondeur du moteur de sauvegarde. L'interface graphique introduite en v2.0 est conservée et enrichie. Cette version apporte la sauvegarde parallèle, le contrôle temps réel des travaux, la gestion des priorités, la limitation de la bande passante, le renforcement du mode mono-instance de CryptoSoft et un service de centralisation des logs via Docker.

---

## Nouveautés majeures

### Sauvegarde parallèle

* Abandon du mode séquentiel : tous les travaux de sauvegarde s'exécutent désormais en parallèle
* Chaque travail tourne dans son propre thread géré par le BackupManager
* Amélioration significative des temps de sauvegarde sur des sources multiples

### Gestion des fichiers prioritaires

* Définition d'une liste d'extensions prioritaires dans les paramètres généraux
* Tant qu'un fichier d'extension prioritaire est en attente sur l'un des travaux actifs, aucun fichier non prioritaire ne peut être transféré sur aucun autre travail
* Les extensions prioritaires sont configurables par l'utilisateur à tout moment
* La règle de priorité s'applique de façon transversale à l'ensemble des travaux en cours

### Limitation des transferts simultanés de fichiers volumineux

* Seuil de taille configurable par l'utilisateur (en Ko) dans les paramètres généraux
* Un seul fichier dont la taille dépasse ce seuil peut être transféré à la fois, tous travaux confondus
* Pendant ce transfert, les autres travaux peuvent continuer à transférer des fichiers de taille inférieure, sous réserve de la règle des fichiers prioritaires
* Évite la saturation de la bande passante réseau sur les environnements partagés

### Contrôle temps réel des travaux (Pause / Play / Stop)

* Chaque travail dispose désormais de trois boutons d'action individuels dans l'interface :
  * **Pause** : mise en pause effective après la fin du transfert du fichier en cours
  * **Play** : démarrage ou reprise depuis une pause
  * **Stop** : arrêt immédiat du travail et de l'opération en cours
* Actions groupées disponibles pour piloter l'ensemble des travaux en une seule action

### Suivi de progression en temps réel

* Barre de progression et pourcentage d'avancement affichés pour chaque travail
* Mise à jour en continu pendant l'exécution
* Affichage du fichier en cours de transfert (source et destination)
* Les états `Active`, `Paused`, `Completed`, `Error` et `Pending` sont reflétés instantanément dans l'interface

### Pause automatique lors de la détection d'un logiciel métier

* Si un logiciel métier configuré est détecté en cours d'exécution, tous les transferts actifs sont mis en pause immédiatement
* Les travaux reprennent automatiquement dès que le logiciel métier est arrêté
* La liste des logiciels métiers à surveiller est configurable dans les paramètres généraux (identique au comportement de blocage introduit en v2.0, désormais étendu à l'arrêt des transferts en cours)

### CryptoSoft en mode mono-instance

* CryptoSoft ne peut plus s'exécuter en parallèle sur un même ordinateur, quelle que soit le nombre de travaux actifs
* Un verrou applicatif (`SemaphoreSlim`) garantit qu'un seul processus CryptoSoft chiffre à la fois
* Les autres travaux se mettent en attente de façon transparente puis reprennent dès que CryptoSoft est disponible
* Suppression des risques de corruption liés à des exécutions concurrentes

### Centralisation des logs via Docker

* Un service Docker de centralisation des logs journaliers est disponible pour les déploiements multi-serveurs
* Trois modes de stockage configurables par l'utilisateur :
  * Logs uniquement sur le serveur Docker (centralisé)
  * Logs uniquement sur le poste local de chaque utilisateur
  * Logs sur le poste local **et** sur le serveur Docker simultanément
* Un seul fichier journalier est maintenu côté Docker, quel que soit le nombre de machines connectées
* Chaque entrée de log identifie l'utilisateur et la machine source pour faciliter l'audit

---

## Modifications techniques

### Architecture

* Introduction du multithreading dans le `BackupManager` : un `Task` par travail de sauvegarde
* Mécanisme de synchronisation inter-travaux pour les règles de priorité et de bande passante (verrous partagés, `SemaphoreSlim`)
* Extension du modèle `BackupJob` avec les méthodes `MarkAsPaused()`, `MarkAsResumed()`, et la propriété `Progress`
* `CryptageManager` : verrou statique `_cryptosoftLock` pour l'accès exclusif à CryptoSoft
* Mise à jour du `StateWriter` pour refléter les nouveaux états en temps réel

### Configuration

* Nouvelles entrées dans les paramètres généraux :
  * Liste des extensions prioritaires
  * Seuil de taille maximale pour les transferts simultanés (Ko)
  * Mode de stockage des logs (local / Docker / les deux)
  * Adresse du service Docker de centralisation

### Journalisation

* Les entrées de log incluent désormais l'identifiant utilisateur et le nom de machine pour la centralisation
* Le temps de chiffrement CryptoSoft est toujours consigné par fichier (introduit en v2.0, maintenu en v3.0)
* Formats JSON et XML conservés

---

## Compatibilité

### Migration

* Installation directe par-dessus la v2.0
* Les fichiers `jobs.json` et `state.json` existants sont repris sans modification
* Les paramètres v2.0 sont conservés ; les nouvelles options apparaissent avec leurs valeurs par défaut

### Fichiers compatibles

* Configuration des travaux (`jobs.json`)
* État en temps réel (`state.json`)
* Fichiers de logs journaliers (JSON/XML)

---

## Tableau comparatif des versions

| Fonction | v1.0 | v1.1 | v2.0 | v3.0 |
|---|---|---|---|---|
| Interface | Console | Console | Graphique | Graphique |
| Multi-langues | FR / EN | FR / EN | FR / EN | FR / EN |
| Travaux de sauvegarde | Limité à 5 | Limité à 5 | Illimité | Illimité |
| Fichier log journalier | JSON | JSON, XML | JSON, XML | JSON, XML |
| Temps de chiffrement dans les logs | Non | Non | Oui | Oui |
| Pause / Play / Stop par travail | Non | Non | Non | Oui |
| Fichier état | Oui | Oui | Oui | Oui |
| Mode de sauvegarde | Séquentiel | Séquentiel | Séquentiel | Parallèle |
| Arrêt si logiciel métier | Non | Non | Blocage au lancement | Arrêt des transferts en cours |
| Cryptage CryptoSoft | Non | Non | Oui | Oui (mono-instance) |
| Fichiers prioritaires | Non | Non | Non | Oui |
| Limitation transferts fichiers volumineux | Non | Non | Non | Oui |
| Centralisation logs Docker | Non | Non | Non | Oui |
| Ligne de commande | Oui | Oui | Oui | Oui |

---

## Configuration requise

* Windows 10 ou version ultérieure
* .NET 8.0 Runtime
* Résolution d'écran minimale : 1280x720 pixels
* Docker Desktop (optionnel, uniquement pour la centralisation des logs)
* Espace disque : 200 Mo (hors logs et données sauvegardées)

---

## Installation et migration

### Procédure

* Installation directe par-dessus v2.0
* Aucune reconfiguration manuelle nécessaire
* Les nouvelles options de paramétrage sont disponibles immédiatement dans l'interface

### Recommandation

* Conserver une sauvegarde du répertoire `AppData\Roaming\EasySave` avant la mise à jour
* Tester le seuil de taille des fichiers volumineux sur votre environnement réseau avant de le fixer définitivement

---

## Support technique

**ProSoft Support**
Email : support@prosoft.fr
Horaires : 8h - 17h, du lundi au vendredi

Les contrats de maintenance v2.0 couvrent automatiquement la mise à jour vers v3.0.

---

**ProSoft - Février 2026**
