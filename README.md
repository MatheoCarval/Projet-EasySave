# EasySave

Application de sauvegarde de fichiers avec interface graphique (Avalonia), interface console et exécution automatisée en ligne de commande.

## Fonctionnalités

- **Interface graphique (GUI)** : Interface Avalonia moderne avec thèmes clair/sombre et couleur d'accent personnalisable
- **Interface console** : Menu interactif pour une gestion en terminal
- **Mode automatisé (CLI)** : Exécution des sauvegardes en ligne de commande
- **Sauvegarde complète ou différentielle** : Copie intégrale ou uniquement les fichiers modifiés
- **Exécution parallèle** : Plusieurs sauvegardes simultanées via `Task.WhenAll` avec gestion thread-safe des ressources partagées
- **Pause / Reprise** : Suspension et reprise individuelle de chaque sauvegarde en cours (manuelle ou automatique)
- **Throttle de transfert** : Limite configurable de données transférées simultanément (valeur + unité KB/MB/GB/TB)
- **Extensions prioritaires** : Liste ordonnée d'extensions traitées en premier dans chaque sauvegarde (ex : `.pdf` avant `.jpg`)
- **Chiffrement optionnel** : Chiffrement des fichiers via un outil externe CryptoSoft configurable
- **Blocage d'applications** : Empêche l'exécution de sauvegardes si certaines applications sont en cours d'exécution — mise en pause automatique si détectées pendant une sauvegarde
- **Détection automatique des processus** : Bouton de détection des applications actives pour les ajouter facilement à la liste de blocage
- **Logs structurés** : Fichiers de log quotidiens avec rotation automatique (`jobs_YYYY-MM-DD.json/xml`)
- **Logs distants** : Envoi des logs vers une API distante (CryptoSoft Manager) avec repli transparent sur les logs locaux en cas d'indisponibilité
- **Suivi d'état en temps réel** : Progression fichier par fichier avec état persistant
- **Support multilingue** : Interface en français et anglais
- **Architecture MVVM** : Séparation propre entre la vue, les ViewModels et les services

## Prérequis

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) ou supérieur
- Windows (x64) — compatible Linux/macOS via .NET mais les dépendances natives (SkiaSharp/HarfBuzz) sont configurées pour Windows

## Installation & Exécution

### Compilation

```bash
cd Projet-EasySave\src
dotnet build
```

### Lancement (GUI par défaut)

```bash
dotnet run --project EasySave.csproj
```

ou directement avec l'exécutable :

```bash
EasySave.exe
```

## Guide d'utilisation

### Mode GUI (par défaut)

Lancez l'application sans argument pour ouvrir l'interface graphique Avalonia :

```bash
EasySave.exe
```

Options de thème au démarrage :

```bash
EasySave.exe --light    # Force le thème clair
EasySave.exe --dark     # Force le thème sombre
```

Vous pouvez :
- Créer, modifier et supprimer des tâches de sauvegarde
- Exécuter plusieurs sauvegardes en parallèle avec suivi de progression en temps réel
- Mettre en pause et reprendre des sauvegardes individuellement
- Visualiser les logs (locaux)
- Configurer la langue, le format de log, le chiffrement, les applications bloquées, les extensions prioritaires et le throttle de transfert

### Mode automatisé (CLI)

Exécutez les sauvegardes directement en ligne de commande.

#### Exécuter une sauvegarde

```bash
EasySave.exe 1
```
Exécute la sauvegarde numéro 1.

#### Exécuter une plage de sauvegardes

```bash
EasySave.exe 1-3
```
Exécute les sauvegardes 1, 2 et 3 dans l'ordre.

#### Exécuter plusieurs sauvegardes spécifiques

```bash
EasySave.exe "1;3"
EasySave.exe "1;3;5"
```
Exécute les sauvegardes sélectionnées dans l'ordre. (Guillemets nécessaires pour les points-virgules.)

## Structure du projet

