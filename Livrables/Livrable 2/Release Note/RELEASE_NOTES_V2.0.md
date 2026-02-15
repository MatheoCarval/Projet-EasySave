# EasySave - Release Notes v2.0

**Date de publication :** 11 février 2026  
**Version :** 2.0.0  
**Société :** ProSoft

---

## Contexte

La version 2.0 d'EasySave représente une évolution majeure du logiciel. Cette version abandonne l'interface en ligne de commande au profit d'une interface graphique développée sous .NET MAUI. Le moteur de sauvegarde demeure inchangé pour garantir la continuité avec les versions précédentes. Cette version supprime également la limitation du nombre de travaux de sauvegarde et intègre le cryptage CryptoSoft ainsi que la détection des logiciels métiers.

---

## Nouveautés majeures

### Interface graphique moderne

* Remplacement complet de l'application console par une interface graphique
* Architecture MVVM (Model-View-ViewModel) pour une structure modulaire
* Page d'accueil avec affichage des travaux sous forme de cartes visuelles
* Statuts disponibles : Pending, Active, Completed, Paused, Error

### Gestion des travaux

* Nombre illimité de travaux de sauvegarde (suppression de la limite de 5)
* Sélection d'un ou plusieurs travaux via des cases à cocher
* Option de sélection globale pour tous les travaux
* Actions groupées : exécution ou suppression des travaux sélectionnés
* Boutons d'action dans la barre supérieure de l'interface

### Recherche et filtrage

* Barre de recherche intégrée pour localiser rapidement un travail
* Filtrage en temps réel à mesure de la saisie
* Filtre par type : sauvegardes complètes ou différentielles
* Filtre par statut : concentration sur les travaux dans un état spécifique (ex: erreurs uniquement)

### Création de travaux

* Bouton d'ajout (symbole +) donnant accès au formulaire de création
* Formulaire modal avec quatre champs obligatoires :
  * Nom du travail
  * Chemin(s) source (nombre illimité de répertoires par tâche)
  * Chemin de destination
  * Type de sauvegarde (complète ou différentielle)
* Support des disques locaux, périphériques externes et partages réseau (format UNC)
* Boutons de sélection de dossiers pour faciliter la navigation

### Paramètres

* Menu centralisé accessible via icône d'engrenage
* **Apparence** : Basculement entre mode clair et mode sombre (application immédiate)
* **Langue** : Sélection entre français et anglais (changement instantané)
* **Journalisation** :
  * Choix du format de log : JSON ou XML
  * Configuration du chemin du répertoire des logs
  * Configuration du chemin du fichier d'état
  * Chemins par défaut dans AppData mais modifiables
* **Applications bloquées** : Gestion des logiciels métiers à bloquer pendant les sauvegardes

### Cryptage CryptoSoft

* Intégration du cryptage des fichiers via CryptoSoft
* Configuration du cryptage dans les paramètres
* Sélection des extensions de fichiers à crypter

### Détection des logiciels métiers

* Détection automatique des applications définies comme bloquantes
* Blocage des sauvegardes si un logiciel métier est en cours d'exécution
* Liste personnalisable des applications à surveiller

### Module de journalisation

* Accès via bouton dédié
* Liste chronologique de tous les travaux de sauvegarde exécutés
* Informations affichées : nom du travail, statut final, horodatage, nombre de fichiers traités
* Clic sur une entrée pour afficher le panneau détaillé des opérations
* Facilite le diagnostic des erreurs et comportements inattendus

### Centre d'aide intégré

* Accès via bouton symbolisé par un point d'interrogation
* Documentation complète des fonctionnalités :
  * Prise en main de l'interface
  * Types de sauvegarde (complète vs différentielle)
  * Gestion des sources multiples
  * Ordre d'exécution des travaux
  * Utilisation de la recherche et des filtres
  * Configuration des paramètres
  * Consultation de l'historique
  * Conseils et bonnes pratiques

---

## Modifications techniques

### Architecture

* Restructuration selon le pattern MVVM
* Séparation claire entre logique de présentation et logique métier
* ViewModels exposant les données et commandes pour l'interface
* Conservation de la logique de sauvegarde héritée de v1.0
* Testabilité améliorée : tests des ViewModels indépendamment de l'interface
* Évolutivité garantie : remplacement possible de l'interface sans affecter le code métier

### Bibliothèques

* `EasyLog.dll` inchangée par rapport à la v1.1
* Support des formats JSON et XML selon la configuration

---

## Compatibilité

### Migration automatique

* Reprise automatique des configurations de v1.0 ou v1.1
* Fichier `jobs.json` : structure et emplacement conservés
* Premier lancement : chargement et affichage des travaux existants dans la nouvelle interface
* Logs existants accessibles via le module de journalisation
* Fichier `state.json` : structure maintenue pour compatibilité avec applications tierces

### Fichiers compatibles

* Configuration des travaux (`jobs.json`)
* État en temps réel (`state.json`)
* Fichiers de logs journaliers (JSON/XML)

---

## Limitations et fonctionnalités supprimées

### Ligne de commande

* Les arguments `EasySave.exe 1-3` et `EasySave.exe 1;3` ne sont plus supportés
* Réintégration possible dans une future mise à jour selon les retours utilisateurs

### Limitations maintenues

* Exécution séquentielle des sauvegardes (pas de parallélisme)

### Fonctionnalités non incluses

* Gestion des fichiers prioritaires (prévu en v3.0)
* Sauvegarde parallèle (prévu en v3.0)
* Contrôles Play/Pause/Stop individuels (prévu en v3.0)
* Centralisation des logs via Docker (prévu en v3.0)

---

## Configuration requise

* Windows 10 ou version ultérieure
* .NET 8.0 Runtime
* Résolution d'écran minimale : 1280x720 pixels
* Espace disque : 200 Mo (hors logs et données sauvegardées)

---

## Installation et migration

### Procédure

* Installation directe par-dessus v1.0 ou v1.1
* Aucune manipulation manuelle des fichiers de configuration
* Reprise automatique des travaux configurés

### Recommandation

* Conserver une sauvegarde du répertoire `AppData\Roaming\EasySave` avant la mise à jour
* Cette sauvegarde permettra une restauration en cas de besoin

---

## Roadmap

### Version 2.1

* Améliorations de l'interface utilisateur
* Optimisations de performance
* Corrections de bugs mineurs

### Version 3.0

* Gestion des fichiers prioritaires
* Exécution parallèle des travaux
* Contrôles Play/Pause/Stop individuels
* Centralisation des logs via service Docker
* Gestion de la bande passante

---

## Support technique

**ProSoft Support**  
Email : support@prosoft.fr  
Horaires : 8h - 17h, du lundi au vendredi

Les contrats de maintenance couvrent la mise à jour vers v2.0 et les futures mises à jour mineures.

---

**ProSoft - Février 2026**
