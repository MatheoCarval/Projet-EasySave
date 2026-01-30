using System;
using System.IO;
using Utilities;

namespace TestFileSystemHelper
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Test de FileSystemHelper ===\n");

            // Créer un dossier de test
            string testDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TestFolder");
            FileSystemHelper.EnsureDirectoryExists(testDirectory);

            try
            {
                // Test 1: EnsureDirectoryExists
                TestEnsureDirectoryExists(testDirectory);

                // Test 2: Créer des fichiers de test
                CreateTestFiles(testDirectory);

                // Test 3: GetFileSize
                TestGetFileSize(testDirectory);

                // Test 4: GetFileCount
                TestGetFileCount(testDirectory);

                // Test 5: GetDirectorySize
                TestGetDirectorySize(testDirectory);

                // Test 6: CopyWithProgress
                TestCopyWithProgress(testDirectory);

                Console.WriteLine("\n✅ Tous les tests sont terminés avec succès !");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Erreur : {ex.Message}");
            }
            finally
            {
                // Nettoyage (optionnel)
                Console.WriteLine("\nAppuyez sur une touche pour nettoyer les fichiers de test...");
                Console.ReadKey();
                CleanupTestFiles(testDirectory);
            }
        }

        static void TestEnsureDirectoryExists(string baseDirectory)
        {
            Console.WriteLine("--- Test 1: EnsureDirectoryExists ---");
            
            string subDirectory = Path.Combine(baseDirectory, "SubFolder");
            FileSystemHelper.EnsureDirectoryExists(subDirectory);
            
            if (Directory.Exists(subDirectory))
                Console.WriteLine($"✅ Dossier créé : {subDirectory}");
            else
                Console.WriteLine("❌ Échec de la création du dossier");
            
            Console.WriteLine();
        }

        static void CreateTestFiles(string testDirectory)
        {
            Console.WriteLine("--- Création de fichiers de test ---");
            
            // Créer quelques fichiers de test
            string file1 = Path.Combine(testDirectory, "fichier1.txt");
            File.WriteAllText(file1, new string('A', 1024)); // 1 KB
            Console.WriteLine($"✅ Créé : fichier1.txt (1 KB)");

            string file2 = Path.Combine(testDirectory, "fichier2.txt");
            File.WriteAllText(file2, new string('B', 2048)); // 2 KB
            Console.WriteLine($"✅ Créé : fichier2.txt (2 KB)");

            // Créer un fichier dans le sous-dossier
            string subFolder = Path.Combine(testDirectory, "SubFolder");
            string file3 = Path.Combine(subFolder, "fichier3.txt");
            File.WriteAllText(file3, new string('C', 3072)); // 3 KB
            Console.WriteLine($"✅ Créé : SubFolder/fichier3.txt (3 KB)");

            Console.WriteLine();
        }

        static void TestGetFileSize(string testDirectory)
        {
            Console.WriteLine("--- Test 2: GetFileSize ---");
            
            string file1 = Path.Combine(testDirectory, "fichier1.txt");
            long size = FileSystemHelper.GetFileSize(file1);
            
            Console.WriteLine($"Taille de fichier1.txt : {size} octets ({size / 1024.0:F2} KB)");
            Console.WriteLine();
        }

        static void TestGetFileCount(string testDirectory)
        {
            Console.WriteLine("--- Test 3: GetFileCount ---");
            
            long fileCount = FileSystemHelper.GetFileCount(testDirectory);
            
            Console.WriteLine($"Nombre total de fichiers : {fileCount}");
            Console.WriteLine("(Devrait être 3 : fichier1.txt, fichier2.txt, SubFolder/fichier3.txt)");
            Console.WriteLine();
        }

        static void TestGetDirectorySize(string testDirectory)
        {
            Console.WriteLine("--- Test 4: GetDirectorySize ---");
            
            long totalSize = FileSystemHelper.GetDirectorySize(testDirectory);
            
            Console.WriteLine($"Taille totale du dossier : {totalSize} octets ({totalSize / 1024.0:F2} KB)");
            Console.WriteLine("(Devrait être environ 6 KB : 1 + 2 + 3)");
            Console.WriteLine();
        }

        static void TestCopyWithProgress(string testDirectory)
        {
            Console.WriteLine("--- Test 5: CopyWithProgress ---");
            
            string sourceFile = Path.Combine(testDirectory, "fichier1.txt");
            string destinationFile = Path.Combine(testDirectory, "fichier1_copie.txt");

            // Créer un Progress pour afficher la progression
            var progress = new Progress<double>(percentage =>
            {
                Console.Write($"\rProgression : {percentage:F1}%");
            });

            FileSystemHelper.CopyWithProgress(sourceFile, destinationFile, progress);
            
            Console.WriteLine("\n✅ Fichier copié avec succès !");
            
            // Vérifier que la copie existe
            if (File.Exists(destinationFile))
            {
                long originalSize = FileSystemHelper.GetFileSize(sourceFile);
                long copiedSize = FileSystemHelper.GetFileSize(destinationFile);
                
                if (originalSize == copiedSize)
                    Console.WriteLine($"✅ Vérification : les tailles correspondent ({originalSize} octets)");
                else
                    Console.WriteLine("❌ Les tailles ne correspondent pas !");
            }
            
            Console.WriteLine();
        }

        static void CleanupTestFiles(string testDirectory)
        {
            try
            {
                if (Directory.Exists(testDirectory))
                {
                    Directory.Delete(testDirectory, true);
                    Console.WriteLine("🧹 Fichiers de test supprimés.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Impossible de supprimer les fichiers de test : {ex.Message}");
            }
        }
    }
}