```
src/
├── Program.cs                    # Point d'entrée
├── EasySave.csproj               # Projet principal (.NET 8.0, WinExe)
├── EasySave.slnx                 # Solution
├── Directory.Build.props         # Propriétés communes MSBuild
│
├── Models/                       # Modèles de données
│   ├── BackupJob.cs              # Définition d'une tâche de sauvegarde
│   ├── Configuration.cs          # Modèle de configuration
│   ├── Entries/                  # Entrées (state/logs)
│   └── Enums/                    # BackupState, BackupType, LogFormat, LogStorageMode
│
├── ViewModels/                   # ViewModels (pattern MVVM)
│   ├── ViewModelBase.cs          # Classe de base (INotifyPropertyChanged)
│   ├── MainViewModel.cs          # ViewModel principal
│   ├── BackupJobViewModel.cs     # Gestion des tâches
│   ├── SettingsViewModel.cs      # Paramètres
│   └── LogsVisualizerViewModel.cs # Visualisation des logs
│
├── View/                         # Interfaces utilisateur
│   ├── GUI/                      # Interface graphique Avalonia
│   │   ├── App.axaml             # Application Avalonia
│   │   ├── MainWindow.axaml      # Fenêtre principale
│   │   ├── LogsVisualizerWindow.axaml # Visualiseur de logs
│   │   └── GUILauncher.cs        # Lanceur GUI
│   └── Console/                  # Interface console
│       ├── ConsoleUI.cs          # UI console principale
│       ├── Screens/              # Écrans (menu, exécution, suppression, paramètres)
│       └── Components/           # Composants réutilisables
│
├── Services/                     # Services métier
│   ├── FileTransferService.cs    # Transfert de fichiers avec suivi de progression
│   ├── LocalizationService.cs    # Gestion multilingue (fr, en)
│   ├── RemoteLogger.cs           # Logger vers API distante (avec repli local)
│   ├── RemoteLogClient.cs        # Client de lecture des logs distants
│   ├── Managers/
│   │   ├── BackupManager.cs      # Gestion des sauvegardes (CRUD + exécution parallèle + pause/reprise)
│   │   ├── ConfigurationManager.cs # Configuration (singleton, thread-safe)
│   │   └── CryptageManager.cs    # Chiffrement via CryptoSoft externe
│   └── Writers/
│       └── StateWriter.cs        # Écriture de l'état des sauvegardes
│
├── Utilities/                    # Utilitaires
│   ├── FileSystemHelper.cs       # Helpers système de fichiers
│   └── PathValidator.cs          # Validation des chemins
│
├── Exceptions/                   # Exceptions applicatives
│   └── FileTransferException.cs
│
├── Datas/                        # Données embarquées
│   └── Languages.json            # Traductions (ressource embarquée)
│
├── EasyLog/                      # Bibliothèque de logging (projet séparé)
│   ├── Abstractions/             # Interface ILogger (générique : Log<T>, ReadLog<T>, Flush)
│   ├── Enums/                    # LogFormat (JSON, XML)
│   ├── Exceptions/               # Exceptions spécifiques au logging
│   ├── Formatters/               # Formatage des entrées de log
│   └── Loggers/                  # DailyJsonLogger, DailyXmlLogger (rotation quotidienne)
│
└── EasySave.Tests/               # Tests unitaires (xUnit + Moq)
```

## Fichiers de données (`%AppData%\EasySave\`)

