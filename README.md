# EasySave

Application de sauvegarde de fichiers avec interface graphique (Avalonia), interface console et exécution automatisée en ligne de commande.

## Fonctionnalités

- **Interface graphique (GUI)** : Interface Avalonia moderne avec thèmes clair/sombre
- **Interface console** : Menu interactif pour une gestion en terminal
- **Mode automatisé (CLI)** : Exécution des sauvegardes en ligne de commande
- **Sauvegarde complète ou différentielle** : Copie intégrale ou uniquement les fichiers modifiés
- **Chiffrement optionnel** : Chiffrement des fichiers via un outil externe CryptoSoft configurable
- **Logs structurés** : Fichiers de log quotidiens au format JSON ou XML
- **Suivi d'état en temps réel** : Progression fichier par fichier avec état persistant
- **Blocage d'applications** : Empêche l'exécution de sauvegardes si certaines applications sont en cours
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

Vous pouvez :
- Créer, modifier et supprimer des tâches de sauvegarde
- Exécuter des sauvegardes avec suivi de progression en temps réel
- Visualiser les logs
- Configurer la langue, le format de log, le chiffrement et les applications bloquées

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
│   └── Enums/                    # BackupState, BackupType, LogFormat
│
├── ViewModels/                   # ViewModels (pattern MVVM)
│   ├── ViewModelBase.cs          # Classe de base (INotifyPropertyChanged)
│   ├── MainViewModel.cs          # ViewModel principal
│   ├── BackupJobViewModel.cs     # Gestion des tâches
│   ├── ProgressViewModel.cs      # Suivi de progression
│   ├── SettingsViewModel.cs      # Paramètres
│   ├── SourcePathViewModel.cs    # Sélection des chemins source
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
│   ├── Managers/
│   │   ├── BackupManager.cs      # Gestion des sauvegardes (CRUD + exécution)
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
│   ├── Abstractions/             # Interface ILogger
│   ├── Enums/                    # LogFormat (JSON, XML)
│   ├── Exceptions/               # Exceptions spécifiques au logging
│   ├── Formatters/               # Formatage des entrées de log
│   └── Loggers/                  # DailyJsonLogger, DailyXmlLogger
│
└── EasySave.Tests/               # Tests unitaires (xUnit + Moq)
```

## Fichiers de données (`%AppData%\EasySave\`)

Toutes les données utilisateur sont stockées dans le dossier **`%AppData%\EasySave\`** :

| Fichier | Description |
|---|---|
| `Config.json` | Configuration de l'application (langue, format de log, chemins, chiffrement, thème, applications bloquées) |
| `jobs.json` | Définition des tâches de sauvegarde |
| `state.json` | État courant de chaque tâche (progression, fichiers restants, etc.) |
| `logs/` | Dossier contenant les fichiers de log quotidiens (JSON ou XML selon la configuration) |

> **Migration automatique** : Si un ancien fichier `Datas/jobs.json` existe à côté de l'exécutable (ancien emplacement), il est automatiquement copié vers `%AppData%\EasySave\jobs.json` au premier lancement.

## Configuration

La configuration est gérée dans `%AppData%\EasySave\Config.json` et peut être modifiée via l'interface (GUI ou console).

| Paramètre | Description | Valeur par défaut |
|---|---|---|
| Langue | Langue de l'interface (`fr`, `en`) | `fr` |
| Format de log | Format des fichiers de log (`JSON`, `XML`) | `JSON` |
| Chemin des logs | Répertoire de stockage des logs | `%AppData%\EasySave\logs\` |
| Chemin du state | Fichier d'état des sauvegardes | `%AppData%\EasySave\state.json` |
| Chemin CryptoSoft | Chemin vers l'exécutable de chiffrement | *(vide — chiffrement désactivé)* |
| Clé publique CryptoSoft | Clé publique pour le chiffrement | *(vide)* |
| Extensions chiffrées | Extensions de fichiers à chiffrer (ex: `.txt`, `.docx`) | *(vide)* |
| Applications bloquées | Applications empêchant l'exécution des sauvegardes | *(vide)* |
| Thème sombre | Activer le thème sombre dans la GUI | `false` |

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
| `PAUSED` | Suspendue temporairement |
| `COMPLETED` | Terminée avec succès |
| `ERROR` | Erreur rencontrée lors de l'exécution |

## Chiffrement

Le chiffrement est optionnel et repose sur un exécutable externe **CryptoSoft** :

1. Configurer le chemin vers CryptoSoft dans les paramètres
2. Fournir la clé publique de chiffrement
3. Définir les extensions de fichiers à chiffrer (ex: `.txt`, `.pdf`, `.docx`)
4. Activer le chiffrement sur les tâches de sauvegarde concernées

Les fichiers correspondants sont chiffrés après copie vers la destination.

## Dépannage

### L'application ne se lance pas

Vérifiez les logs d'erreur dans `%AppData%\EasySave\error.log` et `%AppData%\EasySave\crash.log`.

### Aucune sauvegarde trouvée

Créez au moins une sauvegarde via l'interface avant d'utiliser le mode CLI.

### Erreur d'accès aux fichiers

Vérifiez les permissions de lecture/écriture sur les chemins source et destination.

### Sauvegarde bloquée

Si des applications bloquées sont configurées et en cours d'exécution, la sauvegarde sera refusée. Fermez ces applications ou modifiez la liste dans les paramètres.


