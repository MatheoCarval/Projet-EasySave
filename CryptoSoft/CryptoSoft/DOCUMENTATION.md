# CryptoSoft – Documentation complète

## Table des matières

1. [Vue d'ensemble](#1-vue-densemble)
2. [Architecture](#2-architecture)
3. [Guide d'utilisation CLI](#3-guide-dutilisation-cli)
4. [Intégration .NET](#4-intégration-net)
5. [Format du fichier chiffré](#5-format-du-fichier-chiffré)
6. [Étude de sécurité](#6-étude-de-sécurité)
7. [Performance](#7-performance)
8. [Gestion des clés](#8-gestion-des-clés)

---

## 1. Vue d'ensemble

**CryptoSoft** est un outil CLI .NET 8 de chiffrement hybride asymétrique conçu pour sécuriser des sauvegardes automatisées. Il n'utilise **aucun mot de passe** — uniquement des clés RSA-4096 au format PEM.

### Schéma cryptographique

```
┌─────────────────────────────────────────────────────────────────┐
│                    CHIFFREMENT HYBRIDE                          │
│                                                                 │
│  1. Génération clé AES-256 aléatoire (session key)              │
│  2. Chiffrement données avec AES-256-GCM (par chunks 64 KiB)    │
│  3. Chiffrement clé session avec RSA-OAEP-SHA256 (4096 bits)    │
│  4. Stockage: [Header + Wrapped Key + Chunks chiffrés]          │
│                                                                 │
│  Clé publique  → chiffrement (encrypt)                          │
│  Clé privée    → déchiffrement (decrypt)                        │
└─────────────────────────────────────────────────────────────────┘
```

### Principes de sécurité

| Principe | Implémentation |
|----------|----------------|
| Pas de mot de passe | Chiffrement asymétrique RSA-4096 uniquement |
| Confidentialité | AES-256-GCM (chiffrement authentifié) |
| Intégrité | Tag GCM 128 bits par chunk |
| Anti-relecture | Nonce unique par chunk (base nonce ⊕ counter) |
| Clé éphémère | Nouvelle clé AES-256 par fichier |
| Effacement mémoire | `CryptographicOperations.ZeroMemory()` sur toutes les clés |
| Aléa cryptographique | `System.Security.Cryptography.RandomNumberGenerator` |

---

## 2. Architecture

```
CryptoSoft/
├── Program.cs              # Point d'entrée CLI
├── Constants.cs            # Constantes cryptographiques
├── SecureMemory.cs         # Effacement sécurisé des buffers
├── KeyManagement.cs        # Génération / chargement clés RSA-4096
├── EnvelopeEncryption.cs   # AES-256-GCM + RSA-OAEP wrapping
├── CryptoEngine.cs         # Moteur de chiffrement streaming par chunks
├── CryptoFileHeader.cs     # Format binaire du fichier .enc
├── FileStreamProcessor.cs  # Opérations fichier / dossier / stdin-stdout
├── CliCommands.cs          # Commandes CLI (keygen, encrypt, decrypt, info)
```

### Modules

| Module | Responsabilité |
|--------|----------------|
| `KeyManagement` | Génère des paires RSA-4096, charge des clés PEM (PKCS#1, PKCS#8, SPKI), supporte fichier/stdin/env |
| `EnvelopeEncryption` | Chiffrement/déchiffrement AES-256-GCM, wrapping/unwrapping RSA-OAEP-SHA256 |
| `CryptoEngine` | Streaming par chunks 64 KiB avec nonce dérivé par chunk |
| `CryptoFileHeader` | Sérialisation/désérialisation du header binaire versionné |
| `FileStreamProcessor` | Orchestration fichiers, dossiers compressés, stdin/stdout |
| `CryptoSoftApi` | Façade API publique pour intégration programmatique |

---

## 3. Guide d'utilisation CLI

### Installation

```bash
# Depuis les sources
cd CryptoSoft
dotnet build -c Release
# L'exécutable est dans bin/Release/net8.0/cryptosoft

# Ou exécution directe
dotnet run -- <commande>
```

### Génération de clés

```bash
cryptosoft keygen --private private.key --public public.key
```

Génère une paire RSA-4096 au format PEM. Le fichier de clé privée est automatiquement restreint en permissions (600 sur Unix).

### Chiffrement d'un fichier

```bash
cryptosoft encrypt --input backup.zip --output backup.enc --pubkey public.key
```

### Déchiffrement d'un fichier

```bash
cryptosoft decrypt --input backup.enc --output backup.zip --privkey private.key
```

### Chiffrement d'un dossier

```bash
# Le dossier est compressé (gzip) puis chiffré
cryptosoft encrypt --input /path/to/directory --output backup_dir.enc --pubkey public.key

# Déchiffrement vers un dossier
cryptosoft decrypt --input backup_dir.enc --output /path/to/restore --privkey private.key --directory
```

### Intégration pipeline (stdin/stdout)

```bash
# Pipe depuis un autre processus
tar czf - /data | cryptosoft encrypt --pubkey public.key --output backup.enc

# Pipe complet
cat backup.enc | cryptosoft decrypt --privkey private.key --output restored.tar.gz

# stdin → stdout
cat data.bin | cryptosoft encrypt --pubkey public.key > data.enc
cat data.enc | cryptosoft decrypt --privkey private.key > data.bin
```

### Inspection d'un fichier chiffré

```bash
cryptosoft info --input backup.enc
```

Affiche la version, l'algorithme, la taille du header, etc. sans nécessiter la clé privée.

### Clé via variable d'environnement

```bash
export CRYPTOSOFT_PUBKEY=$(cat public.key)
# L'API .NET peut lire via KeyManagement.ReadKeyFromEnvironment("CRYPTOSOFT_PUBKEY")
```

---

## 4. Intégration .NET

### Référence projet

Ajoutez une référence au projet CryptoSoft :

```xml
<ProjectReference Include="../CryptoSoft/CryptoSoft.csproj" />
```

### API simplifiée

```csharp
using CryptoSoft;

// ── Génération de clés ──
CryptoSoftApi.GenerateKeyPair("private.key", "public.key");

// ── Chiffrement fichier ──
CryptoSoftApi.EncryptFile("backup.zip", "backup.enc", "public.key");

// ── Déchiffrement fichier ──
CryptoSoftApi.DecryptFile("backup.enc", "backup.zip", "private.key");

// ── Chiffrement dossier ──
CryptoSoftApi.EncryptDirectory("/data/backups", "backups.enc", "public.key");

// ── Déchiffrement dossier ──
CryptoSoftApi.DecryptDirectory("backups.enc", "/data/restored", "private.key");
```

### API streaming (pour gros fichiers ou intégration personnalisée)

```csharp
using CryptoSoft;
using System.Security.Cryptography;

// Chargement des clés
using RSA pubKey = KeyManagement.LoadPublicKey("public.key");
using RSA privKey = KeyManagement.LoadPrivateKey("private.key");

// Chiffrement streaming
using var inputStream = File.OpenRead("large_backup.tar.gz");
using var outputStream = File.Create("large_backup.enc");
CryptoSoftApi.Encrypt(inputStream, outputStream, pubKey);

// Déchiffrement streaming
using var encStream = File.OpenRead("large_backup.enc");
using var decStream = File.Create("large_backup.tar.gz");
CryptoSoftApi.Decrypt(encStream, decStream, privKey);
```

### Chargement de clé depuis une chaîne PEM

```csharp
string pemKey = Environment.GetEnvironmentVariable("MY_PUBLIC_KEY")!;
CryptoSoftApi.Encrypt(inputStream, outputStream, pemKey);
```

---

## 5. Format du fichier chiffré

### Header binaire

```
Offset  Taille    Champ                    Encodage
──────  ────────  ───────────────────────  ────────────
0       4         Magic bytes "CSFT"       ASCII
4       2         Version du format        uint16 LE
6       1         Identifiant algorithme   byte
7       2         Longueur clé wrappée     uint16 LE
9       12        Nonce / IV (96 bits)     raw bytes
21      N         Clé session wrappée      raw bytes (N = RSA output size, 512 pour RSA-4096)
```

### Chunks chiffrés

Après le header, les données sont divisées en chunks :

```
[4 bytes taille ciphertext (LE)] [ciphertext] [16 bytes GCM tag]
```

- Chaque chunk fait au maximum 64 KiB de plaintext
- Le nonce par chunk est dérivé : `base_nonce ⊕ chunk_counter` (LE dans les 4 derniers octets)
- Un chunk de taille 0 signale la fin du flux

### Algorithmes supportés

| ID   | Algorithme |
|------|-----------|
| 0x01 | RSA-OAEP-SHA256 + AES-256-GCM |

Le format est versionné et extensible : de nouveaux algorithmes peuvent être ajoutés dans des versions futures.

---

## 6. Étude de sécurité

### 6.1 Justification du choix RSA-OAEP

**Pourquoi RSA-OAEP-SHA256 plutôt que X25519 (ECDH) ?**

| Critère | RSA-OAEP-SHA256 | X25519 + HKDF |
|---------|-----------------|---------------|
| Maturité | Standard depuis 20+ ans | Plus récent (2014) |
| Support .NET natif | ✅ Complet | ⚠️ Nécessite Diffie-Hellman éphémère |
| Interopérabilité | Universel (OpenSSL, Java, Go…) | Bon mais moins répandu |
| Taille clé | 4096 bits (512 octets) | 256 bits (32 octets) |
| Performance | Plus lent (acceptable pour wrapping) | Plus rapide |
| Résistance post-quantique | Aucune (comme X25519) | Aucune |
| Simplicité d'implémentation | ✅ API directe .NET | ⚠️ Pattern DH éphémère requis |

**Choix retenu : RSA-4096 OAEP-SHA256** pour :
- Simplicité et robustesse de l'implémentation
- API .NET native sans dépendance externe
- Interopérabilité maximale
- Le wrapping RSA ne concerne que 32 octets (clé AES) → l'overhead de performance est négligeable

### 6.2 Modèle de menace pour sauvegardes

```
┌────────────────────────────────────────────────────────────────┐
│                    MODÈLE DE MENACE                            │
├────────────────────────────────────────────────────────────────┤
│ Attaquant possède : accès aux fichiers .enc (sauvegarde)      │
│ Attaquant ne possède pas : la clé privée RSA                  │
├────────────────────────────────────────────────────────────────┤
│ Menace              │ Protection                              │
│─────────────────────│─────────────────────────────────────────│
│ Lecture données     │ AES-256-GCM (confidentialité)           │
│ Altération fichier  │ GCM tag 128 bits par chunk              │
│ Relecture (replay)  │ Nonce unique par fichier + par chunk    │
│ Substitution clé    │ RSA-OAEP lie la clé session au pubkey   │
│ Vol clé en mémoire  │ SecureZero après utilisation            │
│ Brute force AES     │ 2^256 possibilités (infaisable)        │
│ Brute force RSA     │ 4096 bits ≈ 140+ bits de sécurité      │
│ Fichier tronqué     │ Détection via chunks manquants/EOS     │
│ Path traversal      │ Vérification GetFullPath avant extract  │
└────────────────────────────────────────────────────────────────┘
```

### 6.3 Protections implémentées

1. **Anti-relecture** : chaque fichier utilise un nonce aléatoire unique (96 bits). Chaque chunk dérive un nonce unique via XOR avec un compteur monotone.

2. **Anti-altération** : AES-256-GCM produit un tag d'authentification 128 bits par chunk. Toute modification est détectée avant écriture du plaintext.

3. **Anti-substitution de clé** : la clé AES session est liée au destinataire via RSA-OAEP. Un attaquant ne peut pas re-chiffrer la clé session avec une autre clé publique sans connaître la clé session.

4. **Effacement mémoire** : tous les buffers contenant des clés ou du plaintext sont effacés via `CryptographicOperations.ZeroMemory()` dans des blocs `finally`.

5. **Refus du déchiffrement invalide** : si le tag GCM ne correspond pas, une `CryptographicException` est levée et aucun plaintext n'est écrit.

6. **Path traversal** : lors de l'extraction de dossiers, les chemins sont validés via `Path.GetFullPath()` pour empêcher l'écriture hors du répertoire cible.

### 6.4 Limitations connues

- **Pas de protection post-quantique** : RSA et AES sont vulnérables aux ordinateurs quantiques (algorithmes de Shor/Grover). Envisager une migration vers des algorithmes post-quantiques (ML-KEM/Kyber) dans une version future.
- **Pas de forward secrecy** : si la clé privée RSA est compromise, tous les fichiers chiffrés avec la clé publique correspondante sont compromis. Mitigation : rotation régulière des clés.
- **Strings .NET** : les chaînes PEM sont immutables ; l'effacement via `unsafe` est un best-effort.

---

## 7. Performance

### 7.1 Caractéristiques

| Paramètre | Valeur |
|-----------|--------|
| Taille chunk streaming | 64 KiB |
| Buffer I/O | 80 KiB |
| Overhead par chunk | 20 octets (4 len + 16 tag) |
| Overhead header | ~533 octets (avec RSA-4096 wrapped key) |

### 7.2 Overhead du chiffrement hybride

- **Overhead fixe par fichier** : ~533 octets (header)
- **Overhead par 64 KiB** : 20 octets = **0.03%**
- **Overhead total** sur un fichier de 1 GiB : ~320 KiB = **0.03%**
- **Opération RSA** : unique par fichier (wrapping ~5-10 ms)
- **AES-256-GCM** : utilise les instructions AES-NI du CPU → débit natif proche du hardware

### 7.3 Benchmark attendu (estimations)

| Taille fichier | Temps chiffrement* | Temps déchiffrement* |
|---------------|-------------------|---------------------|
| 1 KiB | < 10 ms | < 10 ms |
| 1 MiB | < 20 ms | < 20 ms |
| 100 MiB | ~200 ms | ~200 ms |
| 1 GiB | ~2 s | ~2 s |
| 10 GiB | ~20 s | ~20 s |

*\* Sur CPU moderne avec AES-NI, SSD NVMe. Le bottleneck est l'I/O disque.*

### 7.4 Utilisation mémoire

- **Mémoire constante** : ~200 KiB quel que soit la taille du fichier
  - 64 KiB buffer plaintext
  - 64 KiB buffer ciphertext
  - Structures et overhead GC
- **Pas de chargement complet en mémoire** : streaming chunk par chunk
- **Exception** : le mode dossier utilise un fichier temporaire (compressé) avant chiffrement

---

## 8. Gestion des clés

### 8.1 Bonnes pratiques

1. **Stockage clé privée** :
   - Fichier avec permissions `600` (owner-only)
   - Chiffré au repos (LUKS, FileVault, BitLocker)
   - Idéalement dans un HSM ou coffre-fort (Azure Key Vault, HashiCorp Vault)

2. **Stockage clé publique** :
   - Peut être distribuée librement
   - Vérifier l'authenticité via un canal sûr (fingerprint)

3. **Rotation des clés** :
   - Générer une nouvelle paire périodiquement (ex: tous les 6 mois)
   - Re-chiffrer les sauvegardes actives avec la nouvelle clé
   - Conserver les anciennes clés privées pour les archives

4. **Injection sécurisée** :
   ```bash
   # Via variable d'environnement (CI/CD)
   export CRYPTOSOFT_PRIVKEY="$(cat private.key)"

   # Via stdin
   cat private.key | cryptosoft decrypt --input backup.enc --output backup.zip --privkey /dev/stdin

   # Via fichier temporaire en mémoire (Linux)
   cp private.key /dev/shm/tmp_key && cryptosoft decrypt ... --privkey /dev/shm/tmp_key && rm /dev/shm/tmp_key
   ```

5. **Ne jamais** :
   - Logger le contenu d'une clé
   - Stocker une clé privée dans un dépôt Git
   - Transmettre une clé privée par email

### 8.2 Scénario d'intégration automatisée

```
┌──────────────┐     ┌──────────────┐     ┌──────────────────┐
│  Logiciel de │     │  CryptoSoft  │     │  Stockage        │
│  sauvegarde  │────▶│  (encrypt)   │────▶│  distant (S3,    │
│  (.NET)      │     │              │     │  NFS, etc.)      │
└──────────────┘     └──────────────┘     └──────────────────┘
       │                    │
       │                    │ Clé publique
       │                    │ (déployée avec l'application)
       │                    │
       ▼                    ▼
  backup.zip          backup.enc

┌──────────────┐     ┌──────────────┐     ┌──────────────────┐
│  Opérateur   │     │  CryptoSoft  │     │  Stockage        │
│  restauration│◀────│  (decrypt)   │◀────│  distant         │
└──────────────┘     └──────────────┘     └──────────────────┘
                            │
                            │ Clé privée
                            │ (accès restreint, HSM)
```

---

## Licence

Ce logiciel est fourni à des fins éducatives et professionnelles. Utilisez en conformité avec les lois applicables sur la cryptographie dans votre juridiction.
