# EasySave

Application de sauvegarde de fichiers en ligne de commande avec interface interactive et exécution automatisée.
A la racine du projet, vous trouverez un fichier "EasySave.exe" qui est l'exécutable de l'application. Vous pouvez le lancer directement pour accéder à l'interface interactive ou utiliser les commandes en ligne pour exécuter des sauvegardes spécifiques. Une explication détaillée de l'utilisation de l'application est disponible dans la section "Guide d'utilisation" ci-dessous.

## Fonctionnalités

- **Créer et gérer des sauvegardes** : Configurez des tâches de sauvegarde complètes ou différentielles
- **Interface interactive** : Menu console intuitif pour une gestion facile
- **Mode automatisé** : Exécutez les sauvegardes en ligne de commande
- **Logs structurés** : Fichiers de log en JSON 
- **Support multilingue** : Interface en français et anglais

## Installation & Configuration

### Prérequis

- .NET 8.0 ou supérieur
- Windows (ou compatible avec .NET)

### Compilation

```bash
cd Projet-EasySave\src
dotnet build
```

### Exécution

```bash
dotnet run --project EasySave.csproj
```

ou directement avec l'exécutable:

```bash
EasySave.exe
```

## Guide d'utilisation

### Mode interactif

Lance l'interface console interactive par défaut:

```bash
EasySave.exe
```

Vous pouvez:
- Créer une nouvelle sauvegarde
- Voir la liste de vos sauvegardes
- Modifier ou supprimer des sauvegardes
- Exécuter des sauvegardes
- Accéder aux paramètres

### Mode automatisé

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
Exécute les sauvegardes 1 et 3 (ou 1, 3 et 5 pour le second exemple) dans l'ordre. (Notez les guillemets pour éviter les problèmes d'interprétation des points-virgules par le shell.)

##  Structure du projet

```
src/
├── Directory.Build.props         # Propriétés communes MSBuild
├── EasySave.csproj               # Projet principal
├── EasySave.slnx                 # Solution
├── Program.cs                    # Point d'entrée
├── Datas/                        # Données embarquées
│   └── Languages.json            # Traductions
├── EasyLog/                      # Bibliothèque de logging
│   ├── Abstractions/             # Interfaces
│   ├── Enums/                    # Énumérations
│   ├── Exceptions/               # Exceptions spécifiques
│   ├── Formatters/               # Formatage des logs
│   └── Loggers/                  # Implémentations de loggers
├── EasySave.Tests/               # Tests
│   └── ...
├── Exceptions/                   # Exceptions applicatives
├── Models/                       # Modèles de données
│   ├── Entries/                  # Entrées (state/logs)
│   └── Enums/                    # Énumérations (BackupState, BackupType, etc.)
├── Services/                     # Services métier
│   ├── FileTransferService.cs    # Gestion des transferts de fichiers
│   ├── LocalizationService.cs    # Gestion de la localisation
│   ├── Managers/                 # Gestionnaires (config, backups)
│   └── Writers/                  # Écriture de l'état
├── Utilities/                    # Utilitaires divers
├── View/                         # Interface utilisateur
│   └── Console/                  # UI console
│       ├── Components/
│       └── Screens/
└── publish/                      # Artefacts de publication
```

## Fichiers de configuration et données

- **`Datas/jobs.json`** : Stockage des sauvegardes créées
- **`Datas/Languages.json`** : Traductions de l'interface (embarqué en ressource)
- **`%AppData%\EasySave\Config.json`** : Configuration (langue, format de log, chemins)
- **`%AppData%\EasySave\state.json`** : État courant des sauvegardes
- **`%AppData%\EasySave\jobs.json`** ou **`%AppData%\EasySave\jobs.xml`** : Fichiers de logs selon le format choisi

## Types de sauvegarde

- **COMPLETE** : Copie complète de tous les fichiers
- **DIFFERENTIAL** : Copie uniquement des fichiers modifiés depuis la dernière sauvegarde

##  Dépannage

### Aucune sauvegarde trouvée

Assurez-vous d'avoir créé au moins une sauvegarde via le mode interactif avant d'utiliser les modes automatisés.

### Erreur d'accès aux fichiers

Vérifiez que vous avez les permissions de lecture/écriture sur les chemins source et destination.

### Fichier jobs.json non trouvé

Le fichier `Datas/jobs.json` est créé automatiquement lors de la première exécution. Si le dossier `Datas` n'existe pas, il sera créé.