Toutes les données utilisateur sont stockées dans le dossier **`%AppData%\EasySave\`** :

| Fichier | Description |
|---|---|
| `Config.json` | Configuration de l'application (langue, format de log, chemins, chiffrement, thème, couleur d'accent, applications bloquées, extensions prioritaires, throttle) |
| `jobs.json` | Définition des tâches de sauvegarde |
| `state.json` | État courant de chaque tâche (progression, fichiers restants, etc.) |
| `logs/` | Dossier contenant les fichiers de log quotidiens (`jobs_YYYY-MM-DD.json` ou `.xml`) |
| `crash.log` | Log des exceptions non gérées au démarrage |
| `error.log` | Log des erreurs d'initialisation |

> **Migration automatique** : Si un ancien fichier `Datas/jobs.json` existe à côté de l'exécutable (ancien emplacement), il est automatiquement copié vers `%AppData%\EasySave\jobs.json` au premier lancement.

## Configuration

La configuration est gérée dans `%AppData%\EasySave\Config.json` et peut être modifiée via l'interface (GUI ou console).

| Paramètre | Description | Valeur par défaut |
|---|---|---|
| Langue | Langue de l'interface (`fr-FR`, `en-US`) | `fr-FR` |
| Format de log | Format des fichiers de log (`JSON`, `XML`) | `JSON` |
| Chemin des logs | Répertoire ou fichier de stockage des logs (rotation quotidienne automatique) | `%AppData%\EasySave\logs\` |
| Chemin du state | Fichier d'état des sauvegardes | `%AppData%\EasySave\state.json` |
| Chemin CryptoSoft | Chemin vers l'exécutable de chiffrement | *(vide — chiffrement désactivé)* |
| Clé publique CryptoSoft | Clé publique pour le chiffrement | *(vide)* |
| Extensions chiffrées | Extensions de fichiers à chiffrer (ex: `.txt`, `.docx`) | *(vide)* |
| Extensions prioritaires | Extensions traitées en premier dans une sauvegarde (ordre personnalisable) | *(vide)* |
| Taille max transfert parallèle | Limite de données transférées simultanément (0 = illimité) | `0 GB` |
| Applications bloquées | Applications empêchant l'exécution des sauvegardes | *(vide)* |
| Thème sombre | Activer le thème sombre dans la GUI | `false` |
| Couleur d'accent | Couleur principale de l'interface (Blue, Purple, Green, Orange, Red, Teal) | `Blue` |

## Types de sauvegarde

| Type | Description |
|---|---|
| **COMPLETE** | Copie intégrale de tous les fichiers source vers la destination |
| **DIFFERENTIAL** | Copie uniquement les fichiers modifiés depuis la dernière sauvegarde complète |

## États d'une sauvegarde

| État | Description |
|---|---|
| `PENDING` | Tâche créée, en attente d'exécution |
| `ACTIVE` | Exécution en cours |
| `PAUSED` | Suspendue temporairement (manuelle ou automatique) |
| `COMPLETED` | Terminée avec succès |
| `ERROR` | Erreur rencontrée lors de l'exécution |

## Parallélisme

Plusieurs sauvegardes peuvent s'exécuter simultanément. Le parallélisme est géré par :

- **`Task.WhenAll`** : chaque job est lancé dans un thread dédié via `Task.Run`
- **`PauseToken`** (`ManualResetEventSlim`) : pause/reprise sans polling — le thread est suspendu jusqu'au signal de reprise
- **`TransferThrottle`** (`Monitor.Wait/PulseAll`) : garantit que le volume de données transférées simultanément ne dépasse pas le seuil configuré
- **Verrous** (`lock`, `Interlocked`) : protection des structures partagées (liste de jobs, fichier d'état)

Quand une application bloquée est détectée pendant une sauvegarde, tous les jobs actifs sont automatiquement mis en pause jusqu'à la fermeture de l'application.

## Chiffrement

Le chiffrement est optionnel et repose sur un exécutable externe **CryptoSoft** :

1. Configurer le chemin vers CryptoSoft dans les paramètres
2. Fournir la clé publique de chiffrement
3. Définir les extensions de fichiers à chiffrer (ex: `.txt`, `.pdf`, `.docx`)
4. Activer le chiffrement sur les tâches de sauvegarde concernées

Les fichiers correspondants sont chiffrés après copie vers la destination.

## Logs distants

EasySave peut envoyer les logs vers une API distante (CryptoSoft Manager) :

- **`RemoteLogger`** : décore un `ILogger` local — envoie chaque entrée en arrière-plan via une file bornée (Channel), se replie silencieusement sur le logger local en cas d'échec réseau
- **`RemoteLogClient`** : lecture des logs stockés côté serveur via `GET /api/logs`
- **Enrôlement** : enregistrement de l'agent avec une clé d'enrôlement unique pour obtenir une clé API Bearer

## Dépannage

### L'application ne se lance pas

Vérifiez les logs d'erreur dans `%AppData%\EasySave\error.log` et `%AppData%\EasySave\crash.log`.

### Aucune sauvegarde trouvée

Créez au moins une sauvegarde via l'interface avant d'utiliser le mode CLI.

### Erreur d'accès aux fichiers

Vérifiez les permissions de lecture/écriture sur les chemins source et destination.

### Sauvegarde bloquée

Si des applications bloquées sont configurées et en cours d'exécution, la sauvegarde sera refusée au démarrage ou mise en pause automatiquement si détectée pendant l'exécution. Fermez ces applications ou modifiez la liste dans les paramètres.

### Sauvegarde qui ne démarre pas malgré aucune app bloquée

Vérifiez que le throttle de transfert (`Taille max transfert parallèle`) n'est pas trop bas. Une valeur de `0` signifie illimité.
