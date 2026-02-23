# EasySave - Release Notes v1.1

**Date de publication :** 11 février 2026  
**Version :** 1.1.0  
**Société :** ProSoft

---

## Contexte

La version 1.1 d'EasySave est une mise à jour mineure qui répond à une demande client spécifique. Elle maintient l'intégralité des fonctionnalités de la version 1.0 et ajoute le support du format XML pour les fichiers de journalisation.

---

## Nouveautés

### Support du format XML

* Possibilité de générer les logs au format XML en plus du format JSON existant
* Configuration du format via le menu de l'application console
* Le format sélectionné s'applique à tous les fichiers de log journaliers
* Le fichier d'état (`state.json`) reste au format JSON

### Détails techniques

* Extension de la bibliothèque `EasyLog.dll` avec la classe `XmlFormatter`
* Implémentation basée sur le pattern Strategy déjà présent en v1.0
* Ajout d'un paramètre dans le fichier de configuration pour spécifier le format
* Structure XML identique à celle du JSON (horodatage, nom du travail, chemins UNC, taille, temps de transfert)
* Indentation configurée pour une lecture facile dans Notepad

---

## Rétrocompatibilité

* Déploiement direct sans migration de données ni reconfiguration
* Format JSON par défaut si aucun format n'est spécifié
* Les logs générés par la v1.0 restent lisibles et exploitables
* Les fichiers `jobs.json` et `state.json` conservent leur structure et emplacement

---

## Ce qui ne change pas

### Interface et fonctionnalités

* Application console basée sur .NET 8.0 (pas d'interface graphique)
* Limite de cinq travaux de sauvegarde simultanés
* Choix du format via le menu interactif ou modification manuelle du fichier de configuration

### Fonctionnalités non incluses

* Cryptage (prévu en v2.0)
* Détection des logiciels métiers (prévu en v2.0)
* Sauvegarde parallèle (prévu en v3.0)

---

## Configuration requise

Aucun changement par rapport à la v1.0 :
* Windows 10 ou version ultérieure
* .NET 8.0 Runtime
* Aucune dépendance supplémentaire

---

## Installation

La version 1.1 peut être installée directement par-dessus la version 1.0. Elle s'adresse spécifiquement aux organisations nécessitant une intégration avec des systèmes de traitement de logs au format XML.

---

## Support technique

**ProSoft Support**  
Email : support@prosoft.fr  
Horaires : 8h - 17h, du lundi au vendredi

Les contrats de maintenance v1.0 couvrent automatiquement la mise à jour vers v1.1.

---

**ProSoft - Février 2026**
