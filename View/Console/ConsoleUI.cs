using System;
using System.Collections.Generic;
using System.Reflection;

namespace EasySave.View.Console;

internal class ConsoleUI
{
    public void Start()
        {
        while (true)
        {
            DisplayMainMenu();
            HandleUserInput();
        }
    }

    public void DisplayMainMenu()
    {
        System.Console.Clear();
        System.Console.WriteLine("Voici les options :");
        System.Console.WriteLine("[1] Créer une tâche");
        System.Console.WriteLine("[2] Exécuter une tâche");
        System.Console.WriteLine("[3] Modifier une tâche");
        System.Console.WriteLine("[4] Supprimer une tâche");
        System.Console.WriteLine("[5] Paramètres");
        System.Console.WriteLine();
        System.Console.Write("Choisissez une option : ");
    }

    private void HandleUserInput()
    {
        int choice = ReadInt(1, 5);

        switch (choice)
        {
            case 1:
                CreateNewJob();
                break;
            case 2:
                ExecuteJob();
                break;
            case 3:
                ModifyJob();
                break;
            case 4:
                DeleteJob();
                break;
            case 5:
                ChangeSettings();
                break;
        }

        if (choice >= 1 && choice <= 5)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Appuyez sur une touche pour revenir au menu...");
            System.Console.ReadKey(true);
        }
    }

    private void DisplayJobList()
    {
        System.Console.WriteLine("Voici la liste des tâches : ");
        System.Console.WriteLine("[1] Test1");
        System.Console.WriteLine("[2] BB");
        System.Console.WriteLine("[3] CC");
        System.Console.WriteLine("[4] DD");
        System.Console.WriteLine("[5] EE");
        System.Console.WriteLine();
    }

    private void CreateNewJob()
    {
        System.Console.Clear();
        System.Console.WriteLine("[1] Créer une tâche");
        System.Console.WriteLine();

        System.Console.Write("Nom de la tâche : ");
        string taskName = ReadNonEmpty();

        //int maxSource = Configuration.MAX_SIZE TODO: Uncomment
        int maxSource = 5; 

        var sources = new List<string>();
        if sources.Count <= maxSource {
                do
                {
                    System.Console.Write("Source : ");

                } while (ReadInt(1, 2) == 1);

                var destinations = new List<string>();
                do
                {
                    System.Console.Write("Destination : ");
                    destinations.Add(ReadNonEmpty());
                    System.Console.Write("Autre destination ? [1] Oui [2] Non : ");
                } while (ReadInt(1, 2) == 1);
            }

        System.Console.WriteLine();
        System.Console.WriteLine("--- Récapitulatif de la création ---");
        System.Console.WriteLine();
        System.Console.WriteLine($"Nom : {taskName}");
        System.Console.WriteLine($"Source : {string.Join("; ", sources)}");
        System.Console.WriteLine($"Destination : {string.Join("; ", destinations)}");
        System.Console.WriteLine();
        System.Console.Write("Valider ? [1] Oui [2] Non : ");
        int validation = ReadInt(1, 2);

        if (validation == 1)
        {
            System.Console.WriteLine("Tâche créée.");
        }
        else
        {
            System.Console.WriteLine("Création annulée.");
        }
    }

    private void ExecuteJob()
    {
        System.Console.Clear();
        System.Console.WriteLine("[2] Exécuter une tâche");
        System.Console.WriteLine();

        DisplayJobList();
        System.Console.Write("Choisissez la tâche à exécuter : ");
        int jobIndex = ReadInt(1, 5);

        System.Console.WriteLine("--- Tâche exécutée ---");
        System.Console.WriteLine($"Date : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    }

    private void ExecuteAllJobs()
    {
        System.Console.Clear();
        System.Console.WriteLine("Exécution de toutes les tâches...");
        System.Console.WriteLine($"Date : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        System.Console.WriteLine("Toutes les tâches ont été exécutées.");
    }

    private void ModifyJob()
    {
        System.Console.Clear();
        System.Console.WriteLine("[3] Modifier une tâche");
        System.Console.WriteLine();

        DisplayJobList();
        System.Console.Write("Choisissez la tâche à modifier : ");
        int jobIndex = ReadInt(1, 5);

        System.Console.WriteLine();
        System.Console.WriteLine($"Tâche : [{jobIndex}]");
        System.Console.WriteLine("Nom : Test1");
        System.Console.WriteLine("Source : C:\\Users\\Test\\Documents\\Fichier.txt");
        System.Console.WriteLine("Destination : C:\\Users\\Test\\Sauvegardes\\.");
        System.Console.WriteLine();

        bool continueEditing;
        string editedName = "Test1";
        var editedSources = new List<string> { "C:\\Users\\Test\\Documents\\Fichier.txt" };
        var editedDestinations = new List<string> { "C:\\Users\\Test\\Sauvegardes\\" };

        do
        {
            System.Console.WriteLine("--");
            System.Console.WriteLine("Choisissez le champ à modifier");
            System.Console.WriteLine("[1] Nom");
            System.Console.WriteLine("[2] Source");
            System.Console.WriteLine("[3] Destination");
            System.Console.Write("Choisissez le champ à modifier : ");
            int field = ReadInt(1, 3);

            switch (field)
            {
                case 1:
                    System.Console.WriteLine();
                    System.Console.WriteLine($"Ancien Nom : {editedName}");
                    System.Console.Write("Nom : ");
                    editedName = ReadNonEmpty();
                    break;
                case 2:
                    editedSources.Clear();
                    System.Console.WriteLine();
                    System.Console.WriteLine("Indiquez les nouvelles sources :");
                    do
                    {
                        System.Console.Write("Source : ");
                        editedSources.Add(ReadNonEmpty());
                        System.Console.Write("Autre source ? [1] Oui [2] Non : ");
                    } while (ReadInt(1, 2) == 1);
                    break;
                case 3:
                    editedDestinations.Clear();
                    System.Console.WriteLine();
                    System.Console.WriteLine("Indiquez les nouvelles destinations :");
                    do
                    {
                        System.Console.Write("Destination : ");
                        editedDestinations.Add(ReadNonEmpty());
                        System.Console.Write("Autre destination ? [1] Oui [2] Non : ");
                    } while (ReadInt(1, 2) == 1);
                    break;
            }

            System.Console.WriteLine();
            System.Console.Write("Autre champ ? [1] Oui [2] Non : ");
            continueEditing = ReadInt(1, 2) == 1;
        } while (continueEditing);

        System.Console.WriteLine();
        System.Console.WriteLine("--- Récapitulatif de la modification ---");
        System.Console.WriteLine();
        System.Console.WriteLine($"Tâche modifiée : [{jobIndex}]");
        System.Console.WriteLine($"Nom : {editedName}");
        System.Console.WriteLine($"Source : {string.Join("; ", editedSources)}");
        System.Console.WriteLine($"Destination : {string.Join("; ", editedDestinations)}");
        System.Console.WriteLine();
        System.Console.Write("Valider ? [1] Oui [2] Non : ");
        int validation = ReadInt(1, 2);

        if (validation == 1)
        {
            System.Console.WriteLine("Tâche modifiée.");
        }
        else
        {
            System.Console.WriteLine("Modification annulée.");
        }
    }

    private void DeleteJob()
    {
        System.Console.Clear();
        System.Console.WriteLine("[4] Supprimer une tâche");
        System.Console.WriteLine();

        DisplayJobList();
        System.Console.Write("Choisissez une tâche à supprimer : ");
        int jobIndex = ReadInt(1, 5);

        System.Console.WriteLine();
        System.Console.WriteLine("--- Récapitulatif de la tâche supprimée ---");
        System.Console.WriteLine("Nom : Test1");
        System.Console.WriteLine("Source : C:\\Users\\Test\\Documents\\Fichier.txt");
        System.Console.WriteLine("Destination : C:\\Users\\Test\\Sauvegardes\\.");
        System.Console.WriteLine();
        System.Console.Write("Valider ? [1] Oui [2] Non : ");
        int validation = ReadInt(1, 2);

        if (validation == 1)
        {
            System.Console.WriteLine("Tâche supprimée.");
        }
        else
        {
            System.Console.WriteLine("Suppression annulée.");
        }
    }

    private void ChangeSettings()
    {
        System.Console.Clear();
        System.Console.WriteLine("[5] Paramètres");
        System.Console.WriteLine();
        System.Console.WriteLine("Paramètres à implémenter...");
    }

    private string ReadNonEmpty()
    {
        while (true)
        {
            string? input = System.Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(input))
                return input.Trim();
            System.Console.Write("Entrée non valide. Réessayez : ");
        }
    }

    private int ReadInt(int min, int max)
    {
        while (true)
        {
            string? input = System.Console.ReadLine();
            if (int.TryParse(input, out int value) && value >= min && value <= max)
                return value;
            System.Console.Write($"Entrée invalide. Entrez un nombre entre {min} et {max} : ");
        }
    }
}
