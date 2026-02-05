# EasySave

Application de sauvegarde de fichiers en ligne de commande avec interface interactive et support d'exécution automatisée.

## Fonctionnalités

- **Créer et gérer des sauvegardes** : Configurez des tâches de sauvegarde complètes ou différentielles
- **Interface interactive** : Menu console intuitif pour une gestion facile
- **Mode automatisé** : Exécutez les sauvegardes en ligne de commande
- **Logs structurés** : Fichiers de log en JSON avec horodatage
- **Support multilingue** : Interface en français (extensible à d'autres langues)

## Installation & Configuration

### Prérequis

- .NET 8.0 ou supérieur
- Windows (ou compatible avec .NET)

### Compilation

```bash
cd Projet-EasySave
dotnet build
```

### Exécution

```bash
dotnet run
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

### Mode `-s` ou `-show`

Affiche la liste des sauvegardes numérotées avec leurs détails:

```bash
EasySave.exe -s
EasySave.exe -show
```

**Exemple de sortie:**
```
=== Liste des sauvegardes ===

1. Backup Documents
   Type: COMPLETE
   Source: C:\Users\User\Documents
   Destination: D:\Backups\Documents
   État: PENDING

2. Backup Photos
   Type: DIFFERENTIAL
   Source: C:\Users\User\Pictures
   Destination: D:\Backups\Photos
   État: PENDING
```

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
EasySave.exe 1;3
EasySave.exe 1;3;5
```
Exécute les sauvegardes 1 et 3 (ou 1, 3 et 5 pour le second exemple) dans l'ordre.

##  Structure du projet

```
EasySave/
├── Program.cs                    # Point d'entrée
├── Models/                       # Modèles de données
│   ├── BackupJob.cs             # Classe de tâche de sauvegarde
│   └── Enums/                   # Énumérations (BackupState, BackupType, etc.)
├── Services/                    # Services métier
│   ├── FileTransferService.cs   # Gestion des transferts de fichiers
│   ├── LocalizationService.cs   # Gestion de la localisation
│   ├── Managers/
│   │   └── BackupManager.cs     # Gestion des sauvegardes
│   └── Writers/
│       └── StateWriter.cs       # Écriture de l'état
├── View/                        # Interface utilisateur
│   └── Console/
│       ├── ConsoleUI.cs         # Interface principale
│       ├── Components/          # Composants UI
│       ├── Screens/             # Différents écrans
│       └── Helpers/             # Utilitaires UI
├── EasyLog/                     # Bibliothèque de logging
│   ├── Loggers/                 # Implémentations de loggers
│   ├── Formatters/              # Formatage des logs
│   └── Abstractions/            # Interfaces
├── Utilities/                   # Utilitaires divers
└── Datas/                       # Données (languages.json, jobs.json)
```

## Fichiers de configuration

- **`Datas/jobs.json`** : Stockage des sauvegardes créées
- **`Datas/Languages.json`** : Traductions de l'interface
- **`state.json`** : État actuel des sauvegardes
- **`logs-DD-MM-YYYY.json`** : Fichiers de log horodatés

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

