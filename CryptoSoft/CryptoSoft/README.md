# CryptoSoft

Outil CLI .NET 8 de chiffrement hybride asymétrique pour sauvegardes sécurisées.

**RSA-4096 (OAEP-SHA256) + AES-256-GCM** — aucun mot de passe, clés PEM uniquement.

## Démarrage rapide

```bash
# Build
cd CryptoSoft
dotnet build -c Release

# Générer une paire de clés RSA-4096
dotnet run -- keygen --private private.key --public public.key

# Chiffrer un fichier
dotnet run -- encrypt --input backup.zip --output backup.enc --pubkey public.key

# Déchiffrer un fichier
dotnet run -- decrypt --input backup.enc --output backup.zip --privkey private.key

# Inspecter un fichier chiffré
dotnet run -- info --input backup.enc
```

## Fonctionnalités

- Chiffrement hybride RSA-OAEP + AES-256-GCM
- Streaming par chunks 64 KiB (supporte fichiers > 10 GB)
- Support fichiers, dossiers (compression auto), stdin/stdout
- API .NET publique pour intégration programmatique
- Effacement sécurisé des clés en mémoire
- Format de fichier versionné et extensible
- Aucune interaction utilisateur requise

## Documentation

Voir [DOCUMENTATION.md](DOCUMENTATION.md) pour la documentation complète incluant :
- Architecture et modules
- Guide d'intégration .NET
- Étude de sécurité et modèle de menace
- Benchmarks et performance
- Gestion des clés et bonnes pratiques
